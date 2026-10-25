#!/usr/bin/env python3
"""Write ~/.mochang ComfyUI adapter config for 燃烧工作室."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="燃烧工作室 ComfyUI 配置向导")
    parser.add_argument(
        "--base-url",
        default="http://127.0.0.1:8188",
        help="ComfyUI 地址（默认本机 8188）",
    )
    parser.add_argument("--image-workflow", type=Path, help="文生图 / 图生图 API 工作流 JSON")
    parser.add_argument("--video-workflow", type=Path, help="视频 API 工作流 JSON")
    parser.add_argument(
        "--home",
        type=Path,
        default=Path.home() / ".mochang",
        help="配置目录（默认 ~/.mochang）",
    )
    parser.add_argument(
        "--adapter-script",
        type=Path,
        default=Path(__file__).resolve().with_name("comfyui_adapter.py"),
        help="comfyui_adapter.py 路径",
    )
    args = parser.parse_args(argv)

    home = args.home.expanduser().resolve()
    home.mkdir(parents=True, exist_ok=True)
    adapter_script = args.adapter_script.expanduser().resolve()
    if not adapter_script.is_file():
        print(f"找不到 adapter 脚本: {adapter_script}", file=sys.stderr)
        return 2

    comfy = {
        "base_url": args.base_url.rstrip("/"),
        "image_workflow": str(args.image_workflow.expanduser().resolve())
        if args.image_workflow
        else "",
        "video_workflow": str(args.video_workflow.expanduser().resolve())
        if args.video_workflow
        else "",
        "poll_seconds": 2,
        "timeout_seconds": 1800,
    }
    (home / "comfyui.json").write_text(
        json.dumps(comfy, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )

    python = sys.executable
    new_profiles = {
        "comfyui-image": {
            "command": [python, str(adapter_script), "image"],
            "timeout_seconds": 1800,
            "reference_roles": ["reference_image", "first_frame", "last_frame"],
        },
        "comfyui-video": {
            "command": [python, str(adapter_script), "video"],
            "timeout_seconds": 3600,
            "reference_roles": [
                "reference_image",
                "first_frame",
                "last_frame",
                "reference_video",
            ],
        },
    }
    adapters_path = home / "adapters.json"
    existing: dict = {}
    if adapters_path.is_file():
        try:
            loaded = json.loads(adapters_path.read_text(encoding="utf-8"))
            body = loaded.get("adapters") if isinstance(loaded, dict) else None
            if isinstance(body, dict):
                existing = body
        except (OSError, ValueError):
            existing = {}
    existing.update(new_profiles)
    adapters_path.write_text(
        json.dumps({"adapters": existing}, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )

    print(f"已写入 {home / 'comfyui.json'}")
    print(f"已写入 {adapters_path}")
    print("已写入本机 ComfyUI adapter。请在工作室设置 → 生产里点「启动出图」或「启动出视频」。")
    print("工作流 JSON 里请放入占位符 __PROMPT__、__SEED__、__WIDTH__、__HEIGHT__（视频可加 __DURATION__）。")
    if not comfy["image_workflow"]:
        print("提示：尚未设置 --image-workflow，出图前请再跑一遍向导并传入路径。")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
