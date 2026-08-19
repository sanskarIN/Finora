#!/usr/bin/env python3
"""Privacy-safe structural verification for Finora CSV and PDF exports."""

from __future__ import annotations

import argparse
import csv
import hashlib
import io
import json
import re
import sys
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Sequence

SHA256_RE = re.compile(r"^[0-9a-fA-F]{64}$")
CHUNK_SIZE = 1024 * 1024


@dataclass(frozen=True)
class ExportDiagnostic:
    severity: str
    code: str
    message: str


@dataclass(frozen=True)
class ExportReport:
    passed: bool
    format: str
    size_bytes: int
    sha256: str
    row_count: int | None
    column_count: int | None
    diagnostics: tuple[ExportDiagnostic, ...]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Verify Finora CSV/PDF export structure without logging finance contents."
    )
    parser.add_argument("export_file", type=Path)
    parser.add_argument(
        "--format",
        choices=("auto", "csv", "pdf"),
        default="auto",
        dest="format_name",
    )
    parser.add_argument(
        "--require-column",
        action="append",
        default=[],
        help="CSV column that must be present. May be supplied multiple times.",
    )
    parser.add_argument(
        "--min-rows",
        type=int,
        default=0,
        help="Minimum CSV data-row count (header excluded).",
    )
    parser.add_argument(
        "--min-size",
        type=int,
        default=32,
        help="Minimum artifact size in bytes.",
    )
    parser.add_argument("--expected-sha256")
    parser.add_argument("--json", action="store_true", dest="json_output")
    return parser.parse_args()


def infer_format(path: Path, requested: str) -> str:
    if requested != "auto":
        return requested
    suffix = path.suffix.casefold()
    if suffix == ".csv":
        return "csv"
    if suffix == ".pdf":
        return "pdf"
    raise ValueError("Could not infer export format; pass --format csv or --format pdf.")


def add(diagnostics: list[ExportDiagnostic], code: str, message: str) -> None:
    diagnostics.append(ExportDiagnostic("error", code, message))


def sha256_file(path: Path) -> tuple[int, str]:
    digest = hashlib.sha256()
    size = 0
    with path.open("rb") as stream:
        while True:
            chunk = stream.read(CHUNK_SIZE)
            if not chunk:
                break
            size += len(chunk)
            digest.update(chunk)
    return size, digest.hexdigest()


def verify_csv(
    data: bytes,
    diagnostics: list[ExportDiagnostic],
    *,
    required_columns: Sequence[str],
    min_rows: int,
) -> tuple[int | None, int | None]:
    try:
        text = data.decode("utf-8-sig")
    except UnicodeDecodeError:
        add(diagnostics, "csv_not_utf8", "CSV export is not valid UTF-8 text.")
        return None, None

    try:
        rows = list(csv.reader(io.StringIO(text, newline="")))
    except csv.Error:
        add(diagnostics, "csv_parse_error", "CSV export could not be parsed safely.")
        return None, None

    if not rows:
        add(diagnostics, "csv_empty", "CSV export contains no header row.")
        return 0, 0

    headers = [header.strip() for header in rows[0]]
    if not headers or all(not header for header in headers):
        add(diagnostics, "csv_blank_header", "CSV export header row is blank.")

    normalized_headers = [header.casefold() for header in headers]
    if len(set(normalized_headers)) != len(normalized_headers):
        add(
            diagnostics,
            "csv_duplicate_header",
            "CSV export contains duplicate header names.",
        )

    for required in required_columns:
        if required.strip().casefold() not in normalized_headers:
            add(
                diagnostics,
                "csv_missing_required_column",
                "CSV export is missing a configured required column.",
            )

    expected_columns = len(headers)
    data_rows = rows[1:]
    for row in data_rows:
        if len(row) != expected_columns:
            add(
                diagnostics,
                "csv_column_count_mismatch",
                "At least one CSV data row does not match the header column count.",
            )
            break

    if len(data_rows) < min_rows:
        add(
            diagnostics,
            "csv_min_rows_not_met",
            f"CSV export contains fewer than the configured {min_rows} data row(s).",
        )

    return len(data_rows), expected_columns


def verify_pdf(data: bytes, diagnostics: list[ExportDiagnostic]) -> None:
    if not data.startswith(b"%PDF-"):
        add(diagnostics, "pdf_missing_header", "PDF export does not begin with a PDF header.")
    if not data.rstrip().endswith(b"%%EOF"):
        add(diagnostics, "pdf_missing_eof", "PDF export does not end with a PDF EOF marker.")


def inspect_export(
    path: Path,
    *,
    format_name: str = "auto",
    required_columns: Sequence[str] = (),
    min_rows: int = 0,
    min_size: int = 32,
    expected_sha256: str | None = None,
) -> ExportReport:
    if min_rows < 0:
        raise ValueError("min_rows cannot be negative")
    if min_size < 1:
        raise ValueError("min_size must be at least 1")
    if expected_sha256 is not None and SHA256_RE.fullmatch(expected_sha256.strip()) is None:
        raise ValueError("expected_sha256 must contain exactly 64 hexadecimal characters")

    resolved_format = infer_format(path, format_name)
    diagnostics: list[ExportDiagnostic] = []

    if not path.exists():
        add(diagnostics, "missing_file", "Export artifact does not exist.")
        return ExportReport(False, resolved_format, 0, "", None, None, tuple(diagnostics))
    if not path.is_file():
        add(diagnostics, "not_a_file", "Export artifact path is not a regular file.")
        return ExportReport(False, resolved_format, 0, "", None, None, tuple(diagnostics))

    try:
        size, digest = sha256_file(path)
        data = path.read_bytes()
    except OSError as exc:
        add(
            diagnostics,
            "read_error",
            f"Export artifact could not be read safely: {type(exc).__name__}.",
        )
        return ExportReport(False, resolved_format, 0, "", None, None, tuple(diagnostics))

    if size < min_size:
        add(
            diagnostics,
            "too_small",
            f"Export artifact is smaller than the configured {min_size}-byte minimum.",
        )

    row_count: int | None = None
    column_count: int | None = None
    if resolved_format == "csv":
        row_count, column_count = verify_csv(
            data,
            diagnostics,
            required_columns=required_columns,
            min_rows=min_rows,
        )
    else:
        verify_pdf(data, diagnostics)

    if expected_sha256 is not None and digest.lower() != expected_sha256.strip().lower():
        add(diagnostics, "sha256_mismatch", "Export SHA-256 does not match the recorded digest.")

    passed = not any(item.severity == "error" for item in diagnostics)
    return ExportReport(
        passed,
        resolved_format,
        size,
        digest,
        row_count,
        column_count,
        tuple(diagnostics),
    )


def report_payload(report: ExportReport) -> dict[str, object]:
    return {
        "passed": report.passed,
        "format": report.format,
        "sizeBytes": report.size_bytes,
        "sha256": report.sha256,
        "rowCount": report.row_count,
        "columnCount": report.column_count,
        "diagnostics": [asdict(item) for item in report.diagnostics],
    }


def print_text(report: ExportReport) -> None:
    print(
        "Finora export verification: "
        f"passed={str(report.passed).lower()}, format={report.format}, "
        f"sizeBytes={report.size_bytes}, rows={report.row_count}, "
        f"columns={report.column_count}, sha256={report.sha256 or 'unavailable'}"
    )
    for item in report.diagnostics:
        print(f"[{item.severity.upper()}] {item.code}: {item.message}")


def main() -> int:
    args = parse_args()
    try:
        report = inspect_export(
            args.export_file,
            format_name=args.format_name,
            required_columns=args.require_column,
            min_rows=args.min_rows,
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
