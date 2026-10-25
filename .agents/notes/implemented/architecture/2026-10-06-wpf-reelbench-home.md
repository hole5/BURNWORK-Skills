# Agent Note: WPF 首页对齐 Reelbench 并对接项目

Status: implemented

关联：[提案原文](../../proposed/architecture/2026-10-06-wpf-reelbench-home.md)、[WPF 桌面壳](2026-10-06-wpf-desktop-shell.md)

## Problem

WPF 一打开就进分镜工作台，不像独立产品首页；无法在窗口内浏览最近项目、从一句话开写再进入五文档项目。

## Decision

主窗口分两层：**首页**（侧栏导航、最近项目、标语与创作输入框，视觉对齐 Reelbench）与**工作台**（既有阶段栏）。`/api/projects` 发现工作区内项目；最近打开记在 `~/.mochang/recent.json`。新建项目调用 `project_tool.py init`，落到 `<workspace>/projects/<slug>/`，可选写入 `项目开发/创作意图.md`；打开后进入工作台并带入聊天上下文。

## Alternatives considered

- **只改配色** — 最强理由：改动小。不用：对不上「从首页进项目」的产品形态。
- **首页内嵌 WebView** — 最强理由：少做 XAML。不用：与 C# UI 目标冲突。

## Consequences

- **收益**：独立桌面入口像产品主页，又能接到现有项目 API 与五文档真相。
- **代价**：首页与工作台两套布局；新建依赖本机 Python/`project_tool`。首页「资产库」读 `/api/tree` 媒体节点，「创意模板」只填输入框不另存模板库。工作台生产走 prepare → 确认对话框 → confirm → run。
