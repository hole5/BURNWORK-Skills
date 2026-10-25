"""Studio paper-view fields: hook/cliff, line book, relations, quality gate."""

from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path

SUITE = Path(__file__).resolve().parents[1]
SCRIPT = SUITE / "skills/short-drama/scripts/creator_views.py"
SPEC = importlib.util.spec_from_file_location("creator_views_studio", SCRIPT)
assert SPEC and SPEC.loader
views = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(views)

PLAY = """# EP001 夜雨入城

开场钩：伞面先入画。

集末悬念：廊上只剩伞。

## EP001-SC001 外 · 城门雨廊 · 夜

雨打在油纸上。

门吏：名册上没有这个时辰的客。

苏牧：左边那扇。
"""


class StudioPaperFields(unittest.TestCase):
    def test_hook_cliff_and_line_book(self) -> None:
        parsed = views.parse_screenplay(PLAY)
        self.assertEqual(parsed["hook"], "伞面先入画。")
        self.assertEqual(parsed["cliff"], "廊上只剩伞。")
        self.assertEqual(parsed["lineCount"], 2)
        who = {row["who"]: row["n"] for row in parsed["lineBook"]}
        self.assertEqual(who["门吏"], 1)
        self.assertEqual(who["苏牧"], 1)
        metrics = views.metrics(parsed, None)
        self.assertEqual(metrics["scenes"], 1)
        self.assertEqual(metrics["lines"], 2)
        self.assertGreater(metrics["scriptEst"], 0)

    def test_relation_graph_and_gate(self) -> None:
        settings = {
            "items": [
                {
                    "cat": "人物",
                    "name": "苏牧",
                    "fields": {"关系": "沈昭（旧识）；阿宁（引路）"},
                }
            ]
        }
        graph = views.relation_graph(settings)
        self.assertIn("苏牧", graph["nodes"])
        self.assertIn("沈昭", graph["nodes"])
        self.assertIn("阿宁", graph["nodes"])
        pairs = {(e["from"], e["to"], e["label"]) for e in graph["edges"]}
        self.assertIn(("苏牧", "沈昭", "旧识"), pairs)
        self.assertIn(("苏牧", "阿宁", "引路"), pairs)
        inner = views.relation_graph({
            "items": [{
                "cat": "人物",
                "name": "苏牧",
                "fields": {"关系": "闻人紫寒（邻座，扣腕）；空姐（过道看一眼）"},
            }]
        })
        inner_pairs = {(e["from"], e["to"], e["label"]) for e in inner["edges"]}
        self.assertIn(("苏牧", "闻人紫寒", "邻座，扣腕"), inner_pairs)
        self.assertIn(("苏牧", "空姐", "过道看一眼"), inner_pairs)
        gate = views.quality_gate({"findings": [{"sev": "must"}, {"sev": "should"}]})
        self.assertFalse(gate["ok"])
        self.assertEqual(gate["must"], 1)
        self.assertTrue(views.quality_gate({"findings": [{"sev": "could"}]})["ok"])


if __name__ == "__main__":
    unittest.main()
