#!/usr/bin/env python3
"""Check repository-level Finora release-readiness invariants.

The checker is intentionally structural. It does not claim native platform builds,
store signing, or device QA have passed.
"""

from __future__ import annotations

import argparse
import fnmatch
import json
import subprocess
import sys
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Sequence

REQUIRED_FILES = (
    "README.md",
    "LICENSE",
    "SECURITY.md",
    "PRIVACY.md",
    "TERMS.md",
    "CONTRIBUTING.md",
    "what_changed.md",
    "global.json",
    "Directory.Build.props",
    "docs/NEXT_STEPS.md",
    "docs/FINAL_REPOSITORY_CLOSURE.md",
    "docs/localization/LOCALIZATION_IMPLEMENTATION.md",
    "docs/accessibility/NATIVE_ACCESSIBILITY_QA.md",
    "docs/testing/NATIVE_UI_AUTOMATION.md",
    "docs/testing/SAMPLE_DATA.md",
    "docs/testing/REPOSITORY_QA.md",
    "docs/import/CSV_DIAGNOSTICS.md",
    "docs/export/EXPORT_VERIFICATION.md",
    "docs/backup/BACKUP_VERIFICATION.md",
    "scripts/README.md",
    ".github/FUNDING.yml",
    ".github/pull_request_template.md",
    ".github/ISSUE_TEMPLATE/bug_report.yml",
    ".github/ISSUE_TEMPLATE/feature_request.yml",
    ".github/CODEOWNERS",
)

REQUIRED_WORKFLOWS = (
    ".github/workflows/ci.yml",
    ".github/workflows/codeql.yml",
    ".github/workflows/dependency-review.yml",
    ".github/workflows/localization.yml",
    ".github/workflows/sample-data.yml",
    ".github/workflows/csv-diagnostics.yml",
    ".github/workflows/export-artifact.yml",
    ".github/workflows/backup-artifact.yml",
    ".github/workflows/native-ui-harness.yml",
    ".github/workflows/performance.yml",
    ".github/workflows/release-readiness.yml",
)

FORBIDDEN_TRACKED_PATTERNS = (
    ".env",
    ".env.*",
    "*.pfx",
    "*.p12",
    "*.keystore",
    "*.jks",
    "*.mobileprovision",
    "*.cer",
    "*.der",
    "*.key",
    "*.sqlite",
    "*.sqlite3",
    "*.db",
    "*.finora",
)

FORBIDDEN_TRACKED_PREFIXES = (
    "artifacts/",
    "bin/",
    "obj/",
)

TEXT_EXTENSIONS = {
    ".cs",
    ".csproj",
    ".json",
    ".md",
    ".props",
    ".ps1",
    ".py",
    ".resx",
    ".sln",
    ".targets",
    ".txt",
    ".xaml",
    ".xml",
    ".yaml",
    ".yml",
}

CONFLICT_MARKERS = ("<<<<<<< ", ">>>>>>> ", "||||||| ")


@dataclass(frozen=True)
class Finding:
    severity: str
    code: str
    path: str | None
    message: str


@dataclass(frozen=True)
class ReadinessReport:
    passed: bool
    tracked_file_count: int
    findings: tuple[Finding, ...]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Check structural Finora release-readiness invariants."
    )
    parser.add_argument("--root", type=Path, default=Path("."))
    parser.add_argument("--json", action="store_true", dest="json_output")
    return parser.parse_args()


def normalize(path: str) -> str:
    return path.replace("\\", "/").lstrip("./")


def git_tracked_files(root: Path) -> list[str] | None:
    try:
        result = subprocess.run(
            ["git", "-C", str(root), "ls-files", "-z"],
            check=True,
            capture_output=True,
            timeout=20,
        )
    except (FileNotFoundError, subprocess.CalledProcessError, subprocess.TimeoutExpired):
        return None
    return sorted(
        normalize(item.decode("utf-8", errors="strict"))
        for item in result.stdout.split(b"\0")
        if item
    )


def filesystem_files(root: Path) -> list[str]:
    ignored_dirs = {".git", ".vs", ".idea", "bin", "obj", "artifacts", "__pycache__"}
    files: list[str] = []
    for path in root.rglob("*"):
        if not path.is_file():
            continue
        relative = path.relative_to(root)
        if any(part in ignored_dirs for part in relative.parts[:-1]):
            continue
        files.append(normalize(relative.as_posix()))
    return sorted(files)


