using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace MochangStudio.Models;

public class Observable : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        Raise(name);
        return true;
    }
}

public sealed class EpisodeRow : Observable
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Sub { get; set; } = "";
    public int Shots { get; set; }
    public double Seconds { get; set; }
    public int Scenes { get; set; }
    public bool HasBoard { get; set; }
    public bool HasScript { get; set; }
    public bool HasSettings { get; set; }
    private bool _on;
    public bool IsOn { get => _on; set { if (Set(ref _on, value)) Raise(nameof(RowBrush)); } }
    public Brush RowBrush => IsOn
        ? new SolidColorBrush(Color.FromRgb(0x2A, 0x22, 0x12))
        : Brushes.Transparent;
}

public sealed class CoverageRow
{
    public string Id { get; set; } = "";
    public string Scenes { get; set; } = "—";
    public string Shots { get; set; } = "—";
    public string Duration { get; set; } = "—";
    public string Avg { get; set; } = "—";
    public string Suggest { get; set; } = "未设";
    public string Limit { get; set; } = "";
    public bool Over { get; set; }
}

public sealed class CastItem
{
    public string Name { get; set; } = "";
    public string Cat { get; set; } = "";
    public string Anchor { get; set; } = "";
    public string Body { get; set; } = "";
    public string Prompt { get; set; } = "";
}

public sealed class ChatMessage : Observable
{
    public string Role { get; set; } = "assistant";
    private string _text = "";
    public string Text { get => _text; set => Set(ref _text, value); }
    private string _trace = "";
    public string Trace
    {
        get => _trace;
        set
        {
            if (Set(ref _trace, value)) Raise(nameof(ShowTrace));
        }
    }
    public bool ShowTrace => !string.IsNullOrWhiteSpace(Trace);
    public string Label => Role == "user" ? "你" : "小烧助手";
    public string Align => Role == "user" ? "Right" : "Left";
    public string Wire { get; set; } = "";
}

public sealed class PlanStep : Observable
{
    public string Skill { get; set; } = "";
    public string Title { get; set; } = "";
    private string _state = "待执行";
    public string State { get => _state; set => Set(ref _state, value); }
}

public sealed class AttachItem
{
    public string Kind { get; set; } = "novel";
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Excerpt { get; set; }
}

public sealed class ProjectItem : Observable
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Path { get; set; } = "";
    public string Kind { get; set; } = "创";
    public string Meta { get; set; } = "";
    public string When { get; set; } = "";
}
