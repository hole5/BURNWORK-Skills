# 燃烧工作室

独立本机短剧工作室：Windows 上用 **WPF 桌面窗口** 浏览五份创作文档、看分镜覆盖与角色设定，并在右侧和 AI 对话；确认后把任务交给本机 ComfyUI。非 Windows 仍可用浏览器页。

产品说明见 [PRODUCT.md](../PRODUCT.md)；上游版权见 [NOTICE](../NOTICE) 与 [LICENSE](../LICENSE)。

## 启动

Windows 独立窗口：

```bat
scripts\open-studio.bat
```

源码在 [desktop/MochangStudio](../desktop/MochangStudio/)。手动编译：

```bash
dotnet build desktop/MochangStudio/MochangStudio.csproj -c Release
desktop\MochangStudio\bin\Release\net8.0-windows\BurnworkStudio.exe
```

便携发布（zip，可选 Windows `.pfx` 签名；**不是** Android Keystore）：

```powershell
.\scripts\publish-portable.ps1
```

说明见 [release.md](release.md)。

浏览器对照页：

```bash
py -3 skills/short-drama/scripts/studio_launch.py --restart --open
```

## ComfyUI

```bash
py -3 skills/short-drama-produce/scripts/comfyui_wizard.py \
  --base-url http://127.0.0.1:8188 \
  --image-workflow /path/to/image_api.json \
  --video-workflow /path/to/video_api.json
```

细节见 [comfyui.md](../skills/short-drama-produce/references/providers/comfyui.md)。

工作室「设置 → 生产」：保存本机 ComfyUI 或云端 GPT Image / MiniMax H3，点「启动」后，「生产」页才把「预览并生成」交给该后端。

仍可用命令行向导（会**合并**进 `adapters.json`，不覆盖其它 profile）：

## 界面地图

| 左侧 | 内容 |
|---|---|
| 概况 | 全剧进度、分镜覆盖、下一步 |
| 剧本 | 印张：KPI、开场钩/悬念、场次芯片、对白三列、台词簿 |
| 角色 | 花名册 + 设定印张（识别锚点与可选阅读卡） |
| 美术 | 地点/道具同一印张骨架 |
| 分镜 | 景别节奏条、16:9 镜卡、冻结关键帧/MOTION/声音 |
| 提示词 | 可复制 IMG / MOTION / 关键帧 |
| 生产 | 按设置里启动的出图/出视频后端确认投产 |
| 成片 | 剪辑单与成片 |
| 审查 | 审查意见 |

Agent 技能仍可读写同一项目。工作台右侧聊天在设置好 API 后走 `/api/agent/turn`：按 `SKILLS` 目录路由、写入本集 Markdown、流式显示技能与改动的文件。多步任务先出计划；生产只预览，确认仍在「生产」页。

## 目录分仓

默认工作室根：`文档/燃烧工作室/`

| 文件夹 | 内容 | 工作台对接 |
|---|---|---|
| `projects/` | 全部短剧项目 | 左侧项目目录、新建、删除、dashboard 扫描 |
| `skills/` | 已安装 Agent Skills | 设置 → SKILLS、导入 |
| `assets/` | 公共参考图 | 资产库 |

路径写在 `~/.mochang/studio.json`。套件代码仍在本仓库；Python 工具从仓库 `skills/short-drama` 运行。仓库 `examples/` 不会自动出现在项目列表。
