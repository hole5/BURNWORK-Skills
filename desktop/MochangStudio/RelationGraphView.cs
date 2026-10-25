using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MochangStudio;

internal static class RelationGraphView
{
    private sealed record EdgeRec(string A, string B, string Label, Path Stroke, TextBlock? Caption);
    private sealed record NodeRec(string Name, bool InCast, Ellipse Dot, Ellipse Hit, TextBlock Label);

    internal static UIElement Build(
        FrameworkElement owner,
        JsonObject? graph,
        IReadOnlyList<JsonObject> people,
        Action<string> openPerson)
    {
        var names = (graph?["nodes"] as JsonArray ?? []).Select(n => Str(n)).Where(s => s.Length > 0).Distinct().ToList();
        var rawEdges = (graph?["edges"] as JsonArray ?? []).OfType<JsonObject>()
            .Select(e => (A: Str(e["from"]), B: Str(e["to"]), Label: Str(e["label"])))
            .Where(e => e.A.Length > 0 && e.B.Length > 0 && e.A != e.B);
        var merged = new Dictionary<(string A, string B), List<string>>();
        foreach (var e in rawEdges)
        {
            var key = string.CompareOrdinal(e.A, e.B) <= 0 ? (e.A, e.B) : (e.B, e.A);
            if (!merged.TryGetValue(key, out var notes))
            {
                notes = new List<string>();
                merged[key] = notes;
            }
            if (e.Label.Length > 0 && !notes.Contains(e.Label))
                notes.Add(e.Label);
        }
        var edges = merged.Select(kv => (A: kv.Key.A, B: kv.Key.B, Label: string.Join("；", kv.Value))).ToList();
        foreach (var e in edges)
        {
            if (!names.Contains(e.A)) names.Add(e.A);
            if (!names.Contains(e.B)) names.Add(e.B);
        }

        var root = new DockPanel { Margin = new Thickness(8) };
        var head = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(head, Dock.Top);
        head.Children.Add(new TextBlock
        {
            Text = "关系图谱",
            FontFamily = (FontFamily)owner.FindResource("SerifFont"),
            FontSize = 22,
            Foreground = Brush(owner, "InkBrush"),
            Margin = new Thickness(0, 0, 12, 4),
        });
        head.Children.Add(Chip(owner, $"{names.Count} 位角色 · {edges.Count} 组关系"));
        var labelBtn = new Button
        {
            Content = "关系文字",
            Padding = new Thickness(10, 4, 10, 4),
            Margin = new Thickness(8, 0, 8, 4),
            Background = Brush(owner, "Surface2Brush"),
            Foreground = Brush(owner, "GoldBrush"),
            BorderBrush = Brush(owner, "GoldBrush"),
            Cursor = Cursors.Hand,
        };
        head.Children.Add(labelBtn);
        head.Children.Add(new TextBlock
        {
            Text = "悬停看关系，点击进角色",
            Foreground = Brush(owner, "SoftInkBrush"),
            VerticalAlignment = VerticalAlignment.Center,
        });
        root.Children.Add(head);

        if (names.Count == 0)
        {
            root.Children.Add(new TextBlock
            {
                Text = "还没有关系。在人物条目写「- 关系：对方（说明）；对方（说明）」后会出现图谱。",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brush(owner, "SoftInkBrush"),
                Margin = new Thickness(8),
            });
            return root;
        }

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
        const double size = 640;
        const double cx = size / 2, cy = size / 2, radius = 220;
        var canvas = new Canvas
        {
            Width = size,
            Height = size,
            Background = Brush(owner, "SurfaceBrush"),
        };
        var frame = new Border
        {
            BorderBrush = Brush(owner, "LineBrush"),
            BorderThickness = new Thickness(1),
            Background = Brush(owner, "SurfaceBrush"),
            Child = new Viewbox { Stretch = Stretch.Uniform, Child = canvas },
        };

        var deg = names.ToDictionary(n => n, _ => 0);
        foreach (var e in edges)
        {
            deg[e.A] = deg.GetValueOrDefault(e.A) + 1;
            deg[e.B] = deg.GetValueOrDefault(e.B) + 1;
        }
        var maxDeg = deg.Count == 0 ? 0 : deg.Values.Max();
        var inCast = people.Select(p => Str(p["name"])).ToHashSet();
        var n = Math.Max(names.Count, 1);
        var pos = new Dictionary<string, Point>();
        for (var i = 0; i < names.Count; i++)
        {
            var ang = (-Math.PI / 2) + i * (2 * Math.PI / n);
            pos[names[i]] = new Point(cx + radius * Math.Cos(ang), cy + radius * Math.Sin(ang));
        }

        var edgeVis = new List<EdgeRec>();
        var nodeVis = new List<NodeRec>();
        var gold = Brush(owner, "GoldBrush");
        var ink = Brush(owner, "InkBrush");
        var surface2 = Brush(owner, "Surface2Brush");

        foreach (var e in edges)
        {
            if (!pos.TryGetValue(e.A, out var p1) || !pos.TryGetValue(e.B, out var p2)) continue;
            var mid = new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
            var q = new Point(mid.X * 0.4 + cx * 0.6, mid.Y * 0.4 + cy * 0.6);
            var geo = new PathGeometry();
            var fig = new PathFigure { StartPoint = p1, IsClosed = false };
            fig.Segments.Add(new QuadraticBezierSegment(q, p2, true));
            geo.Figures.Add(fig);
            var stroke = new Path
            {
                Stroke = gold,
                StrokeThickness = 2.2,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Data = geo,
                Tag = (e.A, e.B),
            };
            canvas.Children.Add(stroke);
            TextBlock? cap = null;
            if (e.Label.Length > 0)
            {
                cap = new TextBlock
                {
                    Text = e.Label,
                    FontSize = 11,
                    Foreground = Brush(owner, "SoftInkBrush"),
                    Visibility = Visibility.Collapsed,
                    IsHitTestVisible = false,
                };
                Canvas.SetLeft(cap, q.X - 24);
                Canvas.SetTop(cap, q.Y - 8);
                canvas.Children.Add(cap);
            }
            edgeVis.Add(new EdgeRec(e.A, e.B, e.Label, stroke, cap));
        }

        foreach (var name in names)
        {
            var p = pos[name];
            var lead = maxDeg > 0 && deg[name] >= Math.Max(2, maxDeg - 1);
            var r = lead ? 11 : (deg[name] > 0 ? 7 : 5.5);
            var dot = new Ellipse
            {
                Width = r * 2,
                Height = r * 2,
                Fill = lead ? gold : surface2,
                Stroke = lead ? gold : Brush(owner, "SoftInkBrush"),
                StrokeThickness = 1.6,
                IsHitTestVisible = false,
            };
            Canvas.SetLeft(dot, p.X - r);
            Canvas.SetTop(dot, p.Y - r);
            var hit = new Ellipse
            {
                Width = 36,
                Height = 36,
                Fill = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Tag = name,
            };
            Canvas.SetLeft(hit, p.X - 18);
            Canvas.SetTop(hit, p.Y - 18);
            var ox = p.X - cx;
            var oy = p.Y - cy;
            var len = Math.Max(1, Math.Sqrt(ox * ox + oy * oy));
            var lx = p.X + ox / len * 22;
            var ly = p.Y + oy / len * 16;
            var label = new TextBlock
            {
                Text = name,
                FontFamily = (FontFamily)owner.FindResource("SerifFont"),
                FontSize = 13,
                Foreground = ink,
                IsHitTestVisible = false,
            };
            label.Measure(new Size(200, 40));
            Canvas.SetLeft(label, lx < cx ? lx - label.DesiredSize.Width : lx);
            Canvas.SetTop(label, ly - 8);
            canvas.Children.Add(dot);
            canvas.Children.Add(label);
            canvas.Children.Add(hit);
            nodeVis.Add(new NodeRec(name, inCast.Contains(name), dot, hit, label));
        }

        var list = new StackPanel();
        var listCard = new Border
        {
            BorderBrush = Brush(owner, "LineBrush"),
            BorderThickness = new Thickness(1),
            Background = Brush(owner, "SurfaceBrush"),
            Margin = new Thickness(12, 0, 0, 0),
        };
        var listInner = new DockPanel();
        listInner.Children.Add(new TextBlock
        {
            Text = "关系",
            FontSize = 11,
            Foreground = Brush(owner, "SoftInkBrush"),
            Margin = new Thickness(14, 12, 14, 8),
        });
        DockPanel.SetDock(listInner.Children[0], Dock.Top);
        var rowsHost = new StackPanel();
        var rowBtns = new List<Button>();
        foreach (var e in edges)
        {
            var a = e.A;
            var b = e.B;
            var row = new Button
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = Brush(owner, "LineBrush"),
                Padding = new Thickness(14, 10, 14, 10),
                Cursor = Cursors.Hand,
                Tag = (a, b),
            };
            var col = new StackPanel();
            var title = new TextBlock
            {
                FontFamily = (FontFamily)owner.FindResource("SerifFont"),
                FontSize = 14,
                Foreground = ink,
                TextWrapping = TextWrapping.Wrap,
                Text = a + "  ·  " + b,
            };
            col.Children.Add(title);
            if (e.Label.Length > 0)
                col.Children.Add(new TextBlock { Text = e.Label, TextWrapping = TextWrapping.Wrap, Foreground = Brush(owner, "SoftInkBrush"), Margin = new Thickness(0, 4, 0, 0) });
            row.Content = col;
            rowsHost.Children.Add(row);
            rowBtns.Add(row);
        }
        listInner.Children.Add(new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = rowsHost });
        listCard.Child = listInner;

        var showLabels = false;
        void ApplyLabels()
        {
            foreach (var e in edgeVis)
                if (e.Caption is not null)
                    e.Caption.Visibility = showLabels ? Visibility.Visible : Visibility.Collapsed;
            labelBtn.Background = showLabels ? Brush(owner, "GoldWash") : Brush(owner, "Surface2Brush");
        }
        labelBtn.Click += (_, _) => { showLabels = !showLabels; ApplyLabels(); };

        void Clear()
        {
            foreach (var e in edgeVis)
            {
                e.Stroke.Stroke = gold;
                e.Stroke.StrokeThickness = 2.2;
                e.Stroke.Opacity = 0.85;
                if (e.Caption is not null)
                {
                    e.Caption.Foreground = Brush(owner, "SoftInkBrush");
                    e.Caption.Opacity = showLabels ? 1 : e.Caption.Opacity;
                    if (!showLabels) e.Caption.Visibility = Visibility.Collapsed;
                }
            }
            foreach (var nd in nodeVis)
            {
                nd.Dot.Opacity = 1;
                nd.Label.Opacity = 1;
                nd.Label.Foreground = ink;
            }
            foreach (var r in rowBtns) r.Opacity = 1;
        }

        void ByNode(string name)
        {
            var near = new HashSet<string> { name };
            foreach (var e in edgeVis)
            {
                if (e.A == name) near.Add(e.B);
                if (e.B == name) near.Add(e.A);
            }
            foreach (var e in edgeVis)
            {
                var hot = e.A == name || e.B == name;
                e.Stroke.Stroke = gold;
                e.Stroke.StrokeThickness = hot ? 3.2 : 1.6;
                e.Stroke.Opacity = hot ? 1 : 0.22;
                if (e.Caption is not null)
                {
                    e.Caption.Visibility = hot || showLabels ? Visibility.Visible : Visibility.Collapsed;
                    e.Caption.Foreground = hot ? gold : Brush(owner, "SoftInkBrush");
                    e.Caption.Opacity = hot ? 1 : 0.2;
                }
            }
            foreach (var nd in nodeVis)
            {
                var hot = nd.Name == name;
                nd.Dot.StrokeThickness = hot ? 2.6 : 1.6;
                nd.Dot.Stroke = hot || (maxDeg > 0 && deg.GetValueOrDefault(nd.Name) >= Math.Max(2, maxDeg - 1)) ? gold : Brush(owner, "SoftInkBrush");
                nd.Label.Foreground = hot ? gold : ink;
                nd.Dot.Opacity = near.Contains(nd.Name) ? 1 : 0.22;
                nd.Label.Opacity = near.Contains(nd.Name) ? 1 : 0.22;
            }
            foreach (var r in rowBtns)
            {
                var pair = ((string A, string B))r.Tag;
                var hot = pair.A == name || pair.B == name;
                r.Opacity = hot ? 1 : 0.3;
            }
        }

        void ByEdge(string a, string b)
        {
            foreach (var e in edgeVis)
            {
                var hot = e.A == a && e.B == b;
                e.Stroke.Stroke = gold;
                e.Stroke.StrokeThickness = hot ? 3.2 : 1.6;
                e.Stroke.Opacity = hot ? 1 : 0.22;
                if (e.Caption is not null)
                {
                    e.Caption.Visibility = hot || showLabels ? Visibility.Visible : Visibility.Collapsed;
                    e.Caption.Foreground = hot ? gold : Brush(owner, "SoftInkBrush");
                }
            }
            foreach (var nd in nodeVis)
            {
                var hot = nd.Name == a || nd.Name == b;
                nd.Dot.Opacity = hot ? 1 : 0.22;
                nd.Label.Opacity = hot ? 1 : 0.22;
                nd.Label.Foreground = hot ? gold : ink;
            }
            foreach (var r in rowBtns)
            {
                var pair = ((string A, string B))r.Tag;
                r.Opacity = pair.A == a && pair.B == b ? 1 : 0.3;
            }
        }

        foreach (var nd in nodeVis)
        {
            nd.Hit.MouseEnter += (_, _) => ByNode(nd.Name);
            nd.Hit.MouseLeave += (_, _) => Clear();
            nd.Hit.MouseLeftButtonUp += (_, _) =>
            {
                if (nd.InCast) openPerson(nd.Name);
            };
        }
        foreach (var row in rowBtns)
        {
            var pair = ((string A, string B))row.Tag;
            row.MouseEnter += (_, _) => ByEdge(pair.A, pair.B);
            row.MouseLeave += (_, _) => Clear();
            row.Click += (_, _) =>
            {
                if (inCast.Contains(pair.A)) openPerson(pair.A);
                else if (inCast.Contains(pair.B)) openPerson(pair.B);
            };
        }

        Grid.SetColumn(listCard, 1);
        body.Children.Add(frame);
        body.Children.Add(listCard);
        root.Children.Add(body);
        return root;
    }

    private static Border Chip(FrameworkElement owner, string text) => new()
    {
        BorderBrush = Brush(owner, "LineBrush"),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(8, 2, 8, 2),
        Margin = new Thickness(0, 4, 8, 4),
        Child = new TextBlock { Text = text, Foreground = Brush(owner, "GoldBrush"), FontSize = 12 },
    };

    private static Brush Brush(FrameworkElement owner, string key) => (Brush)owner.FindResource(key);

    private static string Str(JsonNode? node, string fallback = "")
    {
        if (node is null) return fallback;
        if (node is JsonValue v && v.TryGetValue<string>(out var s)) return s ?? fallback;
        return fallback;
    }
}
