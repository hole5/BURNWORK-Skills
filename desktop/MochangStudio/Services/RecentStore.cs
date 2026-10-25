using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MochangStudio.Services;

public sealed class RecentProject
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Path { get; set; } = "";
    public string Kind { get; set; } = "创";
    public string Subtitle { get; set; } = "短剧项目";
    public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class RecentStore
{
    private static string PathFile =>
        System.IO.Path.Combine(
            Environment.GetEnvironmentVariable("MOCHANG_HOME")
            ?? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mochang"),
            "recent.json");

    public static List<RecentProject> Load()
    {
        var list = ReadRaw();
        var collapsed = Collapse(list);
        if (!SameEntries(list, collapsed))
            Write(collapsed);
        return collapsed;
    }

    public static void Touch(RecentProject project)
    {
        var list = ReadRaw();
        list.RemoveAll(p => SameProject(p, project));
        project.OpenedAt = DateTimeOffset.UtcNow;
        list.Insert(0, project);
        Write(Collapse(list));
    }

    public static void Remove(string id, string? path = null)
    {
        var probe = new RecentProject { Id = id, Path = path ?? "" };
        var list = ReadRaw();
        list.RemoveAll(p => SameProject(p, probe));
        Write(Collapse(list));
    }

    public static string RelativeTime(DateTimeOffset at)
    {
        var span = DateTimeOffset.UtcNow - at.ToUniversalTime();
        if (span.TotalMinutes < 1) return "刚刚";
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} 分钟前";
        if (span.TotalDays < 1) return $"{(int)span.TotalHours} 小时前";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays} 天前";
        return at.ToLocalTime().ToString("yyyy-MM-dd");
    }

    private static List<RecentProject> ReadRaw()
    {
        try
        {
            if (!File.Exists(PathFile)) return [];
            var list = JsonSerializer.Deserialize<List<RecentProject>>(File.ReadAllText(PathFile),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return list ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<RecentProject> Collapse(IEnumerable<RecentProject> list) =>
        list
            .GroupBy(CanonicalKey, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(p => p.OpenedAt).First())
            .OrderByDescending(p => p.OpenedAt)
            .Take(20)
            .ToList();

    private static bool SameProject(RecentProject a, RecentProject b) =>
        string.Equals(CanonicalKey(a), CanonicalKey(b), StringComparison.OrdinalIgnoreCase);

    private static string CanonicalKey(RecentProject p)
    {
        var leaf = Leaf(p.Path);
        if (!string.IsNullOrEmpty(leaf) && leaf != ".")
            return "leaf:" + leaf;
        var id = (p.Id ?? "").Trim();
        if (id.Length > 0)
            return "id:" + id;
        return "title:" + (p.Title ?? "").Trim();
    }

    private static string Leaf(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        var normalized = path.Replace('/', System.IO.Path.DirectorySeparatorChar).TrimEnd('\\', '/');
        try
        {
            return System.IO.Path.GetFileName(System.IO.Path.GetFullPath(normalized));
        }
        catch
        {
            return System.IO.Path.GetFileName(normalized);
        }
    }

    private static bool SameEntries(List<RecentProject> a, List<RecentProject> b)
    {
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i].Id, b[i].Id, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(a[i].Path, b[i].Path, StringComparison.OrdinalIgnoreCase)) return false;
            if (a[i].OpenedAt != b[i].OpenedAt) return false;
        }
        return true;
    }

    private static void Write(List<RecentProject> list)
    {
        var dir = System.IO.Path.GetDirectoryName(PathFile)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(PathFile, JsonSerializer.Serialize(list, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        }));
    }
}
