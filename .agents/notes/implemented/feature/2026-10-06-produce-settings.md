# 工作室设置页生产 Adapter

## Problem

生产页把出图/出视频写死为 ComfyUI，设置里只有聊天 API。创作者无法在窗口里配置本机 ComfyUI 或云端出图/视频，也无法选用当前要用的后端。

## Decision

设置增加第 4 项「生产」，与聊天 API 分开。项目外三份文件：`~/.mochang/comfyui.json`、`adapters.json`（合并写入、不整文件覆盖）、`produce-secrets.json`（密钥，不进 adapters 正文）、`produce.json`（`active_image` / `active_video`）。「启动」只标记当前轨道的 profile，不拉起 ComfyUI 进程。生产页按启动项提交；未启动则禁用「预览并生成」。云端第一版接 GPT Image 2 与 MiniMax H3。`studio_produce.run` 在启动子进程前把 secrets 补进环境变量（已有环境变量优先）。

## Alternatives considered

- **塞进设置 → API**：最强理由是少一个导航。不用：聊天模型和出图计费会混在一起。
- **窗口里启动 ComfyUI.exe**：最强理由是开箱即用。不用：安装路径未知，超出第一版探测范围。
- **任意自定义 command 编辑器**：最强理由是扩展快。不用：容易把密钥写进错误文件、绕过内置 adapter 契约。

## Consequences

设置页能保存并启动出图/出视频后端；生产弹窗显示真实 adapter。代价是多维护一份 produce 状态 API，MiniMax 仍需用户自填模型 ID 与分辨率集合。
