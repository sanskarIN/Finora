from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from collections import defaultdict
from datetime import date
from pathlib import Path


SCRIPT_PATH = Path(__file__).resolve().parents[1] / "generate_sample_finance_csv.py"
SPEC = importlib.util.spec_from_file_location("generate_sample_finance_csv", SCRIPT_PATH)
assert SPEC is not None and SPEC.loader is not None
generator = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = generator
SPEC.loader.exec_module(generator)


class SampleFinanceCsvGeneratorTests(unittest.TestCase):
    def test_same_options_produce_identical_rows(self) -> None:
        options = generator.SampleOptions(
            rows=250,
            seed=12345,
            start_date=date(2024, 1, 1),
            currency="INR",
        )

        first = generator.generate_rows(options)
        second = generator.generate_rows(options)

        self.assertEqual(first, second)
        self.assertEqual(250, len(first))

    def test_csv_output_is_byte_for_byte_deterministic(self) -> None:
        options = generator.SampleOptions(
            rows=75,
            seed=77,
            start_date=date(2024, 1, 1),
            currency="INR",
        )
        rows = generator.generate_rows(options)

        with tempfile.TemporaryDirectory() as directory:
            first = Path(directory) / "first.csv"
            second = Path(directory) / "second.csv"
            generator.write_csv(first, rows)
            generator.write_csv(second, generator.generate_rows(options))

            self.assertEqual(first.read_bytes(), second.read_bytes())

    def test_transfer_groups_are_complete_mirrored_pairs(self) -> None:
        options = generator.SampleOptions(
            rows=2_000,
            seed=20260819,
            start_date=date(2024, 1, 1),
            currency="INR",
        )
        rows = generator.generate_rows(options)
        grouped: dict[str, list[dict[str, str]]] = defaultdict(list)
        for row in rows:
            if row["TransferGroup"]:
                grouped[row["TransferGroup"]].append(row)

        self.assertTrue(grouped)
        for group_rows in grouped.values():
            self.assertEqual(2, len(group_rows))
            first, second = group_rows
            self.assertEqual("Transfer", first["Type"])
            self.assertEqual("Transfer", second["Type"])
            self.assertEqual(first["Amount"], second["Amount"])
            self.assertEqual(first["Account"], second["CounterpartyAccount"])
            self.assertEqual(second["Account"], first["CounterpartyAccount"])

    def test_amount_format_respects_currency_decimal_places(self) -> None:
        amount = generator.Decimal("12.3456")

        expectations = (
            ("JPY", 0, "12", "12"),
            ("INR", 2, "12.35", "1235"),
            ("BHD", 3, "12.346", "12346"),
            ("CLF", 4, "12.3456", "123456"),
        )
        for currency, places, major, minor in expectations:
            with self.subTest(currency=currency):
                self.assertEqual(places, generator.currency_decimal_places(currency))
                self.assertEqual(
                    major,
                    generator.format_amount(
                        amount,
                        currency=currency,
                        minor_units=False,
                    ),
                )
                self.assertEqual(
                    minor,
                    generator.format_amount(
                        amount,
                        currency=currency,
                        minor_units=True,
                    ),
                )

    def test_generated_rows_use_selected_currency_precision(self) -> None:
        for currency, expected_places in (("JPY", 0), ("INR", 2), ("BHD", 3), ("CLF", 4)):
            with self.subTest(currency=currency):
                rows = generator.generate_rows(
                    generator.SampleOptions(
                        rows=50,
                        seed=42,
                        start_date=date(2024, 1, 1),
                        currency=currency,
                    )
                )
                for row in rows:
                    amount = row["Amount"]
                    actual_places = len(amount.partition(".")[2]) if "." in amount else 0
                    self.assertEqual(expected_places, actual_places)
                    self.assertEqual(currency, row["Currency"])

    def test_default_fixture_window_is_entirely_historical_for_2026_release(self) -> None:
        options = generator.SampleOptions(
            rows=5_000,
            seed=20260819,
            start_date=date(2024, 1, 1),
            currency="INR",
        )
        rows = generator.generate_rows(options)

        latest = max(date.fromisoformat(row["Date"]) for row in rows)
        self.assertLessEqual(latest, date(2025, 12, 30))

    def test_invalid_row_count_and_currency_are_rejected(self) -> None:
        with self.assertRaises(ValueError):
            generator.generate_rows(
                generator.SampleOptions(
                    rows=0,
                    seed=1,
                    start_date=date(2024, 1, 1),
                )
            )
        with self.assertRaises(ValueError):
            generator.generate_rows(
                generator.SampleOptions(
                    rows=1,
                    seed=1,
                    start_date=date(2024, 1, 1),
                    currency="RUPEES",
                )
            )
        with self.assertRaises(ValueError):
            generator.currency_decimal_places("12")


if __name__ == "__main__":
    unittest.main()
