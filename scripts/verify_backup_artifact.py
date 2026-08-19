#!/usr/bin/env python3
"""Verify basic integrity properties of a Finora backup artifact without decrypting it."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from dataclasses import asdict, dataclass
from pathlib import Path

SQLITE_HEADER = b"SQLite format 3\x00"
SHA256_RE = re.compile(r"^[0-9a-fA-F]{64}$")
CHUNK_SIZE = 1024 * 1024


@dataclass(frozen=True)
class BackupDiagnostic:
    severity: str
    code: str
    message: str


@dataclass(frozen=True)
class BackupReport:
    passed: bool
    size_bytes: int
    sha256: str
    diagnostics: tuple[BackupDiagnostic, ...]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Verify a Finora backup artifact without decrypting or logging its contents."
    )
    parser.add_argument("backup_file", type=Path)
    parser.add_argument(
        "--min-size",
        type=int,
        default=128,
        help="Minimum plausible backup size in bytes (default: 128).",
    )
    parser.add_argument(
        "--expected-sha256",
        help="Optional recorded SHA-256 digest used to detect accidental corruption/copy changes.",
    )
    parser.add_argument(
        "--json",
        action="store_true",
        dest="json_output",
        help="Print a machine-readable report. Backup paths and contents are omitted.",
    )
    return parser.parse_args()


def inspect_backup(
    path: Path,
    *,
    min_size: int = 128,
    expected_sha256: str | None = None,
) -> BackupReport:
    diagnostics: list[BackupDiagnostic] = []

    if min_size < 1:
        raise ValueError("min_size must be at least 1")
    if expected_sha256 is not None and SHA256_RE.fullmatch(expected_sha256.strip()) is None:
        raise ValueError("expected_sha256 must contain exactly 64 hexadecimal characters")

    if not path.exists():
        diagnostics.append(
            BackupDiagnostic("error", "missing_file", "Backup artifact does not exist.")
        )
        return BackupReport(False, 0, "", tuple(diagnostics))
    if not path.is_file():
        diagnostics.append(
            BackupDiagnostic("error", "not_a_file", "Backup artifact path is not a regular file.")
        )
        return BackupReport(False, 0, "", tuple(diagnostics))

    digest = hashlib.sha256()
    size = 0
    first_bytes = b""
    has_nonzero_byte = False

    try:
        with path.open("rb") as stream:
            while True:
                chunk = stream.read(CHUNK_SIZE)
                if not chunk:
                    break
                if not first_bytes:
                    first_bytes = chunk[: max(len(SQLITE_HEADER), 32)]
                size += len(chunk)
                digest.update(chunk)
                if not has_nonzero_byte and any(byte != 0 for byte in chunk):
                    has_nonzero_byte = True
    except OSError as exc:
        diagnostics.append(
            BackupDiagnostic(
                "error",
                "read_error",
                f"Backup artifact could not be read safely: {type(exc).__name__}.",
            )
        )
        return BackupReport(False, 0, "", tuple(diagnostics))

    actual_sha256 = digest.hexdigest()

    if size < min_size:
        diagnostics.append(
            BackupDiagnostic(
                "error",
                "too_small",
                f"Backup artifact is smaller than the configured {min_size}-byte minimum.",
            )
        )
    if first_bytes.startswith(SQLITE_HEADER):
        diagnostics.append(
            BackupDiagnostic(
                "error",
                "plaintext_sqlite_header",
                "Backup begins with a plaintext SQLite database header; verify encryption/export behavior before sharing or storing it.",
            )
        )
    if size > 0 and not has_nonzero_byte:
        diagnostics.append(
            BackupDiagnostic(
                "error",
                "all_zero_content",
                "Backup artifact contains only zero bytes and is not a plausible usable backup.",
            )
        )

    if expected_sha256 is not None:
        expected = expected_sha256.strip().lower()
        if actual_sha256.lower() != expected:
            diagnostics.append(
                BackupDiagnostic(
                    "error",
                    "sha256_mismatch",
                    "Backup SHA-256 does not match the recorded digest.",
                )
            )

    passed = not any(item.severity == "error" for item in diagnostics)
    return BackupReport(passed, size, actual_sha256, tuple(diagnostics))


def report_payload(report: BackupReport) -> dict[str, object]:
    return {
        "passed": report.passed,
        "sizeBytes": report.size_bytes,
        "sha256": report.sha256,
        "diagnostics": [asdict(item) for item in report.diagnostics],
    }


def print_text(report: BackupReport) -> None:
    print(
        f"Finora backup verification: passed={str(report.passed).lower()}, "
        f"sizeBytes={report.size_bytes}, sha256={report.sha256 or 'unavailable'}"
    )
    for item in report.diagnostics:
        print(f"[{item.severity.upper()}] {item.code}: {item.message}")


def main() -> int:
    args = parse_args()
    try:
        report = inspect_backup(
            args.backup_file,
            min_size=args.min_size,
            expected_sha256=args.expected_sha256,
        )
    except ValueError as exc:
        print(str(exc), file=sys.stderr)
        return 2

    if args.json_output:
        print(json.dumps(report_payload(report), indent=2))
    else:
        print_text(report)
    return 0 if report.passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
