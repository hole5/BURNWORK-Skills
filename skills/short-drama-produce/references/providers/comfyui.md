# ComfyUI adapter (燃烧工作室)

本机 ComfyUI 通过项目外配置接入 `$short-drama-produce`，不把地址写进短剧项目。

## 配置

```bash
py -3 skills/short-drama-produce/scripts/comfyui_wizard.py \
  --base-url http://127.0.0.1:8188 \
  --image-workflow /path/to/image_api.json \
  --video-workflow /path/to/video_api.json
```

写入：

- `~/.mochang/comfyui.json` — 地址与工作流路径
- `~/.mochang/adapters.json` — 合并 `comfyui-image` / `comfyui-video` 命令
- 窗口设置 → 生产也可写同一套文件，并用 `produce.json` 标记当前启动的出图/出视频 profile


生产时：

```bash
py -3 skills/short-drama-produce/scripts/production_tool.py run <project> \
  --job-id <id> --adapter-config ~/.mochang/adapters.json
```

## 工作流占位

在 ComfyUI「另存为 API 格式」的 JSON 中放入字面量：

| 占位符 | 含义 |
|---|---|
| `__PROMPT__` | job 正文字符串 |
| `__NEGATIVE__` | `parameters.negative` |
| `__SEED__` | 整数种子 |
| `__WIDTH__` / `__HEIGHT__` | 尺寸 |
| `__DURATION__` | 视频秒数 |

## 自检

```bash
py -3 skills/short-drama-produce/scripts/comfyui_adapter.py image --selftest
```
