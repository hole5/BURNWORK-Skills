#!/usr/bin/env python3
"""ComfyUI adapter for 燃烧工作室 image/video jobs.

Reads confirmed production_tool stdin JSON, submits a user-supplied workflow
to a local ComfyUI server, polls until complete, and writes the first matching
output file under output_root.

Configuration (project-external), default ~/.mochang/comfyui.json:

{
  "base_url": "http://127.0.0.1:8188",
  "image_workflow": "C:/path/to/image_api.json",
  "video_workflow": "C:/path/to/video_api.json",
  "poll_seconds": 2,
  "timeout_seconds": 1800
}

Workflow JSON may contain the literal placeholders:
  __PROMPT__   → job prompt
  __NEGATIVE__ → parameters.negative or empty
  __SEED__     → parameters.seed or random int
  __WIDTH__ / __HEIGHT__ → from parameters.size "W×H" or width/height
  __DURATION__ → parameters.duration (video)
"""

from __future__ import annotations

import argparse
import json
import os
import random
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from pathlib import Path
from typing import Any, Mapping


CONFIG_CANDIDATES = (
    Path(os.environ["MOCHANG_HOME"]) / "comfyui.json"
    if os.environ.get("MOCHANG_HOME")
    else None,
    Path.home() / ".mochang" / "comfyui.json",
)


def _load_config() -> dict[str, Any]:
    for path in CONFIG_CANDIDATES:
        if path is None or not path.is_file():
            continue
        data = json.loads(path.read_text(encoding="utf-8"))
        if isinstance(data, dict):
            return data
    return {
        "base_url": os.environ.get("COMFYUI_BASE_URL", "http://127.0.0.1:8188"),
        "image_workflow": os.environ.get("COMFYUI_IMAGE_WORKFLOW", ""),
        "video_workflow": os.environ.get("COMFYUI_VIDEO_WORKFLOW", ""),
        "poll_seconds": 2,
        "timeout_seconds": 1800,
    }


def _http_json(method: str, url: str, body: dict[str, Any] | None = None) -> Any:
    data = None if body is None else json.dumps(body).encode("utf-8")
    request = urllib.request.Request(
        url,
        data=data,
        method=method,
        headers={"Content-Type": "application/json"} if data else {},
    )
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            raw = response.read()
            if not raw:
                return None
            return json.loads(raw.decode("utf-8"))
    except urllib.error.HTTPError as exc:
        raise RuntimeError(f"ComfyUI HTTP {exc.code}") from exc
    except urllib.error.URLError as exc:
        raise RuntimeError(f"ComfyUI unreachable: {exc.reason}") from exc


def _http_bytes(url: str) -> bytes:
    with urllib.request.urlopen(url, timeout=120) as response:
        return response.read()


def _size_wh(parameters: Mapping[str, Any]) -> tuple[str, str]:
    if isinstance(parameters.get("width"), (int, float)) and isinstance(
        parameters.get("height"), (int, float)
    ):
        return str(int(parameters["width"])), str(int(parameters["height"]))
    size = parameters.get("size")
    if isinstance(size, str) and ("x" in size or "×" in size):
        left, _, right = size.replace("×", "x").partition("x")
        if left.strip().isdigit() and right.strip().isdigit():
            return left.strip(), right.strip()
    return "768", "1344"


def _fill_workflow(raw: str, job: Mapping[str, Any]) -> dict[str, Any]:
    parameters = job.get("parameters") if isinstance(job.get("parameters"), Mapping) else {}
    width, height = _size_wh(parameters)
    seed = parameters.get("seed")
    if not isinstance(seed, int):
        seed = random.randint(1, 2_147_483_647)
    negative = parameters.get("negative")
    if not isinstance(negative, str):
        negative = ""
    duration = parameters.get("duration")
    duration_text = str(int(duration)) if isinstance(duration, (int, float)) else "5"
    filled = (
        raw.replace("__PROMPT__", json.dumps(str(job.get("prompt", "")))[1:-1])
        .replace("__NEGATIVE__", json.dumps(negative)[1:-1])
        .replace("__SEED__", str(seed))
        .replace("__WIDTH__", width)
        .replace("__HEIGHT__", height)
        .replace("__DURATION__", duration_text)
    )
    document = json.loads(filled)
    if not isinstance(document, dict):
        raise ValueError("workflow must be a JSON object")
    # API format is usually {"prompt": {node_id: ...}} or bare node map.
    if "prompt" in document and isinstance(document["prompt"], dict):
        return document
    return {"prompt": document}


def _write_handle(path: str | None, prompt_id: str) -> None:
    if not path:
        return
    target = Path(path)
    target.parent.mkdir(parents=True, exist_ok=True)
    temporary = target.with_suffix(target.suffix + ".tmp")
    temporary.write_text(
        json.dumps({"provider_job_id": prompt_id}, ensure_ascii=True),
        encoding="utf-8",
    )
    temporary.replace(target)


def _first_output_file(history_entry: Mapping[str, Any]) -> tuple[str, str, str] | None:
    outputs = history_entry.get("outputs")
    if not isinstance(outputs, Mapping):
        return None
    for _node, payload in outputs.items():
        if not isinstance(payload, Mapping):
            continue
        for key in ("images", "gifs", "videos"):
            items = payload.get(key)
            if not isinstance(items, list) or not items:
                continue
            first = items[0]
            if not isinstance(first, Mapping):
                continue
            name = first.get("filename")
            if not isinstance(name, str) or not name:
                continue
            subfolder = first.get("subfolder") if isinstance(first.get("subfolder"), str) else ""
            folder_type = first.get("type") if isinstance(first.get("type"), str) else "output"
            return name, subfolder, folder_type
    return None


