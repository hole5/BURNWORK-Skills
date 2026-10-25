# Agent Note: 墨场 WPF 独立桌面壳

Status: proposed（已被 [WPF 桌面壳](../../implemented/architecture/2026-10-06-wpf-desktop-shell.md) 采纳）

关联：[燃烧工作室落地](../../implemented/architecture/2026-10-05-mochang-local-studio.md)

## Problem

创作者预览本机 HTTP 工作室时，界面密度和配色与参照工作台（深色金边、左侧阶段、分集覆盖表、底栏）不一致；且入口是系统浏览器，不是独立 PC 桌面应用。需要原生窗口，并在同一窗口里放 AI 聊天框。

## Proposal

新增 `desktop/MochangStudio`（.NET 8 WPF）：独立窗口绘制工作台壳与分镜覆盖/角色/剧本等主视图，聊天面板走项目外 OpenAI 兼容接口。创作真相与解析仍由现有 Python `dashboard_server` 提供；WPF 只作桌面壳，不把 Markdown 迁成第二套 JSON。

## Alternatives considered

- **继续用系统浏览器** — 最强理由：已有完整 HTML。不用：创作者明确要独立桌面应用。
- **WPF 内嵌 WebView2 显示现有页** — 最强理由：最快套上窗口。不用：仍是浏览器渲染，对不上「用 C# 写 UI」的要求。
- **用 C# 重写解析器** — 最强理由：可去掉 Python。不用：会分叉五文档语法；第一期继续调本机 API。

## Consequences

- **收益**：Windows 上有独立窗口、聊天与覆盖表可并排。
- **代价**：要维护 WPF 与 HTML 两套壳；非 Windows 仍用浏览器入口。
