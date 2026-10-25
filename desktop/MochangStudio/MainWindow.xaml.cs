using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MochangStudio.Models;
using MochangStudio.Services;

namespace MochangStudio;

public partial class MainWindow : Window
{
    private static readonly (string Key, string Label)[] Stages =
    {
        ("overview", "概况"),
        ("script", "剧本"),
        ("cast", "角色"),
        ("art", "美术"),
        ("board", "分镜"),
        ("prompts", "提示词"),
        ("produce", "生产"),
        ("film", "成片"),
        ("review", "审查"),
    };

    private readonly ObservableCollection<EpisodeRow> _episodes = new();
    private readonly ObservableCollection<ChatMessage> _chat = new();
    private readonly ObservableCollection<ProjectItem> _projects = new();
    private readonly ObservableCollection<ProjectItem> _recents = new();
    private readonly ObservableCollection<ProjectItem> _assets = new();
    private readonly ChatClient _chatClient = new();
    private StudioClient? _studio;
    private BackendHost? _host;
    private ProjectFactory? _factory;
    private StudioLayout? _layout;
    private JsonObject? _series;
    private JsonObject? _episode;
    private string _stage = "board";
    private string? _episodeId;
    private string? _pendingIdea;
    private string? _projectRoot;
    private bool _sending;
    private bool _busy;
    private readonly List<AttachItem> _composeAttach = new();
    private readonly List<AttachItem> _chatAttach = new();
    private readonly ObservableCollection<PlanStep> _plan = new();
    private CancellationTokenSource? _chatCts;
    private int _planCursor;
    private string _activeImageAdapter = "";
    private string _activeVideoAdapter = "";
    private string _produceMode = "gate";
    private bool _loadingProduceSettings;
    private bool _sessionProduceAuth;

    public MainWindow()
    {
        InitializeComponent();
        EpisodeList.ItemsSource = _episodes;
        ChatList.ItemsSource = _chat;
        if (PlanList is not null) PlanList.ItemsSource = _plan;
        RecentList.ItemsSource = _recents;
        BuildStageRail();
        BuildTemplates();
        ApplyComposePlaceholder();
        _chat.Add(new ChatMessage
        {
            Role = "assistant",
            Text = ChatGreeting,
        });
    }

    private void BuildStageRail()
    {
        StageRail.Children.Clear();
        foreach (var (key, label) in Stages)
        {
            var tab = new RadioButton
            {
                Content = BuildStageContent(key, label),
                Style = (Style)FindResource("StageTab"),
                GroupName = "stage",
                Tag = key,
                IsChecked = key == _stage,
            };
            tab.Checked += (_, _) =>
            {
                if (tab.IsChecked == true)
                {
                    _stage = key;
                    RenderMain();
                }
            };
            StageRail.Children.Add(tab);
        }
    }

