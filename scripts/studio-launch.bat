@echo off
setlocal
set "REPO=%~dp0.."
set "EXE=%REPO%\desktop\MochangStudio\bin\Release\net8.0-windows\BurnworkStudio.exe"
if exist "%EXE%" (
  if not "%~1"=="" (start "" "%EXE%" "%~1") else (start "" "%EXE%")
  endlocal
  exit /b 0
)
dotnet build "%REPO%\desktop\MochangStudio\MochangStudio.csproj" -c Release
if exist "%EXE%" (
  if not "%~1"=="" (start "" "%EXE%" "%~1") else (start "" "%EXE%")
  endlocal
  exit /b 0
)
if not "%~1"=="" (
  py -3 "%REPO%\skills\short-drama\scripts\studio_launch.py" --workspace "%~1" --restart --open
) else (
  py -3 "%REPO%\skills\short-drama\scripts\studio_launch.py" --restart --open
)
if errorlevel 1 (
  echo 启动失败。请安装 Python 3.9+ 与 .NET 8 SDK。
  pause
)
endlocal
