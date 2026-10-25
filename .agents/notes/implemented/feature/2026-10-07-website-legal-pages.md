# Agent Note: 官网服务条款 / 隐私 / 退款页

Status: implemented

关联：[官网主页](2026-10-07-studio-marketing-site.md)

## Problem

官网参照 [Reelbench](https://reelbench.79px.com/) 已有能力与工作流结构，但页脚缺少服务条款、隐私政策、退款政策；外链到裸仓库文件也不符合落地页体验。

## Decision

在 `website/` 增加与首页同风格的 `terms.html`、`privacy.html`、`refund.html`，文案按本机优先、MIT 上游、用户自接 ComfyUI/API 费用撰写。首页增加「获取方式」「更新与支持」导航与同意条款行；各页页脚统一挂三项政策。不照搬 Reelbench 的 $199/$399 定价文案。

## Alternatives considered

- **只链到 LICENSE** — 最强理由：已有开源许可。不用：创作者明确要三项政策页，且隐私/退款不是 MIT 能覆盖的。
- **全文复制 Reelbench 条款** — 最强理由：结构成熟。不用：商业模型与数据流不同，会误导。

## Consequences

- **收益**：官网法务入口与参照站对齐。
- **代价**：条款为产品说明级文本，正式对外销售前仍需人工法务复核。
