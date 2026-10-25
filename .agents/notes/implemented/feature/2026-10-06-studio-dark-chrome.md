# 工作室暗色窗框与对话框

## Problem

主窗口仍用系统浅色标题栏；确认/失败/删除等提醒走 `MessageBox`，浅底蓝条与暗金壳不一致。

## Decision

主窗口、聊天设置和提醒框都是 `WindowStyle=None` 加 `WindowChrome`，自绘 40px 暗色标题栏（墨标、标题、最小化/最大化/关闭）。提醒不再调用 `MessageBox`，改用 `StudioDialog`：Surface 底、可滚动正文、取消用 GhostButton、确定用 GoldButton。系统文件/文件夹选择器仍用操作系统对话框。

## Alternatives considered

- **只改 MessageBox 主题**：最强理由是改动小。不用：Win32 框吃不到 Studio 色板。
- **允许透明亚克力标题栏**：最强理由更接近 Win11。不用：本机壳已是实心底，避免混两套材质。

## Consequences

提醒与主窗同一套按钮和标题栏。代价是要自己处理拖动、缩放与最大化；未处理资源管理器挑文件框。
