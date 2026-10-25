# Agent Note: 墨场 WPF 独立桌面壳

Status: implemented

关联：[提案原文](../../proposed/architecture/2026-10-06-wpf-desktop-shell.md)、[燃烧工作室落地](2026-10-05-mochang-local-studio.md)

## Problem

HTTP 创作台在系统浏览器里打开，视觉密度对不上参照工作台；创作者要独立 PC 窗口，并在同一界面里和 AI 对话。

## Decision

Windows 主入口是 `desktop/MochangStudio`（.NET 8 WPF）。窗口自己画左侧阶段、分集轨、分镜覆盖表、角色/剧本/提示词阅读面和右侧聊天栏。数据仍来自本机 `dashboard_server` 的 `/api/series`、`/api/episode`；聊天走项目外 `~/.mochang/chat.json` 的 OpenAI 兼容 `/chat/completions`。`scripts/open-studio.bat` 启动 `BurnworkStudio.exe`，不再默认打开浏览器。`--open` 仍可打开 HTML 页，供非 Windows 或对照用。

## Alternatives considered

- **继续只用系统浏览器** — 最强理由：HTML 已覆盖全流程。不用：无法满足独立桌面应用。
- **WPF 内嵌 WebView2** — 最强理由：少重做视图。不用：仍是浏览器渲染，不是 C# UI。
- **C# 重写 Markdown 解析** — 最强理由：可去 Python。不用：会分叉五文档语法。

## Consequences

- **收益**：Windows 上有独立窗口、金边深色壳、覆盖表与 AI 聊天并排。
- **代价**：WPF 与 HTML 两套壳要一起改阶段列表；聊天下载依赖用户自备模型接口。
- **已知上限**：第一期 WPF 不重做 HTML 印张大图廊与完整确认生产抽屉；生产预览仍打回 API，确认闸门不绕过。
