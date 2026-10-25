# Agent Note: 桌面壳项目目录与设置页

Status: implemented

关联：[提案原文](../../proposed/architecture/2026-10-06-wpf-settings-projects.md)、[WPF 首页落地](2026-10-06-wpf-reelbench-home.md)

## Problem

左侧不能浏览全部项目、右键打开/删除；也没有设置页去改工作区、聊天模型列表，或把新 skill 放进仓库 `skills/`。

## Decision

左侧导航「项目」页列出 `/api/projects` 全部项：左键打开，右键打开/删除（含 `short-drama.json` 的目录，优先进回收站，禁止删仓库根）。侧栏底部仍是「最近打开」（`~/.mochang/recent.json`），不混入全部项目。「设置」分三栏：1. 项目（目录分仓写入 `~/.mochang/studio.json` 并重启 dashboard）；2. API（`~/.mochang/chat.json` 多套 OpenAI 兼容 profile，聊天栏下拉选用）；3. SKILLS（复制含 `SKILL.md` 的文件夹到技能目录）。

## Alternatives considered

- **删除只从列表移除** — 最强理由：更安全。不用：创作者要删项目；改为确认后删磁盘。
- **Skill 装到用户主目录** — 最强理由：不碰仓库。不用：要求加到文件根目录的 skills。

## Consequences

- **收益**：项目管理与模型/技能安装留在独立窗口。
- **代价**：改工作区会重启本机服务；删项目需确认。
