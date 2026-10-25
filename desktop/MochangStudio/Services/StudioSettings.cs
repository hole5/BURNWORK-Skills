using System.IO;
using System.Text.Json;

namespace MochangStudio.Services;

public sealed class StudioOptions
{
    public string StudioRoot { get; set; } = "";
    public string ProjectsDir { get; set; } = "";
    public string SkillsDir { get; set; } = "";
    public string AssetsDir { get; set; } = "";
    public string Workspace { get; set; } = "";
    public bool FirstRunDone { get; set; }
}

public sealed class StudioLayout
{
    public string StudioRoot { get; init; } = "";
    public string ProjectsDir { get; init; } = "";
    public string SkillsDir { get; init; } = "";
    public string AssetsDir { get; init; } = "";
}

public static class StudioSettings
{
    public static string FilePath =>
        Path.Combine(
            Environment.GetEnvironmentVariable("MOCHANG_HOME")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mochang"),
            "studio.json");

    public static string DefaultStudioRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "燃烧工作室");

    public static StudioOptions Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new StudioOptions();
            return JsonSerializer.Deserialize<StudioOptions>(File.ReadAllText(FilePath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new StudioOptions();
        }
        catch
        {
            return new StudioOptions();
        }
    }

    public static void Save(StudioOptions options)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static StudioLayout Ensure(string? studioRootOverride = null)
    {
        var o = Load();
        var root = FirstExisting(studioRootOverride)
            ?? FirstExisting(o.StudioRoot)
            ?? MigrateRoot(o.Workspace)
            ?? DefaultStudioRoot;

        var layout = new StudioLayout
        {
            StudioRoot = Path.GetFullPath(root),
            ProjectsDir = Path.GetFullPath(First(o.ProjectsDir, Path.Combine(root, "projects"))),
            SkillsDir = Path.GetFullPath(First(o.SkillsDir, Path.Combine(root, "skills"))),
            AssetsDir = Path.GetFullPath(First(o.AssetsDir, Path.Combine(root, "assets"))),
        };
        Directory.CreateDirectory(layout.StudioRoot);
        Directory.CreateDirectory(layout.ProjectsDir);
        Directory.CreateDirectory(layout.SkillsDir);
        Directory.CreateDirectory(layout.AssetsDir);
        WriteGuide(layout);
        Save(ToOptions(layout));
        return layout;
    }

    public static StudioLayout FromForm(string studioRoot, string projects, string skills, string assets)
    {
        var root = string.IsNullOrWhiteSpace(studioRoot) ? DefaultStudioRoot : Path.GetFullPath(studioRoot.Trim());
        return new StudioLayout
        {
            StudioRoot = root,
            ProjectsDir = Path.GetFullPath(First(projects, Path.Combine(root, "projects"))),
            SkillsDir = Path.GetFullPath(First(skills, Path.Combine(root, "skills"))),
            AssetsDir = Path.GetFullPath(First(assets, Path.Combine(root, "assets"))),
        };
    }

    public static void Persist(StudioLayout layout)
    {
        Directory.CreateDirectory(layout.StudioRoot);
        Directory.CreateDirectory(layout.ProjectsDir);
        Directory.CreateDirectory(layout.SkillsDir);
        Directory.CreateDirectory(layout.AssetsDir);
        WriteGuide(layout);
        Save(ToOptions(layout));
    }

    private static StudioOptions ToOptions(StudioLayout layout)
    {
        var prev = Load();
        return new StudioOptions
        {
            StudioRoot = layout.StudioRoot,
            ProjectsDir = layout.ProjectsDir,
            SkillsDir = layout.SkillsDir,
            AssetsDir = layout.AssetsDir,
            Workspace = layout.ProjectsDir,
            FirstRunDone = prev.FirstRunDone,
        };
    }

    public static bool IsFirstRunDone() => Load().FirstRunDone;

    public static void MarkFirstRunDone()
    {
        var o = Load();
        o.FirstRunDone = true;
        Save(o);
    }

    private static string? MigrateRoot(string? workspace)
    {
        if (string.IsNullOrWhiteSpace(workspace) || !Directory.Exists(workspace))
            return null;
        var full = Path.GetFullPath(workspace);
        if (File.Exists(Path.Combine(full, "skills", "short-drama", "scripts", "dashboard_server.py")))
            return null;
        return Path.GetFileName(full).Equals("projects", StringComparison.OrdinalIgnoreCase)
            ? Path.GetDirectoryName(full)
            : full;
    }

    private static string? FirstExisting(string? path) =>
        !string.IsNullOrWhiteSpace(path) && Directory.Exists(path) ? Path.GetFullPath(path) : null;

    private static string First(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static void WriteGuide(StudioLayout layout)
    {
        WriteIfMissing(
            Path.Combine(layout.StudioRoot, "README.txt"),
            "燃烧工作室目录\r\n\r\nprojects\\  全部短剧项目（每项含 short-drama.json）\r\nskills\\    已安装的 Agent Skills（含 SKILL.md）\r\nassets\\    不属于某一集的参考图与素材\r\n");
        WriteIfMissing(
            Path.Combine(layout.ProjectsDir, "README.txt"),
            "把短剧项目放在此文件夹。每个项目是一个含 short-drama.json 的子目录。\r\n");
        WriteIfMissing(
            Path.Combine(layout.SkillsDir, "README.txt"),
            "把含 SKILL.md 的技能文件夹放在这里。可在设置 → SKILLS 里导入。\r\n");
        WriteIfMissing(
            Path.Combine(layout.AssetsDir, "README.txt"),
            "公共参考图与素材。各集制作成果仍在对应项目的 制作成果/ 里。\r\n");
    }

    private static void WriteIfMissing(string path, string text)
    {
        if (!File.Exists(path))
            File.WriteAllText(path, text);
    }
}
