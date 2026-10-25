# 燃烧工作室

本机短剧工作室的产品名是 **燃烧工作室**（程序文件 **BurnworkStudio.exe**；英文品牌 **BURNWORK**）。

## 定位

- 创作者每天打开的是本机工作室，不是对话窗口。
- 创作真相仍是每集五份 Markdown（剧本、视觉设定、分镜、图片提示词、视频提示词）。
- 出图 / 出视频在工作室内预览并确认后执行；ComfyUI 地址与工作流写在项目外配置。
- Agent 技能（drama-skills 套件）仍可读写同一项目，作为可选写手。

## 品牌边界

| 可改 | 必须保留 |
|---|---|
| 窗口标题、图标、启动器文案 | [LICENSE](LICENSE) 全文 |
| 新增工作室 UI / ComfyUI adapter 的版权声明 | [NOTICE](NOTICE) 中的 upstream 段落 |
| 发行包产品名与安装目录名 | 不得声称「从未开源」或删除 MIT 版权行 |

Windows **独立窗口**（推荐）：

```bat
scripts\open-studio.bat
```

需要 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)。首次会编译 `desktop/MochangStudio`。窗口先显示 Reelbench 式首页：左侧最近项目，中间输入故事创建/打开项目，再进工作台。聊天接口写在 `~/.mochang/chat.json`（OpenAI 兼容，默认可对本地 Ollama）。

仍可用浏览器打开同一套后端（对照或非 Windows）：

```text
py -3 skills/short-drama/scripts/studio_launch.py --open
```

ComfyUI 配置向导：

```text
py -3 skills/short-drama-produce/scripts/comfyui_wizard.py
```
