# Agent Note: Windows 安装包

Status: implemented

## Problem

已有便携 zip，但创作者需要可双击安装的 Windows 安装包（开始菜单、卸载、固定安装目录）。

## Decision

用 **Inno Setup 6** 生成 `BurnworkStudio-Setup-*.exe`：

- `scripts/build-installer.ps1`：先 `dotnet publish` **self-contained win-x64**，再调用 `ISCC`
- `installer/BurnworkStudio.iss`：默认 `%LocalAppData%\Programs\BurnworkStudio`（免管理员）；可改「为所有用户」；开始菜单 + 可选桌面图标；卸载项；简体中文/英文
- 简体语言文件落在 `installer/Languages/ChineseSimplified.isl`（官方包不含该 isl）
- 不接入 Android Keystore；可选 `-PfxPath` 签主程序与 Setup

已生成：`dist/BurnworkStudio-Setup-0.1.0.exe`（约 50MB）。

## Alternatives considered

- **WiX MSI** — 最强理由：企业部署常见。不用：链路更重；Inno 单文件 Setup 对创作者更友好。
- **仅便携 zip** — 最强理由：已交付。不用：用户明确要安装包。
- **MSIX** — 最强理由：现代商店形态。不用：签名与侧载门槛高。

## Consequences

- **收益**：可分发安装程序，卸载路径清晰；目标机无需预装 .NET。
- **代价**：self-contained 体积较大；构建依赖本机 Inno Setup（脚本可 winget 安装）。
