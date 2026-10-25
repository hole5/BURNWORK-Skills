# Agent Note: 桌面壳项目目录与设置页

Status: proposed（已被 [项目目录与设置页](../../implemented/architecture/2026-10-06-wpf-settings-projects.md) 采纳）

关联：[WPF 首页落地](../../implemented/architecture/2026-10-06-wpf-reelbench-home.md)

## Problem

左侧只有最近项目片段，不能浏览全部项目、右键打开/删除；也没有设置页去改工作区目录、聊天模型列表，或把新 skill 放进仓库 `skills/`。

## Proposal

左侧导航增加「项目」目录（列出 `/api/projects` 全部项：左键打开，右键打开/删除）和「设置」。设置内再分：1. 项目（工作区目录，写入 `~/.mochang/studio.json` 并重启本机服务）；2. API（多套 OpenAI 兼容模型供聊天选择）；3. SKILLS（把含 `SKILL.md` 的文件夹复制到仓库 `skills/`）。

## Alternatives considered

- **删除只从最近列表移除、不删磁盘** — 最强理由：更安全。不用：创作者要的是删项目；删除前确认，且只删含 `short-drama.json` 的目录。
- **Skill 装到用户主目录** — 最强理由：不碰仓库。不用：用户明确要求加到文件根目录的 skills。

## Consequences

- **收益**：项目管理与设置留在独立窗口内。
- **代价**：改工作区会重启 dashboard；删项目不可轻易撤销（尽量进回收站）。
