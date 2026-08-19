from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT_PATH = Path(__file__).resolve().parents[1] / "android_ui_smoke.py"
SPEC = importlib.util.spec_from_file_location("android_ui_smoke", SCRIPT_PATH)
assert SPEC is not None and SPEC.loader is not None
smoke = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = smoke
SPEC.loader.exec_module(smoke)

SAMPLE_XML = """<?xml version='1.0' encoding='UTF-8' standalone='yes' ?>
<hierarchy rotation="0">
  <node index="0" text="" resource-id="" class="android.widget.FrameLayout" package="example" content-desc="" clickable="false" enabled="true" bounds="[0,0][1080,2400]">
    <node index="0" text="Dashboard" resource-id="example:id/title" class="android.widget.TextView" package="example" content-desc="Dashboard heading" clickable="false" enabled="true" bounds="[24,80][500,160]" />
    <node index="1" text="Add transaction" resource-id="example:id/add" class="android.widget.Button" package="example" content-desc="Create a transaction" clickable="true" enabled="true" bounds="[40,1900][1040,2020]" />
  </node>
</hierarchy>
"""


class AndroidUiSmokeTests(unittest.TestCase):
    def test_parse_bounds_accepts_valid_android_bounds(self) -> None:
        self.assertEqual((1, 2, 300, 400), smoke.parse_bounds("[1,2][300,400]"))
        self.assertIsNone(smoke.parse_bounds("invalid"))
        self.assertIsNone(smoke.parse_bounds("[10,20][5,15]"))

    def test_parse_hierarchy_extracts_accessibility_fields(self) -> None:
        nodes = smoke.parse_hierarchy(SAMPLE_XML)

        self.assertEqual(3, len(nodes))
        button = next(node for node in nodes if node.text == "Add transaction")
        self.assertEqual("Create a transaction", button.description)
        self.assertEqual("example:id/add", button.resource_id)
        self.assertTrue(button.clickable)
        self.assertTrue(button.enabled)
        self.assertEqual((40, 1900, 1040, 2020), button.bounds)

    def test_expected_text_description_and_id_pass(self) -> None:
        nodes = smoke.parse_hierarchy(SAMPLE_XML)

        errors = smoke.validate_nodes(
            nodes,
            expected_text=["dashboard", "ADD TRANSACTION"],
            expected_descriptions=["create a transaction"],
            expected_ids=["id/add"],
            forbidden_text=[],
        )

        self.assertEqual([], errors)

    def test_missing_and_forbidden_values_fail_without_echoing_full_hierarchy(self) -> None:
        nodes = smoke.parse_hierarchy(SAMPLE_XML)

        errors = smoke.validate_nodes(
            nodes,
            expected_text=["Reports"],
            expected_descriptions=[],
            expected_ids=[],
            forbidden_text=["dashboard"],
        )

        self.assertEqual(2, len(errors))
        self.assertTrue(any("expected visible text" in error for error in errors))
        self.assertTrue(any("forbidden UI/accessibility text" in error for error in errors))
        self.assertFalse(any("example:id/title" in error for error in errors))

    def test_report_contains_only_summary_and_requested_failure_messages(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "report.json"
            smoke.write_report(path, node_count=42, errors=["expected visible text not found: 'Reports'"])

            payload = json.loads(path.read_text(encoding="utf-8"))

            self.assertFalse(payload["passed"])
            self.assertEqual(42, payload["nodeCount"])
            self.assertEqual(1, payload["errorCount"])
            self.assertNotIn("nodes", payload)
            self.assertNotIn("hierarchy", payload)


if __name__ == "__main__":
    unittest.main()
