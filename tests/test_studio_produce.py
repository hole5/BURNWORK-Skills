"""Studio produce settings: save adapters, activate lanes, keep secrets out of adapters.json."""

from __future__ import annotations

import importlib.util
import json
import os
import tempfile
import unittest
from pathlib import Path
from unittest import mock

SUITE = Path(__file__).resolve().parents[1]
SCRIPT = SUITE / "skills/short-drama/scripts/studio_produce.py"
SPEC = importlib.util.spec_from_file_location("studio_produce_settings", SCRIPT)
assert SPEC and SPEC.loader
produce = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(produce)


class ProduceSettings(unittest.TestCase):
    def setUp(self) -> None:
        self.tmp = tempfile.TemporaryDirectory()
        self.home = Path(self.tmp.name)
        self.env = mock.patch.dict(os.environ, {"MOCHANG_HOME": str(self.home)})
        self.env.start()

    def tearDown(self) -> None:
        self.env.stop()
        self.tmp.cleanup()

    def test_save_comfyui_merges_and_activate_needs_workflow(self) -> None:
        other = {"adapters": {"keep-me": {"command": ["echo"]}}}
        (self.home / "adapters.json").write_text(json.dumps(other), encoding="utf-8")
        wf = self.home / "image.json"
        wf.write_text("{}", encoding="utf-8")
        with self.assertRaises(ValueError):
            produce.activate("image", "comfyui-image")
        status = produce.save_comfyui(
            base_url="http://127.0.0.1:8188",
            image_workflow=str(wf),
            video_workflow="",
        )
        adapters = json.loads((self.home / "adapters.json").read_text(encoding="utf-8"))["adapters"]
        self.assertIn("keep-me", adapters)
        self.assertIn("comfyui-image", adapters)
        self.assertEqual(status["comfyui"]["image_workflow"], str(wf))
        started = produce.activate("image", "comfyui-image")
        self.assertEqual(started["active_image"], "comfyui-image")
        with self.assertRaises(ValueError):
            produce.activate("video", "comfyui-video")

    def test_cloud_image_keeps_key_out_of_adapters(self) -> None:
        produce.save_cloud_image(api_key="sk-test", base_url="https://api.openai.com/v1")
        adapters = json.loads((self.home / "adapters.json").read_text(encoding="utf-8"))
        blob = json.dumps(adapters)
        self.assertNotIn("sk-test", blob)
        secrets = json.loads((self.home / "produce-secrets.json").read_text(encoding="utf-8"))
        self.assertEqual(secrets["OPENAI_API_KEY"], "sk-test")
        started = produce.activate("image", "gpt-image-2")
        self.assertEqual(started["active_image"], "gpt-image-2")
        produce.save_cloud_image(api_key="", base_url="")
        secrets = json.loads((self.home / "produce-secrets.json").read_text(encoding="utf-8"))
        self.assertEqual(secrets["OPENAI_API_KEY"], "sk-test")

    def test_apply_secrets_does_not_override_existing_env(self) -> None:
        (self.home / "produce-secrets.json").write_text(
            json.dumps({"OPENAI_API_KEY": "from-file"}), encoding="utf-8"
        )
        os.environ["OPENAI_API_KEY"] = "from-env"
        produce.apply_secrets_to_environ()
        self.assertEqual(os.environ["OPENAI_API_KEY"], "from-env")
        del os.environ["OPENAI_API_KEY"]
        produce.apply_secrets_to_environ()
        self.assertEqual(os.environ["OPENAI_API_KEY"], "from-file")

    def test_produce_mode_and_auto_gate(self) -> None:
        self.assertEqual(produce.get_produce_mode(), "gate")
        self.assertFalse(produce.can_auto_produce(session_authorized=True))
        produce.set_produce_mode("session")
        self.assertTrue(produce.can_auto_produce(session_authorized=True))
        self.assertFalse(produce.can_auto_produce(session_authorized=False))
        produce.set_produce_mode("always")
        self.assertTrue(produce.can_auto_produce(session_authorized=False))
        produce.set_allow_config_agent(True)
        status = produce.load_produce_status()
        self.assertEqual(status["mode"], "always")
        self.assertTrue(status["allow_config_agent"])
        env = produce.check_environment(modality="image")
        self.assertFalse(env["ok"])
        self.assertTrue(any("出图" in item for item in env["issues"]))


if __name__ == "__main__":
    unittest.main()
