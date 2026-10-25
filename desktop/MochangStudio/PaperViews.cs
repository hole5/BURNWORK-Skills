using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MochangStudio;

internal static class PaperViews
{
    internal static UIElement Wrap(UIElement inner, FrameworkElement owner) =>
        new Border
        {
            Background = B(owner, "BgBrush"),
            Padding = new Thickness(0),
            Child = inner,
        };

    internal static UIElement Cast(
        FrameworkElement owner,
        JsonObject? episode,
        string[] cats,
        Action<string> copy,
        Action<Image, string?> bindImage)
    {
        var items = (episode?["settings"]?["items"] as JsonArray ?? [])
            .OfType<JsonObject>()
            .Where(i => cats.Contains(Str(i["cat"])))
            .ToList();
        if (items.Count == 0)
            return Empty(owner, "还没有设定条目。写好视觉设定.md 后会排成设定集。");

        var people = items.Where(i => Str(i["cat"]) == "人物").ToList();
        var looks = items.Where(i => Str(i["cat"]) == "造型").ToList();
        var places = items.Where(i => Str(i["cat"]) != "人物" && Str(i["cat"]) != "造型").ToList();
        var tabs = cats.Contains("人物")
            ? new[] { ("人物", people), ("造型", looks) }
            : new[] { ("条目", items) };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(248) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var roster = new DockPanel { Background = B(owner, "SurfaceBrush") };
        var sheetHost = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(18) };
        var selected = items[0];
        var selectedNo = 1;
        var graphMode = false;
        Button? graphBtn = null;

        var search = new TextBox
        {
            Margin = new Thickness(10, 10, 10, 6),
            Padding = new Thickness(8, 4, 8, 4),
            Background = B(owner, "Surface2Brush"),
            Foreground = B(owner, "InkBrush"),
            CaretBrush = B(owner, "GoldBrush"),
            BorderBrush = B(owner, "LineBrush"),
            Tag = "搜索姓名、代称、锚点…",
        };
        DockPanel.SetDock(search, Dock.Top);

