#!/usr/bin/env python3
"""Verify that Finora's repository file reference covers every tracked file.

The canonical inventory lives in docs/development/REPOSITORY_FILE_REFERENCE.md.
Every tracked path must appear as the first cell of a Markdown table row. The
check is intentionally dependency-free and uses `git ls-files`, so ignored or
untracked local files never become part of the public documentation contract.
"""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path
from typing import Iterable, Sequence

REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_REFERENCE = REPO_ROOT / "docs" / "development" / "REPOSITORY_FILE_REFERENCE.md"
TABLE_PATH_PATTERN = re.compile(r"^\|\s*`([^`]+)`\s*\|", re.MULTILINE)


def normalize_paths(paths: Iterable[str]) -> list[str]:
    """Return sorted, unique, repository-relative POSIX paths."""
    normalized: set[str] = set()
    for raw in paths:
        value = raw.strip().replace("\\", "/")
        while value.startswith("./"):
            value = value[2:]
        if value:
            normalized.add(value)
    return sorted(normalized)


def tracked_files(repo_root: Path = REPO_ROOT) -> list[str]:
    """Read the exact tracked-file set from Git without scanning ignored files."""
    completed = subprocess.run(
        ["git", "ls-files", "-z"],
        cwd=repo_root,
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if completed.returncode != 0:
        message = completed.stderr.decode("utf-8", errors="replace").strip()
        raise RuntimeError(f"git ls-files failed: {message or completed.returncode}")

    raw_paths = completed.stdout.decode("utf-8").split("\0")
    return normalize_paths(raw_paths)


def documented_files(markdown: str) -> list[str]:
    """Extract literal file paths from the first column of inventory tables."""
    return normalize_paths(TABLE_PATH_PATTERN.findall(markdown))


def compare_coverage(
    tracked: Sequence[str], documented: Sequence[str]
) -> tuple[list[str], list[str]]:
    """Return (missing_documentation, stale_documentation)."""
    tracked_set = set(normalize_paths(tracked))
    documented_set = set(normalize_paths(documented))
    return (
        sorted(tracked_set - documented_set),
        sorted(documented_set - tracked_set),
    )


def render_failure(missing: Sequence[str], stale: Sequence[str]) -> str:
    lines = ["Finora repository documentation coverage failed."]
    if missing:
        lines.append("\nTracked files missing from the reference:")
        lines.extend(f"  - {path}" for path in missing)
    if stale:
        lines.append("\nReference entries that are no longer tracked:")
        lines.extend(f"  - {path}" for path in stale)
    lines.append(
        "\nUpdate docs/development/REPOSITORY_FILE_REFERENCE.md in the same change."
    )
    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Check that every tracked Finora file is documented."
    )
    parser.add_argument(
        "--reference",
        type=Path,
        default=DEFAULT_REFERENCE,
        help="Markdown inventory to validate.",
    )
    parser.add_argument(
        "--list-missing",
        action="store_true",
        help="Print only missing tracked paths, one per line.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    reference = args.reference
    if not reference.is_absolute():
        reference = REPO_ROOT / reference

    if not reference.is_file():
        print(f"Documentation reference is missing: {reference}", file=sys.stderr)
        return 2

    try:
        tracked = tracked_files(REPO_ROOT)
    except RuntimeError as exc:
        print(str(exc), file=sys.stderr)
        return 2

    markdown = reference.read_text(encoding="utf-8-sig")
    documented = documented_files(markdown)
    missing, stale = compare_coverage(tracked, documented)

    if args.list_missing:
        for path in missing:
            print(path)
        return 1 if missing else 0

    if missing or stale:
        print(render_failure(missing, stale), file=sys.stderr)
        return 1

    print(
        f"Documentation coverage OK: {len(tracked)} tracked files are represented "
        f"in {reference.relative_to(REPO_ROOT).as_posix()}."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
