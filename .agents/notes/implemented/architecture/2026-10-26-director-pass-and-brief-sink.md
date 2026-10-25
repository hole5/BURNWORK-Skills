# Agent Note: 导演总控 pass 与阐述下沉

Status: implemented

## Problem

11 个 Skills 已覆盖短剧导演台主链，但用户说「按导演台做一集」仍靠碎片路由；`director-brief` 常停在 `项目开发/`，不强制下沉到分镜规则；生产失败多靠重生成，缺少编码化修复阶梯。

## Decision

在不拆散五文档所有权的前提下落地 P0：

1. **导演总控**：`short-drama/references/director-pass.md` + 路由入口；一次请求按固定清单：缺则补 develop 阐述 →（按需）write/assets → storyboard（Coverage 默认开关键场）→ image/video prompts → review 自检子集 → **仅确认后** produce。
2. **阐述下沉**：storyboard 开工前读取 `项目开发/director-brief.md`（若存在），把镜头语法/禁区/回报节奏写成分镜前置检查清单；无阐述时不阻断，但在末报列出「未锁定导演阐述」。
3. **QC 修复环 v0**：`short-drama-review/references/qc-repair-codes.md` 映射常见失败机制→最便宜修复；produce/review 引用，不替代现有审查表。

不做：删五文档、取消确认闸门、一次引入 20 套风格透镜（P1）。

## Alternatives considered

- **新建第 12 个独立 director skill** — 最强理由：边界清晰。不用：增加安装与路由成本；总控更适合挂在 core 路由上。
- **把导演书写成第六份创作真相** — 最强理由：与五文档并列。不用：破坏 creator-first 契约；阐述保持项目开发可选产物，用下沉检查连接分镜。

## Consequences

- **收益**：导演命令面可复现；阐述不再悬空；失败有便宜修复路径。
- **代价**：storyboard/review 多读一两份参考；总控清单加长，需避免自动触发未点名的生产。
