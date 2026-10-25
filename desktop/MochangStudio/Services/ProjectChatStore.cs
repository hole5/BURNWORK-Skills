using System.IO;
using System.Text;
using System.Text.Json;
using MochangStudio.Models;

namespace MochangStudio.Services;

public static class ProjectChatStore
{
    public static string FileOf(string projectRoot) =>
        Path.Combine(projectRoot, "项目开发", "对话.jsonl");

    public static List<ChatMessage> Load(string projectRoot)
    {
        var path = FileOf(projectRoot);
        if (!File.Exists(path)) return [];
        var rows = new List<ChatMessage>();
        foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var row = JsonSerializer.Deserialize<ChatLine>(line, Json);
                if (row is null || string.IsNullOrWhiteSpace(row.Role)) continue;
                rows.Add(new ChatMessage
                {
                    Role = row.Role,
                    Text = row.Text ?? "",
                    Trace = row.Trace ?? "",
                    Wire = row.Wire ?? "",
                });
            }
            catch
            {
                /* skip bad line */
            }
        }
        return rows;
    }

    public static void Save(string projectRoot, IEnumerable<ChatMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot)) return;
        var dir = Path.Combine(projectRoot, "项目开发");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "对话.jsonl");
        var sb = new StringBuilder();
        foreach (var msg in messages)
        {
            if (msg.Role is not ("user" or "assistant")) continue;
            if (string.IsNullOrWhiteSpace(msg.Text) && string.IsNullOrWhiteSpace(msg.Wire)) continue;
            sb.AppendLine(JsonSerializer.Serialize(new ChatLine
            {
                Role = msg.Role,
                Text = msg.Text ?? "",
                Trace = msg.Trace ?? "",
                Wire = msg.Wire ?? "",
            }, Json));
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private sealed class ChatLine
    {
        public string Role { get; set; } = "";
        public string? Text { get; set; }
        public string? Trace { get; set; }
        public string? Wire { get; set; }
    }
}
