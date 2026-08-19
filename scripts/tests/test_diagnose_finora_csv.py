from __future__ import annotations

import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT_PATH = Path(__file__).resolve().parents[1] / "diagnose_finora_csv.py"
SPEC = importlib.util.spec_from_file_location("diagnose_finora_csv", SCRIPT_PATH)
assert SPEC is not None and SPEC.loader is not None
diagnostics = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = diagnostics
SPEC.loader.exec_module(diagnostics)


VALID_CSV = """Date,Type,Amount,Account,Currency,Category,Merchant,Note,PaymentMethod,Location,TransferGroup,CounterpartyAccount,Tags
2025-01-02,Expense,12.50,Everyday,INR,Groceries,Sample Market,Fixture,UPI,,,,sample
2025-01-03,Income,5000.00,Everyday,INR,Income,Synthetic Income,Fixture,Bank,,,,sample
2025-01-04,Transfer,1000.00,Everyday,INR,,Internal transfer,Fixture,Internal,,PAIR-1,Savings,sample
2025-01-04,Transfer,1000.00,Savings,INR,,Internal transfer,Fixture,Internal,,PAIR-1,Everyday,sample
"""


def write_csv(directory: str, content: str) -> Path:
    path = Path(directory) / "fixture.csv"
    path.write_text(content, encoding="utf-8")
    return path


class CsvDiagnosticsTests(unittest.TestCase):
    def test_valid_fixture_passes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            report = diagnostics.diagnose(write_csv(directory, VALID_CSV))

        self.assertTrue(report.passed)
        self.assertEqual(4, report.row_count)
        self.assertEqual(0, report.error_count)
        self.assertEqual(1, report.transfer_group_count)

    def test_missing_required_header_and_blank_values_fail(self) -> None:
        content = "Date,Type,Amount\n2025-01-01,Expense,12.00\n"
        with tempfile.TemporaryDirectory() as directory:
            report = diagnostics.diagnose(write_csv(directory, content))

        codes = {item.code for item in report.diagnostics}
        self.assertFalse(report.passed)
        self.assertIn("missing_required_header", codes)

    def test_invalid_date_type_amount_and_currency_are_reported_by_code(self) -> None:
        content = (
            "Date,Type,Amount,Account,Currency\n"
            "01/31/2025,Mystery,not-money,Everyday,RUPEES\n"
        )
        with tempfile.TemporaryDirectory() as directory:
            report = diagnostics.diagnose(write_csv(directory, content))

        codes = {item.code for item in report.diagnostics}
        self.assertIn("invalid_iso_date", codes)
        self.assertIn("unknown_transaction_type", codes)
        self.assertIn("invalid_amount", codes)
        self.assertIn("invalid_currency_code", codes)

    def test_minor_unit_mode_rejects_decimal_amounts(self) -> None:
        content = "Date,Type,Amount,Account\n2025-01-01,Expense,12.50,Everyday\n"
        with tempfile.TemporaryDirectory() as directory:
            report = diagnostics.diagnose(
                write_csv(directory, content),
                minor_units=True,
            )

        self.assertTrue(
            any(item.code == "minor_units_not_integer" for item in report.diagnostics)
        )

    def test_duplicate_fingerprints_are_warnings_not_destructive_actions(self) -> None:
        content = (
            "Date,Type,Amount,Account,Currency,Merchant\n"
            "2025-01-01,Expense,12.00,Everyday,INR,Sample Market\n"
            "2025-01-01,Expense,12.00,Everyday,INR,Sample Market\n"
        )
        with tempfile.TemporaryDirectory() as directory:
            report = diagnostics.diagnose(write_csv(directory, content))

        self.assertTrue(report.passed)
        self.assertEqual(1, report.duplicate_group_count)
        self.assertTrue(
            any(
                item.severity == "warning" and item.code == "possible_duplicate_group"
                for item in report.diagnostics
            )
        )

    def test_transfer_cardinality_and_missing_counterparty_are_warnings(self) -> None:
        content = (
            "Date,Type,Amount,Account,TransferGroup,CounterpartyAccount\n"
            "2025-01-01,Transfer,100.00,Everyday,PAIR-ONLY,\n"
        )
        with tempfile.TemporaryDirectory() as directory:
            report = diagnostics.diagnose(write_csv(directory, content))

        codes = {item.code for item in report.diagnostics}
        self.assertTrue(report.passed)
        self.assertIn("counterparty_account_blank", codes)
        self.assertIn("transfer_group_cardinality", codes)

    def test_machine_report_does_not_include_transaction_values(self) -> None:
        sensitive_marker = "DO-NOT-LOG-THIS-MERCHANT"
        content = (
            "Date,Type,Amount,Account,Merchant\n"
            f"2025-01-01,Expense,0,Everyday,{sensitive_marker}\n"
        )
        with tempfile.TemporaryDirectory() as directory:
            report = diagnostics.diagnose(write_csv(directory, content))

        payload = diagnostics.report_payload(report, max_diagnostics=100)
        serialized = json.dumps(payload)
        self.assertNotIn(sensitive_marker, serialized)
        self.assertNotIn("Everyday", serialized)
        self.assertNotIn('"0"', serialized)

    def test_duplicate_headers_are_rejected(self) -> None:
        content = "Date,Type,Amount,Account,Amount\n2025-01-01,Expense,12.00,Everyday,12.00\n"
        with tempfile.TemporaryDirectory() as directory:
            report = diagnostics.diagnose(write_csv(directory, content))

        self.assertFalse(report.passed)
        self.assertTrue(any(item.code == "duplicate_header" for item in report.diagnostics))


if __name__ == "__main__":
    unittest.main()
