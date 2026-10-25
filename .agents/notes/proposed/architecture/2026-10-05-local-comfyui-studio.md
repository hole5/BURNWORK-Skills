# Agent Note: 本机工作室对接自选 ComfyUI

Status: proposed（已被 [燃烧工作室落地](../../implemented/architecture/2026-10-05-mochang-local-studio.md) 采纳；本篇保留提案原文，事实以落地笔记为准）

关联：[燃烧工作室落地](../../implemented/architecture/2026-10-05-mochang-local-studio.md)、[创作台按阶段分视图](../../implemented/feature/2026-09-27-dashboard-v2-stage-views.md)

## Problem

创作者要的是 Reelbench 一类的**独立本地应用**：在本机选定 ComfyUI 地址，对当前项目的图片提示词和视频提示词直接排队出图、出视频。现有 Drama Skills 只有 Agent 技能加本机 Dashboard。Dashboard 读 Markdown、复制请求回对话；生产走项目外 adapter（GPT Image、MiniMax、Seedance 等），仓库里没有 ComfyUI。两者不是同一类产品。

## Proposal

在本仓库增加一条可选的「本地工作室」产品线，不替换五份 Markdown 真相，也不取消确认闸门。

1. **独立进程，不嵌进对话。** 本机启动一个工作室窗口（继续用现有 Python 标准库 HTTP 服务加原生页面，或后续再包成桌面入口）。它打开的是创作者自己的短剧项目根，不是云端账号。
2. **ComfyUI 是项目外的 adapter。** 地址、工作流模板、超时写在项目外的 adapter 配置里（例如用户主目录），凭据与 API 不进 `short-drama.json`。工作室只选择已配置的 profile 名称。
3. **出图出视频仍走 `production_tool.py`。** 界面列出可投产的 `IMG-*` / `SHOT-*` 冻结帧 / `MOTION-*`，先 `prepare` 展示数量、提示词、参考图、输出路径和 adapter，创作者在**本窗口**点「确认并生成」才 `confirm` + `run`。这算一次明确确认，不等于现在禁止的「页面上的生产按钮偷偷绕过预览」。
4. **内置 `comfyui-image` / `comfyui-video` adapter。** 对用户填写的 ComfyUI `base_url` 提交 workflow、轮询任务、把输出文件拷回 `剧集/<EP>/制作成果/`。工作流 JSON 由创作者提供，adapter 只替换约定占位（正文字段、参考图节点、尺寸、时长），不替每台机器猜节点编号。
5. **技能对话路径保留。** 没有工作室、或不连 ComfyUI 时，行为与现在相同：对话里预览、确认、再跑云端或其它 adapter。

不把 ComfyUI 地址写进项目文件，不在未确认时提交 `/prompt`，不把工作流失败当成质量合格。

## Alternatives considered

- **直接去用 Reelbench** — 最强理由：已经是 Codex + 自部署 ComfyUI 的独立本地工作台，不必再造。不用作本仓库的默认答案：创作者当前打开的是 drama-skills，创作真相是五份 Markdown，不是 shuohao 的 JSON 管线；换产品等于换一整套文件。
- **只改 Dashboard 外观，生成仍回对话** — 最强理由：不碰生产硬闸门，改动小。被否（作为本需求）：创作者要的是在本机选 ComfyUI 并直接出片，不是再复制一句给助手。
- **浏览器直连 ComfyUI、跳过 `prepare/confirm/run`** — 最强理由：最接近「点一下就出图」。不用：会绕开确认、输出核对和项目外配置；也把 ComfyUI 跨域和凭据问题交给不可信的页面。
- **把 ComfyUI 工作流写进每集 Markdown** — 最强理由：项目可带走。不用：工作流绑定某台机器的节点图，会把执行环境泄漏进创作真相。

## Consequences

- **收益**：同一套五文档可以在本机对着自选 ComfyUI 投产；确认仍看得见本次提示词和输出路径。
- **代价**：要维护工作流占位约定、任务轮询、失败重试和本机防火墙/跨源；每台 ComfyUI 的工作流几乎都不同，通用程度低于云厂商 HTTP API。
- **已知上限**：第一期只做「一个 image 工作流 + 一个 video 工作流」的占位替换，不做节点可视化编辑器，也不做云端账号体系。
