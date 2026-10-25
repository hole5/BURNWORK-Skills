# Agent Note: 窗口标题保持燃烧工作室，exe 为 BurnworkStudio

Status: implemented

关联：[exe 名](2026-10-07-burnwork-studio-exe.md)、[误改展示名](2026-10-07-burnwork-studio-name.md)

## Problem

创作者要的是 exe 叫 `BurnworkStudio.exe`；窗口标题应继续显示 **燃烧工作室**。中间误把 Title / ApplicationTitle 改成了 BURNWORK Studio。

## Decision

窗口 Title、标题栏、侧栏产品字、对话框与 `ApplicationTitle` 仍为 **燃烧工作室**。`AssemblyName` 仍为 **BurnworkStudio**（产物 `BurnworkStudio.exe`）。Dashboard 中文产品名恢复「燃烧工作室」，英文字段为 BURNWORK Studio。

## Alternatives considered

- **窗口也显示 BURNWORK Studio** — 最强理由：与 exe 同名。不用：创作者明确要标题显示燃烧工作室。

## Consequences

- **收益**：界面中文名与文件名职责分开，符合创作者意图。
- **代价**：任务栏图标旁可能仍显示 exe 进程名 BurnworkStudio，与窗口标题文案不同，属 Windows 常见行为。
