#!/usr/bin/env python3
"""Studio helpers: list ComfyUI adapters and prepare/run confirmed jobs."""

from __future__ import annotations

import importlib.util
import json
import os
import re
import sys
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any, Mapping

REF_PATH_RE = re.compile(
    r"REF-[A-Z0-9-]+（顺序：(\d+)）·\s*([^\s《]+)《([^》]+)》"
)


def mochang_home() -> Path:
    if os.environ.get("MOCHANG_HOME"):
        return Path(os.environ["MOCHANG_HOME"]).expanduser()
    return Path.home() / ".mochang"


def adapters_config_path() -> Path:
    return mochang_home() / "adapters.json"


IMAGE_ADAPTERS = ("comfyui-image", "gpt-image-2")
VIDEO_ADAPTERS = ("comfyui-video", "minimax-h3")
ADAPTER_LABELS = {
    "comfyui-image": "本机 ComfyUI 出图",
    "gpt-image-2": "云端 GPT Image 2",
    "comfyui-video": "本机 ComfyUI 出视频",
    "minimax-h3": "云端 MiniMax H3",
}
PRODUCE_MODES = ("gate", "session", "always")
PRODUCE_MODE_LABELS = {
    "gate": "确认闸门",
    "session": "会话授权自动",
    "always": "始终自动",
}


def produce_config_path() -> Path:
    return mochang_home() / "produce.json"


def secrets_config_path() -> Path:
    return mochang_home() / "produce-secrets.json"


def comfyui_config_path() -> Path:
    return mochang_home() / "comfyui.json"


def _produce_scripts() -> Path:
    return Path(__file__).resolve().parents[2] / "short-drama-produce" / "scripts"


def _read_json(path: Path) -> dict[str, Any]:
    if not path.is_file():
        return {}
    try:
        document = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return {}
    return document if isinstance(document, dict) else {}


