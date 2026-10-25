# -*- coding: utf-8 -*-
"""无控制台桌面入口：优先启动 WPF 窗口。"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
EXE = REPO / "desktop" / "MochangStudio" / "bin" / "Release" / "net8.0-windows" / "BurnworkStudio.exe"
LAUNCHER = REPO / "skills" / "short-drama" / "scripts" / "studio_launch.py"


def main() -> int:
    workspace = Path(sys.argv[1]).expanduser().resolve() if len(sys.argv) > 1 else REPO
    if EXE.is_file():
        return subprocess.call([str(EXE), str(workspace)])
    if not LAUNCHER.is_file():
        raise SystemExit(f"找不到启动器: {LAUNCHER}")
    return subprocess.call(
        [
            sys.executable,
            str(LAUNCHER),
            "--workspace",
            str(workspace),
            "--restart",
            "--open",
        ]
    )


if __name__ == "__main__":
    raise SystemExit(main())
