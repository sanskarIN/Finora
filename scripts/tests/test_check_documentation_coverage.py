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
    def test_documented_entries_read_only_first_table_column(self) -> None:
        markdown = """
| File or area | Purpose |
|---|---|
| `README.md` | overview |
| `src/Finora.Domain/` | domain files |

Inline `not-a-table-entry.md` is intentionally ignored.
"""
        self.assertEqual(
            ["README.md", "src/Finora.Domain/"],
            coverage.documented_entries(markdown),
        )

    def test_normalize_paths_deduplicates_and_preserves_dotfiles(self) -> None:
        self.assertEqual(
            [".env", ".github/workflows/ci.yml", "src/App.cs"],
            coverage.normalize_paths(
                ["./.env", ".env", ".github\\workflows\\ci.yml", "src/App.cs"]
            ),
        )

    def test_directory_entry_covers_every_tracked_file_below_it(self) -> None:
        missing, stale = coverage.compare_coverage(
            [
                "README.md",
                "src/Finora.Domain/DomainRules.cs",
                "src/Finora.Domain/Money.cs",
            ],
            ["README.md", "src/Finora.Domain/"],
        )
        self.assertEqual([], missing)
        self.assertEqual([], stale)

    def test_compare_coverage_reports_missing_and_unused_entries(self) -> None:
        missing, stale = coverage.compare_coverage(
            ["README.md", "src/App.cs"],
            ["README.md", "docs/legacy/"],
        )
        self.assertEqual(["src/App.cs"], missing)
        self.assertEqual(["docs/legacy/"], stale)

    def test_validate_entries_rejects_broad_top_level_prefixes(self) -> None:
        self.assertEqual(
            ["docs/", "src/"],
            coverage.validate_entries(
                ["README.md", "docs/", "src/", "src/Finora.Domain/"]
            ),
        )

    def test_validate_entries_accepts_granular_prefixes(self) -> None:
        self.assertEqual(
            [],
            coverage.validate_entries(
                [
                    "README.md",
                    ".github/workflows/",
                    "docs/security/",
                    "src/Finora.Domain/",
                    "tests/Finora.UnitTests/",
                ]
            ),
        )

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

    def test_reference_files_adds_default_companion_inventory(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            canonical = root / "reference.md"
            companion = root / "cross-platform.md"
            canonical.write_text("canonical", encoding="utf-8")
            companion.write_text("companion", encoding="utf-8")

            with mock.patch.object(coverage, "DEFAULT_REFERENCE", canonical), mock.patch.object(
                coverage, "DEFAULT_COMPANION_REFERENCES", (companion,)
            ):
                self.assertEqual(
                    [canonical, companion],
                    coverage.reference_files(canonical),
                )

    def test_reference_files_does_not_extend_explicit_custom_inventory(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            canonical = root / "canonical.md"
            custom = root / "custom.md"
            companion = root / "cross-platform.md"
            canonical.write_text("canonical", encoding="utf-8")
            custom.write_text("custom", encoding="utf-8")
            companion.write_text("companion", encoding="utf-8")

            with mock.patch.object(coverage, "DEFAULT_REFERENCE", canonical), mock.patch.object(
                coverage, "DEFAULT_COMPANION_REFERENCES", (companion,)
            ):
                self.assertEqual([custom], coverage.reference_files(custom))

    def test_main_succeeds_for_exact_and_directory_inventory(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            reference = Path(directory) / "reference.md"
            reference.write_text(
                "| File or area | Purpose |\n"
                "|---|---|\n"
                "| `README.md` | overview |\n"
                "| `src/Finora.Domain/` | domain |\n",
                encoding="utf-8",
            )
            with mock.patch.object(
                sys,
                "argv",
                ["check_documentation_coverage.py", "--reference", str(reference)],
            ), mock.patch.object(
                coverage,
                "tracked_files",
                return_value=["README.md", "src/Finora.Domain/Money.cs"],
            ):
                self.assertEqual(0, coverage.main())

    def test_main_fails_when_a_tracked_file_is_missing(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            reference = Path(directory) / "reference.md"
            reference.write_text(
                "| File or area | Purpose |\n|---|---|\n| `README.md` | overview |\n",
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

    def test_list_missing_mode_fails_when_only_stale_entries_exist(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            reference = Path(directory) / "reference.md"
            reference.write_text(
                "| File or area | Purpose |\n"
                "|---|---|\n"
                "| `README.md` | overview |\n"
                "| `docs/legacy/` | stale |\n",
                encoding="utf-8",
            )
            with mock.patch.object(
                sys,
                "argv",
                [
                    "check_documentation_coverage.py",
                    "--reference",
                    str(reference),
                    "--list-missing",
                ],
            ), mock.patch.object(
                coverage,
                "tracked_files",
                return_value=["README.md"],
            ):
                self.assertEqual(1, coverage.main())


if __name__ == "__main__":
    unittest.main()
