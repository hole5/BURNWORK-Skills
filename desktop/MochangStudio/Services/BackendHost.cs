using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MochangStudio.Services;

public sealed class BackendHost
{
    public string RepoRoot { get; }
    public string Workspace { get; }
    public string SessionPath { get; }

    public BackendHost(string? workspace)
    {
        RepoRoot = LocateRepo();
        Workspace = string.IsNullOrWhiteSpace(workspace)
            ? RepoRoot
            : Path.GetFullPath(workspace);
        SessionPath = Path.Combine(Workspace, ".short-drama", "dashboard.json");
    }

    public async Task<(string BaseUrl, string Token)> EnsureRunningAsync(CancellationToken ct)
    {
        var server = Path.Combine(RepoRoot, "skills", "short-drama", "scripts", "dashboard_server.py");
        if (!File.Exists(server))
            throw new InvalidOperationException("找不到 dashboard_server.py，请从仓库根启动。");

        var python = ResolvePython();
        var psi = new ProcessStartInfo
        {
            FileName = python,
            ArgumentList =
            {
                server,
                "--workspace",
                Workspace,
                "--detach",
                "--restart",
                "--port",
                "0",
            },
            WorkingDirectory = RepoRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("无法启动 Python 工作室服务。");
        _ = process.StandardOutput.ReadToEndAsync(ct);
        _ = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            if (TryReadSession(out var url, out var token))
                return (url, token);
            await Task.Delay(200, ct);
        }
        throw new TimeoutException("工作室服务未在 20 秒内写出会话。");
    }

    private bool TryReadSession(out string baseUrl, out string token)
    {
        baseUrl = "";
        token = "";
        if (!File.Exists(SessionPath))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(SessionPath));
            var root = doc.RootElement;
            token = root.GetProperty("token").GetString() ?? "";
            var url = root.GetProperty("url").GetString() ?? "";
            var match = Regex.Match(url, @"^(https?://127\.0\.0\.1:\d+)");
            if (match.Success && token.Length > 0)
            {
                baseUrl = match.Groups[1].Value;
                return true;
            }
        }
        catch (Exception)
        {
            return false;
        }
        return false;
    }

    private static string ResolvePython()
    {
        foreach (var name in new[] { "py", "python", "python3" })
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = name,
                    ArgumentList = { "-3", "-c", "import sys; print(sys.executable)" },
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                };
                if (name != "py")
                {
                    psi.ArgumentList.Clear();
                    psi.ArgumentList.Add("-c");
                    psi.ArgumentList.Add("import sys; print(sys.executable)");
                }
                using var p = Process.Start(psi);
                if (p is null) continue;
                var exe = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(4000);
                if (p.ExitCode == 0 && File.Exists(exe))
                    return exe;
            }
            catch
            {
                // try next
            }
        }
        return "py";
    }

    public async Task StopAsync(CancellationToken ct)
    {
        var server = Path.Combine(RepoRoot, "skills", "short-drama", "scripts", "dashboard_server.py");
        if (!File.Exists(server)) return;
        var python = ResolvePython();
        var psi = new ProcessStartInfo
        {
            FileName = python,
            ArgumentList = { server, "--workspace", Workspace, "--stop" },
            WorkingDirectory = RepoRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(psi);
        if (process is null) return;
        await process.WaitForExitAsync(ct);
    }

    private static string LocateRepo()
    {
        var env = Environment.GetEnvironmentVariable("MOCHANG_REPO");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env))
            return Path.GetFullPath(env);

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var marker = Path.Combine(dir.FullName, "skills", "short-drama", "scripts", "dashboard_server.py");
            if (File.Exists(marker))
                return dir.FullName;
            dir = dir.Parent;
        }
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }
}
