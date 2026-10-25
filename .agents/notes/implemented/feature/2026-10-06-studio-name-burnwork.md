# Agent Note: 产品名改为燃烧工作室

Status: implemented

> 英文 / 程序展示名后续改为 **BURNWORK Studio**，见 [2026-10-07-burnwork-studio-name](2026-10-07-burnwork-studio-name.md)。本篇关于中文名与默认目录的决定仍有效。

## Problem

界面与文档仍用旧中文产品名，创作者要求统一为「燃烧工作室」。

## Decision

产品中文名是 **燃烧工作室**。默认用户目录 `文档/燃烧工作室/`、技能描述与说明文档中的中文称呼用这个名字。已有 `~/.mochang/studio.json` 若仍指向旧目录则继续用旧路径，不自动搬家。

本篇取代 [本机工作室落地](../architecture/2026-10-05-mochang-local-studio.md) 里的中文产品名；那篇其余工作室架构决定仍有效。

## Alternatives considered

- **只改侧栏、文档保留旧名**：最强理由是少动默认目录。不用：创作者要求项目内该中文产品名全部替换。
- **连英文 Mochang Studio 一起改**：最强理由品牌完全统一。不用：当时只指定中文名；英文展示名已另文改为 BURNWORK Studio。

## Consequences

新安装默认工作室根变为 `文档/燃烧工作室`。旧安装若已写过 studio.json，路径不变，直到用户在设置里改目录。
