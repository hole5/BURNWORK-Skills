using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace MochangStudio.Services;

public sealed class ProjectFactory
{
    private readonly string _repoRoot;
    private readonly string _workspace;

    public ProjectFactory(string repoRoot, string projectsDir)
    {
        _repoRoot = repoRoot;
        _workspace = projectsDir;
    }

    public async Task<string> InitializeAsync(
        string title,
        string aspectRatio,
        string? idea,
        CancellationToken ct)
    {
        var slug = Slug(title);
        var root = Path.Combine(_workspace, slug);
        if (Directory.Exists(root) && File.Exists(Path.Combine(root, "short-drama.json")))
            root = Path.Combine(_workspace, $"{slug}-{DateTime.Now:yyyyMMddHHmmss}");

        Directory.CreateDirectory(Path.GetDirectoryName(root)!);
        var tool = Path.Combine(_repoRoot, "skills", "short-drama", "scripts", "project_tool.py");
        if (!File.Exists(tool))
            throw new FileNotFoundException("找不到 project_tool.py", tool);

        var python = ResolvePython();
        var psi = new ProcessStartInfo
        {
            FileName = python,
            WorkingDirectory = _repoRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add(tool);
        psi.ArgumentList.Add("init");
        psi.ArgumentList.Add(root);
        psi.ArgumentList.Add("--title");
        psi.ArgumentList.Add(title);
        psi.ArgumentList.Add("--language");
        psi.ArgumentList.Add("zh-CN");
        psi.ArgumentList.Add("--aspect-ratio");
        psi.ArgumentList.Add(aspectRatio);
        psi.ArgumentList.Add("--episode-count");
        psi.ArgumentList.Add("1");

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("无法启动 project_tool");
        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        if (process.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);

        if (!string.IsNullOrWhiteSpace(idea))
        {
            var note = Path.Combine(root, "项目开发");
            Directory.CreateDirectory(note);
            await File.WriteAllTextAsync(
                Path.Combine(note, "创作意图.md"),
                "# 创作意图\n\n" + idea.Trim() + "\n",
                ct);
        }
        return root;
    }

    private static string Slug(string title)
    {
        var cleaned = Regex.Replace(title.Trim(), @"[\\/:*?""<>|]+", "-");
        cleaned = Regex.Replace(cleaned, @"\s+", "-");
        if (cleaned.Length > 40) cleaned = cleaned[..40];
        return string.IsNullOrWhiteSpace(cleaned) ? "untitled" : cleaned;
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
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                };
                if (name == "py")
                {
                    psi.ArgumentList.Add("-3");
                    psi.ArgumentList.Add("-c");
                    psi.ArgumentList.Add("import sys; print(sys.executable)");
                }
                else
                {
                    psi.ArgumentList.Add("-c");
                    psi.ArgumentList.Add("import sys; print(sys.executable)");
                }
                using var p = Process.Start(psi);
                if (p is null) continue;
                var exe = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(4000);
                if (p.ExitCode == 0 && File.Exists(exe)) return exe;
            }
            catch { /* next */ }
        }
        return "py";
    }

    public int ImportFromRepoProjects()
    {
        var source = Path.Combine(_repoRoot, "projects");
        if (!Directory.Exists(source)) return 0;
        var srcFull = Path.GetFullPath(source).TrimEnd('\\', '/');
        var destFull = Path.GetFullPath(_workspace).TrimEnd('\\', '/');
        if (string.Equals(srcFull, destFull, StringComparison.OrdinalIgnoreCase))
            return 0;

        var count = 0;
        Directory.CreateDirectory(_workspace);
        foreach (var dir in Directory.GetDirectories(source))
        {
            if (!File.Exists(Path.Combine(dir, "short-drama.json")))
                continue;
            var name = Path.GetFileName(dir);
            if (string.IsNullOrWhiteSpace(name) || name.StartsWith('.'))
                continue;
            var dest = Path.Combine(_workspace, name);
            if (Directory.Exists(dest))
                continue;
            CopyTree(dir, dest);
            count++;
        }
        return count;
    }

    private static void CopyTree(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), overwrite: false);
        foreach (var child in Directory.GetDirectories(source))
            CopyTree(child, Path.Combine(dest, Path.GetFileName(child)));
    }
}
