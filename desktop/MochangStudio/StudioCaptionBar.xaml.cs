using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shell;

namespace MochangStudio;

public partial class StudioCaptionBar : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(StudioCaptionBar), new PropertyMetadata("燃烧工作室"));

    public static readonly DependencyProperty ShowMinMaxProperty =
        DependencyProperty.Register(nameof(ShowMinMax), typeof(bool), typeof(StudioCaptionBar), new PropertyMetadata(true, OnShowMinMaxChanged));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool ShowMinMax
    {
        get => (bool)GetValue(ShowMinMaxProperty);
        set => SetValue(ShowMinMaxProperty, value);
    }

    public StudioCaptionBar()
    {
        InitializeComponent();
        Loaded += OnLoadedBar;
        MouseLeftButtonDown += OnCaptionDown;
    }

    private void OnLoadedBar(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window is null) return;
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ShowMinMax ? ResizeMode.CanResize : ResizeMode.NoResize;
        WindowChrome.SetWindowChrome(window, new WindowChrome
        {
            CaptionHeight = 40,
            GlassFrameThickness = new Thickness(0),
            UseAeroCaptionButtons = false,
            ResizeBorderThickness = ShowMinMax ? new Thickness(6) : new Thickness(0),
            CornerRadius = new CornerRadius(0),
        });
        WindowChrome.SetIsHitTestVisibleInChrome(MinBtn, true);
        WindowChrome.SetIsHitTestVisibleInChrome(MaxBtn, true);
        WindowChrome.SetIsHitTestVisibleInChrome(CloseBtn, true);
        ApplyMinMax();
    }

    private static void OnShowMinMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StudioCaptionBar bar) bar.ApplyMinMax();
    }

    private void ApplyMinMax()
    {
        var vis = ShowMinMax ? Visibility.Visible : Visibility.Collapsed;
        MinBtn.Visibility = vis;
        MaxBtn.Visibility = vis;
    }

    private void OnCaptionDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || !ShowMinMax) return;
        OnMax(sender, e);
    }

    private void OnMin(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window is not null) window.WindowState = WindowState.Minimized;
    }

    private void OnMax(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window is null) return;
        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
        MaxBtn.Content = window.WindowState == WindowState.Maximized ? "❐" : "□";
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window is null) return;
        if (window.Owner is not null)
        {
            try { window.DialogResult = false; }
            catch { window.Close(); }
            return;
        }
        window.Close();
    }
}
