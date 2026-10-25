#Requires -Version 5.1
<#
.SYNOPSIS
  Build Windows installer (Inno Setup) for Burnwork Studio.
#>
param(
  [string]$Configuration = "Release",
  [string]$Runtime = "win-x64",
  [string]$Version = "",
  [string]$PfxPath = "",
  [string]$PfxPassword = "",
  [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $RepoRoot "desktop\MochangStudio\MochangStudio.csproj"
$Iss = Join-Path $RepoRoot "installer\BurnworkStudio.iss"
$SrcDir = Join-Path $RepoRoot "dist\BurnworkStudio-installer-src"
$OutDir = Join-Path $RepoRoot "dist"

if (-not (Test-Path $Project)) { throw "Project not found: $Project" }
if (-not (Test-Path $Iss)) { throw "ISS not found: $Iss" }

if (-not $Version) {
  $csproj = Get-Content $Project -Raw
  if ($csproj -match '<Version>([^<]+)</Version>') { $Version = $Matches[1].Trim() }
  else { $Version = "0.1.0" }
}

function Find-ISCC {
  $cmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
  if ($cmd) { return $cmd.Source }
  foreach ($p in @(
      "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
      "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
      "${env:LocalAppData}\Programs\Inno Setup 6\ISCC.exe"
    )) {
    if (Test-Path $p) { return $p }
  }
  return $null
}

$iscc = Find-ISCC
if (-not $iscc) {
  Write-Host "Inno Setup 6 not found. Installing via winget..."
  winget install --id JRSoftware.InnoSetup -e --accept-package-agreements --accept-source-agreements
  $iscc = Find-ISCC
  if (-not $iscc) {
    throw "ISCC.exe still missing. Install Inno Setup 6 from https://jrsoftware.org/isdl.php then re-run."
  }
}

if (-not $SkipPublish) {
  if (Test-Path $SrcDir) { Remove-Item $SrcDir -Recurse -Force }
  New-Item -ItemType Directory -Path $SrcDir -Force | Out-Null
  Write-Host "Publishing self-contained $Runtime ..."
  dotnet publish $Project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $SrcDir `
    /p:PublishSingleFile=false `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:Version=$Version
  if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed: $LASTEXITCODE" }
}

$exe = Join-Path $SrcDir "BurnworkStudio.exe"
if (-not (Test-Path $exe)) { throw "Missing BurnworkStudio.exe in $SrcDir" }

if ($PfxPath) {
  if (-not (Test-Path $PfxPath)) { throw "PFX not found: $PfxPath" }
  $signtoolCmd = Get-Command signtool.exe -ErrorAction SilentlyContinue
  $tool = $null
  if ($signtoolCmd) { $tool = $signtoolCmd.Source }
  else {
    $kits = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
      Sort-Object FullName -Descending | Select-Object -First 1
    if ($kits) { $tool = $kits.FullName }
  }
  if (-not $tool) { throw "signtool.exe not found" }
  Write-Host "Signing app EXE..."
  & $tool sign /fd SHA256 /f $PfxPath /p $PfxPassword /tr http://timestamp.digicert.com /td SHA256 $exe
  if ($LASTEXITCODE -ne 0) { throw "signtool failed on app: $LASTEXITCODE" }
}

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
Write-Host "Compiling installer with $iscc ..."
& $iscc `
  "/DMyAppVersion=$Version" `
  "/DSourceDir=$SrcDir" `
  "/DOutputDir=$OutDir" `
  $Iss
if ($LASTEXITCODE -ne 0) { throw "ISCC failed: $LASTEXITCODE" }

$setup = Join-Path $OutDir "BurnworkStudio-Setup-$Version.exe"
if (-not (Test-Path $setup)) { throw "Setup EXE missing: $setup" }

if ($PfxPath) {
  Write-Host "Signing Setup EXE..."
  $signtoolCmd = Get-Command signtool.exe -ErrorAction SilentlyContinue
  $tool = if ($signtoolCmd) { $signtoolCmd.Source } else {
    (Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
      Sort-Object FullName -Descending | Select-Object -First 1).FullName
  }
  & $tool sign /fd SHA256 /f $PfxPath /p $PfxPassword /tr http://timestamp.digicert.com /td SHA256 $setup
  if ($LASTEXITCODE -ne 0) { throw "signtool failed on setup: $LASTEXITCODE" }
}

$readme = @"
Burnwork Studio Windows installer
=================================
File: BurnworkStudio-Setup-$Version.exe
Install: double-click Setup (default: %LocalAppData%\Programs\BurnworkStudio)
Uninstall: Settings > Apps, or Start Menu uninstall shortcut
Note: Android Keystore cannot sign this package. Optional Windows .pfx via -PfxPath.
Studio data: Documents\燃烧工作室 ; config: %USERPROFILE%\.mochang\
"@
[System.IO.File]::WriteAllText((Join-Path $OutDir "INSTALLER-README.txt"), $readme, [System.Text.UTF8Encoding]::new($false))

Write-Host ""
Write-Host "Done: $setup"
Write-Host ("Size: {0:N1} MB" -f ((Get-Item $setup).Length / 1MB))
