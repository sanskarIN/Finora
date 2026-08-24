from __future__ import annotations

import hashlib
import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT_PATH = Path(__file__).resolve().parents[1] / "verify_export_artifact.py"
SPEC = importlib.util.spec_from_file_location("verify_export_artifact", SCRIPT_PATH)
assert SPEC is not None and SPEC.loader is not None
verifier = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = verifier
SPEC.loader.exec_module(verifier)


class ExportArtifactVerifierTests(unittest.TestCase):
    def test_valid_csv_reports_shape_and_required_columns(self) -> None:
        content = (
            "Date,Type,Amount,Account,Currency\n"
            "2025-01-01,Expense,12.00,Everyday,INR\n"
            "2025-01-02,Income,50.00,Everyday,INR\n"
        ).encode("utf-8")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "export.csv"
            path.write_bytes(content)

            report = verifier.inspect_export(
                path,
                required_columns=("Date", "Amount", "Currency"),
                min_rows=2,
            )

        self.assertTrue(report.passed)
        self.assertEqual("csv", report.format)
        self.assertEqual(2, report.row_count)
        self.assertEqual(5, report.column_count)
        self.assertEqual(hashlib.sha256(content).hexdigest(), report.sha256)

    def test_missing_configured_column_fails_without_echoing_requested_name(self) -> None:
        private_column = "PRIVATE-COLUMN-NAME"
        content = b"Date,Amount\n2025-01-01,12.00\n"
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "export.csv"
            path.write_bytes(content)

            report = verifier.inspect_export(path, required_columns=(private_column,))

        self.assertFalse(report.passed)
        serialized = json.dumps(verifier.report_payload(report))
        self.assertNotIn(private_column, serialized)
        self.assertTrue(
            any(item.code == "csv_missing_required_column" for item in report.diagnostics)
        )

    def test_duplicate_headers_and_row_width_mismatch_fail_without_echoing_values(self) -> None:
        sensitive = "PRIVATE-HEADER"
        content = (
            f"Date,{sensitive},{sensitive}\n"
            "2025-01-01,one,two,unexpected\n"
        ).encode("utf-8")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "export.csv"
            path.write_bytes(content)

            report = verifier.inspect_export(path)

        codes = {item.code for item in report.diagnostics}
        self.assertFalse(report.passed)
        self.assertIn("csv_duplicate_header", codes)
        self.assertIn("csv_column_count_mismatch", codes)
        self.assertNotIn(sensitive, json.dumps(verifier.report_payload(report)))
        self.assertNotIn("unexpected", json.dumps(verifier.report_payload(report)))

    def test_minimum_row_requirement_is_enforced(self) -> None:
        content = b"Date,Amount\n2025-01-01,12.00\n"
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "export.csv"
            path.write_bytes(content)

            report = verifier.inspect_export(path, min_rows=2)

        self.assertFalse(report.passed)
        self.assertTrue(any(item.code == "csv_min_rows_not_met" for item in report.diagnostics))

    def test_valid_pdf_envelope_passes(self) -> None:
        content = b"%PDF-1.7\n% synthetic Finora fixture\n1 0 obj\n<<>>\nendobj\n%%EOF\n"
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "export.pdf"
            path.write_bytes(content)

            report = verifier.inspect_export(path, min_size=16)

        self.assertTrue(report.passed)
        self.assertEqual("pdf", report.format)
        self.assertIsNone(report.row_count)
        self.assertIsNone(report.column_count)

    def test_invalid_pdf_envelope_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "export.pdf"
            path.write_bytes(b"not a pdf but large enough" * 4)

            report = verifier.inspect_export(path)

        codes = {item.code for item in report.diagnostics}
        self.assertFalse(report.passed)
        self.assertIn("pdf_missing_header", codes)
        self.assertIn("pdf_missing_eof", codes)

    def test_expected_sha256_match_and_mismatch(self) -> None:
        content = b"Date,Amount\n2025-01-01,12.00\n"
        expected = hashlib.sha256(content).hexdigest()
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "export.csv"
            path.write_bytes(content)

            matching = verifier.inspect_export(
                path,
                min_size=1,
                expected_sha256=expected,
            )
            mismatching = verifier.inspect_export(
                path,
                min_size=1,
                expected_sha256="0" * 64,
            )

        self.assertTrue(matching.passed)
        self.assertFalse(mismatching.passed)
        self.assertTrue(any(item.code == "sha256_mismatch" for item in mismatching.diagnostics))

    def test_report_does_not_include_path_or_csv_values(self) -> None:
        sensitive_marker = "PRIVATE-MERCHANT-MARKER"
        content = f"Date,Merchant\n2025-01-01,{sensitive_marker}\n".encode("utf-8")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "PRIVATE-FILENAME.csv"
            path.write_bytes(content)

            report = verifier.inspect_export(path)
            serialized = json.dumps(verifier.report_payload(report))

        self.assertNotIn("PRIVATE-FILENAME", serialized)
        self.assertNotIn(sensitive_marker, serialized)
        self.assertNotIn("path", serialized.casefold())

    def test_unknown_auto_format_and_invalid_options_raise(self) -> None:
        with self.assertRaises(ValueError):
            verifier.inspect_export(Path("fixture.bin"))
        with self.assertRaises(ValueError):
            verifier.inspect_export(Path("fixture.csv"), min_rows=-1)
        with self.assertRaises(ValueError):
            verifier.inspect_export(Path("fixture.csv"), min_size=0)
        with self.assertRaises(ValueError):
            verifier.inspect_export(Path("fixture.csv"), expected_sha256="bad")


if __name__ == "__main__":
    unittest.main()
