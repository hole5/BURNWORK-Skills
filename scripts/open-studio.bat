@echo off
REM 优先启动 WPF 独立窗口；没有编译产物时回退到浏览器。
setlocal
cd /d "%~dp0.."
set "EXE=%cd%\desktop\MochangStudio\bin\Release\net8.0-windows\BurnworkStudio.exe"
if exist "%EXE%" (
  start "" "%EXE%"
  endlocal
  exit /b 0
)
echo 未找到 WPF 程序，正在编译...
dotnet build "%cd%\desktop\MochangStudio\MochangStudio.csproj" -c Release
if exist "%EXE%" (
  start "" "%EXE%"
  endlocal
  exit /b 0
)
echo 编译失败，改用浏览器打开。
py -3 "skills\short-drama\scripts\studio_launch.py" --restart --open
if errorlevel 1 pause
endlocal
