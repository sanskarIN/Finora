from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock


SCRIPT_PATH = Path(__file__).resolve().parents[1] / "check_documentation_coverage.py"
SPEC = importlib.util.spec_from_file_location("check_documentation_coverage", SCRIPT_PATH)
assert SPEC is not None and SPEC.loader is not None
coverage = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = coverage
SPEC.loader.exec_module(coverage)


class DocumentationCoverageTests(unittest.TestCase):
    def test_documented_files_reads_only_first_table_column_paths(self) -> None:
        markdown = """
| File | Purpose |
|---|---|
| `README.md` | overview |
| `src/App.cs` | source |

Inline `not-a-table-entry.md` is intentionally ignored.
"""
        self.assertEqual(
            ["README.md", "src/App.cs"],
            coverage.documented_files(markdown),
        )

    def test_normalize_paths_deduplicates_and_preserves_dotfiles(self) -> None:
        self.assertEqual(
            [".env", ".github/workflows/ci.yml", "src/App.cs"],
            coverage.normalize_paths(
                ["./.env", ".env", ".\\github\\workflows\\ci.yml", "src/App.cs"]
            ),
        )

    def test_compare_coverage_reports_missing_and_stale_paths(self) -> None:
        missing, stale = coverage.compare_coverage(
            ["README.md", "src/App.cs"],
            ["README.md", "docs/old.md"],
        )
        self.assertEqual(["src/App.cs"], missing)
        self.assertEqual(["docs/old.md"], stale)

    def test_tracked_files_uses_git_ls_files_null_delimited_output(self) -> None:
        completed = mock.Mock(
            returncode=0,
            stdout=b"README.md\0src/App.cs\0",
            stderr=b"",
        )
        with mock.patch.object(coverage.subprocess, "run", return_value=completed) as run:
            result = coverage.tracked_files(Path("/repo"))

        self.assertEqual(["README.md", "src/App.cs"], result)
        run.assert_called_once()
        command = run.call_args.args[0]
        self.assertEqual(["git", "ls-files", "-z"], command)
        self.assertEqual(Path("/repo"), run.call_args.kwargs["cwd"])

    def test_tracked_files_raises_when_git_fails(self) -> None:
        completed = mock.Mock(returncode=128, stdout=b"", stderr=b"fatal: not a repo")
        with mock.patch.object(coverage.subprocess, "run", return_value=completed):
            with self.assertRaisesRegex(RuntimeError, "not a repo"):
                coverage.tracked_files(Path("/repo"))

    def test_main_succeeds_for_exact_inventory(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            reference = Path(directory) / "reference.md"
            reference.write_text(
                "| File | Purpose |\n|---|---|\n| `README.md` | overview |\n",
                encoding="utf-8",
            )
            with mock.patch.object(
                sys,
                "argv",
                ["check_documentation_coverage.py", "--reference", str(reference)],
            ), mock.patch.object(
                coverage, "tracked_files", return_value=["README.md"]
            ):
                self.assertEqual(0, coverage.main())

    def test_main_fails_when_a_tracked_file_is_missing(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            reference = Path(directory) / "reference.md"
            reference.write_text(
                "| File | Purpose |\n|---|---|\n| `README.md` | overview |\n",
                encoding="utf-8",
            )
            with mock.patch.object(
                sys,
                "argv",
                ["check_documentation_coverage.py", "--reference", str(reference)],
            ), mock.patch.object(
                coverage,
                "tracked_files",
                return_value=["README.md", "src/App.cs"],
            ):
                self.assertEqual(1, coverage.main())


if __name__ == "__main__":
    unittest.main()
