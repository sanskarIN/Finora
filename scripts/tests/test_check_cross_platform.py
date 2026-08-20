from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock


SCRIPT_PATH = Path(__file__).resolve().parents[1] / "check_cross_platform.py"
SPEC = importlib.util.spec_from_file_location("check_cross_platform", SCRIPT_PATH)
assert SPEC is not None and SPEC.loader is not None
checker = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = checker
SPEC.loader.exec_module(checker)


class CrossPlatformContractTests(unittest.TestCase):
    def test_declared_targets_match_cross_platform_contract(self) -> None:
        self.assertEqual(
            "net10.0",
            checker.project_target_framework("src/Finora.Universal/Finora.Universal.csproj"),
        )
        self.assertEqual(
            "net10.0",
            checker.project_target_framework(
                "src/Finora.Universal.Desktop/Finora.Universal.Desktop.csproj"
            ),
        )
        self.assertEqual(
            "net10.0-browser",
            checker.project_target_framework(
                "src/Finora.Universal.Browser/Finora.Universal.Browser.csproj"
            ),
        )

    def test_avalonia_packages_are_centrally_pinned(self) -> None:
        versions = checker.package_versions()
        for package in checker.AVALONIA_PACKAGES:
            self.assertEqual(checker.AVALONIA_VERSION, versions.get(package))

    def test_current_repository_contract_passes(self) -> None:
        self.assertEqual([], checker.validate())

    def test_missing_required_path_is_reported_without_follow_on_noise(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with mock.patch.object(checker, "ROOT", root):
                errors = checker.validate()

        self.assertEqual(len(checker.REQUIRED_PATHS), len(errors))
        self.assertTrue(all(item.startswith("missing required cross-platform path:") for item in errors))

    def test_browser_host_does_not_reference_native_infrastructure(self) -> None:
        browser_project = checker.read(
            "src/Finora.Universal.Browser/Finora.Universal.Browser.csproj"
        )
        self.assertNotIn("Finora.Infrastructure", browser_project)
        self.assertNotIn("SQLite", browser_project)

    def test_pwa_manifest_accepts_standard_relative_icon_path(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            manifest_path = root / "manifest.webmanifest"
            manifest_path.write_text(
                json.dumps(
                    {
                        "name": "Finora",
                        "display": "standalone",
                        "icons": [{"src": "./finora-icon.svg"}],
                    }
                ),
                encoding="utf-8",
            )
            with mock.patch.object(checker, "ROOT", root):
                errors = checker.validate_pwa_manifest("manifest.webmanifest")

        self.assertEqual([], errors)

    def test_pwa_manifest_rejects_missing_finora_icon(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            manifest_path = root / "manifest.webmanifest"
            manifest_path.write_text(
                json.dumps(
                    {
                        "name": "Finora",
                        "display": "standalone",
                        "icons": [{"src": "./other.svg"}],
                    }
                ),
                encoding="utf-8",
            )
            with mock.patch.object(checker, "ROOT", root):
                errors = checker.validate_pwa_manifest("manifest.webmanifest")

        self.assertIn("PWA manifest must reference finora-icon.svg", errors)

    def test_web_docs_preserve_persistence_boundary(self) -> None:
        web_docs = checker.read("docs/platforms/WEB.md")
        self.assertIn("Finance persistence is intentionally disabled", web_docs)


if __name__ == "__main__":
    unittest.main()
