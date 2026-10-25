# Agent Note: 工作室聊天调用技能库完成工作流

Status: implemented（已落地，见 ../../implemented/architecture/2026-10-06-studio-skill-agent.md）

## Problem

工作台右侧聊天只做一次 completions，读不到 `SKILLS` 目录里的 `SKILL.md`，也不能把五份创作 Markdown 写回项目。设置好 API 之后无法按套件路由完成写作工作流。

## Proposal

在 `dashboard_server` 增加 `/api/agent/turn`（NDJSON）。本机循环：技能索引路由 → 按需读 `SKILL.md` → 读写当前项目 Markdown → 事件回传 WPF。生产只 `prepare`，确认仍走工作台。二期：流式状态、中止、阶段芯片、集级上下文。三期：多步计划与「本会话连续写文档」。

## Alternatives considered

- **把 Cursor 嵌进窗口** — 最强理由：技能已经能在 IDE 里跑。不用：工作室需在只配 API 的机器上独立工作。
- **把全部 SKILL.md 塞进 system** — 最强理由：实现快。不用：串台且撑爆上下文。

## Consequences

- **收益**：配好模型即可按 owner 技能改项目文件，工作台能刷新。
- **代价**：多一个依赖 chat.json 的服务端循环；本地模型若不支持 tools 要走 JSON 降级。