        var tabBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 0, 8, 4) };
        DockPanel.SetDock(tabBar, Dock.Top);
        var listHost = new StackPanel();
        var listScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = listHost };
        var activeTab = 0;

        void PaintSheet()
        {
            if (graphMode && cats.Contains("人物"))
            {
                sheetHost.Content = RelationGraphView.Build(
                    owner,
                    episode?["relations"] as JsonObject,
                    people,
                    name =>
                    {
                        var hit = people.FirstOrDefault(p => Str(p["name"]) == name);
                        if (hit is null) return;
                        selected = hit;
                        selectedNo = Math.Max(1, people.IndexOf(hit) + 1);
                        graphMode = false;
                        if (graphBtn is not null)
                        {
                            graphBtn.Background = Brushes.Transparent;
                            graphBtn.BorderBrush = B(owner, "LineBrush");
                        }
                        PaintRoster();
                        PaintSheet();
                    });
                return;
            }
            sheetHost.Content = CharacterSheet(owner, episode, selected, selectedNo, copy, bindImage);
        }

        void PaintRoster()
        {
            listHost.Children.Clear();
            var q = search.Text.Trim();
            var source = tabs[Math.Min(activeTab, tabs.Length - 1)].Item2;
            var n = 0;
            foreach (var item in source)
            {
                var hay = string.Join(" ", new[]
                {
                    Str(item["name"]), Str(item["cat"]),
                    string.Join(" ", (item["designators"] as JsonArray ?? []).Select(n => Str(n))),
                    Str((item["fields"] as JsonObject)?["识别锚点"]),
                });
                if (q.Length > 0 && hay.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                n++;
                var idx = n.ToString("00");
                var on = ReferenceEquals(item, selected);
                var btn = RosterButton(owner, item, idx, on);
                var captured = item;
                var capturedNo = n;
                btn.Click += (_, _) =>
                {
                    selected = captured;
                    selectedNo = capturedNo;
                    graphMode = false;
                    if (graphBtn is not null)
                    {
                        graphBtn.Background = Brushes.Transparent;
                        graphBtn.BorderBrush = B(owner, "LineBrush");
                    }
                    PaintRoster();
                    PaintSheet();
                };
                listHost.Children.Add(btn);
            }
            if (n == 0)
                listHost.Children.Add(Print(owner, "没有匹配的条目。", muted: true));
        }

        for (var i = 0; i < tabs.Length; i++)
        {
            var ti = i;
            var tab = new Button
            {
                Content = $"{tabs[i].Item1} {tabs[i].Item2.Count}",
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 4, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = B(owner, ti == 0 ? "SealBrush" : "PrintMutedBrush"),
                Cursor = Cursors.Hand,
            };
            tab.Click += (_, _) =>
            {
                activeTab = ti;
                var pool = tabs[ti].Item2;
                if (pool.Count > 0) { selected = pool[0]; selectedNo = 1; }
                for (var k = 0; k < tabBar.Children.Count; k++)
                    if (tabBar.Children[k] is Button b)
                        b.Foreground = B(owner, k == ti ? "SealBrush" : "PrintMutedBrush");
                PaintRoster();
                PaintSheet();
            };
            tabBar.Children.Add(tab);
        }

        search.TextChanged += (_, _) => PaintRoster();
        roster.Children.Add(search);
        roster.Children.Add(tabBar);
        if (cats.Contains("人物"))
        {
            graphBtn = new Button
            {
                Content = "关系图谱",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(8, 4, 8, 8),
                Background = Brushes.Transparent,
                BorderBrush = B(owner, "LineBrush"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Foreground = B(owner, "GoldBrush"),
                Cursor = Cursors.Hand,
            };
            graphBtn.Click += (_, _) =>
            {
                graphMode = !graphMode;
                graphBtn!.Background = graphMode ? B(owner, "GoldWash") : Brushes.Transparent;
                graphBtn.BorderBrush = graphMode ? B(owner, "GoldBrush") : B(owner, "LineBrush");
                PaintSheet();
            };
            DockPanel.SetDock(graphBtn, Dock.Top);
            roster.Children.Add(graphBtn);
        }
        roster.Children.Add(listScroll);
        PaintRoster();
        PaintSheet();

        Grid.SetColumn(sheetHost, 1);
        grid.Children.Add(roster);
        grid.Children.Add(sheetHost);
        return Wrap(grid, owner);
    }

    internal static UIElement Script(FrameworkElement owner, JsonObject? episode, double targetSeconds, Action<string> copy)
    {
        var script = episode?["script"] as JsonObject;
        var scenes = (script?["scenes"] as JsonArray)?.OfType<JsonObject>().ToList();
        if (scenes is null || scenes.Count == 0)
            return Empty(owner, "还没有剧本。可在右侧请助手开写。");

        var metrics = episode?["metrics"] as JsonObject;
        var gate = episode?["gate"] as JsonObject;
        var root = new StackPanel { Margin = new Thickness(16) };
        root.Children.Add(KpiRow(owner, new[]
        {
            ("场次", Str(metrics?["scenes"], scenes.Count.ToString()), true),
            ("对白", Str(metrics?["lines"], "0") + " 句", false),
            ("预估", FormatEst(metrics), false),
            ("旁白", VoText(metrics), false),
        }));
        root.Children.Add(GateBar(owner, gate, targetSeconds, Num(metrics?["scriptEst"])));

        var card = PanelCard(owner);
        var inner = new StackPanel();
        var title = Str(script?["title"], "本集");
        inner.Children.Add(new TextBlock
        {
            Text = title,
            FontFamily = Serif(owner),
            FontSize = 22,
            Foreground = B(owner, "SealBrush"),
            Margin = new Thickness(0, 0, 0, 8),
        });
        if (Str(script?["hook"]).Length > 0)
            inner.Children.Add(HookRow(owner, "开场钩", Str(script!["hook"])));
        if (Str(script?["cliff"]).Length > 0)
            inner.Children.Add(HookRow(owner, "集末悬念", Str(script!["cliff"])));

        var settings = (episode?["settings"]?["items"] as JsonArray ?? []).OfType<JsonObject>().ToList();
        var propNames = settings.Where(i => Str(i["cat"]) == "道具").Select(i => Str(i["name"])).Where(s => s.Length > 0).ToList();

        foreach (var scene in scenes)
        {
            inner.Children.Add(SceneHeader(owner, scene, propNames));
            foreach (var block in (scene["blocks"] as JsonArray ?? []).OfType<JsonObject>())
            {
                var kind = Str(block["k"]);
                if (kind == "line")
                    inner.Children.Add(DialogueRow(owner, block, copy));
                else
                    inner.Children.Add(new TextBlock
                    {
                        Text = Str(block["text"]),
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = B(owner, "PrintMutedBrush"),
                        Margin = new Thickness(0, 6, 0, 6),
                        LineHeight = 22,
                    });
            }
        }
        card.Child = inner;
        root.Children.Add(card);
        root.Children.Add(LineBook(owner, script?["lineBook"] as JsonArray, copy));
        return Wrap(root, owner);
    }

    internal static UIElement Board(
        FrameworkElement owner,
        JsonObject? episode,
        Action<string> copy,
        Action<Image, string?> bindImage)
    {
        var shots = (episode?["board"]?["shots"] as JsonArray)?.OfType<JsonObject>().ToList() ?? [];
        if (shots.Count == 0)
            return Empty(owner, "还没有分镜。可在右侧请助手写分镜.md。");

        var metrics = episode?["metrics"] as JsonObject;
        var frames = episode?["media"]?["frames"] as JsonObject;
        var motions = (episode?["videoPrompts"] as JsonArray)?.OfType<JsonObject>().ToList() ?? [];
        var root = new StackPanel { Margin = new Thickness(16) };
        root.Children.Add(KpiRow(owner, new[]
        {
            ("总时长", $"{Num(metrics?["seconds"]):0} 秒", true),
            ("平均镜长", metrics?["avg"] is null ? "—" : $"{Num(metrics?["avg"]):0.#}s", false),
            ("近景类", metrics?["close"] is null ? "—" : $"{Num(metrics?["close"]) * 100:0}%", false),
            ("未写景别", Str(metrics?["unsized"], "0"), false),
        }));
        root.Children.Add(GateBar(owner, episode?["gate"] as JsonObject, 0, 0));
        root.Children.Add(RhythmStrip(owner, shots));

        var grid = new UniformGrid { Columns = 2, Margin = new Thickness(0, 8, 0, 0) };
        foreach (var shot in shots)
            grid.Children.Add(ShotCard(owner, episode, shot, frames, motions, copy, bindImage));
        root.Children.Add(grid);
        root.Children.Add(BatchWall(owner, episode));
        root.Children.Add(AudioTable(owner, episode));
        return Wrap(root, owner);
    }

    private static UIElement CharacterSheet(
        FrameworkElement owner, JsonObject? episode, JsonObject item, int number,
        Action<string> copy, Action<Image, string?> bindImage)
    {
        var fields = item["fields"] as JsonObject ?? [];
        var name = Str(item["name"]);
        var stack = new StackPanel();
        var head = new WrapPanel();
        head.Children.Add(new TextBlock { Text = number.ToString("00"), FontFamily = new FontFamily("Consolas"), Foreground = B(owner, "SealBrush"), Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center });
        head.Children.Add(new TextBlock { Text = name, FontFamily = Serif(owner), FontSize = 26, Foreground = B(owner, "PrintBrush") });
        var aka = string.Join("、", (item["designators"] as JsonArray ?? []).Select(n => Str(n)).Where(s => s.Length > 0));
        if (aka.Length > 0)
            head.Children.Add(new TextBlock { Text = "  画面代称 · " + aka, Foreground = B(owner, "PrintMutedBrush"), VerticalAlignment = VerticalAlignment.Center });
        head.Children.Add(Chip(owner, Str(item["cat"])));
        stack.Children.Add(head);

        var upper = new Grid { Margin = new Thickness(0, 14, 0, 0) };
        upper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        upper.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
        var left = new StackPanel();
        var plate = new Border
        {
            Background = B(owner, "Surface2Brush"),
            BorderBrush = B(owner, "LineBrush"),
            BorderThickness = new Thickness(1),
            Height = 196,
        };
        var img = new Image { Stretch = Stretch.UniformToFill };
        var ph = Print(owner, "设定图 16:9 · 尚未出图", muted: true);
        ph.HorizontalAlignment = HorizontalAlignment.Center;
        ph.VerticalAlignment = VerticalAlignment.Center;
        var plateGrid = new Grid();
        plateGrid.Children.Add(ph);
        plateGrid.Children.Add(img);
        plate.Child = plateGrid;
        left.Children.Add(plate);
        var prompts = LinkedPrompts(episode, name).ToList();
        var thumbs = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
        string? platePath = null;
        foreach (var p in prompts.Take(6))
        {
            thumbs.Children.Add(Chip(owner, Str(p["id"]).Replace("IMG-", "")));
        }
        left.Children.Add(thumbs);
        bindImage(img, platePath);

        var blocks = new UniformGrid { Columns = 2, Margin = new Thickness(0, 12, 0, 0) };
        foreach (var key in new[] { "识别锚点", "本集状态", "声音方向", "连续性锁", "性情", "动机", "弧光" })
        {
            var val = key == "连续性锁" ? LockText(item) : Str(fields[key]);
            if (val.Length == 0) continue;
            blocks.Children.Add(FieldBlock(owner, key, val));
        }
        left.Children.Add(blocks);
        Grid.SetColumn(left, 0);
        upper.Children.Add(left);
        upper.Children.Add(SideCards(owner, episode, item, fields));
        Grid.SetColumn(upper.Children[1], 1);
        stack.Children.Add(upper);

        var quotes = QuoteLines(fields, episode, name);
        if (quotes.Count > 0)
        {
            stack.Children.Add(SealLabel(owner, "原文依据"));
            var qgrid = new UniformGrid { Columns = Math.Min(2, quotes.Count) };
            foreach (var q in quotes)
            {
                qgrid.Children.Add(new Border
                {
                    BorderBrush = B(owner, "SealBrush"),
                    BorderThickness = new Thickness(2, 0, 0, 0),
                    Padding = new Thickness(12, 0, 8, 0),
                    Margin = new Thickness(0, 0, 12, 8),
                    Child = new TextBlock
                    {
                        Text = q,
                        FontFamily = Serif(owner),
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = B(owner, "PrintBrush"),
                        LineHeight = 22,
                    },
                });
            }
            stack.Children.Add(qgrid);
        }

        if (prompts.Count > 0)
        {
            stack.Children.Add(SealLabel(owner, "可复制提示词"));
            var pg = new UniformGrid { Columns = 2 };
            foreach (var p in prompts)
            {
                var body = Str(p["prompt"]);
                var box = PanelCard(owner);
                var col = new StackPanel();
                var row = new DockPanel();
                var copyBtn = MiniCopy(owner, () => copy(body));
                DockPanel.SetDock(copyBtn, Dock.Right);
                row.Children.Add(copyBtn);
                row.Children.Add(Print(owner, Str(p["id"]) + " · " + Str(p["title"])));
                col.Children.Add(row);
                col.Children.Add(new TextBox
                {
                    Text = body,
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap,
                    BorderThickness = new Thickness(1),
                    BorderBrush = B(owner, "RulePaperBrush"),
                    Background = B(owner, "PaperBrush"),
                    Foreground = B(owner, "PrintBrush"),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 6, 0, 0),
                    FontFamily = new FontFamily("Consolas, Cascadia Mono, Microsoft YaHei UI"),
                    FontSize = 11,
                });
                box.Child = col;
                pg.Children.Add(box);
            }
            stack.Children.Add(pg);
        }
        return stack;
    }

    private static UIElement SideCards(FrameworkElement owner, JsonObject? episode, JsonObject item, JsonObject fields)
    {
        var col = new StackPanel { Margin = new Thickness(14, 0, 0, 0) };
        col.Children.Add(MetaCard(owner, "身份", new[]
        {
            ("类别", Str(item["cat"])),
            ("出场", ShotCount(episode, Str(item["name"])) + " 镜"),
            ("声音参考", Str(fields["声音参考"], "未绑定")),
        }));
        var rel = Str(fields["关系"]);
        if (rel.Length > 0)
            col.Children.Add(MetaCard(owner, "关系", new[] { ("", rel) }));
        return col;
    }

    private static Button RosterButton(FrameworkElement owner, JsonObject item, string idx, bool on)
    {
        var body = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
        var top = new WrapPanel();
        top.Children.Add(new TextBlock { Text = idx, FontFamily = new FontFamily("Consolas"), FontSize = 10, Foreground = B(owner, "PrintMutedBrush"), VerticalAlignment = VerticalAlignment.Center });
        top.Children.Add(new TextBlock { Text = " " + Str(item["name"]), FontFamily = Serif(owner), FontSize = 15, Foreground = B(owner, "PrintBrush") });
        top.Children.Add(Chip(owner, Str(item["cat"])));
        body.Children.Add(top);
        var one = Str((item["fields"] as JsonObject)?["识别锚点"]);
        if (one.Length > 0)
            body.Children.Add(new TextBlock { Text = one, TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = B(owner, "PrintMutedBrush"), Margin = new Thickness(0, 4, 0, 0) });
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(new Border { Width = 76, Height = 76, Background = B(owner, "PanelPaperBrush"), BorderBrush = B(owner, "RulePaperBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6) });
        Grid.SetColumn(body, 1);
        grid.Children.Add(body);
        return new Button
        {
            Content = grid,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = on ? B(owner, "GoldWash") : Brushes.Transparent,
            BorderBrush = on ? B(owner, "GoldBrush") : Brushes.Transparent,
            BorderThickness = new Thickness(on ? 2 : 0, 0, 0, 0),
            Padding = new Thickness(10, 10, 12, 10),
            Margin = new Thickness(0),
            Cursor = Cursors.Hand,
        };
    }

    private static UIElement ShotCard(
        FrameworkElement owner, JsonObject? episode, JsonObject shot, JsonObject? frames,
        List<JsonObject> motions, Action<string> copy, Action<Image, string?> bindImage)
    {
        var card = PanelCard(owner);
        card.Margin = new Thickness(0, 0, 10, 10);
        var col = new StackPanel();
        var head = new WrapPanel();
        head.Children.Add(new TextBlock { Text = Str(shot["id"]), FontFamily = new FontFamily("Consolas"), Foreground = B(owner, "SealBrush"), Margin = new Thickness(0, 0, 8, 0) });
        head.Children.Add(new TextBlock { Text = Str(shot["title"]), FontFamily = Serif(owner), Foreground = B(owner, "PrintBrush") });
        head.Children.Add(Chip(owner, Str(shot["scale"], "未写景别")));
        head.Children.Add(Chip(owner, Str(shot["moveKind"])));
        head.Children.Add(Chip(owner, $"{Num(shot["sec"]):0}s"));
        col.Children.Add(head);

        var frame = new Border
        {
            Margin = new Thickness(0, 8, 0, 8),
            BorderBrush = B(owner, "RulePaperBrush"),
            BorderThickness = new Thickness(1),
            Height = 148,
            Background = B(owner, "SidePaperBrush"),
        };
        var img = new Image { Stretch = Stretch.UniformToFill };
        var path = Str(frames?[Str(shot["id"])]);
        var ph = new TextBlock
        {
            Text = path.Length > 0 ? "起始帧" : (Str(shot["keyframe"]).Length > 0 ? "冻结关键帧 · 无 REF" : "无画面"),
            Foreground = B(owner, "PrintMutedBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(10),
            FontSize = 11,
        };
        var g = new Grid();
        g.Children.Add(ph);
        g.Children.Add(img);
        frame.Child = g;
        bindImage(img, path.Length > 0 ? path : null);
        col.Children.Add(frame);

        var chain = new UniformGrid { Columns = 3 };
        chain.Children.Add(FieldBlock(owner, "起", Str(shot["start"], "—")));
        chain.Children.Add(FieldBlock(owner, "动", Str(shot["action"], "—")));
        chain.Children.Add(FieldBlock(owner, "终", Str(shot["end"], "—")));
        col.Children.Add(chain);
        var chips = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
        foreach (var b in (shot["basis"] as JsonArray ?? []).OfType<JsonObject>())
            chips.Children.Add(Chip(owner, $"{Str(b["cat"])}·{Str(b["name"])}"));
        chips.Children.Add(Chip(owner, Str(shot["refLabel"], Str(shot["refState"]))));
        col.Children.Add(chips);

        var key = Str(shot["keyframe"]);
        var motion = motions.FirstOrDefault(m => Str(m["shot"]).Contains(Str(shot["id"])) || Str(m["id"]).Contains(Str(shot["id"])));
        var sound = Str(shot["sound"]);
        col.Children.Add(PromptTabs(owner, copy, ("冻结关键帧", key), ("视频 MOTION", Str(motion?["prompt"])), ("声音", sound)));
        card.Child = col;
        return card;
    }

    private static UIElement PromptTabs(FrameworkElement owner, Action<string> copy, params (string Label, string Body)[] tabs)
    {
        var box = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        var bar = new StackPanel { Orientation = Orientation.Horizontal };
        var body = new TextBox
        {
            IsReadOnly = true, TextWrapping = TextWrapping.Wrap, MinHeight = 48, MaxHeight = 120,
            Background = B(owner, "PanelPaperBrush"), Foreground = B(owner, "PrintBrush"),
            BorderBrush = B(owner, "RulePaperBrush"), Padding = new Thickness(8), FontSize = 11,
            Visibility = Visibility.Collapsed,
        };
        var current = 0;
        void Show(int i)
        {
            current = i;
            var text = tabs[i].Body;
            body.Text = text;
            body.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
            for (var k = 0; k < bar.Children.Count; k++)
                if (bar.Children[k] is Button b && k < tabs.Length)
                    b.Foreground = B(owner, k == i ? "PrintBrush" : "PrintMutedBrush");
        }
        for (var i = 0; i < tabs.Length; i++)
        {
            var ti = i;
            var btn = new Button
            {
                Content = tabs[i].Label,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 6, 4),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = B(owner, "PrintMutedBrush"),
                Cursor = Cursors.Hand,
            };
            btn.Click += (_, _) => Show(ti);
            bar.Children.Add(btn);
        }
        var copyBtn = MiniCopy(owner, () => copy(tabs[current].Body));
        bar.Children.Add(copyBtn);
        box.Children.Add(bar);
        box.Children.Add(body);
        Show(0);
        return box;
    }

    private static UIElement BatchWall(FrameworkElement owner, JsonObject? episode)
    {
        var prompts = (episode?["imagePrompts"] as JsonArray)?.OfType<JsonObject>().ToList() ?? [];
        if (prompts.Count == 0) return new TextBlock();
        var stack = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
        stack.Children.Add(SealLabel(owner, "生成条目 · 本集图片提示词"));
        var grid = new UniformGrid { Columns = 2 };
        foreach (var p in prompts)
        {
            var card = PanelCard(owner);
            card.Margin = new Thickness(0, 0, 8, 8);
            var col = new StackPanel();
            col.Children.Add(Print(owner, Str(p["id"]) + " · " + Str(p["title"])));
            col.Children.Add(Print(owner, Str(p["use"], "图片"), muted: true));
            card.Child = col;
            grid.Children.Add(card);
        }
        stack.Children.Add(grid);
        return stack;
    }

    private static UIElement AudioTable(FrameworkElement owner, JsonObject? episode)
    {
        var cuts = (episode?["cutlist"]?["cuts"] as JsonArray)?.OfType<JsonObject>().ToList() ?? [];
        if (cuts.Count == 0) return new TextBlock();
        var stack = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
        stack.Children.Add(SealLabel(owner, "音频对齐 · 剪辑单"));
        foreach (var cut in cuts)
        {
            stack.Children.Add(Print(owner, $"{Str(cut["id"])}  {Str(cut["title"])}  {Num(cut["in"]):0}–{Num(cut["out"]):0}s  {Str(cut["media"])}", muted: true));
            foreach (var sub in (cut["subs"] as JsonArray ?? []).OfType<JsonObject>())
                stack.Children.Add(new TextBlock
                {
                    Text = $"    {Num(sub["s"]):0}–{Num(sub["e"]):0}s  {Str(sub["text"])}",
                    FontFamily = Serif(owner),
                    Foreground = B(owner, "PrintBrush"),
                    Margin = new Thickness(0, 2, 0, 2),
                    TextWrapping = TextWrapping.Wrap,
                });
        }
        return stack;
    }

    private static UIElement LineBook(FrameworkElement owner, JsonArray? book, Action<string> copy)
    {
        var rows = book?.OfType<JsonObject>().ToList() ?? [];
        if (rows.Count == 0) return new TextBlock();
        var stack = new StackPanel { Margin = new Thickness(0, 16, 0, 8) };
        stack.Children.Add(SealLabel(owner, "角色台词簿"));
        var grid = new UniformGrid { Columns = 2 };
        foreach (var row in rows)
        {
            var card = PanelCard(owner);
            card.Margin = new Thickness(0, 0, 8, 8);
            var col = new StackPanel();
            var hd = new DockPanel();
            var all = string.Join("\n", (row["lines"] as JsonArray ?? []).OfType<JsonObject>().Select(l => Str(l["text"])));
            var btn = MiniCopy(owner, () => copy(all));
            DockPanel.SetDock(btn, Dock.Right);
            hd.Children.Add(btn);
            hd.Children.Add(new TextBlock { Text = $"{Str(row["who"])} · {Str(row["n"])} 句", FontFamily = Serif(owner), FontSize = 16, Foreground = B(owner, "PrintBrush") });
            col.Children.Add(hd);
            var scroll = new ScrollViewer { MaxHeight = 160, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var inner = new StackPanel();
            foreach (var line in (row["lines"] as JsonArray ?? []).OfType<JsonObject>().Take(12))
                inner.Children.Add(new TextBlock
                {
                    Text = $"{Str(line["scene"])}  {Str(line["text"])}",
                    FontFamily = Serif(owner),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = B(owner, "PrintBrush"),
                    Margin = new Thickness(0, 4, 0, 0),
                    FontSize = 13,
                });
            scroll.Content = inner;
            col.Children.Add(scroll);
            card.Child = col;
            grid.Children.Add(card);
        }
        stack.Children.Add(grid);
        return stack;
    }

    private static UIElement RhythmStrip(FrameworkElement owner, List<JsonObject> shots)
    {
        var box = PanelCard(owner);
        box.Margin = new Thickness(0, 10, 0, 10);
        var col = new StackPanel();
        col.Children.Add(Print(owner, "节奏条  宽度 = 时长，颜色 = 景别", muted: true));
        var track = new Grid { Height = 22, Margin = new Thickness(0, 8, 0, 6) };
        for (var i = 0; i < shots.Count; i++)
        {
            var sec = Math.Max(Num(shots[i]["sec"]), 0.4);
            track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(sec, GridUnitType.Star) });
            var cell = new Rectangle { Fill = ScaleBrush(Str(shots[i]["scale"])), Stroke = B(owner, "RulePaperBrush"), StrokeThickness = 0.5, ToolTip = $"{Str(shots[i]["n"])} {Str(shots[i]["title"])} · {Num(shots[i]["sec"]):0}s" };
            Grid.SetColumn(cell, i);
            track.Children.Add(cell);
        }
        col.Children.Add(track);
        box.Child = col;
        return box;
    }

    private static Brush ScaleBrush(string scale) => scale switch
    {
        "远景" or "全景" => new SolidColorBrush(Color.FromRgb(0x4A, 0x48, 0x44)),
        "中景" => new SolidColorBrush(Color.FromRgb(0x6B, 0x64, 0x54)),
        "中近景" => new SolidColorBrush(Color.FromRgb(0x8A, 0x7E, 0x62)),
        "近景" => new SolidColorBrush(Color.FromRgb(0xC4, 0xB4, 0x8C)),
        "特写" or "细节" => new SolidColorBrush(Color.FromRgb(0xE2, 0xD4, 0xB0)),
        "大特写" => new SolidColorBrush(Color.FromRgb(0xF7, 0xF3, 0xEC)),
        _ => new SolidColorBrush(Color.FromRgb(0x3A, 0x38, 0x36)),
    };

    private static UIElement KpiRow(FrameworkElement owner, (string L, string V, bool Accent)[] cells)
    {
        var grid = new UniformGrid { Columns = cells.Length, Margin = new Thickness(0, 0, 0, 10) };
        foreach (var cell in cells)
        {
            var card = PanelCard(owner);
            card.Margin = new Thickness(0, 0, 8, 0);
            card.BorderThickness = new Thickness(1, cell.Accent ? 2 : 1, 1, 1);
            if (cell.Accent) card.BorderBrush = B(owner, "SealBrush");
            var s = new StackPanel { Margin = new Thickness(4) };
            s.Children.Add(new TextBlock { Text = cell.L, FontSize = 10, Foreground = B(owner, "PrintMutedBrush") });
            s.Children.Add(new TextBlock { Text = cell.V, FontFamily = Serif(owner), FontSize = 22, Foreground = B(owner, "PrintBrush") });
            card.Child = s;
            grid.Children.Add(card);
        }
        return grid;
    }

    private static UIElement GateBar(FrameworkElement owner, JsonObject? gate, double target, double est)
    {
        var row = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        if (gate is not null)
        {
            var ok = gate["ok"]?.GetValue<bool>() == true;
            row.Children.Add(new Border
            {
                BorderBrush = B(owner, ok ? "GoldBrush" : "WarnBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(99),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 8, 4),
                Child = Print(owner, ok ? $"质量门 通过 · must {Str(gate["must"])}" : $"质量门 未过 · must {Str(gate["must"])} / 共 {Str(gate["total"])}", muted: !ok),
            });
        }
        if (target > 0 && est > 0)
        {
            var over = est > target * 1.15;
            row.Children.Add(Print(owner, $"时长仪  对白+动作估 {est:0}s / 目标 {target:0}s" + (over ? "  · 偏长" : ""), muted: !over));
        }
        return row;
    }

    private static UIElement SceneHeader(FrameworkElement owner, JsonObject scene, List<string> props)
    {
        var panel = new WrapPanel { Margin = new Thickness(0, 14, 0, 6) };
        panel.Children.Add(new TextBlock { Text = Str(scene["id"]), FontFamily = Serif(owner), FontWeight = FontWeights.SemiBold, Foreground = B(owner, "PrintBrush"), Margin = new Thickness(0, 0, 8, 4) });
        panel.Children.Add(Chip(owner, Str(scene["space"])));
        panel.Children.Add(Chip(owner, Str(scene["place"])));
        panel.Children.Add(Chip(owner, Str(scene["time"])));
        var who = new HashSet<string>();
        foreach (var b in (scene["blocks"] as JsonArray ?? []).OfType<JsonObject>())
            if (Str(b["k"]) == "line") who.Add(Str(b["who"]));
        foreach (var w in who) panel.Children.Add(Chip(owner, w));
        var blob = string.Join(" ", (scene["blocks"] as JsonArray ?? []).OfType<JsonObject>().Select(b => Str(b["text"])));
        foreach (var p in props)
            if (blob.Contains(p)) panel.Children.Add(Chip(owner, p, seal: true));
        return panel;
    }

    private static UIElement DialogueRow(FrameworkElement owner, JsonObject block, Action<string> copy)
    {
        var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(132) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var whoCol = new StackPanel();
        whoCol.Children.Add(new TextBlock { Text = Str(block["who"]), FontFamily = Serif(owner), FontWeight = FontWeights.SemiBold, Foreground = B(owner, "PrintBrush") });
        var tag = Str(block["tag"]);
        var paren = Str(block["paren"]);
        var sub = string.Join(" ", new[] { tag.Length > 0 ? $"[{tag}]" : "", paren }.Where(s => s.Length > 0));
        if (sub.Length > 0)
            whoCol.Children.Add(new TextBlock { Text = sub, FontSize = 11, Foreground = B(owner, "PrintMutedBrush") });
        var said = new TextBlock { Text = Str(block["text"]), FontFamily = Serif(owner), FontSize = 15, TextWrapping = TextWrapping.Wrap, Foreground = B(owner, "PrintBrush"), LineHeight = 24 };
        var copyBtn = MiniCopy(owner, () => copy(Str(block["text"])));
        copyBtn.Opacity = 0.35;
        grid.MouseEnter += (_, _) => copyBtn.Opacity = 1;
        grid.MouseLeave += (_, _) => copyBtn.Opacity = 0.35;
        Grid.SetColumn(said, 1);
        Grid.SetColumn(copyBtn, 2);
        grid.Children.Add(whoCol);
        grid.Children.Add(said);
        grid.Children.Add(copyBtn);
        return new Border { BorderBrush = B(owner, "RulePaperBrush"), BorderThickness = new Thickness(2, 0, 0, 0), Padding = new Thickness(10, 4, 0, 4), Child = grid };
    }

    private static Border PanelCard(FrameworkElement owner) => new()
    {
        Background = B(owner, "PanelPaperBrush"),
        BorderBrush = B(owner, "RulePaperBrush"),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(14),
        Margin = new Thickness(0, 8, 0, 0),
    };

    private static Button MiniCopy(FrameworkElement owner, Action act)
    {
        var btn = new Button
        {
            Content = "复制",
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(6, 0, 0, 0),
            FontSize = 11,
            Background = B(owner, "PaperBrush"),
            Foreground = B(owner, "PrintMutedBrush"),
            BorderBrush = B(owner, "RulePaperBrush"),
            Cursor = Cursors.Hand,
        };
        btn.Click += (_, _) => act();
        return btn;
    }

    private static Border Chip(FrameworkElement owner, string text, bool seal = false)
    {
        if (string.IsNullOrWhiteSpace(text)) return new Border { Width = 0 };
        return new Border
        {
            BorderBrush = B(owner, seal ? "SealBrush" : "RulePaperBrush"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(6, 0, 6, 0),
            Margin = new Thickness(0, 0, 6, 4),
            Background = B(owner, "PaperBrush"),
            Child = new TextBlock { Text = text, FontSize = 11, Foreground = B(owner, seal ? "SealBrush" : "PrintMutedBrush") },
        };
    }

    private static TextBlock SealLabel(FrameworkElement owner, string text) => new()
    {
        Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold,
        Foreground = B(owner, "SealBrush"), Margin = new Thickness(0, 16, 0, 8),
    };

    private static TextBlock Print(FrameworkElement owner, string text, bool muted = false) => new()
    {
        Text = text, TextWrapping = TextWrapping.Wrap,
        Foreground = B(owner, muted ? "PrintMutedBrush" : "PrintBrush"),
        Margin = new Thickness(0, 2, 0, 2),
    };

    private static UIElement FieldBlock(FrameworkElement owner, string label, string body)
    {
        var s = new StackPanel { Margin = new Thickness(0, 0, 12, 10) };
        s.Children.Add(SealLabel(owner, label));
        s.Margin = new Thickness(0, 0, 12, 8);
        ((TextBlock)s.Children[0]).Margin = new Thickness(0, 0, 0, 4);
        s.Children.Add(Print(owner, body));
        return s;
    }

    private static UIElement MetaCard(FrameworkElement owner, string title, (string K, string V)[] rows)
    {
        var card = PanelCard(owner);
        var s = new StackPanel();
        s.Children.Add(new TextBlock { Text = title, FontSize = 11, Foreground = B(owner, "PrintMutedBrush"), Margin = new Thickness(0, 0, 0, 6) });
        foreach (var (k, v) in rows)
        {
            if (string.IsNullOrEmpty(k)) s.Children.Add(Print(owner, v));
            else
            {
                var r = new DockPanel();
                r.Children.Add(new TextBlock { Text = k, Width = 64, Foreground = B(owner, "PrintMutedBrush") });
                r.Children.Add(Print(owner, v));
                s.Children.Add(r);
            }
        }
        card.Child = s;
        return card;
    }

    private static UIElement HookRow(FrameworkElement owner, string label, string text)
    {
        var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(new TextBlock { Text = label, Foreground = B(owner, "SealBrush"), FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 10, 0) });
        var body = Print(owner, text);
        Grid.SetColumn(body, 1);
        grid.Children.Add(body);
        return grid;
    }

    private static UIElement Empty(FrameworkElement owner, string text) =>
        Wrap(new TextBlock { Text = text, Foreground = B(owner, "PrintMutedBrush"), Margin = new Thickness(24), TextWrapping = TextWrapping.Wrap }, owner);

    private static IEnumerable<JsonObject> LinkedPrompts(JsonObject? episode, string name)
    {
        foreach (var p in (episode?["imagePrompts"] as JsonArray ?? []).OfType<JsonObject>())
        {
            var title = Str(p["title"]);
            var id = Str(p["id"]);
            var prompt = Str(p["prompt"]);
            if (title.Contains(name) || id.Contains(name, StringComparison.OrdinalIgnoreCase) || prompt.Contains(name))
                yield return p;
        }
    }

    private static List<string> QuoteLines(JsonObject fields, JsonObject? episode, string name)
    {
        var raw = Str(fields["原文依据"]);
        if (raw.Length > 0)
            return raw.Split(new[] { '；', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).Take(4).ToList();
        var book = (episode?["script"]?["lineBook"] as JsonArray ?? []).OfType<JsonObject>()
            .FirstOrDefault(r => Str(r["who"]) == name);
        return (book?["lines"] as JsonArray ?? []).OfType<JsonObject>().Select(l => Str(l["text"])).Where(s => s.Length > 0).Take(2).ToList();
    }

    private static string LockText(JsonObject item)
    {
        var lockObj = item["lock"] as JsonObject;
        return Str(lockObj?["surface"], Str((item["fields"] as JsonObject)?["连续性锁"]));
    }

    private static int ShotCount(JsonObject? episode, string name) =>
        (episode?["board"]?["shots"] as JsonArray ?? []).OfType<JsonObject>()
            .Count(s => (s["basis"] as JsonArray ?? []).OfType<JsonObject>().Any(b => Str(b["name"]) == name));

    private static string FormatEst(JsonObject? metrics)
    {
        var est = Num(metrics?["scriptEst"]);
        return est > 0 ? $"{est:0}s" : "—";
    }

    private static string VoText(JsonObject? metrics)
    {
        if (metrics?["vo"] is null) return "—";
        return $"{Num(metrics["vo"]) * 100:0}%";
    }

    private static Brush B(FrameworkElement owner, string key)
    {
        var mapped = key switch
        {
            "PaperBrush" => "BgBrush",
            "PanelPaperBrush" => "SurfaceBrush",
            "SidePaperBrush" => "Surface2Brush",
            "PrintBrush" => "InkBrush",
            "PrintMutedBrush" => "SoftInkBrush",
            "SealBrush" => "GoldBrush",
            "RulePaperBrush" => "LineBrush",
            "SealSoftBrush" => "GoldWash",
            _ => key,
        };
        return (Brush)owner.FindResource(mapped);
    }
    private static FontFamily Serif(FrameworkElement owner) => (FontFamily)owner.FindResource("SerifFont");

    private static string Str(JsonNode? node, string fallback = "")
    {
        if (node is null) return fallback;
        if (node is JsonValue v)
        {
            if (v.TryGetValue<string>(out var s)) return s ?? fallback;
            return v.ToJsonString().Trim('"');
        }
        return fallback;
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
