#!/usr/bin/env python3
"""Studio chat agent: route skills, read/write Markdown; produce gated by mode."""

from __future__ import annotations

import hashlib
import json
import os
import re
import urllib.error
import urllib.request
from collections.abc import Callable, Iterator
from pathlib import Path, PurePosixPath
from typing import Any

FRONTMATTER = re.compile(r"\A---\r?\n(.*?)\r?\n---\r?\n?", re.DOTALL)
JSON_FENCE = re.compile(r"```json\s*(\{.*?\})\s*```", re.DOTALL)
SKILL_HINT = re.compile(r"\[skill:([a-z0-9\-]+)\]", re.I)

DOC_BY_SKILL = {
    "short-drama-write": "剧本.md",
    "short-drama-assets": "视觉设定.md",
    "short-drama-storyboard": "分镜.md",
    "short-drama-image-prompts": "图片提示词.md",
    "short-drama-video-prompts": "视频提示词.md",
    "short-drama-review": "审查.md",
    "short-drama-edit": "剪辑单.md",
}

PLAN_HINTS = (
    ("short-drama", ("导演台", "导演 pass", "电影级过一遍", "导演工作流")),
    ("short-drama-novel-analyze", ("原著", "拆书", "分析小说")),
    ("short-drama-develop", ("分集地图", "系列大纲", "改编方案", "导演阐述")),
    ("short-drama-write", ("写剧本", "改剧本", "剧本.md", "对白")),
    ("short-drama-assets", ("视觉设定", "拆角色", "拆资产", "造型", "道具")),
    ("short-drama-storyboard", ("写分镜", "出分镜", "分镜.md", "镜头表")),
    ("short-drama-image-prompts", ("图片提示词", "设定图提示")),
    ("short-drama-video-prompts", ("视频提示词", "运镜提示")),
    ("short-drama-review", ("审稿", "审查", "QC")),
)