def tracked_files(root: Path) -> list[str]:
    return git_tracked_files(root) or filesystem_files(root)


def matches_forbidden_file(path: str) -> bool:
    normalized = normalize(path)
    basename = Path(normalized).name
    if any(normalized.casefold().startswith(prefix.casefold()) for prefix in FORBIDDEN_TRACKED_PREFIXES):
        return True
    return any(
        fnmatch.fnmatch(basename.casefold(), pattern.casefold())
        for pattern in FORBIDDEN_TRACKED_PATTERNS
    )


def should_scan_text(path: Path) -> bool:
    return path.suffix.casefold() in TEXT_EXTENSIONS or path.name in {
        ".editorconfig",
        ".gitignore",
    }


def check_required_files(root: Path, findings: list[Finding]) -> None:
    for relative in (*REQUIRED_FILES, *REQUIRED_WORKFLOWS):
        path = root / relative
        if not path.is_file():
            findings.append(
                Finding(
                    "error",
                    "missing_required_file",
                    relative,
                    "Required release/contributor/validation file is missing.",
                )
            )
            continue
        try:
            if path.stat().st_size == 0:
                findings.append(
                    Finding(
                        "error",
                        "empty_required_file",
                        relative,
                        "Required file is empty.",
                    )
                )
        except OSError:
            findings.append(
                Finding(
                    "error",
                    "required_file_unreadable",
                    relative,
                    "Required file could not be inspected.",
                )
            )


def check_tracked_paths(paths: Sequence[str], findings: list[Finding]) -> None:
    for relative in paths:
        if matches_forbidden_file(relative):
            findings.append(
                Finding(
                    "error",
                    "forbidden_tracked_artifact",
                    relative,
                    "Tracked path matches a secret/signing/database/generated-artifact pattern.",
                )
            )


def check_conflict_markers(root: Path, paths: Sequence[str], findings: list[Finding]) -> None:
    for relative in paths:
        path = root / relative
        if not should_scan_text(path):
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except (OSError, UnicodeError):
            continue
        for marker in CONFLICT_MARKERS:
            if marker in text:
                findings.append(
                    Finding(
                        "error",
                        "merge_conflict_marker",
                        relative,
                        "Tracked text contains an unresolved merge-conflict marker.",
                    )
                )
                break


def check_change_ledgers(root: Path, findings: list[Finding]) -> None:
    for relative in ("what_changed.md", "docs/NEXT_STEPS.md"):
        path = root / relative
        if not path.is_file():
            continue
        try:
            text = path.read_text(encoding="utf-8")
        except (OSError, UnicodeError):
            continue
        if len(text.strip()) < 50:
            findings.append(
                Finding(
                    "error",
                    "release_ledger_too_small",
                    relative,
                    "Project change/roadmap ledger is unexpectedly small.",
                )
            )


def check_release_readiness(root: Path) -> ReadinessReport:
    root = root.resolve()
    findings: list[Finding] = []
    paths = tracked_files(root)
    check_required_files(root, findings)
    check_tracked_paths(paths, findings)
    check_conflict_markers(root, paths, findings)
    check_change_ledgers(root, findings)
    return ReadinessReport(
        passed=not any(item.severity == "error" for item in findings),
        tracked_file_count=len(paths),
        findings=tuple(findings),
    )


def payload(report: ReadinessReport) -> dict[str, object]:
    return {
        "passed": report.passed,
        "trackedFileCount": report.tracked_file_count,
        "findingCount": len(report.findings),
        "findings": [asdict(item) for item in report.findings],
    }


def print_text(report: ReadinessReport) -> None:
    print(
        "Finora release-readiness guard: "
        f"passed={str(report.passed).lower()}, "
        f"trackedFiles={report.tracked_file_count}, findings={len(report.findings)}"
    )
    for item in report.findings:
        location = f" [{item.path}]" if item.path else ""
        print(f"[{item.severity.upper()}] {item.code}{location}: {item.message}")


def main() -> int:
    args = parse_args()
    report = check_release_readiness(args.root)
    if args.json_output:
        print(json.dumps(payload(report), indent=2))
    else:
        print_text(report)
    return 0 if report.passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
