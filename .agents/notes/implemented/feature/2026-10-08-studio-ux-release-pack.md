# Agent Note: 工作室体验补齐与便携发布

Status: implemented

## Problem

主链已通，但阶段芯片不齐、首次配置靠翻设置、投产偏单条、成片/审查闭环弱、无可分发安装物。创作者要求一次落地：三件事优先项、产品体验建议、能力缺口功能；并提到 `D:\Download\APK_Keystore_Tool1.06.exe` 可生成签名。

## Decision

签名边界：已确认本机存在 `APK_Keystore_Tool1.06.exe`，但它是 **Android/Java Keystore**，不能签 `BurnworkStudio.exe`。便携发布脚本只接受可选 Windows `.pfx`；未提供时产物标明未签名。

已落地：

1. **阶段芯片**：分镜「写分镜」；成片「写剪辑单 / 打开成片目录」；概况「原著分析 / 系列开发」；审查「按意见改回」；生产「测连通 / 批量出图 / 批量出视频」。
2. **首次向导 + 三灯**：`studio.json` 增加 `FirstRunDone`；首次启动弹窗引导进设置→生产；生产设置与生产页可「一键测连通」（API Probe + 出图/出视频启动状态）。
3. **生产批量**：按本集图片/关键帧或视频条目排队；闸门逐条确认，会话/始终自动可静默连跑并回报成功/失败数。
4. **五文档进度 + REF**：概况页展示本集五文档齐套芯片；提示词页列出分镜绑定的参考图路径芯片（点击复制）。
5. **便携发布**：`scripts/publish-portable.ps1` + `docs/release.md`；默认 framework-dependent `win-x64`，输出 `dist/BurnworkStudio-portable/` 与 zip。

不做：迁数据库、默认永久自动出片、把 APK 工具接入 Windows 签名流水线。

## Alternatives considered

- **用 APK Keystore 签 exe** — 最强理由：已有工具。不用：证书体系不同，Windows 不认。
- **只做 MSI 安装器** — 最强理由：更像产品。不用：本轮先便携 zip，依赖少、迭代快。

## Consequences

- **收益**：创作者路径更完整，可发出便携包试用；首次配置有入口。
- **代价**：向导与批量生产增加 UI/状态；未签名包 SmartScreen 可能警告；批量在闸门下仍需逐条点确认。
