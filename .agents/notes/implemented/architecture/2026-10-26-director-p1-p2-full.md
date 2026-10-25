# Agent Note: 导演台 P1/P2 全量落地

Status: implemented

## Problem

P0（导演总控、阐述下沉、QC 码）已落地；P1（声音计划、风格透镜、镜头难度/重试预算）与 P2（shot-IR、灯光圣经、工作室导演页）尚未执行。创作者确认执行全部。

## Decision

在不拆五文档所有权、不取消确认闸门的前提下一次落地：

### P1
1. **Sound Plan**：`short-drama-video-prompts/references/sound-plan.md` + 可选 `剧集/<EP>/声音计划.md` 写法；导演 pass 清单加入该步（提示词之后、生产之前，可跳过）。
2. **风格透镜**：`short-drama-storyboard/references/style-lenses/` 提供 6 套可覆盖默认（写实紧张、喜剧轻切、浪漫柔光、动作清晰、悬疑压迫、漫剧平面）；分镜前置可选加载一套。
3. **镜头难度 + 三振预算**：分镜每镜增加「难度」字段约定；`produce` / QC 码共用 `shot-difficulty.md` 与三振账本格式。

### P2
4. **shot-IR（可选）**：`short-drama-storyboard/assets/shot-ir.schema.json` + 从分镜导出说明；不替代 Markdown 真相。
5. **灯光圣经**：扩展/新增 `assets` 或 storyboard 的 `lighting-bible.md` 跨镜光锁约定。
6. **工作室导演页**：概况增加导演台面板（阐述/Coverage/难度分布/未决 QC）。

## Alternatives considered

- **只做文档不改工作室** — 最强理由：改动小。不用：创作者要导演台可感知。
- **强制每集写声音计划与 IR** — 最强理由：工艺完整。不用：破坏「按需五文档」；二者保持可选。

## Consequences

- **收益**：电影级缺口清单闭环；导演 pass 更完整。
- **代价**：参考文件与芯片增多；agent 上下文需按路由按需加载。
