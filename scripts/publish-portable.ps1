#Requires -Version 5.1
param(
  [string]$Configuration = "Release",
  [string]$OutDir = "",
  [string]$Runtime = "win-x64",
  [switch]$SelfContained,
  [string]$PfxPath = "",
  [string]$PfxPassword = ""
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $RepoRoot "desktop\MochangStudio\MochangStudio.csproj"
if (-not (Test-Path $Project)) { throw "Project not found: $Project" }

if (-not $OutDir) {
  $OutDir = Join-Path $RepoRoot "dist\BurnworkStudio-portable"
}

$publishDir = Join-Path $OutDir "app"
if (Test-Path $OutDir) { Remove-Item $OutDir -Recurse -Force }
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

$sc = if ($SelfContained) { "true" } else { "false" }
Write-Host "dotnet publish ($Configuration, $Runtime, self-contained=$sc) ..."
dotnet publish $Project `
  -c $Configuration `
  -r $Runtime `
  --self-contained $sc `
  -o $publishDir `
  /p:PublishSingleFile=false `
  /p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed: $LASTEXITCODE" }

$exe = Join-Path $publishDir "BurnworkStudio.exe"
if (-not (Test-Path $exe)) { throw "BurnworkStudio.exe missing after publish" }

$signed = $false
if ($PfxPath) {
  if (-not (Test-Path $PfxPath)) { throw "PFX not found: $PfxPath" }
  $signtoolCmd = Get-Command signtool.exe -ErrorAction SilentlyContinue
  $tool = $null
  if ($signtoolCmd) {
    $tool = $signtoolCmd.Source
  }
  else {
    $kits = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
      Sort-Object FullName -Descending |
      Select-Object -First 1
    if ($kits) { $tool = $kits.FullName }
  }
  if (-not $tool) { throw "signtool.exe not found (install Windows SDK)" }
  Write-Host "Signing with Authenticode (Windows PFX)..."
  & $tool sign /fd SHA256 /f $PfxPath /p $PfxPassword /tr http://timestamp.digicert.com /td SHA256 $exe
  if ($LASTEXITCODE -ne 0) { throw "signtool failed: $LASTEXITCODE" }
  $signed = $true
}

$stamp = Get-Date -Format "yyyyMMdd-HHmm"
if ($signed) {
  $signNote = "Signed with Windows Authenticode (.pfx)."
}
else {
  $signNote = "UNSIGNED. Android Keystore (APK_Keystore_Tool) cannot sign this EXE. SmartScreen may warn."
}

$readme = @"
Burnwork Studio portable package
================================

Start: double-click app\BurnworkStudio.exe

Studio data default: Documents\BurnworkStudio (or Documents\燃烧工作室)
Local config: %USERPROFILE%\.mochang\

Sign status: $signNote

Built: $stamp

See docs\release.md in the source repo for Chinese notes.
"@
[System.IO.File]::WriteAllText((Join-Path $OutDir "README-PORTABLE.txt"), $readme, [System.Text.UTF8Encoding]::new($false))

$zip = Join-Path (Split-Path $OutDir) ("BurnworkStudio-portable-" + $stamp + ".zip")
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $OutDir "*") -DestinationPath $zip -Force

Write-Host ""
Write-Host "Done:"
Write-Host "  Dir:  $OutDir"
Write-Host "  Zip:  $zip"
if ($signed) { Write-Host "  Sign: yes" } else { Write-Host "  Sign: no (need Windows .pfx, not APK Keystore)" }