def _write_json(path: Path, document: Mapping[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(dict(document), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def _adapter_map() -> dict[str, Any]:
    document = _read_json(adapters_config_path())
    adapters = document.get("adapters")
    return dict(adapters) if isinstance(adapters, Mapping) else {}


def merge_adapter_profile(name: str, body: Mapping[str, Any]) -> None:
    adapters = _adapter_map()
    adapters[name] = dict(body)
    _write_json(adapters_config_path(), {"adapters": adapters})


def load_comfyui_config() -> dict[str, Any]:
    data = _read_json(comfyui_config_path())
    return {
        "base_url": str(data.get("base_url") or "http://127.0.0.1:8188"),
        "image_workflow": str(data.get("image_workflow") or ""),
        "video_workflow": str(data.get("video_workflow") or ""),
        "poll_seconds": data.get("poll_seconds") or 2,
        "timeout_seconds": data.get("timeout_seconds") or 1800,
    }


def load_adapter_profiles() -> dict[str, Any]:
    return load_produce_status()


def load_produce_status() -> dict[str, Any]:
    path = adapters_config_path()
    adapters = _adapter_map()
    profiles = []
    for name, body in adapters.items():
        if not isinstance(name, str) or not isinstance(body, Mapping):
            continue
        profiles.append(
            {
                "id": name,
                "label": ADAPTER_LABELS.get(name, name),
                "timeout_seconds": body.get("timeout_seconds"),
                "reference_roles": body.get("reference_roles") or [],
            }
        )
    produce = _read_json(produce_config_path())
    secrets = _read_json(secrets_config_path())
    active_image = str(produce.get("active_image") or "")
    active_video = str(produce.get("active_video") or "")
    if active_image not in IMAGE_ADAPTERS:
        active_image = ""
    if active_video not in VIDEO_ADAPTERS:
        active_video = ""
    mode = get_produce_mode()
    env = check_environment(modality="")
    return {
        "path": str(path),
        "profiles": profiles,
        "configured": bool(profiles),
        "comfyui": load_comfyui_config(),
        "cloud": {
            "openai_base_url": str(secrets.get("OPENAI_BASE_URL") or ""),
            "openai_key_set": bool(str(secrets.get("OPENAI_API_KEY") or "").strip()),
            "minimax_key_set": bool(str(secrets.get("MINIMAX_API_KEY") or "").strip()),
            "minimax_video_model": str(secrets.get("MINIMAX_VIDEO_MODEL") or ""),
            "minimax_video_resolutions": str(secrets.get("MINIMAX_VIDEO_RESOLUTIONS") or ""),
            "minimax_video_min_duration": str(secrets.get("MINIMAX_VIDEO_MIN_DURATION") or ""),
            "minimax_video_max_duration": str(secrets.get("MINIMAX_VIDEO_MAX_DURATION") or ""),
        },
        "active_image": active_image,
        "active_video": active_video,
        "active_image_label": ADAPTER_LABELS.get(active_image, "未启动"),
        "active_video_label": ADAPTER_LABELS.get(active_video, "未启动"),
        "labels": ADAPTER_LABELS,
        "mode": mode,
        "mode_label": PRODUCE_MODE_LABELS.get(mode, mode),
        "modes": [
            {"id": key, "label": PRODUCE_MODE_LABELS[key]} for key in PRODUCE_MODES
        ],
        "allow_config_agent": allow_config_agent(),
        "environment": env,
    }


def get_produce_mode() -> str:
    mode = str(_read_json(produce_config_path()).get("mode") or "gate").strip().lower()
    return mode if mode in PRODUCE_MODES else "gate"


def set_produce_mode(mode: str) -> dict[str, Any]:
    wanted = str(mode or "").strip().lower()
    if wanted not in PRODUCE_MODES:
        raise ValueError("生产模式须为 gate / session / always")
    produce = _read_json(produce_config_path())
    produce["mode"] = wanted
    _write_json(produce_config_path(), produce)
    return load_produce_status()


def allow_config_agent() -> bool:
    return bool(_read_json(produce_config_path()).get("allow_config_agent"))


def set_allow_config_agent(enabled: bool) -> dict[str, Any]:
    produce = _read_json(produce_config_path())
    produce["allow_config_agent"] = bool(enabled)
    _write_json(produce_config_path(), produce)
    return load_produce_status()


def can_auto_produce(*, session_authorized: bool) -> bool:
    mode = get_produce_mode()
    if mode == "always":
        return True
    if mode == "session":
        return bool(session_authorized)
    return False


def chat_config_path() -> Path:
    return mochang_home() / "chat.json"


def check_environment(*, modality: str = "") -> dict[str, Any]:
    """Hard gate before chat auto-produce. modality: '' | image | video."""
    issues: list[str] = []
    chat = _read_json(chat_config_path())
    profiles = chat.get("Profiles") or chat.get("profiles") or []
    active = str(chat.get("Active") or chat.get("active") or "")
    has_chat = False
    if isinstance(profiles, list):
        for row in profiles:
            if not isinstance(row, Mapping):
                continue
            name = str(row.get("Name") or row.get("name") or "")
            base = str(row.get("BaseUrl") or row.get("base_url") or "").strip()
            if base and (not active or name == active or not active):
                has_chat = True
                break
            if base:
                has_chat = True
    if not has_chat:
        issues.append("聊天 API 未配置：请到设置 → API 填写可用模型地址")

    status_image = str(_read_json(produce_config_path()).get("active_image") or "")
    status_video = str(_read_json(produce_config_path()).get("active_video") or "")
    if status_image not in IMAGE_ADAPTERS:
        status_image = ""
    if status_video not in VIDEO_ADAPTERS:
        status_video = ""

    lane = (modality or "").strip().lower()
    if lane in ("", "image") and not status_image:
        issues.append("出图后端未启动：请到设置 → 生产保存并启动 ComfyUI 出图或云端 GPT Image")
    if lane in ("", "video") and not status_video:
        issues.append("出视频后端未启动：请到设置 → 生产保存并启动 ComfyUI 出视频或 MiniMax H3")

    return {
        "ok": not issues,
        "issues": issues,
        "chat_ok": has_chat,
        "image_ok": bool(status_image),
        "video_ok": bool(status_video),
        "active_image": status_image,
        "active_video": status_video,
        "mode": get_produce_mode(),
        "allow_config_agent": allow_config_agent(),
    }


def _load_production_tool():
    root = Path(__file__).resolve().parents[2] / "short-drama-produce" / "scripts"
    path = root / "production_tool.py"
    spec = importlib.util.spec_from_file_location("studio_production_tool", path)
    if spec is None or spec.loader is None:
        raise RuntimeError("production_tool unavailable")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _load_views():
    path = Path(__file__).resolve().with_name("creator_views.py")
    spec = importlib.util.spec_from_file_location("studio_creator_views", path)
    if spec is None or spec.loader is None:
        raise RuntimeError("creator_views unavailable")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _bindings_from_ref_field(text: str) -> list[dict[str, Any]]:
    bindings = []
    for match in REF_PATH_RE.finditer(text or ""):
        order = int(match.group(1))
        path = match.group(2).strip()
        label = match.group(3).strip()
        bindings.append(
            {
                "slot_id": f"REF-{order}",
                "order": order,
                "path": path,
                "label": label,
                "role": "reference_image",
                "may_control": ["身份", "造型"],
                "must_not_control": ["构图", "动作"],
            }
        )
    return bindings


def build_job(
    project_root: Path,
    *,
    episode: str,
    modality: str,
    source_entry: str,
    adapter: str,
    overwrite: bool = False,
) -> dict[str, Any]:
    views = _load_views()
    episode_dir = None
    for name in ("剧集", "episodes"):
        candidate = project_root / name / episode
        if candidate.is_dir():
            episode_dir = candidate
            break
    if episode_dir is None:
        raise ValueError(f"episode not found: {episode}")

    if modality == "image" and source_entry.startswith("SHOT-"):
        source_name = "分镜.md"
        doc_path = episode_dir / source_name
        text = doc_path.read_text(encoding="utf-8")
        board = views.parse_storyboard(text)
        shot = next((item for item in board["shots"] if item["id"] == source_entry), None)
        if shot is None or not shot.get("keyframe"):
            raise ValueError("shot keyframe prompt not found")
        prompt = shot["keyframe"]
        bindings = []
        for slot in shot.get("refs") or []:
            if slot.get("kind") != "REF" or not slot.get("path"):
                continue
            bindings.append(
                {
                    "slot_id": slot["id"],
                    "order": int(slot["order"]),
                    "path": slot["path"],
                    "label": slot.get("name") or slot["id"],
                    "role": "first_frame" if slot.get("use") == "起始帧" else "reference_image",
                    "may_control": ["身份", "造型"],
                    "must_not_control": ["构图", "动作"],
                }
            )
    elif modality == "image":
        source_name = "图片提示词.md"
        doc_path = episode_dir / source_name
        text = doc_path.read_text(encoding="utf-8")
        prompts = views.parse_image_prompts(text)
        entry = next((item for item in prompts if item["id"] == source_entry), None)
        if entry is None or not entry.get("prompt"):
            raise ValueError("image prompt entry not found")
        prompt = entry["prompt"]
        bindings = _bindings_from_ref_field(entry.get("ref") or "")
    elif modality == "video":
        source_name = "视频提示词.md"
        doc_path = episode_dir / source_name
        text = doc_path.read_text(encoding="utf-8")
        prompts = views.parse_video_prompts(text)
        entry = next((item for item in prompts if item["id"] == source_entry), None)
        if entry is None or not entry.get("prompt"):
            raise ValueError("video prompt entry not found")
        prompt = entry["prompt"]
        bindings = []
    else:
        raise ValueError("modality must be image or video")

    root_name = "剧集" if (project_root / "剧集").is_dir() else episode_dir.parent.name
    source = f"{root_name}/{episode}/{source_name}"
    outputs = [
        f"{root_name}/{episode}/制作成果/"
        f"{'images' if modality == 'image' else 'videos'}/"
        f"{source_entry}{'.png' if modality == 'image' else '.mp4'}"
    ]

    job: dict[str, Any] = {
        "schema_version": "1.0",
        "job_id": f"{episode}-{source_entry}-{modality}"[:80],
        "modality": modality,
        "adapter": adapter,
        "prompt": prompt,
        "source": source,
        "source_entry": source_entry,
        "outputs": outputs,
        "parameters": {"prompt_language": "en"},
        "overwrite": bool(overwrite),
    }
    if bindings:
        job["reference_bindings"] = bindings
        job["references"] = [item["path"] for item in bindings]
    return job


def prepare(project_root: Path, job: Mapping[str, Any]) -> dict[str, Any]:
    tool = _load_production_tool()
    jobs_dir = project_root / ".short-drama" / "studio-jobs"
    jobs_dir.mkdir(parents=True, exist_ok=True)
    job_id = str(job["job_id"])
    path = jobs_dir / f"{job_id}.json"
    path.write_text(json.dumps(job, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return tool.prepare_job(project_root, path)


def confirm(project_root: Path, job_id: str, confirmation: str) -> dict[str, Any]:
    tool = _load_production_tool()
    return tool.confirm_job(project_root, job_id=job_id, confirmation=confirmation)


def apply_secrets_to_environ() -> None:
    secrets = _read_json(secrets_config_path())
    for name, value in secrets.items():
        if not isinstance(name, str) or not isinstance(value, str) or not value.strip():
            continue
        if not os.environ.get(name):
            os.environ[name] = value.strip()


def run(project_root: Path, job_id: str) -> dict[str, Any]:
    apply_secrets_to_environ()
    tool = _load_production_tool()
    config = adapters_config_path()
    if not config.is_file():
        raise FileNotFoundError("还没有 adapter 配置。请先在设置 → 生产里保存并启动。")
    return tool.run_job(project_root, job_id=job_id, adapter_config=config)


def _python() -> str:
    return sys.executable


def save_comfyui(*, base_url: str, image_workflow: str, video_workflow: str) -> dict[str, Any]:
    url = (base_url or "http://127.0.0.1:8188").strip().rstrip("/")
    if not url:
        raise ValueError("请填写 ComfyUI 地址")
    image_wf = str(image_workflow or "").strip()
    video_wf = str(video_workflow or "").strip()
    current = load_comfyui_config()
    current.update(
        {
            "base_url": url,
            "image_workflow": image_wf,
            "video_workflow": video_wf,
        }
    )
    _write_json(comfyui_config_path(), current)
    script = _produce_scripts() / "comfyui_adapter.py"
    if not script.is_file():
        raise FileNotFoundError(f"找不到 comfyui_adapter.py：{script}")
    python = _python()
    merge_adapter_profile(
        "comfyui-image",
        {
            "command": [python, str(script), "image"],
            "timeout_seconds": 1800,
            "reference_roles": ["reference_image", "first_frame", "last_frame"],
        },
    )
    merge_adapter_profile(
        "comfyui-video",
        {
            "command": [python, str(script), "video"],
            "timeout_seconds": 3600,
            "reference_roles": [
                "reference_image",
                "first_frame",
                "last_frame",
                "reference_video",
            ],
        },
    )
    return load_produce_status()


def _merge_secret(name: str, value: str, *, keep_if_blank: bool = True) -> None:
    secrets = _read_json(secrets_config_path())
    text = (value or "").strip()
    if text:
        secrets[name] = text
    elif not keep_if_blank:
        secrets.pop(name, None)
    _write_json(secrets_config_path(), secrets)


def save_cloud_image(*, api_key: str, base_url: str) -> dict[str, Any]:
    _merge_secret("OPENAI_API_KEY", api_key)
    url = (base_url or "").strip().rstrip("/")
    if url:
        _merge_secret("OPENAI_BASE_URL", url)
    secrets = _read_json(secrets_config_path())
    if not str(secrets.get("OPENAI_API_KEY") or "").strip() and not os.environ.get("OPENAI_API_KEY"):
        raise ValueError("请填写 GPT Image 密钥")
    script = _produce_scripts() / "provider_adapters.py"
    if not script.is_file():
        raise FileNotFoundError(f"找不到 provider_adapters.py：{script}")
    merge_adapter_profile(
        "gpt-image-2",
        {
            "command": [_python(), str(script), "gpt-image-2"],
            "timeout_seconds": 600,
            "reference_roles": ["reference_image"],
        },
    )
    return load_produce_status()


def save_cloud_video(
    *,
    api_key: str,
    model: str,
    resolutions: str,
    min_duration: str,
    max_duration: str,
) -> dict[str, Any]:
    _merge_secret("MINIMAX_API_KEY", api_key)
    for env_name, value in (
        ("MINIMAX_VIDEO_MODEL", model),
        ("MINIMAX_VIDEO_RESOLUTIONS", resolutions),
        ("MINIMAX_VIDEO_MIN_DURATION", min_duration),
        ("MINIMAX_VIDEO_MAX_DURATION", max_duration),
    ):
        text = (value or "").strip()
        if text:
            _merge_secret(env_name, text)
    secrets = _read_json(secrets_config_path())
    if not str(secrets.get("MINIMAX_API_KEY") or "").strip() and not os.environ.get("MINIMAX_API_KEY"):
        raise ValueError("请填写 MiniMax 密钥")
    if not str(secrets.get("MINIMAX_VIDEO_MODEL") or "").strip() and not os.environ.get("MINIMAX_VIDEO_MODEL"):
        raise ValueError("请填写 MiniMax 视频模型 ID")
    if not str(secrets.get("MINIMAX_VIDEO_RESOLUTIONS") or "").strip() and not os.environ.get(
        "MINIMAX_VIDEO_RESOLUTIONS"
    ):
        raise ValueError("请填写 MiniMax 分辨率集合，例如 768P")
    script = _produce_scripts() / "provider_adapters.py"
    if not script.is_file():
        raise FileNotFoundError(f"找不到 provider_adapters.py：{script}")
    merge_adapter_profile(
        "minimax-h3",
        {
            "command": [_python(), str(script), "minimax-h3"],
            "timeout_seconds": 3600,
            "reference_roles": [
                "first_frame",
                "last_frame",
                "reference_image",
                "reference_video",
                "reference_audio",
            ],
        },
    )
    return load_produce_status()


def activate(lane: str, adapter: str) -> dict[str, Any]:
    lane = (lane or "").strip()
    adapter = (adapter or "").strip()
    produce = _read_json(produce_config_path())
    adapters = _adapter_map()
    if lane == "image":
        if adapter not in IMAGE_ADAPTERS:
            raise ValueError("出图只能启动本机 ComfyUI 或云端 GPT Image 2")
        if adapter not in adapters:
            raise ValueError("请先保存该出图配置")
        if adapter == "comfyui-image" and not load_comfyui_config()["image_workflow"]:
            raise ValueError("请先填写并保存出图工作流 JSON")
        secrets = _read_json(secrets_config_path())
        if adapter == "gpt-image-2" and not (
            str(secrets.get("OPENAI_API_KEY") or "").strip() or os.environ.get("OPENAI_API_KEY")
        ):
            raise ValueError("请先保存 GPT Image 密钥")
        produce["active_image"] = adapter
    elif lane == "video":
        if adapter not in VIDEO_ADAPTERS:
            raise ValueError("出视频只能启动本机 ComfyUI 或云端 MiniMax H3")
        if adapter not in adapters:
            raise ValueError("请先保存该出视频配置")
        if adapter == "comfyui-video" and not load_comfyui_config()["video_workflow"]:
            raise ValueError("请先填写并保存出视频工作流 JSON")
        secrets = _read_json(secrets_config_path())
        if adapter == "minimax-h3" and not (
            str(secrets.get("MINIMAX_API_KEY") or "").strip() or os.environ.get("MINIMAX_API_KEY")
        ):
            raise ValueError("请先保存 MiniMax 密钥")
        produce["active_video"] = adapter
    else:
        raise ValueError("lane 必须是 image 或 video")
    _write_json(produce_config_path(), produce)
    return load_produce_status()


def probe_comfyui(base_url: str = "") -> dict[str, Any]:
    url = (base_url or load_comfyui_config()["base_url"]).strip().rstrip("/")
    if not url:
        raise ValueError("请填写 ComfyUI 地址")
    target = url + "/system_stats"
    request = urllib.request.Request(target, method="GET")
    try:
        with urllib.request.urlopen(request, timeout=5) as response:
            ok = 200 <= int(response.status) < 300
    except urllib.error.URLError as exc:
        raise ValueError(f"连不上 ComfyUI（{url}）：{exc}") from exc
    except TimeoutError as exc:
        raise ValueError(f"ComfyUI 超时（{url}）") from exc
    if not ok:
        raise ValueError(f"ComfyUI 返回异常状态（{url}）")
    status = load_produce_status()
    status["probe"] = {"ok": True, "url": url}
    return status


def resolve_adapter(modality: str, adapter: str = "") -> str:
    name = (adapter or "").strip()
    if name:
        return name
    status = load_produce_status()
    name = str(status.get("active_video" if modality == "video" else "active_image") or "")
    if not name:
        raise ValueError("请先在设置 → 生产里启动对应的出图或出视频配置")
    return name


def apply_config(payload: Mapping[str, Any]) -> dict[str, Any]:
    action = str(payload.get("action") or "").strip()
    if action == "save_comfyui":
        return save_comfyui(
            base_url=str(payload.get("base_url") or ""),
            image_workflow=str(payload.get("image_workflow") or ""),
            video_workflow=str(payload.get("video_workflow") or ""),
        )
    if action == "save_cloud_image":
        return save_cloud_image(
            api_key=str(payload.get("api_key") or ""),
            base_url=str(payload.get("base_url") or ""),
        )
    if action == "save_cloud_video":
        return save_cloud_video(
            api_key=str(payload.get("api_key") or ""),
            model=str(payload.get("model") or ""),
            resolutions=str(payload.get("resolutions") or ""),
            min_duration=str(payload.get("min_duration") or ""),
            max_duration=str(payload.get("max_duration") or ""),
        )
    if action == "activate":
        return activate(str(payload.get("lane") or ""), str(payload.get("adapter") or ""))
    if action == "probe_comfyui":
        return probe_comfyui(str(payload.get("base_url") or ""))
    if action == "set_mode":
        return set_produce_mode(str(payload.get("mode") or ""))
    if action == "set_allow_config_agent":
        return set_allow_config_agent(bool(payload.get("enabled")))
    raise ValueError("未知的生产配置动作")