TOOLS = [
    {
        "type": "function",
        "function": {
            "name": "list_skills",
            "description": "列出已安装技能的 name 与 description",
            "parameters": {"type": "object", "properties": {}},
        },
    },
    {
        "type": "function",
        "function": {
            "name": "read_skill",
            "description": "读取一份 SKILL.md 正文（按需，不要一次读全部）",
            "parameters": {
                "type": "object",
                "properties": {"name": {"type": "string"}},
                "required": ["name"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "list_episode_docs",
            "description": "列出当前集已有创作文档",
            "parameters": {
                "type": "object",
                "properties": {"episode": {"type": "string"}},
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "read_project_file",
            "description": "读取项目内 Markdown",
            "parameters": {
                "type": "object",
                "properties": {"path": {"type": "string"}},
                "required": ["path"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "write_project_file",
            "description": "写入或创建项目内 Markdown。只写当前任务需要的那一份。",
            "parameters": {
                "type": "object",
                "properties": {
                    "path": {"type": "string"},
                    "content": {"type": "string"},
                },
                "required": ["path", "content"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "propose_plan",
            "description": "多步工作流先列出步骤，不要一次写完所有文档",
            "parameters": {
                "type": "object",
                "properties": {
                    "steps": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "skill": {"type": "string"},
                                "title": {"type": "string"},
                            },
                            "required": ["skill", "title"],
                        },
                    }
                },
                "required": ["steps"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "prepare_produce",
            "description": "预览生产任务。确认闸门模式下不会自动提交；会话/始终自动且环境齐全时会继续 confirm+run。",
            "parameters": {
                "type": "object",
                "properties": {
                    "episode": {"type": "string"},
                    "modality": {"type": "string"},
                    "source_entry": {"type": "string"},
                    "adapter": {"type": "string"},
                },
                "required": ["episode", "modality", "source_entry"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "check_produce_environment",
            "description": "检查聊天 API 与出图/出视频后端是否已配置并启动",
            "parameters": {
                "type": "object",
                "properties": {"modality": {"type": "string"}},
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "propose_studio_config",
            "description": "在允许改本机配置时，预览将写入 ~/.mochang 的变更（需用户确认后 apply）",
            "parameters": {
                "type": "object",
                "properties": {
                    "patch": {
                        "type": "object",
                        "description": "可含 chat_base_url/chat_model/comfy_base_url/comfy_image_workflow/comfy_video_workflow 等非密钥字段；密钥字段只标记已提供",
                    }
                },
                "required": ["patch"],
            },
        },
    },
    {
        "type": "function",
        "function": {
            "name": "apply_studio_config",
            "description": "用户已确认后，按 propose 返回的 token 落盘本机配置",
            "parameters": {
                "type": "object",
                "properties": {"token": {"type": "string"}},
                "required": ["token"],
            },
        },
    },
]


def parse_frontmatter(text: str) -> dict[str, str]:
    match = FRONTMATTER.match(text)
    if not match:
        return {}
    meta: dict[str, str] = {}
    for line in match.group(1).splitlines():
        if ":" not in line:
            continue
        key, value = line.split(":", 1)
        meta[key.strip()] = value.strip().strip("\"'")
    return meta


def catalog_skills(skills_dir: str | Path) -> list[dict[str, str]]:
    root = Path(skills_dir)
    if not root.is_dir():
        return []
    items: list[dict[str, str]] = []
    for child in sorted(root.iterdir(), key=lambda p: p.name.casefold()):
        skill = child / "SKILL.md"
        if not child.is_dir() or not skill.is_file():
            continue
        try:
            text = skill.read_text(encoding="utf-8")
        except OSError:
            continue
        meta = parse_frontmatter(text)
        name = meta.get("name") or child.name
        items.append(
            {
                "name": name,
                "description": meta.get("description") or "",
                "path": str(skill),
            }
        )
    return items


def infer_plan(text: str) -> list[dict[str, str]]:
    hinted = SKILL_HINT.search(text)
    if hinted:
        name = hinted.group(1).casefold()
        title = DOC_BY_SKILL.get(name, name)
        return [{"skill": name, "title": f"按 {title} 执行"}]
    hits: list[dict[str, str]] = []
    seen: set[str] = set()
    for skill, keys in PLAN_HINTS:
        if skill in seen:
            continue
        if any(key in text for key in keys):
            seen.add(skill)
            hits.append(
                {
                    "skill": skill,
                    "title": f"写{DOC_BY_SKILL.get(skill, skill)}",
                }
            )
    return hits


def clamp_relative(path: str) -> str:
    raw = (path or "").replace("\\", "/").lstrip("/")
    pure = PurePosixPath(raw)
    if pure.is_absolute() or ".." in pure.parts or not pure.parts:
        raise ValueError("path escapes the project")
    if pure.suffix.casefold() not in {".md", ".txt"}:
        raise ValueError("only Markdown/text can be written by the assistant")
    if pure.parts[0] in {".short-drama", ".git"}:
        raise ValueError("protected path")
    if pure.name.casefold() == "short-drama.json":
        raise ValueError("project manifest is protected")
    return pure.as_posix()


def episode_doc_path(episode: str, filename: str) -> str:
    ep = episode.strip() or "EP001"
    return f"剧集/{ep}/{filename}"


def list_docs(root: Path, episode: str) -> list[dict[str, Any]]:
    folder = root / "剧集" / (episode or "EP001")
    rows: list[dict[str, Any]] = []
    if not folder.is_dir():
        return rows
    for path in sorted(folder.glob("*.md")):
        rows.append(
            {
                "name": path.name,
                "path": path.relative_to(root).as_posix(),
                "bytes": path.stat().st_size,
            }
        )
    return rows


def read_limited(path: Path, limit: int = 12000) -> str:
    text = path.read_text(encoding="utf-8")
    if len(text) <= limit:
        return text
    return text[:limit] + "\n\n…(截断)"


def upsert_text(root: Path, relative: str, content: str) -> dict[str, str]:
    rel = clamp_relative(relative)
    dest = (root / rel).resolve()
    try:
        dest.relative_to(root.resolve())
    except ValueError as exc:
        raise ValueError("path escapes the project") from exc
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(content, encoding="utf-8")
    return {"path": rel, "bytes": str(dest.stat().st_size)}


def load_chat_profile() -> dict[str, str]:
    home = Path(
        os.environ.get("MOCHANG_HOME")
        or (Path.home() / ".mochang")
    )
    path = home / "chat.json"
    default = {
        "name": "Ollama",
        "baseUrl": "http://127.0.0.1:11434/v1",
        "apiKey": "ollama",
        "model": "qwen2.5",
    }
    if not path.is_file():
        return default
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return default
    if isinstance(data.get("profiles"), list) and data["profiles"]:
        active = data.get("active")
        profiles = data["profiles"]
        hit = next((p for p in profiles if p.get("name") == active), profiles[0])
        return {
            "name": str(hit.get("name") or "默认"),
            "baseUrl": str(hit.get("baseUrl") or default["baseUrl"]),
            "apiKey": str(hit.get("apiKey") or ""),
            "model": str(hit.get("model") or default["model"]),
        }
    return {
        "name": str(data.get("name") or "默认"),
        "baseUrl": str(data.get("baseUrl") or default["baseUrl"]),
        "apiKey": str(data.get("apiKey") or ""),
        "model": str(data.get("model") or default["model"]),
    }


def openai_complete(
    messages: list[dict[str, Any]],
    tools: list[dict[str, Any]] | None = None,
) -> dict[str, Any]:
    cfg = load_chat_profile()
    url = cfg["baseUrl"].rstrip("/") + "/chat/completions"
    payload: dict[str, Any] = {
        "model": cfg["model"],
        "temperature": 0.35,
        "messages": messages,
    }
    if tools:
        payload["tools"] = tools
        payload["tool_choice"] = "auto"
    raw = json.dumps(payload, ensure_ascii=False).encode("utf-8")
    req = urllib.request.Request(
        url,
        data=raw,
        method="POST",
        headers={
            "Content-Type": "application/json",
            "Authorization": "Bearer " + (cfg["apiKey"] or "none"),
        },
    )
    try:
        with urllib.request.urlopen(req, timeout=180) as res:
            body = json.loads(res.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode("utf-8", "replace")[:400]
        raise RuntimeError(f"模型接口 {exc.code}：{detail}") from exc
    except OSError as exc:
        raise RuntimeError(f"无法连接模型接口 {url}：{exc}") from exc
    choice = (body.get("choices") or [{}])[0]
    message = choice.get("message") or {}
    return {
        "content": message.get("content") or "",
        "tool_calls": message.get("tool_calls") or [],
    }


def parse_json_actions(text: str) -> list[dict[str, Any]]:
    actions: list[dict[str, Any]] = []
    for blob in JSON_FENCE.findall(text or ""):
        try:
            data = json.loads(blob)
        except ValueError:
            continue
        if isinstance(data, dict) and data.get("tool"):
            actions.append(data)
    return actions


def _args(raw: Any) -> dict[str, Any]:
    if isinstance(raw, dict):
        return raw
    if isinstance(raw, str) and raw.strip():
        try:
            data = json.loads(raw)
            return data if isinstance(data, dict) else {}
        except ValueError:
            return {}
    return {}


def _pending_config_path() -> Path:
    home = Path(os.environ.get("MOCHANG_HOME") or (Path.home() / ".mochang"))
    return home / "config-pending.json"


def _studio_produce():
    import importlib.util
    import sys

    here = Path(__file__).resolve().parent
    if str(here) not in sys.path:
        sys.path.insert(0, str(here))
    path = here / "studio_produce.py"
    spec = importlib.util.spec_from_file_location("studio_produce", path)
    if spec is None or spec.loader is None:
        raise ImportError("studio_produce unavailable")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _prepare_produce(root: Path, args: dict[str, Any]) -> dict[str, Any]:
    try:
        studio_produce = _studio_produce()
    except Exception as exc:
        return {"error": f"无法加载生产模块：{exc}"}
    job = studio_produce.build_job(
        root,
        episode=str(args.get("episode") or ""),
        modality=str(args.get("modality") or ""),
        source_entry=str(args.get("source_entry") or ""),
        adapter=studio_produce.resolve_adapter(
            str(args.get("modality") or ""),
            str(args.get("adapter") or ""),
        ),
        overwrite=True,
    )
    preview = studio_produce.prepare(root, job)
    return {"job": job, "preview": preview, "note": "未确认，不会提交后端"}


def _auto_run_produce(root: Path, prepared: dict[str, Any]) -> dict[str, Any]:
    studio_produce = _studio_produce()

    preview = prepared.get("preview") if isinstance(prepared.get("preview"), dict) else {}
    job = prepared.get("job") if isinstance(prepared.get("job"), dict) else {}
    job_id = str(job.get("job_id") or preview.get("job_id") or "")
    confirmation = str(preview.get("confirmation") or "")
    if not job_id or not confirmation:
        return {"error": "预览缺少 job_id 或 confirmation，无法自动提交"}
    studio_produce.confirm(root, job_id, confirmation)
    result = studio_produce.run(root, job_id)
    return {"ok": True, "job_id": job_id, "result": result}


def _propose_studio_config(patch: dict[str, Any]) -> dict[str, Any]:
    studio_produce = _studio_produce()

    if not studio_produce.allow_config_agent():
        return {"error": "未允许小烧改本机配置。请在设置 → 生产打开该开关。"}
    safe_patch = {str(k): v for k, v in patch.items() if isinstance(k, str)}
    preview_lines = []
    for key, value in safe_patch.items():
        lowered = key.lower()
        if "key" in lowered or "secret" in lowered:
            preview_lines.append(f"{key}: （已提供，落盘时写入，预览脱敏）")
        else:
            preview_lines.append(f"{key}: {value}")
    raw = json.dumps(safe_patch, ensure_ascii=False, sort_keys=True)
    token = hashlib.sha256(raw.encode("utf-8")).hexdigest()[:24]
    path = _pending_config_path()
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps({"token": token, "patch": safe_patch}, ensure_ascii=False, indent=2)
        + "\n",
        encoding="utf-8",
    )
    return {
        "token": token,
        "preview": preview_lines,
        "note": "请向用户展示预览；用户确认后调用 apply_studio_config",
    }


def _apply_studio_config(token: str) -> dict[str, Any]:
    studio_produce = _studio_produce()

    if not studio_produce.allow_config_agent():
        return {"error": "未允许小烧改本机配置。"}
    path = _pending_config_path()
    if not path.is_file():
        return {"error": "没有待确认的配置预览，请先 propose_studio_config"}
    try:
        pending = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return {"error": "待确认配置损坏"}
    if str(pending.get("token") or "") != token:
        return {"error": "确认 token 不匹配，请重新预览"}
    patch = pending.get("patch") if isinstance(pending.get("patch"), dict) else {}
    applied: list[str] = []
    if any(str(k).startswith("comfy_") for k in patch):
        studio_produce.save_comfyui(
            base_url=str(patch.get("comfy_base_url") or ""),
            image_workflow=str(patch.get("comfy_image_workflow") or ""),
            video_workflow=str(patch.get("comfy_video_workflow") or ""),
        )
        applied.append("comfyui.json")
    if any(
        str(k).startswith("cloud_image") or str(k) in {"openai_api_key", "openai_base_url"}
        for k in patch
    ):
        studio_produce.save_cloud_image(
            api_key=str(patch.get("openai_api_key") or patch.get("cloud_image_api_key") or ""),
            base_url=str(patch.get("cloud_image_base_url") or patch.get("openai_base_url") or ""),
        )
        applied.append("cloud-image")
    if any(str(k).startswith("cloud_video") or str(k).startswith("minimax") for k in patch):
        studio_produce.save_cloud_video(
            api_key=str(patch.get("minimax_api_key") or patch.get("cloud_video_api_key") or ""),
            model=str(patch.get("minimax_video_model") or patch.get("cloud_video_model") or ""),
            resolutions=str(patch.get("minimax_video_resolutions") or ""),
            min_duration=str(patch.get("minimax_video_min_duration") or ""),
            max_duration=str(patch.get("minimax_video_max_duration") or ""),
        )
        applied.append("cloud-video")
    if any(str(k).startswith("chat_") for k in patch):
        chat_path = studio_produce.chat_config_path()
        chat = studio_produce._read_json(chat_path)
        profiles = chat.get("Profiles")
        if not isinstance(profiles, list) or not profiles:
            profiles = [{"Name": "默认", "BaseUrl": "", "ApiKey": "ollama", "Model": ""}]
        row = profiles[0] if isinstance(profiles[0], dict) else {}
        if patch.get("chat_base_url"):
            row["BaseUrl"] = str(patch.get("chat_base_url"))
        if patch.get("chat_model"):
            row["Model"] = str(patch.get("chat_model"))
        if patch.get("chat_api_key"):
            row["ApiKey"] = str(patch.get("chat_api_key"))
        profiles[0] = row
        chat["Profiles"] = profiles
        if not chat.get("Active"):
            chat["Active"] = str(row.get("Name") or "默认")
        studio_produce._write_json(chat_path, chat)
        applied.append("chat.json")
    try:
        path.unlink()
    except OSError:
        pass
    if not applied:
        return {"error": "预览中没有可识别的配置字段"}
    return {"ok": True, "applied": applied}


def run_tool(
    name: str,
    args: dict[str, Any],
    *,
    root: Path,
    skills: list[dict[str, str]],
    episode: str,
    allow_chain: bool,
    session_produce_auth: bool = False,
) -> tuple[Any, dict[str, Any] | None]:
    if name == "list_skills":
        return (
            [
                {"name": item["name"], "description": item["description"]}
                for item in skills
            ],
            None,
        )
    if name == "read_skill":
        wanted = str(args.get("name") or "")
        hit = next((item for item in skills if item["name"] == wanted), None)
        if hit is None:
            return {"error": f"未安装技能 {wanted}"}, None
        event = {"type": "skill", "name": wanted}
        return {"name": wanted, "text": read_limited(Path(hit["path"]))}, event
    if name == "list_episode_docs":
        ep = str(args.get("episode") or episode)
        return list_docs(root, ep), None
    if name == "read_project_file":
        rel = clamp_relative(str(args.get("path") or ""))
        path = root / rel
        if not path.is_file():
            return {"error": f"没有 {rel}"}, None
        return {"path": rel, "content": read_limited(path, 20000)}, None
    if name == "write_project_file":
        saved = upsert_text(
            root, str(args.get("path") or ""), str(args.get("content") or "")
        )
        return saved, {"type": "file_write", "path": saved["path"]}
    if name == "propose_plan":
        steps = args.get("steps") or []
        event = {"type": "plan", "steps": steps, "allowChainWrites": allow_chain}
        return {"ok": True, "steps": steps, "awaiting": not allow_chain}, event
    if name == "check_produce_environment":
        studio_produce = _studio_produce()

        env = studio_produce.check_environment(modality=str(args.get("modality") or ""))
        return env, {"type": "environment", "payload": env}
    if name == "propose_studio_config":
        patch = args.get("patch") if isinstance(args.get("patch"), dict) else {}
        result = _propose_studio_config(patch)
        if result.get("error"):
            return result, None
        return result, {"type": "config_preview", "payload": result}
    if name == "apply_studio_config":
        result = _apply_studio_config(str(args.get("token") or ""))
        if result.get("error"):
            return result, None
        return result, {"type": "config_applied", "payload": result}
    if name == "prepare_produce":
        studio_produce = _studio_produce()

        modality = str(args.get("modality") or "")
        env = studio_produce.check_environment(modality=modality)
        if not env.get("ok"):
            return {
                "error": "环境未就绪",
                "issues": env.get("issues") or [],
            }, {"type": "environment", "payload": env}
        prepared = _prepare_produce(root, args)
        if prepared.get("error"):
            return prepared, None
        auto = studio_produce.can_auto_produce(session_authorized=session_produce_auth)
        if not auto:
            event = {"type": "need_confirm", "kind": "produce", "payload": prepared}
            return prepared, event
        try:
            ran = _auto_run_produce(root, prepared)
        except Exception as exc:
            return {"error": f"自动生产失败：{exc}", "prepared": prepared}, {
                "type": "need_confirm",
                "kind": "produce",
                "payload": prepared,
            }
        if ran.get("error"):
            return ran, {"type": "need_confirm", "kind": "produce", "payload": prepared}
        return ran, {"type": "produce_done", "payload": ran}
    if name in {"confirm_produce", "run_produce", "run_project_tool"}:
        studio_produce = _studio_produce()

        if not studio_produce.can_auto_produce(session_authorized=session_produce_auth):
            return {
                "error": "该动作被禁止。当前为确认闸门模式，请到「生产」页确认；或在设置改为会话/始终自动并完成本会话授权。",
            }, None
        return {
            "error": "请使用 prepare_produce：在自动模式下它会在预览通过后继续提交。",
        }, None
    return {"error": f"未知工具 {name}"}, None


CompleteFn = Callable[[list[dict[str, Any]], list[dict[str, Any]] | None], dict[str, Any]]


def build_system(
    *,
    title: str,
    episode: str,
    stage: str,
    skills: list[dict[str, str]],
    docs: list[dict[str, Any]],
    plan_step: dict[str, str] | None,
    allow_chain: bool,
    produce_mode: str = "gate",
    session_produce_auth: bool = False,
    allow_config: bool = False,
) -> str:
    catalog = "\n".join(
        f"- {item['name']}: {item['description'][:180]}" for item in skills
    ) or "（技能目录为空）"
    existing = "、".join(item["name"] for item in docs) or "尚无文档"
    step = ""
    if plan_step:
        step = (
            f"本轮只执行技能 {plan_step.get('skill')}（{plan_step.get('title')}）。"
            "读对应 SKILL.md 后写入那一份文档。"
        )
    chain = (
        "用户允许本会话连续写文档；连续写文档不等于自动生产。"
        if allow_chain
        else "多步任务先 propose_plan，等用户执行下一步；不要一次写完所有文档。"
    )
    if produce_mode == "always" or (produce_mode == "session" and session_produce_auth):
        produce_rule = (
            "生产模式允许自动提交：用户点名出图/出视频时用 prepare_produce；"
            "服务端会在环境检查通过后自动 confirm+run。缺配置时先 check_produce_environment 并说明去设置补齐。"
        )
    elif produce_mode == "session":
        produce_rule = (
            "生产模式为会话授权，但本会话尚未授权：生产只许 prepare_produce 预览，"
            "并提示用户勾选「本会话允许小烧自动生产」或去「生产」页确认。"
        )
    else:
        produce_rule = (
            "生产模式为确认闸门：只许 prepare_produce 预览；禁止自行 confirm/run；"
            "明确提示用户到「生产」页确认。"
        )
    config_rule = (
        "允许改本机配置：可 propose_studio_config 展示预览，用户口头确认后再 apply_studio_config(token)。"
        if allow_config
        else "禁止改本机配置；不要调用 propose/apply_studio_config。"
    )
    return f"""你是燃烧工作室的技能路由器（小烧助手），不是空谈助手。
创作真相是每集五份 Markdown：剧本.md、视觉设定.md、分镜.md、图片提示词.md、视频提示词.md。不要改成 JSON。
当前项目：{title}
当前集：{episode or "无"}
当前阶段：{stage}
本集已有：{existing}
生产模式：{produce_mode}
{step}
{chain}
已安装技能：
{catalog}
规则：
1. 先 list_skills 或按用户点名 read_skill，再读写文件。
2. 用 write_project_file 把结果写入 剧集/{episode or "EP001"}/ 对应文件。
3. 不调用审查/生产，除非用户点名。{produce_rule}
4. {config_rule}
5. 结束时用中文说明调用了哪个技能、改了哪个文件、未决项和下一步。
6. 若模型不能用 tools，可输出 ```json {{"tool":"write_project_file","path":"...","content":"..."}} ``` 再继续。
"""


def trim_history(messages: list[dict[str, str]], limit: int = 12) -> list[dict[str, str]]:
    keep = [item for item in messages if item.get("role") in {"user", "assistant"}]
    return keep[-limit:]


def turn(
    *,
    root: Path,
    payload: dict[str, Any],
    complete: CompleteFn | None = None,
) -> Iterator[dict[str, Any]]:
    complete = complete or openai_complete
    skills_dir = payload.get("skillsDir") or ""
    skills = catalog_skills(skills_dir)
    episode = str(payload.get("episode") or "")
    stage = str(payload.get("stage") or "")
    title = str(payload.get("title") or "未命名短剧")
    allow_chain = bool(payload.get("allowChainWrites"))
    session_produce_auth = bool(payload.get("sessionProduceAuth"))
    try:
        studio_produce = _studio_produce()

        produce_mode = studio_produce.get_produce_mode()
        allow_config = studio_produce.allow_config_agent()
    except Exception:
        produce_mode = "gate"
        allow_config = False
    plan_step = payload.get("planStep")
    if not isinstance(plan_step, dict):
        plan_step = None
    history = trim_history(list(payload.get("messages") or []))
    user_text = next(
        (item["content"] for item in reversed(history) if item.get("role") == "user"),
        "",
    )
    yield {"type": "status", "text": "正在按技能库路由…"}
    yield {
        "type": "policy",
        "mode": produce_mode,
        "sessionProduceAuth": session_produce_auth,
        "allowConfigAgent": allow_config,
    }

    plan = infer_plan(user_text)
    if plan_step is None and len(plan) >= 2 and not allow_chain:
        yield {"type": "plan", "steps": plan, "allowChainWrites": False}
        yield {
            "type": "done",
            "text": "这是一个多步任务。已列出计划：先点「执行下一步」，或勾选「本会话连续写文档」后再发送。生产是否自动取决于设置中的生产模式。",
            "files": [],
            "skill": "",
        }
        return

    if plan_step is None and allow_chain and len(plan) >= 2:
        yield {"type": "plan", "steps": plan, "allowChainWrites": True}

    if isinstance(plan_step, dict) and plan_step.get("skill"):
        yield {"type": "skill", "name": str(plan_step["skill"])}

    docs = list_docs(root, episode)
    messages: list[dict[str, Any]] = [
        {
            "role": "system",
            "content": build_system(
                title=title,
                episode=episode,
                stage=stage,
                skills=skills,
                docs=docs,
                plan_step=plan_step,
                allow_chain=allow_chain,
                produce_mode=produce_mode,
                session_produce_auth=session_produce_auth,
                allow_config=allow_config,
            ),
        }
    ]
    messages.extend(history)

    files: list[str] = []
    skill_used = ""
    used_tools = 0
    final_text = ""

    for _round in range(8):
        yield {"type": "status", "text": f"模型回合 {_round + 1}…"}
        result = complete(messages, TOOLS)
        calls = result.get("tool_calls") or []
        content = str(result.get("content") or "")
        if not calls:
            calls = [
                {
                    "id": f"json-{index}",
                    "function": {
                        "name": action.get("tool"),
                        "arguments": json.dumps(
                            {k: v for k, v in action.items() if k != "tool"},
                            ensure_ascii=False,
                        ),
                    },
                }
                for index, action in enumerate(parse_json_actions(content))
            ]
        if not calls:
            final_text = content.strip()
            break
        messages.append(
            {
                "role": "assistant",
                "content": content or None,
                "tool_calls": calls,
            }
        )
        for call in calls:
            used_tools += 1
            fn = call.get("function") or {}
            name = str(fn.get("name") or "")
            args = _args(fn.get("arguments"))
            yield {"type": "status", "text": f"调用 {name}"}
            try:
                output, event = run_tool(
                    name,
                    args,
                    root=root,
                    skills=skills,
                    episode=episode,
                    allow_chain=allow_chain,
                    session_produce_auth=session_produce_auth,
                )
            except ValueError as exc:
                output, event = {"error": str(exc)}, None
            if event:
                yield event
                if event.get("type") == "skill":
                    skill_used = str(event.get("name") or skill_used)
                if event.get("type") == "file_write":
                    files.append(str(event.get("path") or ""))
                if event.get("type") == "plan" and not allow_chain:
                    yield {
                        "type": "done",
                        "text": "已列出多步计划。点「执行下一步」或勾选连续写文档。",
                        "files": files,
                        "skill": skill_used,
                    }
                    return
            messages.append(
                {
                    "role": "tool",
                    "tool_call_id": str(call.get("id") or name),
                    "content": json.dumps(output, ensure_ascii=False)[:20000],
                }
            )

    if not final_text:
        changed = "、".join(files) if files else "无文件改动"
        skill_line = skill_used or (plan_step or {}).get("skill") or "未点名技能"
        final_text = f"本轮技能：{skill_line}。改动：{changed}。"
    yield {
        "type": "delta",
        "text": final_text,
    }
    yield {
        "type": "done",
        "text": final_text,
        "files": files,
        "skill": skill_used,
        "tools": used_tools,
    }
