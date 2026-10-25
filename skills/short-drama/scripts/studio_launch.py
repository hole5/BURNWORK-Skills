#!/usr/bin/env python3
"""One-click launcher for 燃烧工作室 (local short-drama studio)."""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
DASHBOARD = HERE / "dashboard_server.py"


def _mochang_home() -> Path:
    raw = os.environ.get("MOCHANG_HOME")
    if raw:
        return Path(raw).expanduser()
    return Path.home() / ".mochang"


def _user_documents() -> Path:
    for name in ("Documents", "文档"):
        candidate = Path.home() / name
        if candidate.is_dir():
            return candidate
    return Path.home() / "Documents"


def default_projects_dir() -> Path:
    cfg = _mochang_home() / "studio.json"
    if cfg.is_file():
        try:
            data = json.loads(cfg.read_text(encoding="utf-8"))
            raw = data.get("ProjectsDir") or data.get("projectsDir") or ""
            if str(raw).strip():
                path = Path(str(raw).strip()).expanduser()
                path.mkdir(parents=True, exist_ok=True)
                return path.resolve()
        except (OSError, json.JSONDecodeError, TypeError):
            pass
    path = _user_documents() / "燃烧工作室" / "projects"
    path.mkdir(parents=True, exist_ok=True)
    return path.resolve()


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="启动燃烧工作室")
    parser.add_argument(
        "--workspace",
        type=Path,
        default=None,
        help="要扫描的项目文件夹（含 short-drama.json 的子目录）。默认读 ~/.mochang/studio.json 的 ProjectsDir",
    )
    parser.add_argument("--port", type=int, default=0, help="端口，0 表示自动挑选")
    parser.add_argument("--open", action="store_true", help="用默认浏览器打开")
    parser.add_argument("--restart", action="store_true", help="先停掉旧实例再启动")
    parser.add_argument("--status", action="store_true", help="只打印当前链接")
    parser.add_argument("--stop", action="store_true", help="停止本工作区的工作室")
    args = parser.parse_args(argv)

    workspace = (
        args.workspace.expanduser().resolve()
        if args.workspace is not None
        else default_projects_dir()
    )
    cmd = [
        sys.executable,
        str(DASHBOARD),
        "--workspace",
        str(workspace),
    ]
    if args.status:
        cmd.append("--status")
    elif args.stop:
        cmd.append("--stop")
    else:
        cmd.extend(["--port", str(args.port), "--detach"])
        if args.open:
            cmd.append("--open")
        if args.restart:
            cmd.append("--restart")
    return subprocess.call(cmd)


if __name__ == "__main__":
    raise SystemExit(main())
