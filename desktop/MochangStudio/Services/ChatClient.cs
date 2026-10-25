using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MochangStudio.Services;

public sealed class ChatProfile
{
    public string Name { get; set; } = "默认";
    public string BaseUrl { get; set; } = "http://127.0.0.1:11434/v1";
    public string ApiKey { get; set; } = "ollama";
    public string Model { get; set; } = "qwen2.5";
}

public sealed class ChatBundle
{
    public string Active { get; set; } = "Ollama";
    public List<ChatProfile> Profiles { get; set; } = [];

    [JsonIgnore]
    public ChatProfile Current =>
        Profiles.FirstOrDefault(p => p.Name == Active) ?? Profiles.FirstOrDefault() ?? new ChatProfile();
}

public sealed class ChatClient
{
    private static string ConfigPath =>
        Path.Combine(
            Environment.GetEnvironmentVariable("MOCHANG_HOME")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mochang"),
            "chat.json");

    public static string ConfigLocation => ConfigPath;

    public static ChatBundle LoadBundle()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return DefaultBundle();
            using var doc = JsonDocument.Parse(File.ReadAllText(ConfigPath));
            var root = doc.RootElement;
            if (root.TryGetProperty("profiles", out var profilesEl) && profilesEl.ValueKind == JsonValueKind.Array)
            {
                var bundle = JsonSerializer.Deserialize<ChatBundle>(root.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? DefaultBundle();
                if (bundle.Profiles.Count == 0) bundle.Profiles.Add(new ChatProfile());
                if (bundle.Profiles.All(p => p.Name != bundle.Active))
                    bundle.Active = bundle.Profiles[0].Name;
                return bundle;
            }
            var legacy = JsonSerializer.Deserialize<ChatProfile>(root.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ChatProfile();
            legacy.Name = string.IsNullOrWhiteSpace(legacy.Name) ? "默认" : legacy.Name;
            return new ChatBundle { Active = legacy.Name, Profiles = [legacy] };
        }
        catch
        {
            return DefaultBundle();
        }
    }

    public static ChatProfile Load() => LoadBundle().Current;

    public static void SaveBundle(ChatBundle bundle)
    {
        if (bundle.Profiles.Count == 0)
            bundle.Profiles.Add(new ChatProfile());
        if (bundle.Profiles.All(p => p.Name != bundle.Active))
            bundle.Active = bundle.Profiles[0].Name;
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(bundle, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void Save(ChatProfile profile)
    {
        var bundle = LoadBundle();
        var hit = bundle.Profiles.FirstOrDefault(p => p.Name == profile.Name);
        if (hit is null)
        {
            bundle.Profiles.Add(profile);
            bundle.Active = profile.Name;
        }
        else
        {
            hit.BaseUrl = profile.BaseUrl;
            hit.ApiKey = profile.ApiKey;
            hit.Model = profile.Model;
            bundle.Active = profile.Name;
        }
        SaveBundle(bundle);
    }

    public static ChatBundle DefaultBundle() => new()
    {
        Active = "Ollama",
        Profiles =
        [
            new ChatProfile { Name = "Ollama", BaseUrl = "http://127.0.0.1:11434/v1", ApiKey = "ollama", Model = "qwen2.5" },
        ],
    };

    public async Task<string> CompleteAsync(
        IReadOnlyList<(string Role, string Text)> history,
        string system,
        CancellationToken ct)
    {
        var cfg = Load();
        if (string.IsNullOrWhiteSpace(cfg.BaseUrl) || string.IsNullOrWhiteSpace(cfg.Model))
            throw new InvalidOperationException("请先在设置 → API 里填写模型地址。");

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        if (!string.IsNullOrWhiteSpace(cfg.ApiKey))
            http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + cfg.ApiKey);

        var messages = new JsonArray
        {
            new JsonObject { ["role"] = "system", ["content"] = system },
        };
        foreach (var (role, contentText) in history)
            messages.Add(new JsonObject { ["role"] = role, ["content"] = contentText });

        var payload = new JsonObject
        {
            ["model"] = cfg.Model,
            ["temperature"] = 0.4,
            ["messages"] = messages,
        };
        var url = cfg.BaseUrl.TrimEnd('/') + "/chat/completions";
        using var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
        using var res = await http.PostAsync(url, content, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException($"模型接口 {((int)res.StatusCode)}：{Trim(body, 400)}");
        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
        return string.IsNullOrWhiteSpace(text) ? "（模型没有返回正文）" : text;
    }

    /// <summary>轻量连通：发一条极短 completion，确认地址/密钥/模型可用。</summary>
    public async Task<(bool Ok, string Detail)> ProbeAsync(CancellationToken ct)
    {
        var cfg = Load();
        if (string.IsNullOrWhiteSpace(cfg.BaseUrl) || string.IsNullOrWhiteSpace(cfg.Model))
            return (false, "未配置模型地址或模型名");
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            if (!string.IsNullOrWhiteSpace(cfg.ApiKey))
                http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + cfg.ApiKey);
            var payload = new JsonObject
            {
                ["model"] = cfg.Model,
                ["max_tokens"] = 8,
                ["messages"] = new JsonArray
                {
                    new JsonObject { ["role"] = "user", ["content"] = "ping" },
                },
            };
            var url = cfg.BaseUrl.TrimEnd('/') + "/chat/completions";
            using var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
            using var res = await http.PostAsync(url, content, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                return (false, $"HTTP {(int)res.StatusCode}：{Trim(body, 160)}");
            return (true, $"{cfg.Name} · {cfg.Model}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string Trim(string s, int n) => s.Length <= n ? s : s[..n] + "…";
}
