using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MochangStudio.Services;

public sealed class StudioClient : IDisposable
{
    private readonly HttpClient _http;
    public string BaseUrl { get; }
    public string? ProjectId { get; private set; }

    public StudioClient(string baseUrl, string token)
    {
        BaseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Short-Drama-Token", token);
    }

    public async Task EstablishSessionAsync(CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/session");
        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task<JsonArray> ListProjectsAsync(CancellationToken ct)
    {
        var json = await GetAsync("/api/projects", ct);
        return json["projects"] as JsonArray ?? [];
    }

    public async Task SelectFirstProjectAsync(CancellationToken ct)
    {
        var projects = await ListProjectsAsync(ct);
        ProjectId = projects.FirstOrDefault()?["id"]?.GetValue<string>();
        if (string.IsNullOrEmpty(ProjectId))
            throw new InvalidOperationException("工作区里还没有 short-drama 项目。");
    }

    public void SelectProject(string projectId)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("project id required", nameof(projectId));
        ProjectId = projectId;
    }

    public void ClearProject() => ProjectId = null;

    public Task<JsonObject> SeriesAsync(CancellationToken ct) =>
        GetObjectAsync($"/api/series?project={Uri.EscapeDataString(ProjectId!)}", ct);

    public Task<JsonObject> EpisodeAsync(string episode, CancellationToken ct) =>
        GetObjectAsync(
            $"/api/episode?project={Uri.EscapeDataString(ProjectId!)}&ep={Uri.EscapeDataString(episode)}",
            ct);

    public Task<JsonObject> FileAsync(string path, CancellationToken ct) =>
        GetObjectAsync(
            $"/api/file?project={Uri.EscapeDataString(ProjectId!)}&path={Uri.EscapeDataString(path)}",
            ct);

    public Task<JsonObject> TreeAsync(string? projectId, CancellationToken ct)
    {
        var id = projectId ?? ProjectId ?? throw new InvalidOperationException("no project");
        return GetObjectAsync($"/api/tree?project={Uri.EscapeDataString(id)}", ct);
    }

    public Task<JsonObject> AdaptersAsync(CancellationToken ct) =>
        GetObjectAsync("/api/adapters", ct);

    public string MediaContentUrl(string projectId, string path) =>
        $"{BaseUrl}/api/media/content?project={Uri.EscapeDataString(projectId)}&path={Uri.EscapeDataString(path)}";

    public async Task<byte[]?> GetBytesAsync(string url, CancellationToken ct)
    {
        using var res = await _http.GetAsync(url, ct);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<JsonObject> ProducePrepareAsync(
        string episode, string modality, string sourceEntry, string adapter, CancellationToken ct)
    {
        var body = new JsonObject
        {
            ["project"] = ProjectId,
            ["episode"] = episode,
            ["modality"] = modality,
            ["source_entry"] = sourceEntry,
            ["adapter"] = adapter,
            ["overwrite"] = true,
        };
        return await PostAsync("/api/produce/prepare", body, ct);
    }

    public Task<JsonObject> ProduceConfirmAsync(string jobId, string confirmation, CancellationToken ct) =>
        PostAsync("/api/produce/confirm", new JsonObject
        {
            ["project"] = ProjectId,
            ["job_id"] = jobId,
            ["confirmation"] = confirmation,
        }, ct);

    public Task<JsonObject> ProduceConfigAsync(JsonObject body, CancellationToken ct) =>
        PostAsync("/api/produce/config", body, ct);

    public Task<JsonObject> ProduceRunAsync(string jobId, CancellationToken ct) =>
        PostAsync("/api/produce/run", new JsonObject
        {
            ["project"] = ProjectId,
            ["job_id"] = jobId,
        }, ct);

    public async IAsyncEnumerable<JsonObject> AgentTurnAsync(
        JsonObject body,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/api/agent/turn") { Content = content };
        using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        var text = "";
        if (!res.IsSuccessStatusCode)
        {
            text = await res.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(ExtractError(text, (int)res.StatusCode));
        }
        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (line is null) yield break;
            if (string.IsNullOrWhiteSpace(line)) continue;
            var node = JsonNode.Parse(line) as JsonObject ?? [];
            yield return node;
        }
    }

    private async Task<JsonObject> GetObjectAsync(string path, CancellationToken ct)
    {
        var node = await GetAsync(path, ct);
        return node as JsonObject ?? [];
    }

    private async Task<JsonNode> GetAsync(string path, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, BaseUrl + path);
        using var res = await _http.SendAsync(req, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException(ExtractError(text, (int)res.StatusCode));
        return JsonNode.Parse(text) ?? new JsonObject();
    }

    private async Task<JsonObject> PostAsync(string path, JsonObject body, CancellationToken ct)
    {
        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var req = new HttpRequestMessage(HttpMethod.Post, BaseUrl + path) { Content = content };
        using var res = await _http.SendAsync(req, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException(ExtractError(text, (int)res.StatusCode));
        return JsonNode.Parse(text) as JsonObject ?? [];
    }

    private static string ExtractError(string text, int status)
    {
        try
        {
            var node = JsonNode.Parse(text);
            return node?["error"]?.GetValue<string>() ?? $"HTTP {status}";
        }
        catch
        {
            return $"HTTP {status}";
        }
    }

    public void Dispose() => _http.Dispose();
}
