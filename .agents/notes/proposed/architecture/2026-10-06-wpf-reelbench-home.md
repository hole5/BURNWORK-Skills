# Agent Note: WPF 首页对齐 Reelbench 并对接项目

Status: proposed（已被 [WPF 首页落地](../../implemented/architecture/2026-10-06-wpf-reelbench-home.md) 采纳）

关联：[WPF 桌面壳](../../implemented/architecture/2026-10-06-wpf-desktop-shell.md)

## Problem

现有 WPF 一打开就进分镜工作台，不像独立产品首页；创作者无法在窗口里浏览最近项目、从一句话开写，并再进入已有五文档项目。

## Proposal

主窗口分两层：首页（侧栏导航、最近项目、标语与创作输入框）与工作台（既有阶段栏）。打开/创建项目后切到工作台；项目列表来自本机 `dashboard_server` 的 `/api/projects`，最近打开记在 `~/.mochang/recent.json`。新建项目用现有 `project_tool.py initialize`，仍写五份 Markdown 真相。

## Alternatives considered

- **只改配色、不改首页信息架构** — 最强理由：改动小。不用：对不上创作者要的「像 Reelbench 那样从首页进项目」。
- **首页内嵌浏览器** — 最强理由：少做 XAML。不用：与「C# 写 UI」冲突。

## Consequences

- **收益**：独立桌面入口像产品主页，又能接到现有项目 API。
- **代价**：要维护首页与工作台两套布局状态。
