# BURNWORK 程序图标与首页字标

## Problem

工作室窗口没有自有图标，首页口号中间仍写「墨场」，和创作者提供的 BURNWORK 三张标识不一致。

## Decision

三张标识收入 `desktop/MochangStudio/Assets/`（火焰字标、火焰图形标、霓虹标），打成含三帧 PNG 的 `app.ico` 作为 exe / 窗口图标。界面小标用图形标 `brand-mark.png`。首页主口号中间的「墨场」改为 **BURNWORK**。产品目录名「燃烧工作室」未改。

## Alternatives considered

- **只换 exe 图标、首页不改字**：最强理由是少动文案。不用：创作者明确要求首页中间改成 BURNWORK。
- **整产品改名为 BURNWORK**：最强理由品牌统一。不用：本次只指定图标和首页中间那一处字。

## Consequences

任务栏与标题栏显示火焰标。ICO 依赖 Vista 起的 PNG 帧；极旧资源管理器可能只认第一帧。
