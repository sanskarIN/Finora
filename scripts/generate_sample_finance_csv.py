#!/usr/bin/env python3
"""Generate deterministic synthetic Finora CSV data for development and QA.

The generator never reads user data and never contacts a network service. Given the
same arguments it produces byte-for-byte equivalent CSV output.
"""

from __future__ import annotations

import argparse
import csv
import random
from dataclasses import dataclass
from datetime import date, timedelta
from decimal import Decimal, ROUND_HALF_UP
from pathlib import Path
from typing import Iterable, Sequence

HEADERS = [
    "Date",
    "Type",
    "Amount",
    "Account",
    "Currency",
    "Category",
    "Merchant",
    "Note",
    "PaymentMethod",
    "Location",
    "TransferGroup",
    "CounterpartyAccount",
    "Tags",
]

ACCOUNTS = ("Everyday", "Savings", "Cash")
EXPENSE_CATEGORIES = (
    "Groceries",
    "Transport",
    "Utilities",
    "Education",
    "Health",
    "Dining",
    "Household",
)
MERCHANTS = (
    "Sample Market",
    "Demo Transit",
    "Example Utilities",
    "Practice Books",
    "Test Pharmacy",
    "Mock Cafe",
    "Fixture Store",
)
PAYMENT_METHODS = ("Card", "UPI", "Cash")
TAGS = ("sample", "qa", "fixture", "demo")


@dataclass(frozen=True)
class SampleOptions:
    rows: int
    seed: int
    start_date: date
    currency: str = "INR"
    minor_units: bool = False


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate deterministic synthetic CSV data for Finora QA/import testing."
    )
    parser.add_argument(
        "output",
        nargs="?",
        type=Path,
        default=Path("artifacts/sample_finora_transactions.csv"),
        help="Output CSV path.",
    )
    parser.add_argument("--rows", type=int, default=250, help="Number of CSV rows to generate.")
    parser.add_argument("--seed", type=int, default=20260819, help="Deterministic random seed.")
    parser.add_argument(
        "--start-date",
        type=date.fromisoformat,
        default=date(2025, 1, 1),
        help="First possible transaction date in YYYY-MM-DD format.",
    )
    parser.add_argument(
        "--currency",
        default="INR",
        help="Three-letter currency code used in generated rows.",
    )
    parser.add_argument(
        "--minor-units",
        action="store_true",
        help="Write integer minor-unit amounts instead of major-unit decimal amounts.",
    )
    return parser.parse_args()


def validate_options(options: SampleOptions) -> None:
    if options.rows < 1:
        raise ValueError("rows must be at least 1")
    if options.rows > 1_000_000:
        raise ValueError("rows cannot exceed 1,000,000")
    if len(options.currency.strip()) != 3 or not options.currency.isalpha():
        raise ValueError("currency must be a three-letter alphabetic code")


def format_amount(amount: Decimal, *, minor_units: bool) -> str:
    normalized = amount.quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)
    if minor_units:
        return str(int((normalized * 100).to_integral_value(rounding=ROUND_HALF_UP)))
    return f"{normalized:.2f}"


def expense_row(rng: random.Random, index: int, when: date, options: SampleOptions) -> dict[str, str]:
    category_index = rng.randrange(len(EXPENSE_CATEGORIES))
    category = EXPENSE_CATEGORIES[category_index]
    merchant = MERCHANTS[category_index]
    amount = Decimal(rng.randint(125, 18000)) / Decimal(100)
    account = rng.choices(ACCOUNTS, weights=(70, 10, 20), k=1)[0]
    method = "Cash" if account == "Cash" else rng.choice(PAYMENT_METHODS[:2])
    return {
        "Date": when.isoformat(),
        "Type": "Expense",
        "Amount": format_amount(amount, minor_units=options.minor_units),
        "Account": account,
        "Currency": options.currency.upper(),
        "Category": category,
        "Merchant": merchant,
        "Note": f"Synthetic QA expense {index}",
        "PaymentMethod": method,
        "Location": "",
        "TransferGroup": "",
        "CounterpartyAccount": "",
        "Tags": ";".join(("sample", rng.choice(TAGS[1:]))),
    }


def income_row(rng: random.Random, index: int, when: date, options: SampleOptions) -> dict[str, str]:
    amount = Decimal(rng.randint(250000, 850000)) / Decimal(100)
    return {
        "Date": when.isoformat(),
        "Type": "Income",
        "Amount": format_amount(amount, minor_units=options.minor_units),
        "Account": "Everyday",
        "Currency": options.currency.upper(),
        "Category": "Income",
        "Merchant": "Synthetic Income",
        "Note": f"Synthetic QA income {index}",
        "PaymentMethod": "Bank",
        "Location": "",
        "TransferGroup": "",
        "CounterpartyAccount": "",
        "Tags": "sample;income",
    }


def transfer_rows(
    rng: random.Random,
    pair_index: int,
    when: date,
    options: SampleOptions,
) -> tuple[dict[str, str], dict[str, str]]:
    source, destination = rng.sample(ACCOUNTS[:2], 2)
    amount = Decimal(rng.randint(50000, 250000)) / Decimal(100)
    group = f"SAMPLE-TRANSFER-{pair_index:05d}"
    amount_text = format_amount(amount, minor_units=options.minor_units)
    common = {
        "Date": when.isoformat(),
        "Type": "Transfer",
        "Amount": amount_text,
        "Currency": options.currency.upper(),
        "Category": "",
        "Merchant": "Internal transfer",
        "Note": f"Synthetic transfer pair {pair_index}",
        "PaymentMethod": "Internal",
        "Location": "",
        "TransferGroup": group,
        "Tags": "sample;transfer",
    }
    outgoing = dict(common, Account=source, CounterpartyAccount=destination)
    incoming = dict(common, Account=destination, CounterpartyAccount=source)
    return outgoing, incoming


def generate_rows(options: SampleOptions) -> list[dict[str, str]]:
    validate_options(options)
    rng = random.Random(options.seed)
    rows: list[dict[str, str]] = []
    logical_index = 1
    transfer_index = 1

    while len(rows) < options.rows:
        when = options.start_date + timedelta(days=rng.randrange(0, 730))
        remaining = options.rows - len(rows)
        roll = rng.random()

        if roll < 0.08 and remaining >= 2:
            outgoing, incoming = transfer_rows(rng, transfer_index, when, options)
            rows.extend((outgoing, incoming))
            transfer_index += 1
        elif roll < 0.18:
            rows.append(income_row(rng, logical_index, when, options))
        else:
            rows.append(expense_row(rng, logical_index, when, options))
        logical_index += 1

    rows.sort(key=lambda row: (row["Date"], row["TransferGroup"], row["Account"], row["Note"]))
    return rows[: options.rows]


def write_csv(path: Path, rows: Iterable[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=HEADERS, extrasaction="raise")
        writer.writeheader()
        writer.writerows(rows)


def main() -> int:
    args = parse_args()
    options = SampleOptions(
        rows=args.rows,
        seed=args.seed,
        start_date=args.start_date,
        currency=args.currency.strip().upper(),
        minor_units=args.minor_units,
    )
    try:
        rows = generate_rows(options)
    except ValueError as exc:
        raise SystemExit(str(exc)) from exc

    write_csv(args.output, rows)
    print(
        f"Wrote {len(rows):,} deterministic synthetic Finora row(s) to {args.output} "
        f"using seed {options.seed}."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
