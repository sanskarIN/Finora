#!/usr/bin/env python3
"""Privacy-safe structural diagnostics for Finora-style CSV imports.

This developer/QA helper reports row numbers and diagnostic codes without echoing
transaction values. It is intentionally conservative and does not replace the
application's own transactional import validation.
"""

from __future__ import annotations

import argparse
import csv
import json
import re
import sys
from collections import Counter, defaultdict
from dataclasses import asdict, dataclass
from datetime import date
from decimal import Decimal, InvalidOperation
from pathlib import Path
from typing import Iterable, Sequence

REQUIRED_HEADERS = ("Date", "Type", "Amount", "Account")
KNOWN_TYPES = {"expense", "income", "transfer", "refund", "adjustment"}
CURRENCY_RE = re.compile(r"^[A-Za-z]{3}$")
INTEGER_RE = re.compile(r"^[+-]?\d+$")


@dataclass(frozen=True)
class Diagnostic:
    severity: str
    code: str
    row: int | None
    message: str


@dataclass(frozen=True)
class DiagnosticReport:
    row_count: int
    error_count: int
    warning_count: int
    duplicate_group_count: int
    transfer_group_count: int
    diagnostics: tuple[Diagnostic, ...]

    @property
    def passed(self) -> bool:
        return self.error_count == 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Run privacy-safe structural diagnostics on a Finora-style CSV file."
    )
    parser.add_argument("csv_file", type=Path)
    parser.add_argument(
        "--minor-units",
        action="store_true",
        help="Require Amount values to be integer minor-unit values.",
    )
    parser.add_argument(
        "--json",
        action="store_true",
        dest="json_output",
        help="Print a machine-readable summary. Transaction values are never included.",
    )
    parser.add_argument(
        "--max-diagnostics",
        type=int,
        default=100,
        help="Maximum diagnostics included in output (1-10000).",
    )
    return parser.parse_args()


def add(
    diagnostics: list[Diagnostic],
    severity: str,
    code: str,
    row: int | None,
    message: str,
) -> None:
    diagnostics.append(Diagnostic(severity, code, row, message))


def validate_header(headers: Sequence[str], diagnostics: list[Diagnostic]) -> dict[str, int]:
    stripped = [header.strip() for header in headers]
    counts = Counter(stripped)
    for header, count in sorted(counts.items()):
        if header and count > 1:
            add(
                diagnostics,
                "error",
                "duplicate_header",
                None,
                f"Header {header!r} appears {count} times.",
            )

    index: dict[str, int] = {}
    for position, header in enumerate(stripped):
        if header and header not in index:
            index[header] = position

    for required in REQUIRED_HEADERS:
        if required not in index:
            add(
                diagnostics,
                "error",
                "missing_required_header",
                None,
                f"Required header {required!r} is missing.",
            )
    return index


def field(row: Sequence[str], index: dict[str, int], name: str) -> str:
    position = index.get(name)
    if position is None or position >= len(row):
        return ""
    return row[position].strip()


def validate_row(
    row_number: int,
    row: Sequence[str],
    header_count: int,
    index: dict[str, int],
    diagnostics: list[Diagnostic],
    *,
    minor_units: bool,
) -> None:
    if len(row) != header_count:
        add(
            diagnostics,
            "error",
            "column_count_mismatch",
            row_number,
            "Row column count does not match the header column count.",
        )

    for required in REQUIRED_HEADERS:
        if required in index and not field(row, index, required):
            add(
                diagnostics,
                "error",
                "blank_required_value",
                row_number,
                f"Required column {required!r} is blank.",
            )

    date_text = field(row, index, "Date")
    if date_text:
        try:
            date.fromisoformat(date_text)
        except ValueError:
            add(
                diagnostics,
                "error",
                "invalid_iso_date",
                row_number,
                "Date is not in canonical YYYY-MM-DD form for this preflight tool.",
            )

    type_text = field(row, index, "Type")
    if type_text and type_text.casefold() not in KNOWN_TYPES:
        add(
            diagnostics,
            "error",
            "unknown_transaction_type",
            row_number,
            "Type is not one of Expense, Income, Transfer, Refund, or Adjustment.",
        )

    amount_text = field(row, index, "Amount")
    if amount_text:
        if minor_units and INTEGER_RE.fullmatch(amount_text) is None:
            add(
                diagnostics,
                "error",
                "minor_units_not_integer",
                row_number,
                "Amount must be an integer when minor-unit mode is enabled.",
            )
        else:
            try:
                amount = Decimal(amount_text)
                if not amount.is_finite():
                    raise InvalidOperation
                if amount == 0:
                    add(
                        diagnostics,
                        "warning",
                        "zero_amount",
                        row_number,
                        "Amount is zero; verify that this row is intentional.",
                    )
            except InvalidOperation:
                add(
                    diagnostics,
                    "error",
                    "invalid_amount",
                    row_number,
                    "Amount is not a finite decimal value.",
                )

    currency = field(row, index, "Currency")
    if currency and CURRENCY_RE.fullmatch(currency) is None:
        add(
            diagnostics,
            "error",
            "invalid_currency_code",
            row_number,
            "Currency must be a three-letter alphabetic code when present.",
        )

    if type_text.casefold() == "transfer":
        if "TransferGroup" in index and not field(row, index, "TransferGroup"):
            add(
                diagnostics,
                "warning",
                "transfer_group_blank",
                row_number,
                "Transfer row has a blank TransferGroup value.",
            )
        if "CounterpartyAccount" in index and not field(row, index, "CounterpartyAccount"):
            add(
                diagnostics,
                "warning",
                "counterparty_account_blank",
                row_number,
                "Transfer row has a blank CounterpartyAccount value.",
            )


