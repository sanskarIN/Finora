from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock


SCRIPT_PATH = Path(__file__).resolve().parents[1] / "check_release_readiness.py"
SPEC = importlib.util.spec_from_file_location("check_release_readiness", SCRIPT_PATH)
assert SPEC is not None and SPEC.loader is not None
checker = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = checker
SPEC.loader.exec_module(checker)


def create_required_tree(root: Path) -> None:
    for relative in (*checker.REQUIRED_FILES, *checker.REQUIRED_WORKFLOWS):
        path = root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(
            "This is a deterministic test fixture with enough content for release-ledger checks.\n",
            encoding="utf-8",
        )


class ReleaseReadinessGuardTests(unittest.TestCase):
    def test_complete_fixture_passes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            create_required_tree(root)
            (root / "src").mkdir()
            (root / "src" / "Program.cs").write_text("class Program {}\n", encoding="utf-8")
            with mock.patch.object(
                checker,
                "git_tracked_files",
                return_value=[
                    *checker.REQUIRED_FILES,
                    *checker.REQUIRED_WORKFLOWS,
                    "src/Program.cs",
                ],
            ):
                report = checker.check_release_readiness(root)

        self.assertTrue(report.passed)
        self.assertEqual((), report.findings)

    def test_governance_contract_requires_sdk_funding_security_and_performance(self) -> None:
        for path in (
            "global.json",
            "Directory.Build.props",
            "docs/FINAL_REPOSITORY_CLOSURE.md",
            "scripts/README.md",
            ".github/FUNDING.yml",
        ):
            self.assertIn(path, checker.REQUIRED_FILES)

        for path in (
            ".github/workflows/codeql.yml",
            ".github/workflows/dependency-review.yml",
            ".github/workflows/performance.yml",
            ".github/workflows/release-readiness.yml",
        ):
            self.assertIn(path, checker.REQUIRED_WORKFLOWS)

    def test_action_major_policy_rejects_outdated_runtime_actions(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            create_required_tree(root)
            workflow = root / ".github/workflows/ci.yml"
            workflow.write_text(
                "steps:\n  - uses: actions/checkout@v4\n  - uses: actions/setup-python@v5\n",
                encoding="utf-8",
            )
            with mock.patch.object(checker, "git_tracked_files", return_value=[]):
                report = checker.check_release_readiness(root)

        outdated = [item for item in report.findings if item.code == "outdated_action_major"]
        self.assertEqual(2, len(outdated))
        self.assertTrue(any("actions/checkout@v4" in item.message for item in outdated))
        self.assertTrue(any("actions/setup-python@v5" in item.message for item in outdated))

    def test_current_action_majors_are_accepted(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            create_required_tree(root)
            workflow = root / ".github/workflows/ci.yml"
            workflow.write_text(
                "steps:\n"
                "  - uses: actions/checkout@v7\n"
                "  - uses: actions/setup-python@v7\n"
                "  - uses: actions/setup-dotnet@v6\n"
                "  - uses: actions/upload-artifact@v7\n",
                encoding="utf-8",
            )
            with mock.patch.object(checker, "git_tracked_files", return_value=[]):
                report = checker.check_release_readiness(root)

        self.assertFalse(any(item.code == "outdated_action_major" for item in report.findings))

    def test_missing_required_file_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            create_required_tree(root)
            (root / "SECURITY.md").unlink()
            with mock.patch.object(checker, "git_tracked_files", return_value=[]):
                report = checker.check_release_readiness(root)

        self.assertFalse(report.passed)
        self.assertTrue(
            any(
                item.code == "missing_required_file" and item.path == "SECURITY.md"
                for item in report.findings
            )
        )

    def test_forbidden_secret_signing_database_and_artifact_paths_fail(self) -> None:
        paths = [
            ".env",
            "signing/release.pfx",
            "android/upload.keystore",
            "fixtures/private.db",
            "artifacts/generated.csv",
        ]
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            create_required_tree(root)
            with mock.patch.object(checker, "git_tracked_files", return_value=paths):
                report = checker.check_release_readiness(root)

        blocked = [item.path for item in report.findings if item.code == "forbidden_tracked_artifact"]
        self.assertEqual(paths, blocked)

    def test_normalize_preserves_dotfiles_and_removes_only_dot_slash_prefix(self) -> None:
        self.assertEqual(".env", checker.normalize(".env"))
        self.assertEqual(".env", checker.normalize("./.env"))
        self.assertEqual(".github/workflows/ci.yml", checker.normalize("./.github/workflows/ci.yml"))

    def test_conflict_marker_in_tracked_text_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            create_required_tree(root)
            source = root / "src" / "Feature.cs"
            source.parent.mkdir(parents=True)
            source.write_text("<<<<<<< HEAD\nclass A {}\n>>>>>>> other\n", encoding="utf-8")
            with mock.patch.object(checker, "git_tracked_files", return_value=["src/Feature.cs"]):
                report = checker.check_release_readiness(root)

        self.assertFalse(report.passed)
        self.assertTrue(any(item.code == "merge_conflict_marker" for item in report.findings))

    def test_generated_bin_and_obj_are_blocked_when_tracked(self) -> None:
        self.assertTrue(checker.matches_forbidden_file("src/App/bin/Release/app.dll"))
        self.assertTrue(checker.matches_forbidden_file("obj/project.assets.json"))
        self.assertTrue(checker.matches_forbidden_file("tools/check/artifacts/result.json"))

    def test_regular_source_and_document_paths_are_allowed(self) -> None:
        self.assertFalse(checker.matches_forbidden_file("src/Finora.App/App.xaml"))
        self.assertFalse(checker.matches_forbidden_file("docs/backup/BACKUP_VERIFICATION.md"))
        self.assertFalse(checker.matches_forbidden_file("src/Subject/Object.cs"))

    def test_small_change_ledger_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            create_required_tree(root)
            (root / "what_changed.md").write_text("tiny\n", encoding="utf-8")
            with mock.patch.object(checker, "git_tracked_files", return_value=[]):
                report = checker.check_release_readiness(root)

        self.assertFalse(report.passed)
        self.assertTrue(
            any(
                item.code == "release_ledger_too_small" and item.path == "what_changed.md"
                for item in report.findings
            )
        )


if __name__ == "__main__":
    unittest.main()