def _run(job: Mapping[str, Any], modality: str) -> dict[str, Any]:
    if job.get("collect_provider_job_id"):
        prompt_id = str(job["collect_provider_job_id"])
        config = _load_config()
        base = str(config.get("base_url") or "http://127.0.0.1:8188").rstrip("/")
        return _collect(job, base, prompt_id, config)

    config = _load_config()
    base = str(config.get("base_url") or "http://127.0.0.1:8188").rstrip("/")
    workflow_key = "image_workflow" if modality == "image" else "video_workflow"
    workflow_path = config.get(workflow_key) or os.environ.get(
        "COMFYUI_IMAGE_WORKFLOW" if modality == "image" else "COMFYUI_VIDEO_WORKFLOW",
        "",
    )
    if not isinstance(workflow_path, str) or not workflow_path.strip():
        raise ValueError(f"missing {workflow_key} in ~/.mochang/comfyui.json")
    path = Path(workflow_path).expanduser()
    if not path.is_file():
        raise ValueError(f"workflow file not found: {path}")
    workflow = _fill_workflow(path.read_text(encoding="utf-8"), job)
    client_id = uuid.uuid4().hex
    body = {"prompt": workflow["prompt"], "client_id": client_id}
    submitted = _http_json("POST", f"{base}/prompt", body)
    if not isinstance(submitted, Mapping) or not submitted.get("prompt_id"):
        raise RuntimeError("ComfyUI did not return prompt_id")
    prompt_id = str(submitted["prompt_id"])
    _write_handle(job.get("handle_path") if isinstance(job.get("handle_path"), str) else None, prompt_id)
    return _collect(job, base, prompt_id, config)


def _collect(
    job: Mapping[str, Any], base: str, prompt_id: str, config: Mapping[str, Any]
) -> dict[str, Any]:
    poll = float(config.get("poll_seconds") or 2)
    timeout = float(config.get("timeout_seconds") or 1800)
    deadline = time.time() + timeout
    history_entry: Mapping[str, Any] | None = None
    while time.time() < deadline:
        history = _http_json("GET", f"{base}/history/{urllib.parse.quote(prompt_id)}")
        if isinstance(history, Mapping) and prompt_id in history:
            entry = history[prompt_id]
            if isinstance(entry, Mapping):
                history_entry = entry
                status = entry.get("status")
                if isinstance(status, Mapping) and status.get("completed"):
                    break
                if entry.get("outputs"):
                    break
        time.sleep(max(0.5, poll))
    else:
        raise TimeoutError("ComfyUI timed out")
    assert history_entry is not None
    found = _first_output_file(history_entry)
    if found is None:
        raise RuntimeError("ComfyUI finished without image/video outputs")
    filename, subfolder, folder_type = found
    query = urllib.parse.urlencode(
        {"filename": filename, "subfolder": subfolder, "type": folder_type}
    )
    blob = _http_bytes(f"{base}/view?{query}")
    output_root = Path(str(job["output_root"]))
    suffix = Path(filename).suffix or (".png" if job.get("modality") == "image" else ".mp4")
    staged_name = f"comfyui-result{suffix}"
    destination = output_root / staged_name
    destination.write_bytes(blob)
    return {
        "outputs": [{"target": job["outputs"][0], "source": str(destination)}],
        "provider_job_id": prompt_id,
    }


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("modality", choices=("image", "video"))
    parser.add_argument("--selftest", action="store_true")
    args = parser.parse_args(argv)
    if args.selftest:
        filled = _fill_workflow(
            '{"prompt":{"1":{"inputs":{"text":"__PROMPT__","seed":__SEED__,"w":__WIDTH__}}}}',
            {"prompt": 'a "quote"', "parameters": {"seed": 7, "size": "512x768"}},
        )
        assert filled["prompt"]["1"]["inputs"]["seed"] == 7
        assert str(filled["prompt"]["1"]["inputs"]["w"]) == "512"
        print("comfyui_adapter selftest ok")
        return 0
    try:
        job = json.load(sys.stdin.buffer)
        if not isinstance(job, Mapping):
            raise ValueError("adapter input must be an object")
        if job.get("modality") not in (None, args.modality):
            # production_tool always sends modality; refuse mismatches.
            if job.get("modality") != args.modality:
                raise ValueError("job modality does not match adapter")
        result = _run(job, args.modality)
        json.dump(result, sys.stdout, ensure_ascii=True)
        return 0
    except Exception as exc:
        category = "provider_error"
        retryable = True
        code = "comfyui_failure"
        if isinstance(exc, ValueError):
            category, retryable, code = "invalid_request", False, "invalid_job"
        elif isinstance(exc, TimeoutError):
            category, code = "timeout", "comfyui_timeout"
        json.dump(
            {
                "error": {
                    "provider": f"comfyui-{args.modality}",
                    "category": category,
                    "code": code,
                    "retryable": retryable,
                }
            },
            sys.stdout,
            ensure_ascii=True,
        )
        print(f"comfyui adapter failed: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