    private static object BuildStageContent(string key, string label)
    {
        var gold = (Brush)Application.Current.FindResource("GoldBrush");
        var stack = new StackPanel();
        var plate = new Border
        {
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(0x22, 0xE2, 0xD4, 0xB0)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = StageGlyph(key, gold),
        };
        stack.Children.Add(plate);
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0),
        });
        return stack;
    }

    private static UIElement StageGlyph(string key, Brush stroke)
    {
        var canvas = new Canvas { Width = 20, Height = 20, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        void Add(Shape shape)
        {
            shape.Stroke = stroke;
            shape.StrokeThickness = 1.4;
            shape.Fill = Brushes.Transparent;
            shape.StrokeStartLineCap = PenLineCap.Round;
            shape.StrokeEndLineCap = PenLineCap.Round;
            shape.StrokeLineJoin = PenLineJoin.Round;
            canvas.Children.Add(shape);
        }
        switch (key)
        {
            case "overview":
                Add(new Rectangle { Width = 7, Height = 7, RadiusX = 1.5, RadiusY = 1.5 });
                Canvas.SetLeft(canvas.Children[0], 2);
                Canvas.SetTop(canvas.Children[0], 2);
                Add(new Rectangle { Width = 7, Height = 7, RadiusX = 1.5, RadiusY = 1.5 });
                Canvas.SetLeft(canvas.Children[1], 11);
                Canvas.SetTop(canvas.Children[1], 2);
                Add(new Rectangle { Width = 7, Height = 7, RadiusX = 1.5, RadiusY = 1.5 });
                Canvas.SetLeft(canvas.Children[2], 2);
                Canvas.SetTop(canvas.Children[2], 11);
                Add(new Rectangle { Width = 7, Height = 7, RadiusX = 1.5, RadiusY = 1.5 });
                Canvas.SetLeft(canvas.Children[3], 11);
                Canvas.SetTop(canvas.Children[3], 11);
                break;
            case "script":
                Add(new Rectangle { Width = 12, Height = 16, RadiusX = 2, RadiusY = 2 });
                Canvas.SetLeft(canvas.Children[0], 4);
                Canvas.SetTop(canvas.Children[0], 2);
                Add(new Line { X1 = 7, Y1 = 7, X2 = 13, Y2 = 7 });
                Add(new Line { X1 = 7, Y1 = 10.5, X2 = 13, Y2 = 10.5 });
                Add(new Line { X1 = 7, Y1 = 14, X2 = 11, Y2 = 14 });
                break;
            case "cast":
                Add(new Ellipse { Width = 7, Height = 7 });
                Canvas.SetLeft(canvas.Children[0], 6.5);
                Canvas.SetTop(canvas.Children[0], 2);
                Add(new Path { Data = Geometry.Parse("M4,18 C4,13 16,13 16,18") });
                break;
            case "art":
                Add(new Rectangle { Width = 14, Height = 12, RadiusX = 2, RadiusY = 2 });
                Canvas.SetLeft(canvas.Children[0], 3);
                Canvas.SetTop(canvas.Children[0], 4);
                Add(new Path { Data = Geometry.Parse("M5,14 L9,9 L12,12 L14,10 L17,14") });
                Add(new Ellipse { Width = 3, Height = 3 });
                Canvas.SetLeft(canvas.Children[2], 6);
                Canvas.SetTop(canvas.Children[2], 7);
                break;
            case "board":
                Add(new Rectangle { Width = 14, Height = 4, RadiusX = 1, RadiusY = 1 });
                Canvas.SetLeft(canvas.Children[0], 3);
                Canvas.SetTop(canvas.Children[0], 3);
                Add(new Rectangle { Width = 14, Height = 4, RadiusX = 1, RadiusY = 1 });
                Canvas.SetLeft(canvas.Children[1], 3);
                Canvas.SetTop(canvas.Children[1], 8);
                Add(new Rectangle { Width = 14, Height = 4, RadiusX = 1, RadiusY = 1 });
                Canvas.SetLeft(canvas.Children[2], 3);
                Canvas.SetTop(canvas.Children[2], 13);
                break;
            case "prompts":
                Add(new Path { Data = Geometry.Parse("M10,2 L11.5,7.5 L17,9 L11.5,10.5 L10,16 L8.5,10.5 L3,9 L8.5,7.5 Z") });
                break;
            case "produce":
                Add(new Ellipse { Width = 16, Height = 16 });
                Canvas.SetLeft(canvas.Children[0], 2);
                Canvas.SetTop(canvas.Children[0], 2);
                Add(new Path { Data = Geometry.Parse("M8,6 L14,10 L8,14 Z"), Fill = stroke, StrokeThickness = 0 });
                break;
            case "film":
                Add(new Rectangle { Width = 16, Height = 12, RadiusX = 2, RadiusY = 2 });
                Canvas.SetLeft(canvas.Children[0], 2);
                Canvas.SetTop(canvas.Children[0], 4);
                Add(new Ellipse { Width = 4, Height = 4 });
                Canvas.SetLeft(canvas.Children[1], 5);
                Canvas.SetTop(canvas.Children[1], 8);
                Add(new Ellipse { Width = 4, Height = 4 });
                Canvas.SetLeft(canvas.Children[2], 11);
                Canvas.SetTop(canvas.Children[2], 8);
                break;
            default:
                Add(new Ellipse { Width = 12, Height = 12 });
                Canvas.SetLeft(canvas.Children[0], 4);
                Canvas.SetTop(canvas.Children[0], 4);
                Add(new Path { Data = Geometry.Parse("M7,10 L9,12 L13,8") });
                break;
        }
        return canvas;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ShowHome();
        var sw = Stopwatch.StartNew();
        try
        {
            HomeStatus.Text = "正在启动本机服务…";
            var arg = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault();
            string? rootOverride = null;
            if (!string.IsNullOrWhiteSpace(arg)
                && System.IO.Directory.Exists(arg)
                && !System.IO.File.Exists(System.IO.Path.Combine(arg, "skills", "short-drama", "scripts", "dashboard_server.py")))
            {
                rootOverride = arg;
            }
            _layout = StudioSettings.Ensure(rootOverride);
            _host = new BackendHost(_layout.ProjectsDir);
            _factory = new ProjectFactory(_host.RepoRoot, _layout.ProjectsDir);
            var imported = _factory.ImportFromRepoProjects();
            if (imported > 0)
                HomeStatus.Text = $"已把 {imported} 个项目归入工作室文件夹，正在启动服务…";
            var (url, token) = await _host.EnsureRunningAsync(CancellationToken.None);
            _studio = new StudioClient(url, token);
            await _studio.EstablishSessionAsync(CancellationToken.None);
            await RefreshProjectListAsync();
            await RefreshAssetsAsync();
            RefreshChatModels();
            RefreshSkillList();
            FillLayoutBoxes();
            sw.Stop();
            LatencyText.Text = $"{sw.ElapsedMilliseconds} ms";
            HomeStatus.Text = "";
            MaybeShowFirstRunWizard();
        }
        catch (Exception ex)
        {
            HomeStatus.Text = ex.Message;
            LatencyText.Text = "离线";
            StudioDialog.Alert(this, "燃烧工作室", ex.Message);
        }
    }

    private void MaybeShowFirstRunWizard()
    {
        if (StudioSettings.IsFirstRunDone()) return;
        var go = StudioDialog.Confirm(
            this,
            "欢迎使用燃烧工作室",
            "首次使用建议先完成三步环境：\n1. 设置 → API：配置聊天模型\n2. 设置 → 生产：保存并启动出图后端\n3. 设置 → 生产：保存并启动出视频后端（可选）\n\n随后可用「一键测连通」确认三灯。现在打开设置吗？",
            "去设置",
            "稍后");
        StudioSettings.MarkFirstRunDone();
        if (!go) return;
        NavSettings.IsChecked = true;
        OnHomeNav(this, new RoutedEventArgs());
        SetNavProduce.IsChecked = true;
        OnSettingsNav(this, new RoutedEventArgs());
        _ = LoadProduceSettingsAsync();
    }

    private void ShowHome()
    {
        HomeView.Visibility = Visibility.Visible;
        WorkbenchView.Visibility = Visibility.Collapsed;
    }

    private void ShowWorkbench()
    {
        PauseHeroMedia();
        HomeView.Visibility = Visibility.Collapsed;
        WorkbenchView.Visibility = Visibility.Visible;
    }

    private void OnBackHome(object sender, RoutedEventArgs e)
    {
        SaveProjectChat();
        _studio?.ClearProject();
        ShowHome();
        _ = RefreshProjectListAsync();
    }

    private void OnHomeNav(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        var projects = NavProjects.IsChecked == true;
        var assets = NavAssets.IsChecked == true;
        var templates = NavTemplates.IsChecked == true;
        var settings = NavSettings.IsChecked == true;
        HomeLanding.Visibility = projects || assets || templates || settings ? Visibility.Collapsed : Visibility.Visible;
        HomeProjects.Visibility = projects ? Visibility.Visible : Visibility.Collapsed;
        HomeAssets.Visibility = assets ? Visibility.Visible : Visibility.Collapsed;
        HomeTemplates.Visibility = templates ? Visibility.Visible : Visibility.Collapsed;
        HomeSettings.Visibility = settings ? Visibility.Visible : Visibility.Collapsed;
        if (HomeLanding.Visibility != Visibility.Visible)
            PauseHeroMedia();
        if (projects)
        {
            HomeStatus.Text = "";
            _ = RefreshProjectListAsync();
        }
        else if (assets)
        {
            HomeStatus.Text = "";
            _ = RefreshAssetsAsync();
        }
        else if (templates)
            HomeStatus.Text = "";
        else if (settings)
        {
            HomeStatus.Text = "";
            FillLayoutBoxes();
            RefreshChatModels();
            RefreshSkillList();
            OnSettingsNav(sender, e);
        }
        else
        {
            HomeStatus.Text = "";
            FocusHero(HeroCenter, play: false);
        }
    }

    private void BuildTemplates()
    {
        TemplatePanel.Children.Clear();
        var cards = new (string Title, string Ratio, string Body)[]
        {
            ("都市复仇 · 竖屏", "9:16", "都市情感复仇短剧，女主被当众羞辱后翻盘。需要角色和场景美术，先写一集钩子。"),
            ("民国权谋 · 竖屏", "9:16", "民国宅门权谋，晚宴上当众对质。人物造型锁连续性，地点用夜宴厅。"),
            ("小说改编", "9:16", "根据小说改编竖屏短剧，保留原著核心冲突，先出改编契约再写第一集剧本。"),
            ("漫剧静态", "9:16", "静态漫剧：关键帧切换加配音，不写视频提示词，先出角色设定和分镜关键帧。"),
            ("横屏单元", "16:9", "16:9 横屏单元剧，每集一个封闭事件，人物可跨集复用。"),
        };
        foreach (var card in cards)
        {
            var captured = card;
            var border = new Border
            {
                Width = 280,
                Margin = new Thickness(0, 0, 16, 16),
                Padding = new Thickness(16),
                CornerRadius = new CornerRadius(14),
                Background = (Brush)FindResource("SurfaceBrush"),
                BorderBrush = (Brush)FindResource("LineBrush"),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = captured.Title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 16,
                Foreground = (Brush)FindResource("InkBrush"),
            });
            stack.Children.Add(new TextBlock
            {
                Text = captured.Ratio,
                Foreground = (Brush)FindResource("GoldBrush"),
                Margin = new Thickness(0, 4, 0, 8),
            });
            stack.Children.Add(new TextBlock
            {
                Text = captured.Body,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)FindResource("SoftInkBrush"),
                LineHeight = 22,
            });
            border.Child = stack;
            border.MouseLeftButtonUp += (_, _) => ApplyTemplate(captured.Title, captured.Ratio, captured.Body);
            TemplatePanel.Children.Add(border);
        }
    }

    private void ApplyTemplate(string title, string ratio, string body)
    {
        NavHome.IsChecked = true;
        ComposeBox.Foreground = (Brush)FindResource("InkBrush");
        ComposeBox.Text = title + "\n" + body;
        Ratio169.IsChecked = ratio == "16:9";
        Ratio916.IsChecked = ratio != "16:9";
        NeedCast.IsChecked = true;
        NeedArt.IsChecked = true;
        ComposeBox.Focus();
        HomeStatus.Text = "模板已填入。点 ↑ 或 Ctrl+Enter 创建项目。";
    }

    private async Task RefreshAssetsAsync()
    {
        if (_studio is null) return;
        _assets.Clear();
        foreach (var project in _projects.Take(8))
        {
            try
            {
                var tree = await _studio.TreeAsync(project.Id, CancellationToken.None);
                FlattenMedia(FindChildren(tree), project);
            }
            catch
            {
                // skip unreadable project
            }
        }
        if (_assets.Count == 0 && _layout is not null && System.IO.Directory.Exists(_layout.AssetsDir))
        {
            foreach (var file in System.IO.Directory.EnumerateFiles(_layout.AssetsDir, "*.*", System.IO.SearchOption.AllDirectories))
            {
                var ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
                if (ext is not ".png" and not ".jpg" and not ".jpeg" and not ".webp" and not ".gif" and not ".mp4" and not ".webm" and not ".wav" and not ".mp3")
                    continue;
                _assets.Add(new ProjectItem
                {
                    Title = System.IO.Path.GetFileName(file),
                    Path = file,
                    Meta = "公共资产 · " + _layout.AssetsDir,
                    Kind = IsVideoPath(file) ? "视" : "图",
                });
            }
        }
        if (_assets.Count == 0)
            _assets.Add(new ProjectItem { Title = "还没有媒体", Meta = "项目制作成果或工作室 assets 文件夹里的图会出现在这里。" });
        BindHomeHeroes();
        BuildAssetCards();
    }

    private static JsonArray FindChildren(JsonObject tree)
    {
        if (tree["children"] is JsonArray a) return a;
        // dashboard tree() returns {"tree": [...], "warnings": ...} or the list itself
        foreach (var prop in tree)
        {
            if (prop.Value is JsonArray arr && prop.Key is "tree" or "nodes" or "files")
                return arr;
        }
        return [];
    }

    private void FlattenMedia(JsonArray nodes, ProjectItem project)
    {
        foreach (var node in nodes)
        {
            if (node is not JsonObject o) continue;
            var type = Str(o["type"]);
            if (type == "directory")
                FlattenMedia(o["children"] as JsonArray ?? [], project);
            else if (type == "media")
            {
                var rel = Str(o["name"]);
                var pathRel = Str(o["path"]);
                var local = "";
                try
                {
                    local = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                        ResolveProjectPath(project),
                        pathRel.Replace('/', System.IO.Path.DirectorySeparatorChar)));
                }
                catch
                {
                    local = pathRel;
                }
                _assets.Add(new ProjectItem
                {
                    Id = project.Id,
                    Title = string.IsNullOrEmpty(rel) ? System.IO.Path.GetFileName(pathRel) : rel,
                    Path = local,
                    Kind = IsVideoPath(local) ? "视" : "图",
                    Meta = $"{project.Title} · {pathRel} · {FormatSize(Num(o["size"]))}",
                });
            }
        }
    }

    private static bool IsVideoPath(string path)
    {
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return ext is ".mp4" or ".webm" or ".mov" or ".mkv" or ".avi" or ".wmv";
    }

    private void BindHomeHeroes()
    {
        if (HeroLeft is null) return;
        var files = _assets
            .Where(a => !string.IsNullOrEmpty(a.Path) && System.IO.File.Exists(a.Path) && a.Title != "还没有媒体")
            .ToList();
        var videos = files.Where(a => IsVideoPath(a.Path)).ToList();
        var stills = files.Where(a => !IsVideoPath(a.Path)).ToList();
        var picks = videos.Concat(stills).Take(3).ToList();
        BindHeroSlot(HeroLeft, HeroLeftMedia, HeroLeftImage, HeroLeftLabel, picks.ElementAtOrDefault(0), "SCENE 01");
        BindHeroSlot(HeroCenter, HeroCenterMedia, HeroCenterImage, HeroCenterLabel, picks.ElementAtOrDefault(1) ?? picks.ElementAtOrDefault(0), "YOUR STORY");
        BindHeroSlot(HeroRight, HeroRightMedia, HeroRightImage, HeroRightLabel, picks.ElementAtOrDefault(2) ?? picks.ElementAtOrDefault(0), "LOOK");
        FocusHero(HeroCenter, play: false);
        WireHeroLoop(HeroLeftMedia);
        WireHeroLoop(HeroCenterMedia);
        WireHeroLoop(HeroRightMedia);
    }

    private static void WireHeroLoop(MediaElement media)
    {
        media.MediaEnded -= HeroMediaEnded;
        media.MediaEnded += HeroMediaEnded;
    }

    private static void HeroMediaEnded(object sender, RoutedEventArgs e)
    {
        if (sender is not MediaElement media) return;
        try
        {
            media.Position = TimeSpan.Zero;
            media.Play();
        }
        catch { /* decoder unavailable */ }
    }

    private void BindHeroSlot(Border card, MediaElement media, Image image, TextBlock label, ProjectItem? item, string fallback)
    {
        try { media.Stop(); } catch { /* ignore */ }
        media.Source = null;
        image.Source = null;
        card.Tag = item;
        label.Text = fallback;
        var hasFile = item is not null && System.IO.File.Exists(item.Path);
        if (!hasFile)
            return;
        if (IsVideoPath(item!.Path))
            media.Source = new Uri(item.Path);
        else
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(item.Path);
            bmp.EndInit();
            bmp.Freeze();
            image.Source = bmp;
        }
    }

    private IEnumerable<Border> HeroCards()
    {
        yield return HeroLeft;
        yield return HeroCenter;
        yield return HeroRight;
    }

    private MediaElement? HeroMediaOf(Border card)
    {
        if (ReferenceEquals(card, HeroLeft)) return HeroLeftMedia;
        if (ReferenceEquals(card, HeroCenter)) return HeroCenterMedia;
        if (ReferenceEquals(card, HeroRight)) return HeroRightMedia;
        return null;
    }

    private void OnHeroEnter(object sender, MouseEventArgs e)
    {
        if (sender is Border card)
            FocusHero(card, play: true);
    }

    private void OnHeroLeave(object sender, MouseEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (HeroCards().Any(c => c.IsMouseOver)) return;
            PauseHeroMedia();
        });
    }

    private async void OnHeroClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border card) return;
        if (card.Tag is ProjectItem item && !string.IsNullOrEmpty(item.Id))
        {
            var project = _projects.FirstOrDefault(p => p.Id == item.Id);
            if (project is not null)
            {
                await OpenProjectAsync(project);
                return;
            }
        }
        NavAssets.IsChecked = true;
    }

    private void FocusHero(Border card, bool play)
    {
        var gold = (Brush)FindResource("GoldBrush");
        var rest = (Brush)FindResource("LineBrush");
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        var duration = TimeSpan.FromMilliseconds(200);
        foreach (var b in HeroCards())
        {
            var on = ReferenceEquals(b, card);
            Panel.SetZIndex(b, on ? 5 : 1);
            b.BorderBrush = on ? gold : rest;
            b.BorderThickness = new Thickness(on ? 1.2 : 1);
            b.Opacity = on ? 1 : 0.82;
            b.Effect = on
                ? new DropShadowEffect
                {
                    Color = Color.FromRgb(0xD4, 0xC4, 0xA0),
                    BlurRadius = 22,
                    ShadowDepth = 0,
                    Opacity = 0.55,
                }
                : null;
            var scale = ScaleOf(b);
            if (scale is not null)
            {
                var to = on ? 1.06 : 1.0;
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(to, duration) { EasingFunction = ease });
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(to, duration) { EasingFunction = ease });
            }
            var art = HeroArtOf(b);
            var media = HeroMediaOf(b);
            var image = HeroImageOf(b);
            var showMedia = on && play && media?.Source is not null;
            if (art is not null)
                art.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(showMedia ? 0.18 : 1, duration));
            if (media is not null)
                media.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(showMedia ? 1 : 0.28, duration));
            if (image is not null)
                image.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(on && image.Source is not null ? 0.55 : 0.28, duration));
            SetHeroPlayback(b, on && play);
        }
    }

    private static ScaleTransform? ScaleOf(Border card)
    {
        if (card.RenderTransform is TransformGroup group)
            return group.Children.OfType<ScaleTransform>().FirstOrDefault();
        return card.RenderTransform as ScaleTransform;
    }

    private Grid? HeroArtOf(Border card)
    {
        if (ReferenceEquals(card, HeroLeft)) return HeroLeftArt;
        if (ReferenceEquals(card, HeroCenter)) return HeroCenterArt;
        if (ReferenceEquals(card, HeroRight)) return HeroRightArt;
        return null;
    }

    private Image? HeroImageOf(Border card)
    {
        if (ReferenceEquals(card, HeroLeft)) return HeroLeftImage;
        if (ReferenceEquals(card, HeroCenter)) return HeroCenterImage;
        if (ReferenceEquals(card, HeroRight)) return HeroRightImage;
        return null;
    }

    private void SetHeroPlayback(Border card, bool play)
    {
        var media = HeroMediaOf(card);
        if (media?.Source is null) return;
        try
        {
            if (play) media.Play();
            else media.Pause();
        }
        catch { /* Windows may lack a decoder */ }
    }

    private void PauseHeroMedia()
    {
        if (HeroLeft is null) return;
        foreach (var card in HeroCards())
            SetHeroPlayback(card, false);
    }

    private static string FormatSize(double bytes)
    {
        if (bytes < 1024) return $"{bytes:0} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024:0} KB";
        return $"{bytes / (1024 * 1024):0.0} MB";
    }

    private async Task RefreshProjectListAsync()
    {
        if (_studio is null) return;
        var discovered = await _studio.ListProjectsAsync(CancellationToken.None);
        var recentHits = RecentStore.Load()
            .GroupBy(r => r.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        _projects.Clear();
        foreach (var node in discovered.OfType<JsonObject>())
        {
            var id = Str(node["id"]);
            var title = Str(node["title"], "未命名短剧");
            var path = Str(node["path"]);
            recentHits.TryGetValue(id, out var hit);
            _projects.Add(new ProjectItem
            {
                Id = id,
                Title = title,
                Path = path,
                Kind = title.Contains("小说") || path.Contains("novel", StringComparison.OrdinalIgnoreCase) ? "文" : "创",
                Meta = string.IsNullOrEmpty(path) || path == "." ? "项目根" : path,
                When = hit is null ? "未打开过" : RecentStore.RelativeTime(hit.OpenedAt),
            });
        }
        FillRecents();
        BuildProjectCards();
        if (ProjectsDirHint is not null && _layout is not null)
            ProjectsDirHint.Text = $"统一目录：{_layout.ProjectsDir}\n左键打开，右键打开或删除。";
        if (ProjectsEmptyHint is not null)
        {
            ProjectsEmptyHint.Visibility = _projects.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ProjectsEmptyHint.Text = _projects.Count == 0
                ? "这里还没有短剧项目。首页输入故事或点「+ 小说项目」创建后，会出现在本页。"
                : "";
        }
        if (ProjectCardsHost is not null)
            ProjectCardsHost.Visibility = _projects.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void BuildProjectCards()
    {
        if (ProjectPanel is null) return;
        ProjectPanel.Children.Clear();
        var ink = (Brush)FindResource("InkBrush");
        var gold = (Brush)FindResource("GoldBrush");
        var soft = (Brush)FindResource("SoftInkBrush");
        var surface = (Brush)FindResource("SurfaceBrush");
        var line = (Brush)FindResource("LineBrush");
        foreach (var item in _projects)
        {
            var captured = item;
            var border = new Border
            {
                Width = 280,
                Margin = new Thickness(0, 0, 16, 16),
                Padding = new Thickness(16),
                CornerRadius = new CornerRadius(14),
                Background = surface,
                BorderBrush = line,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Tag = captured,
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = captured.Title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 16,
                Foreground = ink,
                TextWrapping = TextWrapping.Wrap,
            });
            stack.Children.Add(new TextBlock
            {
                Text = captured.Kind == "文" ? "小说项目" : "短剧项目",
                Foreground = gold,
                Margin = new Thickness(0, 4, 0, 8),
            });
            stack.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(captured.When)
                    ? captured.Meta
                    : captured.When + "\n" + captured.Meta,
                TextWrapping = TextWrapping.Wrap,
                Foreground = soft,
                LineHeight = 22,
            });
            border.Child = stack;
            border.MouseLeftButtonUp += async (_, e) =>
            {
                if (e.ChangedButton != MouseButton.Left) return;
                e.Handled = true;
                await OpenProjectAsync(captured);
            };
            var openItem = new MenuItem { Header = "打开" };
            openItem.Click += async (_, _) => await OpenProjectAsync(captured);
            var deleteItem = new MenuItem { Header = "删除" };
            deleteItem.Click += async (_, _) => await DeleteProjectAsync(captured);
            var menu = new ContextMenu();
            menu.Items.Add(openItem);
            menu.Items.Add(deleteItem);
            border.ContextMenu = menu;
            ProjectPanel.Children.Add(border);
        }
    }

    private void BuildAssetCards()
    {
        if (AssetPanel is null) return;
        AssetPanel.Children.Clear();
        var ink = (Brush)FindResource("InkBrush");
        var gold = (Brush)FindResource("GoldBrush");
        var soft = (Brush)FindResource("SoftInkBrush");
        var surface = (Brush)FindResource("SurfaceBrush");
        var line = (Brush)FindResource("LineBrush");
        var files = _assets.Where(a => a.Title != "还没有媒体" && !string.IsNullOrEmpty(a.Path)).ToList();
        if (files.Count == 0)
        {
            AssetEmptyHint.Visibility = Visibility.Visible;
            AssetCardsHost.Visibility = Visibility.Collapsed;
            return;
        }
        AssetEmptyHint.Visibility = Visibility.Collapsed;
        AssetCardsHost.Visibility = Visibility.Visible;
        foreach (var item in files)
        {
            var captured = item;
            var border = new Border
            {
                Width = 220,
                Margin = new Thickness(0, 0, 16, 16),
                Padding = new Thickness(12),
                CornerRadius = new CornerRadius(14),
                Background = surface,
                BorderBrush = line,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
            };
            var stack = new StackPanel();
            if (!IsVideoPath(captured.Path) && System.IO.File.Exists(captured.Path))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(captured.Path);
                    bmp.DecodePixelWidth = 400;
                    bmp.EndInit();
                    bmp.Freeze();
                    stack.Children.Add(new Image
                    {
                        Source = bmp,
                        Height = 110,
                        Stretch = Stretch.UniformToFill,
                        Margin = new Thickness(0, 0, 0, 8),
                    });
                }
                catch { /* skip broken image */ }
            }
            stack.Children.Add(new TextBlock { Text = captured.Title, FontWeight = FontWeights.SemiBold, Foreground = ink, TextWrapping = TextWrapping.Wrap });
            stack.Children.Add(new TextBlock
            {
                Text = captured.Kind == "视" ? "视频" : "图片",
                Foreground = gold,
                Margin = new Thickness(0, 4, 0, 4),
            });
            stack.Children.Add(new TextBlock { Text = captured.Meta, Foreground = soft, FontSize = 11, TextWrapping = TextWrapping.Wrap });
            border.Child = stack;
            border.MouseLeftButtonUp += async (_, e) =>
            {
                if (e.ChangedButton != MouseButton.Left) return;
                if (string.IsNullOrEmpty(captured.Id)) return;
                var project = _projects.FirstOrDefault(p => p.Id == captured.Id);
                if (project is not null) await OpenProjectAsync(project);
            };
            AssetPanel.Children.Add(border);
        }
    }

    private void FillRecents()
    {
        _recents.Clear();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rec in RecentStore.Load())
        {
            var match = _projects.FirstOrDefault(p => p.Id == rec.Id)
                ?? _projects.FirstOrDefault(p => SameProjectPath(p.Path, rec.Path));
            var key = match?.Id
                ?? (string.IsNullOrEmpty(LeafPath(rec.Path)) ? rec.Id : LeafPath(rec.Path));
            if (string.IsNullOrEmpty(key) || !seen.Add(key))
                continue;
            if (match is not null)
            {
                _recents.Add(new ProjectItem
                {
                    Id = match.Id,
                    Title = match.Title,
                    Path = match.Path,
                    Kind = match.Kind,
                    Meta = "短剧项目",
                    When = RecentStore.RelativeTime(rec.OpenedAt),
                });
            }
            else
            {
                _recents.Add(new ProjectItem
                {
                    Id = rec.Id,
                    Title = rec.Title,
                    Path = rec.Path,
                    Kind = rec.Kind,
                    Meta = "短剧项目",
                    When = RecentStore.RelativeTime(rec.OpenedAt),
                });
            }
        }
    }

    private static bool SameProjectPath(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
        return string.Equals(LeafPath(a), LeafPath(b), StringComparison.OrdinalIgnoreCase);
    }

    private static string LeafPath(string path)
    {
        var normalized = path.Replace('/', System.IO.Path.DirectorySeparatorChar).TrimEnd('\\', '/');
        return System.IO.Path.GetFileName(normalized);
    }

    private async void OnRecentActivate(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        if ((e.OriginalSource as FrameworkElement)?.DataContext is ProjectItem item)
            await OpenProjectAsync(item);
    }

    private async void OnRecentKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && RecentList.SelectedItem is ProjectItem item)
        {
            e.Handled = true;
            await OpenProjectAsync(item);
        }
    }

    private string ResolveProjectPath(ProjectItem item)
    {
        if (_host is null) return "";
        if (string.IsNullOrEmpty(item.Path) || item.Path == ".")
            return _host.Workspace;
        return System.IO.Path.GetFullPath(System.IO.Path.Combine(
            _host.Workspace, item.Path.Replace('/', System.IO.Path.DirectorySeparatorChar)));
    }

    private async Task DeleteProjectAsync(ProjectItem item)
    {
        var dir = ResolveProjectPath(item);
        if (!System.IO.Directory.Exists(dir) || !System.IO.File.Exists(System.IO.Path.Combine(dir, "short-drama.json")))
        {
            StudioDialog.Alert(this, "删除项目", "找不到项目目录或不是短剧项目。");
            return;
        }
        if (_host is not null && PathsEqual(dir, _host.RepoRoot))
        {
            StudioDialog.Alert(this, "删除项目", "不能删除工作室程序所在的仓库根目录。");
            return;
        }
        if (!StudioDialog.Confirm(
            this,
            "删除项目",
            $"确定删除《{item.Title}》？\n{dir}\n\n将移到回收站（若系统允许）。",
            "删除",
            "取消"))
            return;
        try
        {
            if (_studio?.ProjectId == item.Id)
            {
                _studio.ClearProject();
                ResetChatGreeting();
                _projectRoot = null;
                ShowHome();
            }
            TryDeleteDirectory(dir);
            RecentStore.Remove(item.Id, item.Path);
            await RefreshProjectListAsync();
            HomeStatus.Text = $"已删除《{item.Title}》";
        }
        catch (Exception ex)
        {
            StudioDialog.Alert(this, "删除失败", ex.Message);
        }
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(
            System.IO.Path.GetFullPath(a).TrimEnd('\\', '/'),
            System.IO.Path.GetFullPath(b).TrimEnd('\\', '/'),
            StringComparison.OrdinalIgnoreCase);

    private static void TryDeleteDirectory(string dir)
    {
        try
        {
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
                dir,
                Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
        }
        catch
        {
            System.IO.Directory.Delete(dir, recursive: true);
        }
    }

    private void OnSettingsNav(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || SetPanelProject is null) return;
        var api = SetNavApi.IsChecked == true;
        var skills = SetNavSkills.IsChecked == true;
        var produce = SetNavProduce.IsChecked == true;
        SetPanelProject.Visibility = api || skills || produce ? Visibility.Collapsed : Visibility.Visible;
        SetPanelApi.Visibility = api ? Visibility.Visible : Visibility.Collapsed;
        SetPanelSkills.Visibility = skills ? Visibility.Visible : Visibility.Collapsed;
        SetPanelProduce.Visibility = produce ? Visibility.Visible : Visibility.Collapsed;
        if (api) RefreshChatModels();
        if (skills) RefreshSkillList();
        if (produce) _ = LoadProduceSettingsAsync();
    }

    private void FillLayoutBoxes()
    {
        if (_layout is null) return;
        StudioRootBox.Text = _layout.StudioRoot;
        ProjectsDirBox.Text = _layout.ProjectsDir;
        SkillsDirBox.Text = _layout.SkillsDir;
        AssetsDirBox.Text = _layout.AssetsDir;
    }

    private static string ProduceLabel(string id) => id switch
    {
        "comfyui-image" => "本机 ComfyUI 出图",
        "gpt-image-2" => "云端 GPT Image 2",
        "comfyui-video" => "本机 ComfyUI 出视频",
        "minimax-h3" => "云端 MiniMax H3",
        _ => string.IsNullOrEmpty(id) ? "未启动" : id,
    };

    private JsonObject ApplyProduceStatus(JsonObject data)
    {
        _activeImageAdapter = Str(data["active_image"]);
        _activeVideoAdapter = Str(data["active_video"]);
        _produceMode = Str(data["mode"], "gate");
        if (_produceMode is not ("gate" or "session" or "always"))
            _produceMode = "gate";
        if (ProduceActiveHint is not null)
        {
            ProduceActiveHint.Text =
                $"当前：出图 {ProduceLabel(_activeImageAdapter)}  ·  出视频 {ProduceLabel(_activeVideoAdapter)}  ·  模式 {Str(data["mode_label"], "确认闸门")}";
        }
        if (ProduceEnvHint is not null)
        {
            var env = data["environment"] as JsonObject;
            var chatOk = env?["chat_ok"]?.GetValue<bool>() == true;
            var imageOk = env?["image_ok"]?.GetValue<bool>() == true;
            var videoOk = env?["video_ok"]?.GetValue<bool>() == true;
            ProduceEnvHint.Text =
                $"环境：API {(chatOk ? "就绪" : "未配置")} · 出图 {(imageOk ? "已启动" : "未启动")} · 出视频 {(videoOk ? "已启动" : "未启动")}";
        }
        SyncProduceModeRadios();
        if (AllowConfigAgentBox is not null)
        {
            _loadingProduceSettings = true;
            AllowConfigAgentBox.IsChecked = data["allow_config_agent"]?.GetValue<bool>() == true;
            _loadingProduceSettings = false;
        }
        if (AllowSessionProduceBox is not null)
            AllowSessionProduceBox.Visibility = _produceMode == "session" ? Visibility.Visible : Visibility.Collapsed;
        return data;
    }

    private void SyncProduceModeRadios()
    {
        if (ProduceModeGate is null) return;
        _loadingProduceSettings = true;
        ProduceModeGate.IsChecked = _produceMode == "gate";
        ProduceModeSession.IsChecked = _produceMode == "session";
        ProduceModeAlways.IsChecked = _produceMode == "always";
        _loadingProduceSettings = false;
    }

    private async void OnProduceModeChanged(object sender, RoutedEventArgs e)
    {
        if (_loadingProduceSettings || _studio is null) return;
        var mode = ProduceModeAlways.IsChecked == true ? "always"
            : ProduceModeSession.IsChecked == true ? "session"
            : "gate";
        if (mode == "always")
        {
            var ok = StudioDialog.Confirm(
                this,
                "始终自动",
                "开启后，聊天里点名出图/出视频且环境齐全时会自动提交，可能产生费用。确定吗？",
                "开启",
                "取消");
            if (!ok)
            {
                SyncProduceModeRadios();
                return;
            }
        }
        await PostProduceConfigAsync(new JsonObject
        {
            ["action"] = "set_mode",
            ["mode"] = mode,
        }, "已更新生产模式。");
        if (mode != "session")
        {
            _sessionProduceAuth = false;
            if (AllowSessionProduceBox is not null)
                AllowSessionProduceBox.IsChecked = false;
        }
    }

    private async void OnAllowConfigAgentChanged(object sender, RoutedEventArgs e)
    {
        if (_loadingProduceSettings || _studio is null || AllowConfigAgentBox is null) return;
        await PostProduceConfigAsync(new JsonObject
        {
            ["action"] = "set_allow_config_agent",
            ["enabled"] = AllowConfigAgentBox.IsChecked == true,
        }, AllowConfigAgentBox.IsChecked == true
            ? "已允许小烧在预览确认后改本机配置。"
            : "已关闭小烧改本机配置。");
    }

    private void OnSessionProduceAuth(object sender, RoutedEventArgs e)
    {
        _sessionProduceAuth = AllowSessionProduceBox?.IsChecked == true;
        if (StatusText is not null)
            StatusText.Text = _sessionProduceAuth
                ? "本会话已允许小烧自动生产（环境齐全时）。"
                : "已撤销本会话自动生产授权。";
    }

    private async Task LoadProduceSettingsAsync()
    {
        if (_studio is null || ComfyUrlBox is null) return;
        try
        {
            var data = ApplyProduceStatus(await _studio.AdaptersAsync(CancellationToken.None));
            var comfy = data["comfyui"] as JsonObject;
            ComfyUrlBox.Text = Str(comfy?["base_url"], "http://127.0.0.1:8188");
            ComfyImageWfBox.Text = Str(comfy?["image_workflow"]);
            ComfyVideoWfBox.Text = Str(comfy?["video_workflow"]);
            var cloud = data["cloud"] as JsonObject;
            CloudImageUrlBox.Text = Str(cloud?["openai_base_url"]);
            CloudVideoModelBox.Text = Str(cloud?["minimax_video_model"]);
            CloudVideoResBox.Text = Str(cloud?["minimax_video_resolutions"]);
            CloudVideoMinBox.Text = Str(cloud?["minimax_video_min_duration"]);
            CloudVideoMaxBox.Text = Str(cloud?["minimax_video_max_duration"]);
            var imageKey = cloud?["openai_key_set"]?.GetValue<bool>() == true;
            var videoKey = cloud?["minimax_key_set"]?.GetValue<bool>() == true;
            ProduceHint.Text = (imageKey ? "GPT Image 密钥已保存。" : "尚未保存 GPT Image 密钥。")
                + " "
                + (videoKey ? "MiniMax 密钥已保存。" : "尚未保存 MiniMax 密钥。");
        }
        catch (Exception ex)
        {
            ProduceHint.Text = "无法读取生产配置：" + ex.Message;
        }
    }

    private static string? PickJsonFile(Window owner, string title)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            Filter = "API 工作流 (*.json)|*.json|所有文件|*.*",
        };
        return dlg.ShowDialog(owner) == true ? dlg.FileName : null;
    }

    private void OnBrowseComfyImageWf(object sender, RoutedEventArgs e)
    {
        var path = PickJsonFile(this, "选择出图工作流 JSON");
        if (path is not null) ComfyImageWfBox.Text = path;
    }

    private void OnBrowseComfyVideoWf(object sender, RoutedEventArgs e)
    {
        var path = PickJsonFile(this, "选择出视频工作流 JSON");
        if (path is not null) ComfyVideoWfBox.Text = path;
    }

    private async Task PostProduceConfigAsync(JsonObject body, string okText)
    {
        if (_studio is null) return;
        try
        {
            var data = ApplyProduceStatus(await _studio.ProduceConfigAsync(body, CancellationToken.None));
            ProduceHint.Text = okText;
            if (data["probe"] is JsonObject probe && probe["ok"]?.GetValue<bool>() == true)
                ProduceHint.Text = "已连上 ComfyUI：" + Str(probe["url"]);
        }
        catch (Exception ex)
        {
            ProduceHint.Text = ex.Message;
            StudioDialog.Alert(this, "生产设置", ex.Message);
        }
    }

    private async void OnSaveComfyui(object sender, RoutedEventArgs e) =>
        await PostProduceConfigAsync(new JsonObject
        {
            ["action"] = "save_comfyui",
            ["base_url"] = ComfyUrlBox.Text.Trim(),
            ["image_workflow"] = ComfyImageWfBox.Text.Trim(),
            ["video_workflow"] = ComfyVideoWfBox.Text.Trim(),
        }, "已保存本机 ComfyUI。再点「启动出图」或「启动出视频」。");

    private async void OnProbeComfyui(object sender, RoutedEventArgs e) =>
        await PostProduceConfigAsync(new JsonObject
        {
            ["action"] = "probe_comfyui",
            ["base_url"] = ComfyUrlBox.Text.Trim(),
        }, "检测完成");

    private async void OnStartComfyImage(object sender, RoutedEventArgs e) =>
        await PostProduceConfigAsync(new JsonObject
        {
            ["action"] = "activate",
            ["lane"] = "image",
            ["adapter"] = "comfyui-image",
        }, "已启动本机 ComfyUI 出图");

    private async void OnStartComfyVideo(object sender, RoutedEventArgs e) =>
        await PostProduceConfigAsync(new JsonObject
        {
            ["action"] = "activate",
            ["lane"] = "video",
            ["adapter"] = "comfyui-video",
        }, "已启动本机 ComfyUI 出视频");

    private async void OnSaveCloudImage(object sender, RoutedEventArgs e) =>
        await PostProduceConfigAsync(new JsonObject
        {
            ["action"] = "save_cloud_image",
            ["api_key"] = CloudImageKeyBox.Password,
            ["base_url"] = CloudImageUrlBox.Text.Trim(),
        }, "已保存云端出图。再点「启动出图」。");

    private async void OnStartCloudImage(object sender, RoutedEventArgs e) =>
        await PostProduceConfigAsync(new JsonObject
        {
            ["action"] = "activate",
            ["lane"] = "image",
            ["adapter"] = "gpt-image-2",
        }, "已启动云端 GPT Image 2 出图");

    private async void OnSaveCloudVideo(object sender, RoutedEventArgs e) =>
        await PostProduceConfigAsync(new JsonObject
        {
            ["action"] = "save_cloud_video",
            ["api_key"] = CloudVideoKeyBox.Password,
            ["model"] = CloudVideoModelBox.Text.Trim(),
            ["resolutions"] = CloudVideoResBox.Text.Trim(),
            ["min_duration"] = CloudVideoMinBox.Text.Trim(),
            ["max_duration"] = CloudVideoMaxBox.Text.Trim(),
        }, "已保存云端视频。再点「启动出视频」。");

    private async void OnStartCloudVideo(object sender, RoutedEventArgs e) =>
        await PostProduceConfigAsync(new JsonObject
        {
            ["action"] = "activate",
            ["lane"] = "video",
            ["adapter"] = "minimax-h3",
        }, "已启动云端 MiniMax H3 出视频");

    private static string? PickFolder(Window owner, string title, string? initial)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = title };
        if (!string.IsNullOrWhiteSpace(initial)) dlg.InitialDirectory = initial;
        return dlg.ShowDialog(owner) == true ? dlg.FolderName : null;
    }

    private void OnBrowseStudioRoot(object sender, RoutedEventArgs e)
    {
        var path = PickFolder(this, "选择工作室根目录", _layout?.StudioRoot);
        if (path is null) return;
        StudioRootBox.Text = path;
        ProjectsDirBox.Text = System.IO.Path.Combine(path, "projects");
        SkillsDirBox.Text = System.IO.Path.Combine(path, "skills");
        AssetsDirBox.Text = System.IO.Path.Combine(path, "assets");
    }

    private void OnBrowseProjectsDir(object sender, RoutedEventArgs e)
    {
        var path = PickFolder(this, "选择项目文件夹", _layout?.ProjectsDir);
        if (path is not null) ProjectsDirBox.Text = path;
    }

    private void OnBrowseSkillsDir(object sender, RoutedEventArgs e)
    {
        var path = PickFolder(this, "选择 SKILLS 文件夹", _layout?.SkillsDir);
        if (path is not null) SkillsDirBox.Text = path;
    }

    private void OnBrowseAssetsDir(object sender, RoutedEventArgs e)
    {
        var path = PickFolder(this, "选择资产文件夹", _layout?.AssetsDir);
        if (path is not null) AssetsDirBox.Text = path;
    }

    private async void OnSaveLayout(object sender, RoutedEventArgs e)
    {
        try
        {
            WorkspaceHint.Text = "正在保存目录并重启对接…";
            var layout = StudioSettings.FromForm(
                StudioRootBox.Text, ProjectsDirBox.Text, SkillsDirBox.Text, AssetsDirBox.Text);
            StudioSettings.Persist(layout);
            _layout = layout;
            if (_host is not null)
                await _host.StopAsync(CancellationToken.None);
            _studio?.Dispose();
            _host = new BackendHost(layout.ProjectsDir);
            _factory = new ProjectFactory(_host.RepoRoot, layout.ProjectsDir);
            _factory.ImportFromRepoProjects();
            var (url, token) = await _host.EnsureRunningAsync(CancellationToken.None);
            _studio = new StudioClient(url, token);
            await _studio.EstablishSessionAsync(CancellationToken.None);
            await RefreshProjectListAsync();
            FillLayoutBoxes();
            RefreshSkillList();
            WorkspaceHint.Text = $"项目目录 {_layout.ProjectsDir} · {_projects.Count} 个项目\n技能目录 {_layout.SkillsDir}";
            HomeStatus.Text = WorkspaceHint.Text;
        }
        catch (Exception ex)
        {
            WorkspaceHint.Text = ex.Message;
        }
    }

    private void RefreshChatModels()
    {
        var bundle = ChatClient.LoadBundle();
        ApiHint.Text = "配置文件：" + ChatClient.ConfigLocation;
        ApiProfileList.ItemsSource = null;
        ApiProfileList.ItemsSource = bundle.Profiles;
        var current = bundle.Profiles.FirstOrDefault(p => p.Name == bundle.Active) ?? bundle.Profiles.FirstOrDefault();
        ApiProfileList.SelectedItem = current;
        FillApiForm(current);
        if (ChatModelBox is not null)
        {
            ChatModelBox.ItemsSource = null;
            ChatModelBox.ItemsSource = bundle.Profiles;
            ChatModelBox.SelectedItem = current;
        }
    }

    private void FillApiForm(ChatProfile? profile)
    {
        if (profile is null) return;
        ApiNameBox.Text = profile.Name;
        ApiUrlBox.Text = profile.BaseUrl;
        ApiKeyBox.Password = profile.ApiKey;
        ApiModelBox.Text = profile.Model;
    }

    private void OnApiProfileSelect(object sender, SelectionChangedEventArgs e)
    {
        if (ApiProfileList.SelectedItem is ChatProfile profile)
            FillApiForm(profile);
    }

    private ChatProfile ReadApiForm() => new()
    {
        Name = string.IsNullOrWhiteSpace(ApiNameBox.Text) ? "未命名模型" : ApiNameBox.Text.Trim(),
        BaseUrl = ApiUrlBox.Text.Trim(),
        ApiKey = ApiKeyBox.Password.Trim(),
        Model = ApiModelBox.Text.Trim(),
    };

    private void OnApiSaveCurrent(object sender, RoutedEventArgs e)
    {
        var profile = ReadApiForm();
        var bundle = ChatClient.LoadBundle();
        var hit = bundle.Profiles.FirstOrDefault(p => p.Name == profile.Name);
        if (hit is null) bundle.Profiles.Add(profile);
        else
        {
            hit.BaseUrl = profile.BaseUrl;
            hit.ApiKey = profile.ApiKey;
            hit.Model = profile.Model;
        }
        bundle.Active = profile.Name;
        ChatClient.SaveBundle(bundle);
        RefreshChatModels();
        ApiHint.Text = $"已保存并选用「{profile.Name}」。";
    }

    private void OnApiAdd(object sender, RoutedEventArgs e)
    {
        var profile = ReadApiForm();
        var bundle = ChatClient.LoadBundle();
        var name = profile.Name;
        var i = 2;
        while (bundle.Profiles.Any(p => p.Name == profile.Name))
            profile.Name = $"{name} {i++}";
        bundle.Profiles.Add(profile);
        bundle.Active = profile.Name;
        ChatClient.SaveBundle(bundle);
        RefreshChatModels();
        ApiHint.Text = $"已添加「{profile.Name}」，可在聊天栏选择。";
    }

    private void OnApiDelete(object sender, RoutedEventArgs e)
    {
        if (ApiProfileList.SelectedItem is not ChatProfile profile) return;
        var bundle = ChatClient.LoadBundle();
        bundle.Profiles.RemoveAll(p => p.Name == profile.Name);
        if (bundle.Profiles.Count == 0)
            bundle = ChatClient.DefaultBundle();
        else if (bundle.Active == profile.Name)
            bundle.Active = bundle.Profiles[0].Name;
        ChatClient.SaveBundle(bundle);
        RefreshChatModels();
    }

    private void OnChatModelPicked(object sender, SelectionChangedEventArgs e)
    {
        if (ChatModelBox.SelectedItem is not ChatProfile profile) return;
        var bundle = ChatClient.LoadBundle();
        if (bundle.Profiles.All(p => p.Name != profile.Name)) return;
        bundle.Active = profile.Name;
        ChatClient.SaveBundle(bundle);
    }

    private void RefreshSkillList()
    {
        if (SkillList is null) return;
        var root = _layout?.SkillsDir ?? "";
        SkillHint.Text = "技能文件夹：" + root;
        var names = System.IO.Directory.Exists(root)
            ? System.IO.Directory.GetDirectories(root).Select(System.IO.Path.GetFileName).Where(n => n is not null).Cast<string>().OrderBy(n => n).ToList()
            : [];
        SkillList.ItemsSource = names;
    }

    private void OnAddSkill(object sender, RoutedEventArgs e)
    {
        if (_layout is null) return;
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "选择含 SKILL.md 的技能文件夹" };
        if (dlg.ShowDialog(this) != true) return;
        InstallSkillFrom(dlg.FolderName);
    }

    private void OnImportBundledSkill(object sender, RoutedEventArgs e)
    {
        if (_host is null || _layout is null) return;
        var bundled = System.IO.Path.Combine(_host.RepoRoot, "skills");
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "从套件 skills 导入（选一个含 SKILL.md 的文件夹）",
            InitialDirectory = bundled,
        };
        if (dlg.ShowDialog(this) != true) return;
        InstallSkillFrom(dlg.FolderName);
    }

    private void InstallSkillFrom(string source)
    {
        if (_layout is null) return;
        if (!System.IO.File.Exists(System.IO.Path.Combine(source, "SKILL.md")))
        {
            SkillHint.Text = "所选文件夹没有 SKILL.md。";
            return;
        }
        var name = System.IO.Path.GetFileName(source.TrimEnd('\\', '/'));
        var dest = System.IO.Path.Combine(_layout.SkillsDir, name);
        if (System.IO.Directory.Exists(dest))
        {
            SkillHint.Text = $"{name} 已在技能文件夹中。";
            return;
        }
        System.IO.Directory.CreateDirectory(_layout.SkillsDir);
        CopyDirectory(source, dest);
        RefreshSkillList();
        SkillHint.Text = $"已安装到 {_layout.SkillsDir}\\{name}";
    }

    private static void CopyDirectory(string source, string dest)
    {
        System.IO.Directory.CreateDirectory(dest);
        foreach (var file in System.IO.Directory.GetFiles(source))
            System.IO.File.Copy(file, System.IO.Path.Combine(dest, System.IO.Path.GetFileName(file)), overwrite: false);
        foreach (var child in System.IO.Directory.GetDirectories(source))
            CopyDirectory(child, System.IO.Path.Combine(dest, System.IO.Path.GetFileName(child)));
    }

    private Task OpenProjectAsync(ProjectItem item) => OpenProjectAsync(item, manageBusy: true);

    private async Task OpenProjectAsync(ProjectItem item, bool manageBusy)
    {
        if (_studio is null) return;
        if (manageBusy && _busy) return;
        try
        {
            if (manageBusy) _busy = true;
            HomeStatus.Text = $"正在打开《{item.Title}》…";
            if (!string.IsNullOrEmpty(_projectRoot) && !PathsEqual(_projectRoot, ResolveProjectRoot(item.Path)))
                SaveProjectChat();
            _studio.SelectProject(item.Id);
            _projectRoot = ResolveProjectRoot(item.Path);
            LoadProjectChat();
            _stage = "overview";
            await ReloadSeriesAsync();
            RecentStore.Touch(new RecentProject
            {
                Id = item.Id,
                Title = item.Title,
                Path = item.Path,
                Kind = item.Kind,
                Subtitle = item.Meta,
            });
            FillRecents();
            ShowWorkbench();
            RefreshChatModels();
            SyncRail();
            if (!string.IsNullOrWhiteSpace(_pendingIdea))
            {
                InsertChat("根据创作意图继续：\n" + _pendingIdea);
                _pendingIdea = null;
            }
            StatusText.Text = "分镜、生成状态和素材记录都在项目里";
        }
        catch (Exception ex)
        {
            ShowHome();
            HomeStatus.Text = ex.Message;
            StudioDialog.Alert(this, "打开项目", ex.Message);
            if (IsMissingProject(ex))
            {
                RecentStore.Remove(item.Id, item.Path);
                FillRecents();
                HomeStatus.Text = $"《{item.Title}》已不在项目文件夹，已从最近打开移除。";
            }
        }
        finally
        {
            if (manageBusy) _busy = false;
        }
    }

    private static bool IsMissingProject(Exception ex)
    {
        var text = ex.Message ?? "";
        return text.Contains("project not found", StringComparison.OrdinalIgnoreCase)
            || text.Contains("还没有 short-drama", StringComparison.OrdinalIgnoreCase);
    }

    private void OnRatioToggle(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(sender, Ratio169) && Ratio169.IsChecked == true)
            Ratio916.IsChecked = false;
        else if (ReferenceEquals(sender, Ratio916) && Ratio916.IsChecked == true)
            Ratio169.IsChecked = false;
        if (Ratio169.IsChecked != true && Ratio916.IsChecked != true)
            Ratio916.IsChecked = true;
    }

    private void OnAttachRef(object sender, RoutedEventArgs e)
    {
        ShowAttachMenu(sender as UIElement, AddComposeAttachment);
    }

    private void OnChatAttach(object sender, RoutedEventArgs e)
    {
        ShowAttachMenu(sender as UIElement, AddChatAttachment);
    }

    private void OnComposeDragOver(object sender, DragEventArgs e) => OnAttachDragOver(e);

    private void OnComposeDrop(object sender, DragEventArgs e) =>
        OnAttachDrop(e, AddComposeAttachment, msg =>
        {
            if (HomeStatus is not null) HomeStatus.Text = msg;
        });

    private void OnChatDragOver(object sender, DragEventArgs e) => OnAttachDragOver(e);

    private void OnChatDrop(object sender, DragEventArgs e) =>
        OnAttachDrop(e, AddChatAttachment, msg =>
        {
            if (StatusText is not null) StatusText.Text = msg;
        });

    private static void OnAttachDragOver(DragEventArgs e)
    {
        e.Effects = HasDropFiles(e) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private static void OnAttachDrop(DragEventArgs e, Action<string> add, Action<string> onReject)
    {
        e.Handled = true;
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files) return;
        var added = 0;
        foreach (var path in files)
        {
            if (!IsAttachable(path)) continue;
            add(path);
            added++;
        }
        if (added == 0)
            onReject("只接受小说原文（txt/md）或参考图。");
    }

    private static bool HasDropFiles(DragEventArgs e) =>
        e.Data.GetDataPresent(DataFormats.FileDrop)
        && e.Data.GetData(DataFormats.FileDrop) is string[] files
        && files.Any(IsAttachable);

    private static bool IsAttachable(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || System.IO.Directory.Exists(path)) return false;
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return ext is ".txt" or ".md" or ".markdown" or ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" or ".bmp";
    }

    private string ChatGreeting =>
        _produceMode switch
        {
            "always" => "配好设置 → API 与生产后端后，我会按技能库写 Markdown。当前为始终自动：点名出图/出视频且环境齐全时，预览通过后会自动提交。",
            "session" => "配好设置 → API 后，我会按技能库写 Markdown。当前为会话授权：勾选「本会话允许小烧自动生产」后，点名出图/出视频才可自动提交；否则请到「生产」页确认。",
            _ => "配好设置 → API 后，我会按技能库路由并写入本集 Markdown。可以说「写剧本」或点阶段芯片。生产默认须在工作台确认。",
        };

    private void ResetChatGreeting()
    {
        _chat.Clear();
        _chat.Add(new ChatMessage { Role = "assistant", Text = ChatGreeting });
    }

    private void LoadProjectChat()
    {
        _chat.Clear();
        var rows = string.IsNullOrEmpty(_projectRoot) ? [] : ProjectChatStore.Load(_projectRoot);
        if (rows.Count == 0)
            _chat.Add(new ChatMessage { Role = "assistant", Text = ChatGreeting });
        else
            foreach (var row in rows)
                _chat.Add(row);
        ScrollChat();
    }

    private void SaveProjectChat()
    {
        if (string.IsNullOrEmpty(_projectRoot)) return;
        try
        {
            ProjectChatStore.Save(_projectRoot, _chat);
        }
        catch
        {
            /* keep UI usable if disk is locked */
        }
    }

    private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e) =>
        SaveProjectChat();

    private void ShowAttachMenu(UIElement? anchor, Action<string> add)
    {
        var menu = new ContextMenu();
        var novel = new MenuItem { Header = "添加小说原文…" };
        novel.Click += (_, _) => PickAttachments("小说原文|*.txt;*.md;*.markdown|所有文件|*.*", "novel", add);
        var image = new MenuItem { Header = "添加参考图…" };
        image.Click += (_, _) => PickAttachments("图片|*.png;*.jpg;*.jpeg;*.webp;*.gif;*.bmp|所有文件|*.*", "image", add);
        menu.Items.Add(novel);
        menu.Items.Add(image);
        if (anchor is not null)
        {
            menu.PlacementTarget = anchor;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
        }
        menu.IsOpen = true;
    }

    private void PickAttachments(string filter, string kind, Action<string> add)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = kind == "image" ? "选择参考图" : "选择小说原文",
            Filter = filter,
            Multiselect = true,
            CheckFileExists = true,
        };
        if (dlg.ShowDialog(this) != true) return;
        foreach (var path in dlg.FileNames)
            add(path);
    }

    private void AddComposeAttachment(string path) =>
        AttachFile(_composeAttach, ComposeAttachHost, path, copyIntoOpenProject: false);

    private void AddChatAttachment(string path) =>
        AttachFile(_chatAttach, ChatAttachHost, path, copyIntoOpenProject: true);

    private void AttachFile(List<AttachItem> list, WrapPanel? host, string path, bool copyIntoOpenProject)
    {
        if (list.Count >= 8)
        {
            if (HomeStatus is not null) HomeStatus.Text = "一次最多 8 个参考附件。";
            return;
        }
        if (list.Any(a => string.Equals(a.Path, path, StringComparison.OrdinalIgnoreCase)))
            return;
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        var image = ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" or ".bmp";
        var item = new AttachItem
        {
            Kind = image ? "image" : "novel",
            Path = path,
            Name = System.IO.Path.GetFileName(path),
            Excerpt = image ? null : ReadExcerpt(path),
        };
        if (copyIntoOpenProject && !string.IsNullOrEmpty(_projectRoot))
            CopyAttachmentIntoProject(_projectRoot, item);
        list.Add(item);
        RenderAttachChips(list, host);
        if (HomeStatus is not null && host == ComposeAttachHost)
            HomeStatus.Text = image ? $"已附加参考图 {item.Name}" : $"已附加原文 {item.Name}";
    }

    private static string? ReadExcerpt(string path)
    {
        try
        {
            var text = System.IO.File.ReadAllText(path);
            if (text.Length <= 8000) return text;
            return text[..8000] + "\n\n…(原文已截取前 8000 字)";
        }
        catch
        {
            return null;
        }
    }

    private void RenderAttachChips(List<AttachItem> list, WrapPanel? host)
    {
        if (host is null) return;
        host.Children.Clear();
        var ink = (Brush)FindResource("InkBrush");
        var gold = (Brush)FindResource("GoldBrush");
        var line = (Brush)FindResource("LineBrush");
        var surface = (Brush)FindResource("Surface2Brush");
        foreach (var item in list.ToList())
        {
            var captured = item;
            var chip = new Border
            {
                CornerRadius = new CornerRadius(8),
                BorderBrush = line,
                BorderThickness = new Thickness(1),
                Background = surface,
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 0, 8, 6),
            };
            var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            if (item.Kind == "image" && System.IO.File.Exists(item.Path))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(item.Path);
                    bmp.DecodePixelWidth = 48;
                    bmp.EndInit();
                    bmp.Freeze();
                    row.Children.Add(new Image { Source = bmp, Width = 28, Height = 28, Stretch = Stretch.UniformToFill, Margin = new Thickness(0, 0, 6, 0) });
                }
                catch { /* skip thumb */ }
            }
            else
            {
                row.Children.Add(new TextBlock { Text = "文", Foreground = gold, FontWeight = FontWeights.Bold, Width = 18, VerticalAlignment = VerticalAlignment.Center });
            }
            row.Children.Add(new TextBlock
            {
                Text = item.Name,
                Foreground = ink,
                FontSize = 12,
                MaxWidth = 140,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
            });
            var remove = new Button
            {
                Content = "×",
                Style = (Style)FindResource("GhostButton"),
                Width = 22,
                Height = 22,
                MinHeight = 22,
                Padding = new Thickness(0),
                Margin = new Thickness(6, 0, 0, 0),
            };
            remove.Click += (_, _) =>
            {
                list.Remove(captured);
                RenderAttachChips(list, host);
            };
            row.Children.Add(remove);
            chip.Child = row;
            host.Children.Add(chip);
        }
        host.Visibility = list.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private string ResolveProjectRoot(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        if (System.IO.Directory.Exists(path)) return System.IO.Path.GetFullPath(path);
        if (_layout is not null)
        {
            var joined = System.IO.Path.Combine(_layout.ProjectsDir, path);
            if (System.IO.Directory.Exists(joined)) return System.IO.Path.GetFullPath(joined);
        }
        return path;
    }

    private void CopyComposeAttachmentsInto(string projectRoot)
    {
        foreach (var item in _composeAttach)
            CopyAttachmentIntoProject(projectRoot, item);
        if (_composeAttach.Count > 0)
        {
            HomeStatus.Text = $"已把 {_composeAttach.Count} 个参考放进项目。";
            _composeAttach.Clear();
            RenderAttachChips(_composeAttach, ComposeAttachHost);
        }
    }

    private static void CopyAttachmentIntoProject(string projectRoot, AttachItem item)
    {
        if (string.IsNullOrWhiteSpace(projectRoot) || !System.IO.File.Exists(item.Path)) return;
        var folder = item.Kind == "image"
            ? System.IO.Path.Combine(projectRoot, "制作成果", "参考")
            : System.IO.Path.Combine(projectRoot, "项目开发", "原著");
        System.IO.Directory.CreateDirectory(folder);
        var dest = System.IO.Path.Combine(folder, item.Name);
        if (!string.Equals(item.Path, dest, StringComparison.OrdinalIgnoreCase))
            System.IO.File.Copy(item.Path, dest, overwrite: true);
        item.Path = dest;
    }

    private static string FormatAttachments(IReadOnlyList<AttachItem> list)
    {
        if (list.Count == 0) return "";
        var sb = new StringBuilder();
        foreach (var item in list)
        {
            if (item.Kind == "image")
                sb.Append("\n[附件:参考图] ").Append(item.Name).Append(" → ").Append(item.Path);
            else
            {
                sb.Append("\n[附件:小说原文] ").Append(item.Name);
                if (!string.IsNullOrEmpty(item.Excerpt))
                    sb.Append("\n").Append(item.Excerpt);
            }
        }
        return sb.ToString();
    }

    private static string AttachCaption(IReadOnlyList<AttachItem> list)
    {
        if (list.Count == 0) return "";
        return "\n📎 " + string.Join("、", list.Select(a => a.Name));
    }

    private void OnComposeKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            e.Handled = true;
            OnComposeSubmit(sender, e);
        }
    }

    private async void OnComposeSubmit(object sender, RoutedEventArgs e) =>
        await CreateFromComposerAsync();

    private async void OnNewNovelProject(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ComposeBox.Text) || IsPlaceholder(ComposeBox.Text))
            ComposeBox.Text = "根据小说改编一部竖屏短剧，保留原著核心冲突。";
        NeedCast.IsChecked = true;
        NeedArt.IsChecked = true;
        await CreateFromComposerAsync();
    }

    private async Task CreateFromComposerAsync()
    {
        if (_factory is null || _studio is null || _busy) return;
        var idea = ComposeBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(idea) || IsPlaceholder(idea))
        {
            HomeStatus.Text = "先写一句镜头、梗概或故事。";
            return;
        }
        var title = DeriveTitle(idea);
        var ratio = Ratio169.IsChecked == true ? "16:9" : "9:16";
        var notes = new StringBuilder(idea);
        if (NeedCast.IsChecked == true) notes.Append("\n- 需要角色设定");
        if (NeedArt.IsChecked == true) notes.Append("\n- 需要美术设定");
        notes.Append("\n- 画幅 ").Append(ratio);
        foreach (var attach in _composeAttach)
            notes.Append(attach.Kind == "image" ? "\n- 参考图 " : "\n- 小说原文 ").Append(attach.Name);
        try
        {
            _busy = true;
            HomeStatus.Text = $"正在创建《{title}》…";
            var createdRoot = await _factory.InitializeAsync(title, ratio, notes.ToString(), CancellationToken.None);
            await RefreshProjectListAsync();
            var rel = PathRelative(createdRoot);
            var created = _projects.FirstOrDefault(p =>
                    p.Path.Replace('\\', '/') == rel.Replace('\\', '/')
                    || p.Path.Replace('\\', '/').EndsWith(rel.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
                ?? _projects.FirstOrDefault(p => p.Title == title)
                ?? _projects.FirstOrDefault();
            if (created is null)
            {
                HomeStatus.Text = "项目已创建，但列表尚未发现。请点刷新或重启工作区。";
                return;
            }
            _pendingIdea = notes.ToString();
            CopyComposeAttachmentsInto(createdRoot);
            await OpenProjectAsync(created, manageBusy: false);
            HomeStatus.Text = "";
        }
        catch (Exception ex)
        {
            HomeStatus.Text = ex.Message;
            StudioDialog.Alert(this, "创建项目", ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    private string PathRelative(string absolute)
    {
        var root = _host?.Workspace ?? "";
        var full = System.IO.Path.GetFullPath(absolute);
        var basePath = System.IO.Path.GetFullPath(root).TrimEnd('\\', '/') + System.IO.Path.DirectorySeparatorChar;
        if (full.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            return full[basePath.Length..].Replace('\\', '/');
        return full.Replace('\\', '/');
    }

    private static string DeriveTitle(string idea)
    {
        var line = idea.Replace("\r", "").Split('\n')[0].Trim();
        foreach (var prefix in new[] { "我想做", "做一个", "创作", "写一部", "拍一部" })
        {
            if (line.StartsWith(prefix, StringComparison.Ordinal))
                line = line[prefix.Length..].Trim();
        }
        line = line.Trim('。', '.', '！', '!', '？', '?', '，', ',');
        if (line.Length > 24) line = line[..24];
        return string.IsNullOrWhiteSpace(line) ? "未命名短剧" : line;
    }

    private void ApplyComposePlaceholder()
    {
        var hint = ComposeBox.Tag as string ?? "";
        ComposeBox.Text = hint;
        ComposeBox.Foreground = (Brush)FindResource("SoftInkBrush");
        ComposeBox.GotFocus += (_, _) =>
        {
            if (IsPlaceholder(ComposeBox.Text))
            {
                ComposeBox.Text = "";
                ComposeBox.Foreground = (Brush)FindResource("InkBrush");
            }
        };
        ComposeBox.LostFocus += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(ComposeBox.Text))
            {
                ComposeBox.Text = hint;
                ComposeBox.Foreground = (Brush)FindResource("SoftInkBrush");
            }
        };
    }

    private bool IsPlaceholder(string text) =>
        text == (ComposeBox.Tag as string);

    private async Task ReloadSeriesAsync()
    {
        if (_studio is null) return;
        _series = await _studio.SeriesAsync(CancellationToken.None);
        ProjectTitle.Text = "《" + Str(_series["title"]) + "》";
        var rhythm = _series["rhythm"] as JsonObject;
        var target = Num(_series["format"]?["target_seconds_per_episode"]);
        QuotaText.Text = target > 0 ? $"目标 {target:0}s" : "";

        _episodes.Clear();
        var list = _series["episodes"] as JsonArray ?? [];
        var pending = 0;
        foreach (var item in list.OfType<JsonObject>())
        {
            var has = item["has"] as JsonObject;
            var row = new EpisodeRow
            {
                Id = Str(item["id"]),
                Title = Str(item["title"], "未命名"),
                Shots = (int)Num(item["shots"]),
                Seconds = Num(item["seconds"]),
                HasBoard = has?["board"]?.GetValue<bool>() == true,
                HasScript = has?["script"]?.GetValue<bool>() == true,
                HasSettings = has?["settings"]?.GetValue<bool>() == true,
            };
            row.Sub = row.HasBoard
                ? $"{row.Seconds:0}s · {row.Shots} 镜"
                : row.HasScript ? "已有剧本" : "未开始";
            if (!row.HasBoard && row.HasScript) pending++;
            _episodes.Add(row);
        }
        PendingHint.Text = pending > 0 ? $"{pending} 集 未认领" : $"{_episodes.Count} 集";
        if (_episodes.Count > 0)
        {
            var pick = _episodes.FirstOrDefault(e => e.Id == _episodeId) ?? _episodes[0];
            EpisodeList.SelectedItem = pick;
            await LoadEpisodeAsync(pick.Id);
        }
        else
            RenderMain();
    }

    private async void OnEpisodeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EpisodeList.SelectedItem is not EpisodeRow row) return;
        foreach (var item in _episodes) item.IsOn = item.Id == row.Id;
        await LoadEpisodeAsync(row.Id);
    }

    private async Task LoadEpisodeAsync(string id)
    {
        if (_studio is null) return;
        _episodeId = id;
        try
        {
            _episode = await _studio.EpisodeAsync(id, CancellationToken.None);
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
            _episode = null;
        }
        RenderMain();
    }

    private void RenderMain()
    {
        ActionBar.Children.Clear();
        var heading = Stages.First(s => s.Key == _stage).Label;
        MainHeading.Text = heading;
        StageKicker.Text = $"STAGE  ·  {heading}";
        LeadText.Text = _stage switch
        {
            "board" => "印张分镜：时长×景别节奏条、起止链、冻结关键帧。提示词可复制，生产仍走确认闸门。",
            "cast" => "花名册与设定印张。识别锚点来自视觉设定.md；性情/关系等阅读卡有则显示。",
            "art" => "地点与道具共用角色印张骨架，数据来自视觉设定.md。",
            "script" => "当前集剧本卡：场次芯片、对白三列、钩子与台词簿。开场钩写在标题下。",
            "prompts" => "可直接复制 IMG / MOTION / 冻结关键帧。",
            "produce" => "先预览再确认。提交给设置里当前启动的出图或出视频后端。",
            "film" => "剪辑单入出点与成片来源。",
            "review" => "审查意见与必须修订项。",
            "overview" => "全剧进度、分镜覆盖和下一步。",
            _ => "创作真相是五份 Markdown，助手只读写同一套文件。",
        };

        if (_stage == "board")
        {
            AddChip("写分镜", () => QueueSkill("short-drama-storyboard", $"请为 {_episodeId ?? "这一集"} 写分镜.md。"));
            AddChip("写成图片提示词", () => QueueSkill("short-drama-image-prompts", $"请按 {_episodeId ?? "这一集"} 的分镜写图片提示词.md。"));
            AddChip("写成视频提示词", () => QueueSkill("short-drama-video-prompts", $"请按 {_episodeId ?? "这一集"} 的分镜写视频提示词.md。"));
            AddChip("去生产", () => { _stage = "produce"; SyncRail(); RenderMain(); });
            AddChip("导出 JSON", ExportEpisodeView);
            MainHost.Content = PaperViews.Board(this, _episode, CopyPlain, BindAuthImage);
            return;
        }
        if (_stage == "cast")
        {
            AddChip("拆视觉设定", () => QueueSkill("short-drama-assets", $"请为 {_episodeId ?? "这一集"} 写视觉设定.md。"));
            AddChip("导出 JSON", ExportEpisodeView);
            MainHost.Content = PaperViews.Cast(this, _episode, new[] { "人物", "造型" }, CopyPlain, BindAuthImage);
            return;
        }
        if (_stage == "art")
        {
            AddChip("拆地点道具", () => QueueSkill("short-drama-assets", $"请为 {_episodeId ?? "这一集"} 补地点与道具到视觉设定.md。"));
            AddChip("导出 JSON", ExportEpisodeView);
            MainHost.Content = PaperViews.Cast(this, _episode, new[] { "地点", "道具" }, CopyPlain, BindAuthImage);
            return;
        }
        if (_stage == "script")
        {
            AddChip("写成剧本", () => QueueSkill("short-drama-write", $"请为 {_episodeId ?? "EP001"} 写剧本.md。"));
            AddChip("导出 JSON", ExportEpisodeView);
            MainHost.Content = PaperViews.Script(this, _episode, Num(_series?["format"]?["target_seconds_per_episode"]), CopyPlain);
            return;
        }
        if (_stage == "prompts")
        {
            AddChip("图片提示词", () => QueueSkill("short-drama-image-prompts", $"请写 {_episodeId ?? "这一集"} 的图片提示词.md。"));
            AddChip("视频提示词", () => QueueSkill("short-drama-video-prompts", $"请写 {_episodeId ?? "这一集"} 的视频提示词.md。"));
            MainHost.Content = BuildPrompts();
            return;
        }
        if (_stage == "produce")
        {
            AddChip("测连通", () => _ = ProbeEnvironmentAsync());
            AddChip("批量出图", () => _ = BatchProduceAsync("image"));
            AddChip("批量出视频", () => _ = BatchProduceAsync("video"));
            MainHost.Content = BuildProduce();
            return;
        }
        if (_stage == "film")
        {
            AddChip("写剪辑单", () => QueueSkill("short-drama-edit", $"请为 {_episodeId ?? "这一集"} 写剪辑单.md。"));
            AddChip("打开成片目录", OpenFilmFolder);
            MainHost.Content = BuildFilm();
            return;
        }
        if (_stage == "review")
        {
            AddChip("审查本集", () => QueueSkill("short-drama-review", $"请审查 {_episodeId ?? "这一集"}。"));
            AddChip("按意见改回", () => InsertChatAndSend($"请按 {_episodeId ?? "这一集"} 的审查意见逐项改回对应 Markdown，不要跳过 must。"));
            MainHost.Content = BuildReview();
            return;
        }
        if (_stage == "overview")
        {
            AddChip("导演 pass", () => QueueSkill("short-drama", $"请按导演总控 pass 过一遍 {_episodeId ?? "本集"}：阐述下沉、分镜 Coverage、提示词对账与自检；不要自动生产。"));
            AddChip("原著分析", () => QueueSkill("short-drama-novel-analyze", "请对当前项目关联的小说/原文做原著分析。"));
            AddChip("系列开发", () => QueueSkill("short-drama-develop", "请按题材补齐系列开发文档（改编方案、创作简报、导演阐述、故事引擎与分集地图）。"));
            AddChip("打开分镜", () => { _stage = "board"; SyncRail(); RenderMain(); });
            AddChip("写成剧本", () => QueueSkill("short-drama-write", $"请为 {_episodeId ?? "EP001"} 写剧本.md。"));
            AddChip("设定再出分镜", () => InsertChatAndSend("按这集剧本补视觉设定再出分镜"));
            AddChip("导出 JSON", ExportEpisodeView);
            MainHost.Content = BuildCoverage();
            return;
        }
        MainHost.Content = BuildDocFallback();
    }

    private UIElement BuildMarkdownStage()
    {
        var docs = _episode?["docs"] as JsonObject;
        var path = _stage == "film"
            ? Str(docs?["剪辑单.md"])
            : Str(docs?["review"]);
        if (string.IsNullOrEmpty(path))
            return Hint(_stage == "film" ? "还没有剪辑单。" : "还没有审查意见。");
        return BuildFileBox(path, "正在读取…");
    }

    private UIElement BuildFileBox(string path, string loading)
    {
        var box = new TextBox
        {
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            Background = (Brush)FindResource("SurfaceBrush"),
            Foreground = (Brush)FindResource("InkBrush"),
            BorderBrush = (Brush)FindResource("LineBrush"),
            Padding = new Thickness(12),
            MinHeight = 180,
            FontFamily = new FontFamily("Cascadia Mono, Consolas, Microsoft YaHei UI"),
            Text = loading,
        };
        _ = LoadDocAsync(path, box);
        return box;
    }

    private UIElement BuildFilm()
    {
        var panel = new StackPanel();
        var cuts = (_episode?["cutlist"]?["cuts"] as JsonArray)?.OfType<JsonObject>().ToList() ?? [];
        if (cuts.Count > 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"剪辑单 · {cuts.Count} 条",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8),
            });
            foreach (var cut in cuts)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"{Str(cut["id"])}  {Str(cut["title"])}  ·  {Str(cut["media"])}  {Num(cut["in"]):0}–{Num(cut["out"]):0}s",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 6),
                    Foreground = (Brush)FindResource("SoftInkBrush"),
                });
            }
        }
        var path = Str((_episode?["docs"] as JsonObject)?["剪辑单.md"]);
        if (!string.IsNullOrEmpty(path))
        {
            panel.Children.Add(new TextBlock { Text = "原文", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 8) });
            panel.Children.Add(BuildFileBox(path, "正在读取剪辑单…"));
        }
        else if (cuts.Count == 0)
            return Hint("还没有剪辑单。写好剪辑单.md 后会在这里列出入出点。");
        return panel;
    }

    private UIElement BuildReview()
    {
        var panel = new StackPanel();
        var findings = (_episode?["review"]?["findings"] as JsonArray)?.OfType<JsonObject>().ToList() ?? [];
        if (findings.Count > 0)
        {
            foreach (var item in findings)
            {
                var card = new Border
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(14),
                    CornerRadius = new CornerRadius(12),
                    Background = (Brush)FindResource("SurfaceBrush"),
                    BorderBrush = (Brush)FindResource("LineBrush"),
                    BorderThickness = new Thickness(1),
                };
                var stack = new StackPanel();
                stack.Children.Add(new TextBlock
                {
                    Text = Str(item["sev"]).ToUpperInvariant() + "  ·  " + Str(item["title"]),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)FindResource("GoldBrush"),
                });
                stack.Children.Add(new TextBlock
                {
                    Text = Str(item["fix"], Str(item["body"])),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 6, 0, 0),
                    Foreground = (Brush)FindResource("SoftInkBrush"),
                });
                card.Child = stack;
                panel.Children.Add(card);
            }
        }
        var path = Str((_episode?["docs"] as JsonObject)?["review"]);
        if (!string.IsNullOrEmpty(path))
        {
            panel.Children.Add(new TextBlock { Text = "审查原文", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 8) });
            panel.Children.Add(BuildFileBox(path, "正在读取审查意见…"));
        }
        else if (findings.Count == 0)
            return Hint("还没有审查意见。");
        return panel;
    }

    private async Task LoadDocAsync(string path, TextBox box)
    {
        if (_studio is null) return;
        try
        {
            var file = await _studio.FileAsync(path, CancellationToken.None);
            box.Text = Str(file["content"]);
        }
        catch (Exception ex)
        {
            box.Text = ex.Message;
        }
    }

    private void SyncRail()
    {
        foreach (var child in StageRail.Children.OfType<RadioButton>())
            child.IsChecked = (string)child.Tag == _stage;
    }

    private void AddChip(string label, Action? click)
    {
        var btn = new Button { Content = label, Style = (Style)FindResource("GhostButton") };
        if (click is null) btn.IsEnabled = false;
        else btn.Click += (_, _) => click();
        ActionBar.Children.Add(btn);
    }

    private void AddChip(string label, bool enabled) => AddChip(label, enabled ? () => { } : null);

    private UIElement BuildCoverage()
    {
        var panel = new StackPanel();
        if (_episodes.Count == 0)
            return Hint("项目刚创建，还没有分集。在右侧请助手写第一集剧本.md。");

        panel.Children.Add(new TextBlock
        {
            Text = "五文档进度（本集）",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8),
        });
        panel.Children.Add(BuildFiveDocProgress());
        panel.Children.Add(new Border
        {
            Height = 1,
            Background = (Brush)FindResource("LineBrush"),
            Margin = new Thickness(0, 16, 0, 16),
        });

        var target = Num(_series?["format"]?["target_seconds_per_episode"]);
        var table = MakeTable(new[] { "集", "段", "镜", "总时长", "均镜长", "建议时长", "阶段上限" });
        foreach (var ep in _episodes.Where(e => e.HasBoard))
        {
            var avg = ep.Shots > 0 ? ep.Seconds / ep.Shots : 0;
            var over = target > 0 && ep.Seconds > target;
            AddTableRow(table, new[]
            {
                ep.Id,
                SceneCount(ep.Id).ToString(),
                ep.Shots.ToString(),
                target > 0 ? $"{ep.Seconds:0}s / {target:0}s" : $"{ep.Seconds:0}s",
                avg > 0 ? $"{avg:0.0}s" : "—",
                target > 0 ? $"{target:0} 秒" : "未设",
                over ? "镜超节拍" : "✓",
            }, over);
        }
        panel.Children.Add(table);

        var targetSec = target;
        var gauge = new StackPanel { Margin = new Thickness(0, 18, 0, 0) };
        gauge.Children.Add(new TextBlock { Text = "全剧时长仪", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        foreach (var ep in _episodes.Where(e => e.HasBoard || e.HasScript))
        {
            var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            row.Children.Add(new TextBlock { Text = ep.Id, FontFamily = new FontFamily("Consolas"), VerticalAlignment = VerticalAlignment.Center });
            var track = new Border { Height = 14, Background = (Brush)FindResource("Surface2Brush"), BorderBrush = (Brush)FindResource("LineBrush"), BorderThickness = new Thickness(1) };
            var fillW = targetSec > 0 ? Math.Min(1, ep.Seconds / targetSec) : (ep.Seconds > 0 ? 1 : 0);
            track.Child = new Border { Width = 4, HorizontalAlignment = HorizontalAlignment.Left, Background = (Brush)FindResource("GoldBrush"), Margin = new Thickness(0) };
            // ratio via grid
            var inner = new Grid();
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(0.02, fillW), GridUnitType.Star) });
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(0.02, 1 - fillW), GridUnitType.Star) });
            var bar = new Border { Background = (Brush)FindResource("GoldBrush") };
            Grid.SetColumn(bar, 0);
            inner.Children.Add(bar);
            track.Child = inner;
            Grid.SetColumn(track, 1);
            row.Children.Add(track);
            var label = new TextBlock { Text = targetSec > 0 ? $"{ep.Seconds:0}/{targetSec:0}s" : $"{ep.Seconds:0}s", Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Foreground = (Brush)FindResource("SoftInkBrush") };
            Grid.SetColumn(label, 2);
            row.Children.Add(label);
            gauge.Children.Add(row);
        }
        if (_episodes.Any(e => e.HasBoard || e.HasScript))
            panel.Children.Add(gauge);

        var missing = _episodes.Where(e => e.HasScript && !e.HasBoard).ToList();
        if (missing.Count > 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "尚无分镜",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 24, 0, 6),
            });
            panel.Children.Add(new TextBlock
            {
                Text = "这些集已有剧本但还没有分镜。生成以现有剧本为输入。",
                Foreground = (Brush)FindResource("SoftInkBrush"),
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap,
            });
            foreach (var ep in missing)
            {
                var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
                var btn = new Button
                {
                    Content = "生成本集",
                    Style = (Style)FindResource("GhostButton"),
                    Margin = new Thickness(0),
                };
                btn.Click += (_, _) => InsertChat($"请为 {ep.Id} 写分镜.md。");
                DockPanel.SetDock(btn, Dock.Right);
                row.Children.Add(btn);
                row.Children.Add(new TextBlock
                {
                    Text = $"{ep.Id}  {ep.Title}",
                    VerticalAlignment = VerticalAlignment.Center,
                });
                panel.Children.Add(row);
            }
        }
        return panel;
    }

    private int SceneCount(string id)
    {
        if (_episodeId == id)
        {
            var scenes = _episode?["script"]?["scenes"] as JsonArray;
            if (scenes is not null) return scenes.Count;
        }
        return 0;
    }

    private UIElement BuildFiveDocProgress()
    {
        var wrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
        var ep = _episodes.FirstOrDefault(e => e.Id == _episodeId) ?? _episodes.FirstOrDefault();
        var has = ep is null ? null : (_series?["episodes"] as JsonArray)?
            .OfType<JsonObject>()
            .FirstOrDefault(o => Str(o["id"]) == ep.Id)?["has"] as JsonObject;
        foreach (var (key, label) in new[]
                 {
                     ("script", "剧本"),
                     ("settings", "视觉设定"),
                     ("board", "分镜"),
                     ("imgp", "图片提示词"),
                     ("vidp", "视频提示词"),
                 })
        {
            var ok = has?[key]?.GetValue<bool>() == true;
            wrap.Children.Add(new Border
            {
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(10, 4, 10, 4),
                CornerRadius = new CornerRadius(8),
                Background = (Brush)FindResource(ok ? "Surface2Brush" : "SurfaceBrush"),
                BorderBrush = (Brush)FindResource(ok ? "GoldBrush" : "LineBrush"),
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = (ok ? "✓ " : "○ ") + label,
                    Foreground = (Brush)FindResource(ok ? "InkBrush" : "SoftInkBrush"),
                    FontSize = 12,
                },
            });
        }
        return wrap;
    }

    private UIElement BuildPrompts()
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = "本集参考图（REF）",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6),
        });
        panel.Children.Add(BuildRefChips());
        void Dump(string title, JsonArray? rows, string textKey)
        {
            panel.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 6) });
            var any = false;
            foreach (var row in rows?.OfType<JsonObject>() ?? [])
            {
                var text = Str(row[textKey]);
                if (text.Length == 0) continue;
                any = true;
                var heading = (Str(row["id"]) + "  " + Str(row["title"])).Trim();
                panel.Children.Add(BuildPromptHoverCard(heading, text));
            }
            if (!any) panel.Children.Add(new TextBlock { Text = "这一类还没有提示词。", Foreground = (Brush)FindResource("SoftInkBrush") });
        }
        Dump("图片", _episode?["imagePrompts"] as JsonArray, "prompt");
        Dump("视频", _episode?["videoPrompts"] as JsonArray, "prompt");
        var keys = new JsonArray();
        foreach (var shot in (_episode?["board"]?["shots"] as JsonArray ?? []).OfType<JsonObject>())
        {
            if (string.IsNullOrEmpty(Str(shot["keyframe"]))) continue;
            keys.Add(new JsonObject
            {
                ["id"] = Str(shot["id"]),
                ["title"] = Str(shot["title"]),
                ["prompt"] = Str(shot["keyframe"]),
            });
        }
        Dump("冻结关键帧", keys, "prompt");
        return panel;
    }

    private UIElement BuildRefChips()
    {
        var wrap = new WrapPanel();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var shot in (_episode?["board"]?["shots"] as JsonArray ?? []).OfType<JsonObject>())
        {
            foreach (var slot in (shot["refs"] as JsonArray ?? []).OfType<JsonObject>())
            {
                var path = Str(slot["path"]);
                if (string.IsNullOrEmpty(path) || !seen.Add(path)) continue;
                var use = Str(slot["use"], Str(slot["kind"], "REF"));
                var chip = new Border
                {
                    Margin = new Thickness(0, 0, 8, 8),
                    Padding = new Thickness(8, 4, 8, 4),
                    CornerRadius = new CornerRadius(8),
                    Background = (Brush)FindResource("SurfaceBrush"),
                    BorderBrush = (Brush)FindResource("LineBrush"),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                    ToolTip = path,
                };
                var label = System.IO.Path.GetFileName(path.Replace('/', '\\'));
                if (string.IsNullOrEmpty(label)) label = path;
                chip.Child = new TextBlock
                {
                    Text = $"{use} · {label}",
                    FontSize = 12,
                    Foreground = (Brush)FindResource("SoftInkBrush"),
                };
                var copyPath = path;
                chip.MouseLeftButtonUp += (_, _) =>
                {
                    CopyPlain(copyPath);
                    if (StatusText is not null) StatusText.Text = "已复制参考路径";
                };
                wrap.Children.Add(chip);
            }
        }
        if (wrap.Children.Count == 0)
            return new TextBlock
            {
                Text = "分镜里还没有绑定参考图路径。写分镜时填「输入参考图」后会显示在这里。",
                Foreground = (Brush)FindResource("SoftInkBrush"),
                TextWrapping = TextWrapping.Wrap,
            };
        return wrap;
    }

    private UIElement BuildPromptHoverCard(string heading, string prompt)
    {
        var block = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        if (!string.IsNullOrWhiteSpace(heading))
        {
            block.Children.Add(new TextBlock
            {
                Text = heading,
                Foreground = (Brush)FindResource("SoftInkBrush"),
                Margin = new Thickness(0, 0, 0, 4),
                TextWrapping = TextWrapping.Wrap,
            });
        }

        var shell = new Border
        {
            Background = (Brush)FindResource("SurfaceBrush"),
            BorderBrush = (Brush)FindResource("LineBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(0),
        };
        var grid = new Grid();
        var box = new TextBox
        {
            Text = prompt,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = (Brush)FindResource("InkBrush"),
            CaretBrush = (Brush)FindResource("GoldBrush"),
            Padding = new Thickness(10, 10, 10, 44),
            MinHeight = 72,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 220,
        };
        grid.Children.Add(box);

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 8, 8),
            Visibility = Visibility.Collapsed,
        };
        Panel.SetZIndex(toolbar, 2);

        var copyBtn = new Button
        {
            Content = "复制提示词",
            Style = (Style)FindResource("GhostButton"),
            Margin = new Thickness(0, 0, 6, 0),
            Padding = new Thickness(10, 4, 10, 4),
            Height = 28,
            FontSize = 12,
        };
        copyBtn.Click += (_, _) =>
        {
            CopyPlain(prompt);
            if (StatusText is not null) StatusText.Text = "已复制提示词";
        };

        var feedBtn = new Button
        {
            Content = "喂给小烧",
            Style = (Style)FindResource("GoldButton"),
            Margin = new Thickness(0),
            Padding = new Thickness(10, 4, 10, 4),
            Height = 28,
            FontSize = 12,
        };
        feedBtn.Click += (_, _) =>
        {
            InsertChat(prompt);
            if (StatusText is not null) StatusText.Text = "已喂给小烧";
        };

        toolbar.Children.Add(copyBtn);
        toolbar.Children.Add(feedBtn);
        grid.Children.Add(toolbar);
        shell.Child = grid;

        shell.MouseEnter += (_, _) =>
        {
            toolbar.Visibility = Visibility.Visible;
            shell.BorderBrush = (Brush)FindResource("GoldBrush");
        };
        shell.MouseLeave += (_, _) =>
        {
            toolbar.Visibility = Visibility.Collapsed;
            shell.BorderBrush = (Brush)FindResource("LineBrush");
        };

        block.Children.Add(shell);
        return block;
    }

    private UIElement BuildProduce()
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = "先预览提示词与输出路径，再明确确认。确认后才会调用设置里已启动的出图或出视频配置。批量时闸门模式仍逐条确认；始终/会话授权可连跑并回报条数。",
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)FindResource("SoftInkBrush"),
            Margin = new Thickness(0, 0, 0, 12),
        });
        var adapterHint = new TextBlock
        {
            Name = "AdapterHint",
            Foreground = (Brush)FindResource("SoftInkBrush"),
            Margin = new Thickness(0, 0, 0, 12),
            Text = "正在读取生产配置…",
        };
        panel.Children.Add(adapterHint);
        var rows = new List<(Button Btn, string Modality)>();

        void AddEntry(string kind, string id, string title, string modality)
        {
            var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
            var btn = new Button { Content = "预览并生成", Style = (Style)FindResource("GoldButton"), Margin = new Thickness(0), IsEnabled = false };
            btn.Click += async (_, _) =>
            {
                var adapter = modality == "video" ? _activeVideoAdapter : _activeImageAdapter;
                if (string.IsNullOrEmpty(adapter))
                {
                    StudioDialog.Alert(this, "生产", "请先在设置 → 生产里保存并启动对应后端。");
                    return;
                }
                await PreviewAndRunAsync(modality, id, adapter);
            };
            DockPanel.SetDock(btn, Dock.Right);
            row.Children.Add(btn);
            row.Children.Add(new TextBlock
            {
                Text = $"{kind}  {id}  {title}",
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            panel.Children.Add(row);
            rows.Add((btn, modality));
        }

        foreach (var p in _episode?["imagePrompts"] as JsonArray ?? [])
        {
            if (p is not JsonObject o || string.IsNullOrEmpty(Str(o["prompt"]))) continue;
            AddEntry("图片", Str(o["id"]), Str(o["title"]), "image");
        }
        foreach (var shot in _episode?["board"]?["shots"] as JsonArray ?? [])
        {
            if (shot is not JsonObject o || string.IsNullOrEmpty(Str(o["keyframe"]))) continue;
            AddEntry("关键帧", Str(o["id"]), Str(o["title"]), "image");
        }
        foreach (var p in _episode?["videoPrompts"] as JsonArray ?? [])
        {
            if (p is not JsonObject o || string.IsNullOrEmpty(Str(o["prompt"]))) continue;
            AddEntry("视频", Str(o["id"]), Str(o["title"]), "video");
        }
        if (panel.Children.Count <= 2)
            panel.Children.Add(new TextBlock { Text = "还没有可投产的提示词。先写图片提示词、冻结关键帧或视频提示词。", Foreground = (Brush)FindResource("SoftInkBrush") });
        _ = LoadAdapterHintAsync(adapterHint, rows);
        return panel;
    }

    private List<(string Id, string Title)> CollectProduceEntries(string modality)
    {
        var list = new List<(string Id, string Title)>();
        if (modality == "image")
        {
            foreach (var p in (_episode?["imagePrompts"] as JsonArray ?? []).OfType<JsonObject>())
            {
                if (string.IsNullOrEmpty(Str(p["prompt"]))) continue;
                list.Add((Str(p["id"]), Str(p["title"])));
            }
            foreach (var shot in (_episode?["board"]?["shots"] as JsonArray ?? []).OfType<JsonObject>())
            {
                if (string.IsNullOrEmpty(Str(shot["keyframe"]))) continue;
                list.Add((Str(shot["id"]), Str(shot["title"])));
            }
        }
        else
        {
            foreach (var p in (_episode?["videoPrompts"] as JsonArray ?? []).OfType<JsonObject>())
            {
                if (string.IsNullOrEmpty(Str(p["prompt"]))) continue;
                list.Add((Str(p["id"]), Str(p["title"])));
            }
        }
        return list;
    }

    private async Task BatchProduceAsync(string modality)
    {
        var adapter = modality == "video" ? _activeVideoAdapter : _activeImageAdapter;
        if (string.IsNullOrEmpty(adapter))
        {
            StudioDialog.Alert(this, "批量生产", "请先在设置 → 生产里保存并启动对应后端。");
            return;
        }
        var entries = CollectProduceEntries(modality);
        if (entries.Count == 0)
        {
            StudioDialog.Alert(this, "批量生产", modality == "video" ? "本集没有可投产的视频提示词。" : "本集没有可投产的图片/关键帧条目。");
            return;
        }
        var auto = _produceMode == "always" || (_produceMode == "session" && _sessionProduceAuth);
        var ok = StudioDialog.Confirm(
            this,
            modality == "video" ? "批量出视频" : "批量出图",
            $"将处理 {entries.Count} 条（{ProduceLabel(adapter)}）。\n" +
            (auto ? "当前为自动模式：确认后连跑，不再逐条弹窗。" : "闸门模式：每条仍会预览确认，可随时取消跳出。") +
            "\n可能产生费用，继续吗？",
            "开始",
            "取消");
        if (!ok) return;

        var done = 0;
        var failed = 0;
        foreach (var (id, _) in entries)
        {
            try
            {
                StatusText.Text = $"批量生产 {done + failed + 1}/{entries.Count} · {id}";
                if (auto)
                {
                    await RunProduceSilentAsync(modality, id, adapter);
                    done++;
                }
                else
                {
                    var ran = await PreviewAndRunAsync(modality, id, adapter);
                    if (!ran)
                    {
                        // 取消或单条失败：闸门下停止，避免连弹
                        if (StatusText?.Text?.Contains("失败") == true) failed++;
                        break;
                    }
                    done++;
                }
            }
            catch
            {
                failed++;
                if (!auto) break;
            }
        }
        if (StatusText is not null)
            StatusText.Text = $"批量结束 · 成功 {done} · 失败/跳过 {failed} · 共 {entries.Count}";
        if (_episodeId is not null)
        {
            await LoadEpisodeAsync(_episodeId);
            await RefreshAssetsAsync();
        }
    }

    private async Task RunProduceSilentAsync(string modality, string id, string adapter)
    {
        if (_studio is null || _episodeId is null) return;
        var preview = await _studio.ProducePrepareAsync(_episodeId, modality, id, adapter, CancellationToken.None);
        var job = preview["job"] as JsonObject;
        var confirmation = Str(preview["preview"]?["confirmation"]);
        var jobId = Str(job?["job_id"]);
        if (string.IsNullOrEmpty(jobId) || string.IsNullOrEmpty(confirmation))
            throw new InvalidOperationException("预览缺少 job_id 或确认语");
        await _studio.ProduceConfirmAsync(jobId, confirmation, CancellationToken.None);
        await _studio.ProduceRunAsync(jobId, CancellationToken.None);
    }

    private void OpenFilmFolder()
    {
        if (string.IsNullOrEmpty(_projectRoot))
        {
            StudioDialog.Alert(this, "成片", "还没有打开项目。");
            return;
        }
        var candidates = new[]
        {
            System.IO.Path.Combine(_projectRoot, "制作成果", "成片"),
            System.IO.Path.Combine(_projectRoot, "制作成果"),
            _projectRoot,
        };
        var dir = candidates.FirstOrDefault(System.IO.Directory.Exists) ?? _projectRoot;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StudioDialog.Alert(this, "成片", ex.Message);
        }
    }

    private async void OnProbeEnvironment(object sender, RoutedEventArgs e) => await ProbeEnvironmentAsync();

    private async Task ProbeEnvironmentAsync()
    {
        if (_studio is null) return;
        StatusText.Text = "正在测连通…";
        var lines = new List<string>();
        try
        {
            var (apiOk, apiDetail) = await _chatClient.ProbeAsync(CancellationToken.None);
            lines.Add($"API：{(apiOk ? "通" : "断")} · {apiDetail}");
        }
        catch (Exception ex)
        {
            lines.Add("API：断 · " + ex.Message);
        }

        try
        {
            var data = ApplyProduceStatus(await _studio.AdaptersAsync(CancellationToken.None));
            var env = data["environment"] as JsonObject;
            var imageOk = env?["image_ok"]?.GetValue<bool>() == true || _activeImageAdapter.Length > 0;
            var videoOk = env?["video_ok"]?.GetValue<bool>() == true || _activeVideoAdapter.Length > 0;
            lines.Add($"出图：{(imageOk ? "已启动" : "未启动")} · {ProduceLabel(_activeImageAdapter)}");
            lines.Add($"出视频：{(videoOk ? "已启动" : "未启动")} · {ProduceLabel(_activeVideoAdapter)}");
            if (ProduceEnvHint is not null)
                ProduceEnvHint.Text = string.Join("  ·  ", lines);
        }
        catch (Exception ex)
        {
            lines.Add("生产配置：" + ex.Message);
        }

        StatusText.Text = "连通测试完成";
        StudioDialog.Alert(this, "环境三灯", string.Join("\n", lines));
    }

    private async Task LoadAdapterHintAsync(TextBlock target, List<(Button Btn, string Modality)> rows)
    {
        if (_studio is null) return;
        try
        {
            var data = ApplyProduceStatus(await _studio.AdaptersAsync(CancellationToken.None));
            var imageOn = _activeImageAdapter.Length > 0;
            var videoOn = _activeVideoAdapter.Length > 0;
            target.Text = $"当前出图：{(imageOn ? ProduceLabel(_activeImageAdapter) : "未启动")}  ·  出视频：{(videoOn ? ProduceLabel(_activeVideoAdapter) : "未启动")}";
            foreach (var (btn, modality) in rows)
                btn.IsEnabled = modality == "video" ? videoOn : imageOn;
        }
        catch (Exception ex)
        {
            target.Text = "无法读取 adapter：" + ex.Message;
        }
    }

    private async Task<bool> PreviewAndRunAsync(string modality, string id, string adapter)
    {
        if (_studio is null || _episodeId is null) return false;
        try
        {
            var preview = await _studio.ProducePrepareAsync(_episodeId, modality, id, adapter, CancellationToken.None);
            var job = preview["job"] as JsonObject;
            var confirmation = Str(preview["preview"]?["confirmation"]);
            var jobId = Str(job?["job_id"]);
            var outputs = string.Join("、", (job?["outputs"] as JsonArray ?? []).Select(n => Str(n)));
            var prompt = Str(job?["prompt"]);
            if (prompt.Length > 400) prompt = prompt[..400] + "…";
            var body = $"条目：{id}\nadapter：{ProduceLabel(adapter)}（{adapter}）\n输出：{outputs}\n确认语：{confirmation}\n\n{prompt}\n\n确认后将提交该后端。失败需重新预览。";
            var ok = StudioDialog.Confirm(this, "生产确认", body, "确定", "取消");
            if (!ok) return false;
            await _studio.ProduceConfirmAsync(jobId, confirmation, CancellationToken.None);
            StatusText.Text = "已确认，正在生成…";
            var result = await _studio.ProduceRunAsync(jobId, CancellationToken.None);
            var files = (result["outputs"] as JsonArray)?.Count ?? 0;
            StatusText.Text = $"生成完成 · {files} 个文件";
            await LoadEpisodeAsync(_episodeId);
            await RefreshAssetsAsync();
            return true;
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
            StudioDialog.Alert(this, "生产失败", ex.Message);
            return false;
        }
    }

    private UIElement BuildDocFallback() =>
        Hint("打开 " + (_episodeId ?? "项目") + " 的「" + Stages.First(s => s.Key == _stage).Label + "」。复杂修订可在右侧对话里进行。");

    private UIElement Hint(string text) =>
        new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 0),
            Foreground = (Brush)FindResource("InkBrush"),
        };

    private Grid MakeTable(string[] headers)
    {
        var grid = new Grid();
        for (var i = 0; i < headers.Length; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var i = 0; i < headers.Length; i++)
        {
            var h = new TextBlock
            {
                Text = headers[i],
                Foreground = (Brush)FindResource("SoftInkBrush"),
                FontSize = 11,
                Margin = new Thickness(8, 6, 8, 6),
            };
            Grid.SetColumn(h, i);
            grid.Children.Add(h);
        }
        var line = new Border
        {
            BorderBrush = (Brush)FindResource("LineBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Margin = new Thickness(0, 28, 0, 0),
        };
        Grid.SetColumnSpan(line, headers.Length);
        grid.Children.Add(line);
        return grid;
    }

    private void AddTableRow(Grid grid, string[] cells, bool warn)
    {
        var row = grid.RowDefinitions.Count;
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (var i = 0; i < cells.Length; i++)
        {
            var t = new TextBlock
            {
                Text = cells[i],
                Margin = new Thickness(8, 10, 8, 10),
                FontWeight = i == 0 ? FontWeights.Bold : FontWeights.Normal,
                Foreground = warn && i == cells.Length - 1
                    ? (Brush)FindResource("WarnBrush")
                    : (Brush)FindResource("InkBrush"),
            };
            Grid.SetRow(t, row);
            Grid.SetColumn(t, i);
            grid.Children.Add(t);
        }
    }

    private void InsertChat(string text)
    {
        ChatInput.Text = text;
        ChatInput.Focus();
        ChatInput.CaretIndex = text.Length;
    }

    private void QueueSkill(string skill, string text)
    {
        InsertChatAndSend($"[skill:{skill}]\n{text}");
    }

    private void InsertChatAndSend(string text)
    {
        ChatInput.Text = text;
        OnSendChat(this, new RoutedEventArgs());
    }

    private void OnAllowChain(object sender, RoutedEventArgs e)
    {
        /* flag is read from AllowChainBox when sending */
    }

    private void OnPlanNext(object sender, RoutedEventArgs e)
    {
        if (_planCursor >= _plan.Count) return;
        var step = _plan[_planCursor];
        QueueSkill(step.Skill, step.Title);
    }

    private void ApplyPlan(JsonArray? steps)
    {
        _plan.Clear();
        _planCursor = 0;
        foreach (var node in steps ?? [])
        {
            if (node is not JsonObject row) continue;
            _plan.Add(new PlanStep
            {
                Skill = Str(row["skill"]),
                Title = Str(row["title"], Str(row["skill"])),
                State = "待执行",
            });
        }
        PlanBar.Visibility = _plan.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MarkPlanSkill(string skill)
    {
        if (string.IsNullOrEmpty(skill)) return;
        for (var i = 0; i < _plan.Count; i++)
        {
            if (_plan[i].Skill != skill) continue;
            _plan[i].State = "已执行";
            _planCursor = i + 1;
            break;
        }
    }

    private void OnChatSettings(object sender, RoutedEventArgs e)
    {
        ShowHome();
        NavSettings.IsChecked = true;
        SetNavApi.IsChecked = true;
        RefreshChatModels();
    }

    private async void OnFiles(object sender, RoutedEventArgs e)
    {
        ShowHome();
        NavAssets.IsChecked = true;
        await RefreshAssetsAsync();
    }

    private void OnChatInputChanged(object sender, TextChangedEventArgs e)
    {
        if (ChatPlaceholder is null) return;
        ChatPlaceholder.Visibility = string.IsNullOrWhiteSpace(ChatInput.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnChatKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            e.Handled = true;
            OnSendChat(sender, e);
        }
    }

    private async void OnSendChat(object sender, RoutedEventArgs e)
    {
        if (_sending)
        {
            _chatCts?.Cancel();
            return;
        }
        var text = ChatInput.Text.Trim();
        if (text.Length == 0 && _chatAttach.Count == 0) return;
        if (text.Length == 0) text = "请根据附件继续。";
        if (_studio is null || string.IsNullOrEmpty(_studio.ProjectId))
        {
            _chat.Add(new ChatMessage { Role = "assistant", Text = "先打开一个项目，再让助手按技能库改文档。" });
            return;
        }
        ChatInput.Text = "";
        var caption = AttachCaption(_chatAttach);
        var wire = text + FormatAttachments(_chatAttach);
        _chat.Add(new ChatMessage { Role = "user", Text = text + caption, Wire = wire });
        if (_chatAttach.Count > 0)
        {
            _chatAttach.Clear();
            RenderAttachChips(_chatAttach, ChatAttachHost);
        }
        var reply = new ChatMessage { Role = "assistant", Text = "正在调用技能库…" };
        _chat.Add(reply);
        ScrollChat();
        _sending = true;
        ChatSendButton.Content = "■";
        _chatCts = new CancellationTokenSource();
        var wrote = false;
        try
        {
            var history = new JsonArray();
            foreach (var msg in _chat.Where(m => m.Role is "user" or "assistant").TakeLast(12))
            {
                if (ReferenceEquals(msg, reply)) continue;
                history.Add(new JsonObject
                {
                    ["role"] = msg.Role,
                    ["content"] = string.IsNullOrEmpty(msg.Wire) ? msg.Text : msg.Wire,
                });
            }
            var body = new JsonObject
            {
                ["project"] = _studio.ProjectId,
                ["episode"] = _episodeId ?? "",
                ["stage"] = _stage,
                ["title"] = Str(_series?["title"]),
                ["skillsDir"] = _layout?.SkillsDir ?? "",
                ["allowChainWrites"] = AllowChainBox?.IsChecked == true,
                ["sessionProduceAuth"] = _sessionProduceAuth || AllowSessionProduceBox?.IsChecked == true,
                ["messages"] = history,
            };
            if (_planCursor < _plan.Count && text.Contains("[skill:" + _plan[_planCursor].Skill, StringComparison.Ordinal))
            {
                body["planStep"] = new JsonObject
                {
                    ["skill"] = _plan[_planCursor].Skill,
                    ["title"] = _plan[_planCursor].Title,
                };
            }
            await foreach (var ev in _studio.AgentTurnAsync(body, _chatCts.Token))
            {
                var kind = Str(ev["type"]);
                if (kind == "status")
                    reply.Text = Str(ev["text"], reply.Text);
                else if (kind == "skill")
                {
                    var name = Str(ev["name"]);
                    reply.Trace = string.IsNullOrEmpty(reply.Trace) ? "技能 " + name : reply.Trace + "\n技能 " + name;
                    MarkPlanSkill(name);
                }
                else if (kind == "file_write")
                {
                    wrote = true;
                    var path = Str(ev["path"]);
                    reply.Trace = string.IsNullOrEmpty(reply.Trace) ? "写入 " + path : reply.Trace + "\n写入 " + path;
                }
                else if (kind == "plan")
                    ApplyPlan(ev["steps"] as JsonArray);
                else if (kind == "delta" || kind == "done")
                    reply.Text = Str(ev["text"], reply.Text);
                else if (kind == "error")
                    reply.Text = "失败：" + Str(ev["text"]);
                else if (kind == "need_confirm" && Str(ev["kind"]) == "produce")
                    reply.Text = _produceMode == "session" && !_sessionProduceAuth
                        ? "生产预览已生成。请勾选「本会话允许小烧自动生产」后再试，或到「生产」页确认。"
                        : "生产预览已生成。请到「生产」页确认后才会提交。";
                else if (kind == "environment")
                {
                    var issues = ev["payload"]?["issues"] as JsonArray;
                    var lines = issues?.Select(n => "· " + Str(n)).Where(s => s.Length > 2).ToList() ?? [];
                    reply.Text = lines.Count > 0
                        ? "环境未就绪：\n" + string.Join("\n", lines)
                        : "环境检查通过。";
                }
                else if (kind == "produce_done")
                    reply.Text = "已按当前生产模式自动提交。可在「生产 / 成片」查看结果。";
                else if (kind == "config_preview")
                {
                    var preview = ev["payload"]?["preview"] as JsonArray;
                    var lines = preview?.Select(n => Str(n)).Where(s => s.Length > 0).ToList() ?? [];
                    var token = Str(ev["payload"]?["token"]);
                    reply.Text = "配置预览：\n" + string.Join("\n", lines)
                        + (string.IsNullOrEmpty(token) ? "" : $"\n确认后请回复：确认配置 {token}");
                }
                else if (kind == "config_applied")
                    reply.Text = "本机配置已按确认写入。";
                ScrollChat();
            }
            if (wrote && !string.IsNullOrEmpty(_episodeId))
            {
                await ReloadSeriesAsync();
                await LoadEpisodeAsync(_episodeId);
            }
        }
        catch (OperationCanceledException)
        {
            reply.Text = "已停止。";
        }
        catch (Exception ex)
        {
            reply.Text = "调用失败：" + ex.Message + "\n可在设置 → API 检查模型地址。";
        }
        finally
        {
            _sending = false;
            ChatSendButton.Content = "↑";
            _chatCts?.Dispose();
            _chatCts = null;
            SaveProjectChat();
            ScrollChat();
        }
    }

    private void ScrollChat()
    {
        ChatScroll.Dispatcher.InvokeAsync(() => ChatScroll.ScrollToEnd());
    }

    private void CopyPlain(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        try
        {
            Clipboard.SetText(text);
            StatusText.Text = "已复制";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }

    private void ExportEpisodeView()
    {
        if (_episode is null || string.IsNullOrEmpty(_projectRoot) || string.IsNullOrEmpty(_episodeId))
        {
            StatusText.Text = "没有打开的集，无法导出。";
            return;
        }
        try
        {
            var dir = System.IO.Path.Combine(_projectRoot, "项目开发");
            System.IO.Directory.CreateDirectory(dir);
            var path = System.IO.Path.Combine(dir, $"{_episodeId}-视图.json");
            System.IO.File.WriteAllText(path, _episode.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            StatusText.Text = "已导出 " + path;
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
    }

    private async void BindAuthImage(Image image, string? path)
    {
        if (_studio is null || string.IsNullOrEmpty(_studio.ProjectId) || string.IsNullOrEmpty(path))
            return;
        try
        {
            var bytes = await _studio.GetBytesAsync(_studio.MediaContentUrl(_studio.ProjectId, path), CancellationToken.None);
            if (bytes is null || bytes.Length == 0) return;
            var bmp = new BitmapImage();
            using var ms = new System.IO.MemoryStream(bytes);
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            image.Source = bmp;
        }
        catch
        {
            /* missing media */
        }
    }

    private static string Str(JsonNode? node, string fallback = "")
    {
        if (node is null) return fallback;
        if (node is JsonValue v)
        {
            if (v.TryGetValue<string>(out var s)) return s;
            return v.ToJsonString().Trim('"');
        }
        return node.ToJsonString();
    }

    private static double Num(JsonNode? node)
    {
        if (node is JsonValue v)
        {
            if (v.TryGetValue<double>(out var d)) return d;
            if (v.TryGetValue<int>(out var i)) return i;
        }
        return 0;
    }
}
