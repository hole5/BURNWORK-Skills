# Agent Note: 程序展示名改为 BURNWORK Studio

Status: implemented

关联：[燃烧工作室中文名](2026-10-06-studio-name-burnwork.md)、[官网主页](2026-10-07-studio-marketing-site.md)

## Problem

窗口标题、ApplicationTitle 与英文展示名仍是「燃烧工作室 / Mochang Studio」，与品牌 BURNWORK 不一致；创作者要求生成的程序名为 **BURNWORK Studio**。官网若干链接指向仓库 Markdown/LICENSE 原文，风格与落地页断裂。

## Decision

可见程序名（窗口 Title、标题栏、对话框、ApplicationTitle、Product/AssemblyTitle、Dashboard 产品名、PRODUCT / NOTICE 英文展示名）统一为 **BURNWORK Studio**。中文目录名 `文档/燃烧工作室` 与代码命名空间暂不动。官网 `website/` 增加技能库、工作室说明、产品边界、LICENSE、NOTICE 同风格简介页，首页改链到这些页。

> **更正**：创作者随后明确「程序名」指 exe。输出文件名改为 `BurnworkStudio.exe`，见 [2026-10-07-burnwork-studio-exe](2026-10-07-burnwork-studio-exe.md)。

## Alternatives considered

- **连 exe / 程序集也改成 BurnworkStudio** — 当时未采纳；已由后篇落地。
- **中文 UI 全部改成英文 BURNWORK Studio** — 最强理由：单一字符串。不用：创作者只点名程序名；官网导航仍保留中文「燃烧工作室」字标。

## Consequences

- **收益**：任务栏与窗口标题与品牌一致；官网内链风格统一。
- **代价**：旧笔记曾写 Mochang Studio；exe 文件名见后篇。