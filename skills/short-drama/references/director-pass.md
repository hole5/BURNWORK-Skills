# 导演总控 pass

用户说「按导演台做一集 / 导演 pass / 电影级过一遍本集」时，由 `$short-drama` 按本清单编排，
**不**自动开始用户没点名的付费生产。各阶段正文仍由对应 owner 写入五文档。

## 何时启用

命中任一即可：

- 「导演台」「导演 pass」「按导演工作流」「电影级过一遍」+ 集号或「本集」；
- 「从阐述做到分镜/提示词」（未点名 produce）。

只点名单阶段（如「只写分镜」）时，不要强行跑完整 pass。

## 固定清单（顺序）

对目标集 `EP###`（默认当前集或用户点名集）：

| 步 | 条件 | Owner | 产出 / 动作 |
|---|---|---|---|
| 0 | 项目未 init | `$short-drama` | `project_tool init`；不预建空五文档 |
| 1 | 无 `项目开发/creative-brief.md` 或无故事引擎且用户要系列承诺 | `$short-drama-develop` | 简报 / 引擎；需要时 `director-brief.md` |
| 2 | 无本集 `剧本.md` 或用户要求改本 | `$short-drama-write` | `剧集/EP###/剧本.md` |
| 3 | 无本集 `视觉设定.md` 或关键帧所需身份未锁 | `$short-drama-assets` | `视觉设定.md` |
| 4 | 要镜头 | `$short-drama-storyboard` | `分镜.md`；每场 Coverage；阐述下沉；可选风格透镜；难度 S/M/L/XL |
| 5 | 要图侧提示词且资产需设定图 | `$short-drama-image-prompts` | `图片提示词.md`（可与 4 并行，结束后对账） |
| 6 | 要运动提示词 | `$short-drama-video-prompts` | `视频提示词.md` |
| 6b | 用户点名声音计划或对白/底声冲突 | `$short-drama-video-prompts` | 可选 `剧集/EP###/声音计划.md` |
| 7 | 用户点名审或 pass 含「自检」 | `$short-drama-review` | 子集审查 + QC 码；可对照灯光圣经 |
| 7b | 用户点名导出 shot-IR | `$short-drama-storyboard` | 可选 `制作成果/shot-ir.json`（不替代分镜） |
| 8 | **仅**用户明确确认生产 | `$short-drama-produce` | preview → confirm → run；按难度重试预算 |

批次只控制上下文；同一步内可续跑。跨 4/5/6 时结束前做一次视觉依赖对账（与主 SKILL 第 6 条相同）。

## 硬禁止

- 不把 pass 默认等同于自动出图/出视频。
- 不为凑流程伪造上游文档。
- 不跳过已存在且用户未要求重做的阶段（报告「已有，跳过」即可）。

## 结束回报（一次）

1. 本 pass 实际跑过 / 跳过的步骤；
2. 关键决定（阐述是否锁定、风格透镜、Coverage、难度分布 S/M/L/XL、未绑 REF）；
3. 真实未决项；
4. 可选下一步（声音计划 / shot-IR / 是否进入生产页确认）。

## 与燃烧工作室

桌面端概况「导演 pass」芯片与导演台面板不替代本清单。用户在聊天说「导演 pass EP001」时，
agent 按本文件编排；工作室负责展示进度与生产闸门。
