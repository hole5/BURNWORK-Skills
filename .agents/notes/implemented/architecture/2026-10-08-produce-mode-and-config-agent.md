# Agent Note: 生产模式、会话自动生产与小烧改本机配置

Status: implemented

关联：[提案原文](../../proposed/architecture/2026-10-08-produce-mode-and-config-agent.md)

## Problem

工作室已能聊天路由 skills 写五文档，但出图出视频必须在「生产」页确认；创作者希望可选「确认后交给小烧跑完」，并希望聊天协助配置 API / ComfyUI / 云端。若只靠提示词约束，模型可能临场越权；若默认全自动，易误花算力与密钥风险。

## Decision

分两阶段均已落地，**限制写在 `produce.json` 模式位 + agent/API 拦截**：

**阶段 A**：`produce.mode` = `gate`（默认）/ `session` / `always`。聊天 `prepare_produce` 前强制 `check_environment`（API、出图、出视频启动态）。仅 `session` 且本会话授权，或 `always`，才自动 confirm+run；否则 `need_confirm`。`confirm_produce`/`run_produce` 工具在闸门下仍拒绝。设置 → 生产展示三档、环境灯；计划栏增加「本会话允许小烧自动生产」。

**阶段 B**：`allow_config_agent`（默认关）。开启后 `propose_studio_config` 写脱敏预览 + token，`apply_studio_config` 确认后落盘 `~/.mochang`（chat/comfy/cloud）。未开开关直接拒绝。

「连续写文档」不等于自动生产。系统提示词按模式说明，真正能不能跑以服务端为准。

## Alternatives considered

- **只改系统提示词让模型别自动生产** — 最强理由：零工程。不用：不可靠。
- **默认始终自动** — 最强理由：最省事。不用：误触与费用风险高。
- **聊天永久可改配置且无确认** — 最强理由：上手最快。不用：密钥敏感，须预览确认。

## Consequences

- **收益**：默认仍安全；可会话/始终自动生产；可协助改本机配置。
- **代价**：设置与 agent 工具面更复杂；工作室 `skills` 目录若是拷贝，需用仓库脚本侧（BackendHost 跑仓库 Python）才能吃到本改动。