def fingerprint(row: Sequence[str], index: dict[str, int]) -> tuple[str, ...]:
    return tuple(
        field(row, index, name).casefold()
        for name in (
            "Date",
            "Type",
            "Amount",
            "Account",
            "Currency",
            "Merchant",
            "TransferGroup",
        )
    )


def diagnose(path: Path, *, minor_units: bool = False) -> DiagnosticReport:
    diagnostics: list[Diagnostic] = []
    try:
        with path.open("r", encoding="utf-8-sig", newline="") as stream:
            reader = csv.reader(stream)
            try:
                headers = next(reader)
            except StopIteration:
                add(diagnostics, "error", "empty_file", None, "CSV file is empty.")
                return build_report(0, diagnostics, 0, 0)

            index = validate_header(headers, diagnostics)
            rows: list[tuple[int, list[str]]] = []
            for row_number, row in enumerate(reader, start=2):
                if not row or all(not value.strip() for value in row):
                    add(
                        diagnostics,
                        "warning",
                        "blank_row",
                        row_number,
                        "Blank CSV row will not contribute finance data.",
                    )
                    continue
                rows.append((row_number, row))
                validate_row(
                    row_number,
                    row,
                    len(headers),
                    index,
                    diagnostics,
                    minor_units=minor_units,
                )
    except (OSError, UnicodeError, csv.Error) as exc:
        add(
            diagnostics,
            "error",
            "file_read_error",
            None,
            f"CSV file could not be read safely: {type(exc).__name__}.",
        )
        return build_report(0, diagnostics, 0, 0)

    fingerprints: defaultdict[tuple[str, ...], list[int]] = defaultdict(list)
    transfer_groups: defaultdict[str, list[int]] = defaultdict(list)
    for row_number, row in rows:
        fingerprints[fingerprint(row, index)].append(row_number)
        transfer_group = field(row, index, "TransferGroup")
        if transfer_group:
            transfer_groups[transfer_group.casefold()].append(row_number)

    duplicate_groups = [numbers for numbers in fingerprints.values() if len(numbers) > 1]
    for numbers in duplicate_groups:
        add(
            diagnostics,
            "warning",
            "possible_duplicate_group",
            numbers[0],
            f"A possible duplicate fingerprint appears on {len(numbers)} row(s).",
        )

    for numbers in transfer_groups.values():
        if len(numbers) != 2:
            add(
                diagnostics,
                "warning",
                "transfer_group_cardinality",
                numbers[0],
                f"TransferGroup appears on {len(numbers)} row(s); paired fixtures normally use two.",
            )

    return build_report(
        len(rows),
        diagnostics,
        len(duplicate_groups),
        len(transfer_groups),
    )


def build_report(
    row_count: int,
    diagnostics: Sequence[Diagnostic],
    duplicate_group_count: int,
    transfer_group_count: int,
) -> DiagnosticReport:
    error_count = sum(item.severity == "error" for item in diagnostics)
    warning_count = sum(item.severity == "warning" for item in diagnostics)
    return DiagnosticReport(
        row_count=row_count,
        error_count=error_count,
        warning_count=warning_count,
        duplicate_group_count=duplicate_group_count,
        transfer_group_count=transfer_group_count,
        diagnostics=tuple(diagnostics),
    )


def report_payload(report: DiagnosticReport, *, max_diagnostics: int) -> dict[str, object]:
    return {
        "passed": report.passed,
        "rowCount": report.row_count,
        "errorCount": report.error_count,
        "warningCount": report.warning_count,
        "duplicateGroupCount": report.duplicate_group_count,
        "transferGroupCount": report.transfer_group_count,
        "diagnostics": [
            asdict(item) for item in report.diagnostics[:max_diagnostics]
        ],
        "diagnosticsTruncated": len(report.diagnostics) > max_diagnostics,
    }


def print_text(report: DiagnosticReport, *, max_diagnostics: int) -> None:
    print(
        "Finora CSV diagnostics: "
        f"rows={report.row_count}, errors={report.error_count}, "
        f"warnings={report.warning_count}, duplicateGroups={report.duplicate_group_count}, "
        f"transferGroups={report.transfer_group_count}"
    )
    for item in report.diagnostics[:max_diagnostics]:
        location = f"row {item.row}" if item.row is not None else "file"
        print(f"[{item.severity.upper()}] {item.code} ({location}): {item.message}")
    if len(report.diagnostics) > max_diagnostics:
        print(
            f"... {len(report.diagnostics) - max_diagnostics} additional diagnostic(s) omitted."
        )


def main() -> int:
    args = parse_args()
    if args.max_diagnostics < 1 or args.max_diagnostics > 10_000:
        raise SystemExit("--max-diagnostics must be between 1 and 10000")

    report = diagnose(args.csv_file, minor_units=args.minor_units)
    if args.json_output:
        print(json.dumps(report_payload(report, max_diagnostics=args.max_diagnostics), indent=2))
    else:
        print_text(report, max_diagnostics=args.max_diagnostics)
    return 0 if report.passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
