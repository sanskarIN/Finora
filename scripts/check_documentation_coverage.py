#!/usr/bin/env python3
"""Verify that Finora's repository file reference covers every tracked file.

The canonical inventory lives in docs/development/REPOSITORY_FILE_REFERENCE.md.
The first cell of each inventory table row is either an exact tracked file path
or a granular directory prefix ending in `/`. Directory prefixes deliberately
must contain at least two path components, preventing broad declarations such as
`src/` or `docs/` from making the coverage check meaningless.

The check is dependency-free and uses `git ls-files`, so ignored or untracked
local files never become part of the public documentation contract.
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


def documented_entries(markdown: str) -> list[str]:
    """Extract exact paths and granular directory prefixes from inventory tables."""
    return normalize_paths(TABLE_PATH_PATTERN.findall(markdown))


def validate_entries(entries: Sequence[str]) -> list[str]:
    """Return invalid coverage declarations.

    Prefix declarations must be narrow enough to identify a concrete repository
    area (for example `docs/security/` or `src/Finora.Domain/`). Top-level
    prefixes such as `docs/`, `src/`, `tests/`, or `.github/` are rejected.
    """
    invalid: list[str] = []
    for entry in normalize_paths(entries):
        if not entry.endswith("/"):
            continue
        components = [component for component in entry.rstrip("/").split("/") if component]
        if len(components) < 2:
            invalid.append(entry)
    return invalid


def entry_covers(entry: str, path: str) -> bool:
    if entry.endswith("/"):
        return path.startswith(entry)
    return path == entry


def compare_coverage(
    tracked: Sequence[str], documented: Sequence[str]
) -> tuple[list[str], list[str]]:
    """Return (missing_documentation, stale_or_unused_entries)."""
    tracked_paths = normalize_paths(tracked)
    entries = normalize_paths(documented)

    missing = [
        path for path in tracked_paths if not any(entry_covers(entry, path) for entry in entries)
    ]
    stale = [
        entry for entry in entries if not any(entry_covers(entry, path) for path in tracked_paths)
    ]
    return missing, stale


def render_failure(
    missing: Sequence[str], stale: Sequence[str], invalid: Sequence[str] = ()
) -> str:
    lines = ["Finora repository documentation coverage failed."]
    if invalid:
        lines.append("\nInvalid broad directory coverage entries:")
        lines.extend(f"  - {path}" for path in invalid)
    if missing:
        lines.append("\nTracked files missing from the reference:")
        lines.extend(f"  - {path}" for path in missing)
    if stale:
        lines.append("\nReference entries that cover no tracked file:")
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


def display_reference_path(reference: Path) -> str:
    """Return a stable display path for repository-local or external references."""
    try:
        return reference.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return str(reference)


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
    entries = documented_entries(markdown)
    invalid = validate_entries(entries)
    missing, stale = compare_coverage(tracked, entries)

    if args.list_missing:
        for path in missing:
            print(path)
        return 1 if missing or stale or invalid else 0

    if missing or stale or invalid:
        print(render_failure(missing, stale, invalid), file=sys.stderr)
        return 1

    print(
        f"Documentation coverage OK: {len(tracked)} tracked files are covered by "
        f"{len(entries)} reference entries in "
        f"{display_reference_path(reference)}."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
