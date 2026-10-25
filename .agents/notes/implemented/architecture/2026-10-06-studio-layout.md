# Agent Note: 工作室目录分仓并对接工作台

Status: implemented

关联：[项目目录与设置页](../../implemented/architecture/2026-10-06-wpf-settings-projects.md)

## Problem

工作区若指向仓库根，项目列表会混进 examples 等夹具；技能与用户项目散落。需要把用户项目、技能、资产收到固定文件夹，工作台只对接这些目录。

## Decision

默认工作室根为 `文档/燃烧工作室`，其下固定：`projects/`（WPF `BackendHost` 与 dashboard `--workspace`、新建/打开/删除只发生在这里）、`skills/`（设置列表与导入）、`assets/`（资产库额外扫描）。路径写入 `~/.mochang/studio.json`。套件代码仍在 drama-skills 仓库；Python 工具从 `RepoRoot/skills/short-drama` 运行。启动参数若指向本仓库则忽略，避免把仓库当工作室根。`studio_launch.py` 未传 `--workspace` 时读 `studio.json` 的 `ProjectsDir`。启动时把仓库 `projects/` 里含 `short-drama.json` 的目录复制到工作室 `projects/`（不覆盖同名、不拷 examples/evaluations）。打开失败且为 `project not found` 时，确认对话框后从最近打开移除该项。

## Alternatives considered

- **继续扫描整个仓库当工作区** — 最强理由：样例项目立刻可见。不用：和「项目统一到一个文件夹」冲突，夹具会污染列表。
- **把套件 skills 复制进用户目录** — 最强理由：完全自包含。不用：体积大、升级不同步；改为可选「从套件导入」复制单个技能。

## Consequences

- **收益**：用户内容与仓库代码分离，列表干净。
- **代价**：首次打开看不到仓库 examples；要把样例拷进 `projects/` 才出现。
