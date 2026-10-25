import importlib.util
import json
import os
import tempfile
import unittest
from pathlib import Path
from unittest import mock

SUITE = Path(__file__).resolve().parents[1]
SCRIPT = SUITE / "skills/short-drama/scripts/studio_agent.py"
SPEC = importlib.util.spec_from_file_location("studio_agent", SCRIPT)
assert SPEC and SPEC.loader
agent = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(agent)


class StudioAgentTests(unittest.TestCase):
    def test_catalog_reads_frontmatter(self) -> None:
        items = agent.catalog_skills(SUITE / "skills")
        names = {item["name"] for item in items}
        self.assertIn("short-drama", names)
        self.assertIn("short-drama-write", names)
        write = next(item for item in items if item["name"] == "short-drama-write")
        self.assertTrue(write["description"])

    def test_infer_plan_multi_step_and_skill_hint(self) -> None:
        plan = agent.infer_plan("按这集剧本补视觉设定再出分镜")
        skills = [step["skill"] for step in plan]
        self.assertEqual(skills, ["short-drama-assets", "short-drama-storyboard"])
        hinted = agent.infer_plan("[skill:short-drama-write]\n写第一集")
        self.assertEqual(hinted[0]["skill"], "short-drama-write")

    def test_clamp_rejects_escape_and_json(self) -> None:
        with self.assertRaises(ValueError):
            agent.clamp_relative("../secret.md")
        with self.assertRaises(ValueError):
            agent.clamp_relative("short-drama.json")
        self.assertEqual(agent.clamp_relative("剧集/EP001/剧本.md"), "剧集/EP001/剧本.md")

    def test_upsert_and_list_docs(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            saved = agent.upsert_text(root, "剧集/EP001/剧本.md", "# 第一场\n")
            self.assertEqual(saved["path"], "剧集/EP001/剧本.md")
            docs = agent.list_docs(root, "EP001")
            self.assertEqual(docs[0]["name"], "剧本.md")

    def test_forbidden_tools(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            home = Path(tmp) / "mochang"
            home.mkdir()
            with mock.patch.dict(os.environ, {"MOCHANG_HOME": str(home)}):
                output, event = agent.run_tool(
                    "run_produce",
                    {},
                    root=root,
                    skills=[],
                    episode="EP001",
                    allow_chain=False,
                    session_produce_auth=False,
                )
            self.assertIsNone(event)
            self.assertIn("禁止", output["error"])

    def test_config_agent_blocked_by_default(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            home = Path(tmp) / "mochang"
            home.mkdir()
            with mock.patch.dict(os.environ, {"MOCHANG_HOME": str(home)}):
                output, event = agent.run_tool(
                    "propose_studio_config",
                    {"patch": {"chat_base_url": "http://127.0.0.1:11434/v1"}},
                    root=root,
                    skills=[],
                    episode="EP001",
                    allow_chain=False,
                )
            self.assertIsNone(event)
            self.assertIn("未允许", output["error"])

    def test_turn_writes_file_with_fake_model(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            skills = SUITE / "skills"

            def complete(messages, tools):
                last = messages[-1]
                if last.get("role") != "tool":
                    return {
                        "content": "",
                        "tool_calls": [
                            {
                                "id": "1",
                                "function": {
                                    "name": "read_skill",
                                    "arguments": json.dumps(
                                        {"name": "short-drama-write"}
                                    ),
                                },
                            },
                            {
                                "id": "2",
                                "function": {
                                    "name": "write_project_file",
                                    "arguments": json.dumps(
                                        {
                                            "path": "剧集/EP001/剧本.md",
                                            "content": "# 开场\n角色上。\n",
                                        },
                                        ensure_ascii=False,
                                    ),
                                },
                            },
                        ],
                    }
                return {"content": "已调用 short-drama-write，写入剧本.md。", "tool_calls": []}

            events = list(
                agent.turn(
                    root=root,
                    payload={
                        "title": "测试",
                        "episode": "EP001",
                        "stage": "script",
                        "skillsDir": str(skills),
                        "messages": [
                            {"role": "user", "content": "请为 EP001 写剧本.md"}
                        ],
                    },
                    complete=complete,
                )
            )
            kinds = [event["type"] for event in events]
            self.assertIn("skill", kinds)
            self.assertIn("file_write", kinds)
            self.assertEqual(events[-1]["type"], "done")
            self.assertTrue((root / "剧集/EP001/剧本.md").is_file())

    def test_turn_stops_on_multi_plan(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)

            def complete(messages, tools):
                self.fail("model should not run before plan confirm")

            events = list(
                agent.turn(
                    root=root,
                    payload={
                        "episode": "EP001",
                        "skillsDir": str(SUITE / "skills"),
                        "allowChainWrites": False,
                        "messages": [
                            {
                                "role": "user",
                                "content": "按这集剧本补视觉设定再出分镜",
                            }
                        ],
                    },
                    complete=complete,
                )
            )
            kinds = [event["type"] for event in events]
            self.assertIn("plan", kinds)
            plan = next(event for event in events if event["type"] == "plan")
            self.assertEqual(len(plan["steps"]), 2)
            self.assertEqual(events[-1]["type"], "done")


if __name__ == "__main__":
    unittest.main()
