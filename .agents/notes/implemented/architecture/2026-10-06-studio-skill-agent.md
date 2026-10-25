# Agent Note: 工作室聊天调用技能库完成工作流

Status: implemented

关联：[提案](../../proposed/architecture/2026-10-06-studio-skill-agent.md)

## Problem

工作台右侧聊天只做一次 completions，读不到技能库，也不能把五份创作 Markdown 写回项目。

## Decision

`dashboard_server` 提供 `/api/agent/turn`（NDJSON）与 `/api/agent/skills`。`studio_agent.py` 读取 `SKILLS` 目录 frontmatter 做路由，工具限于 list/read skill、列出本集文档、读写项目内 md/txt、propose_plan、prepare_produce。禁止 confirm/run 与任意 shell。WPF 流式显示技能与写入路径，可停止；阶段芯片带 `[skill:…]`；多步先出计划，「执行下一步」或勾选「本会话连续写文档」。生产确认仍在工作台。

## Alternatives considered

- **把 Cursor 嵌进窗口** — 最强理由：技能已能在 IDE 跑。不用：工作室需在只配 API 的机器上独立工作。
- **全部 SKILL.md 塞进 system** — 最强理由：实现快。不用：串台且撑爆上下文。

## Consequences

- **收益**：配好模型后可按 owner 技能改本集文档，工作台刷新；多步有停点。
- **代价**：循环依赖 `~/.mochang/chat.json`；不支持 tools 的模型需 JSON 降级；写文件走项目路径而非 dashboard 带版本号的 PUT。
