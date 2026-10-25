# Agent Note: 燃烧工作室——drama 内核的独立本机应用

Status: implemented

中文产品名现以 [燃烧工作室更名](../../feature/2026-10-06-studio-name-burnwork.md) 为准。

关联：[本机 ComfyUI 提案](../../proposed/architecture/2026-10-05-local-comfyui-studio.md)（采纳并落地）、[创作台 v2](../../implemented/feature/2026-09-27-dashboard-v2-stage-views.md)（解析同源保留；「Dashboard 永不提供生产按钮」由本篇推翻为「带预览的确认生产」）、[bench 壳](../../implemented/feature/2026-10-05-dashboard-bench-shell.md)。

## Problem

创作者需要 Reelbench 式独立本机应用：左侧阶段、shuohao 式角色/美术阅读面、本机可选 ComfyUI 出图出视频；同时保留 drama-skills 更完整的工作流与五份 Markdown 真相，并合法做成自有品牌发行。

## Decision

产品名定为 **燃烧工作室**（Mochang Studio）。发行物带自有品牌，同时保留 `LICENSE` 与 `NOTICE` 中的 drama-skills MIT 声明；新增工作室代码可另标发行方版权。「自有」指品牌、发行与新增代码，不是删除上游 NOTICE。

创作真相仍是五份 Markdown。工作室壳扩展现有 dashboard：常驻左侧阶段（概况、剧本、角色、美术、分镜、提示词、生产、成片、审查）、分集轨、底栏。角色/美术页用印张三栏（花名册、图廊、元数据、可复制 `IMG-*`）；「生成设定图」打开全局确认抽屉，不直连 ComfyUI。生产页与设定集共用同一套 prepare → 确认 → run。新增 `comfyui-image` / `comfyui-video` adapter，配置在项目外（默认 `~/.mochang/adapters.json`）。启动器 `studio_launch.py`、`scripts/open-studio.bat` 与 Windows WPF 壳 `desktop/MochangStudio` 打开本机环回服务；聊天配置在 `~/.mochang/chat.json`。

## Alternatives considered

- **把真相迁到 cast.json** — 最强理由：与 shuohao report 一一对应。不用：破坏 0.6 creator-first 契约，制造第二真相。
- **浏览器未确认直连 ComfyUI** — 最强理由：点一下就出图。不用：绕开确认闸门与项目外配置边界。
- **删除 MIT 版权伪装从未开源** — 最强理由：品牌更「干净」。不用：违反 MIT；合法路径是保留 NOTICE + 自有品牌。

## Consequences

- **收益**：同一套项目文件可在独立工作室里浏览、编辑原文、经确认后用本机 ComfyUI 投产。
- **代价**：要维护 ComfyUI 工作流占位约定与工作室生产 API；每台机器的 workflow 仍需创作者提供。
- **已知上限**：第一期不做节点编辑器、不做云账号、不做字段级双向写回（只保存整份 Markdown）。
