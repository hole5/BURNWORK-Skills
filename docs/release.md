# 发布：燃烧工作室 (Windows)

## 安装包（推荐分发）

Inno Setup 生成 `BurnworkStudio-Setup-*.exe`（self-contained，目标机无需预装 .NET 8）。

```powershell
# 首次会尝试 winget 安装 Inno Setup 6
.\scripts\build-installer.ps1

# 可选：对主程序与 Setup 做 Authenticode（Windows .pfx，不是 Android Keystore）
.\scripts\build-installer.ps1 -PfxPath C:\certs\burnwork.pfx -PfxPassword '***'
```

设计要点（见 `installer/BurnworkStudio.iss`）：

| 项 | 行为 |
|---|---|
| 默认目录 | `%LocalAppData%\Programs\BurnworkStudio`（免管理员） |
| 可选 | 安装向导可改「为所有用户」→ Program Files |
| 快捷方式 | 开始菜单；可选桌面图标 |
| 卸载 | 「应用和功能」+ 开始菜单卸载项 |
| 语言 | 简体中文 / English |

产物：`dist\BurnworkStudio-Setup-0.1.0.exe`

## 便携 zip

```powershell
.\scripts\publish-portable.ps1
.\scripts\publish-portable.ps1 -PfxPath C:\certs\burnwork.pfx -PfxPassword '***'
```

默认输出 `dist\BurnworkStudio-portable\` + zip（framework-dependent）。

## 关于签名

`APK_Keystore_Tool` 生成的是 **Android/Java Keystore**，**不能**给 Windows EXE 做 Authenticode。

- 未提供 `.pfx` 时产物未签名；SmartScreen 可能警告。
- 需要 Windows 代码签名证书后再用 `-PfxPath`。
