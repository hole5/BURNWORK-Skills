# Agent Note: 输出 exe 改为 BurnworkStudio.exe

Status: implemented

关联：[展示名 BURNWORK Studio](2026-10-07-burnwork-studio-name.md)（该篇曾误判「不要改 exe」，以本篇为准）

## Problem

创作者说的「程序名」是生成的 **BurnworkStudio.exe**，不是仅窗口标题。上一篇只改了展示名，输出仍是 `MochangStudio.exe`。

## Decision

`MochangStudio.csproj` 的 `AssemblyName` 为 **BurnworkStudio**，Release 产物为 `BurnworkStudio.exe`。`scripts/open-studio.bat`、`studio-launch.bat`、`mochang-desktop.pyw` 与 `docs/studio.md` 启动路径同步。项目文件夹与 C# 命名空间仍为 `MochangStudio`。

## Alternatives considered

- **连目录 / 命名空间整改 BurnworkStudio** — 最强理由：全程同名。不用：牵动面大，本次只要 exe 文件名。
- **仅复制/重命名产物不改 AssemblyName** — 最强理由：少动 csproj。不用：每次 build 仍写出旧名，易再次漂移。

## Consequences

- **收益**：生成与启动入口文件名与品牌一致。
- **代价**：旧快捷方式若指向 `MochangStudio.exe` 需改一次；obj/bin 里旧 exe 可删。
