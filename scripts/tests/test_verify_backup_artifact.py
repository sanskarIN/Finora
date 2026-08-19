from __future__ import annotations

import hashlib
import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT_PATH = Path(__file__).resolve().parents[1] / "verify_backup_artifact.py"
SPEC = importlib.util.spec_from_file_location("verify_backup_artifact", SCRIPT_PATH)
assert SPEC is not None and SPEC.loader is not None
verifier = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = verifier
SPEC.loader.exec_module(verifier)


class BackupArtifactVerifierTests(unittest.TestCase):
    def test_non_plaintext_nonzero_artifact_passes(self) -> None:
        payload = (b"FINORA-ENCRYPTED-FIXTURE\x01\x02\x03" * 16)[:512]
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "backup.bin"
            path.write_bytes(payload)

            report = verifier.inspect_backup(path)

        self.assertTrue(report.passed)
        self.assertEqual(len(payload), report.size_bytes)
        self.assertEqual(hashlib.sha256(payload).hexdigest(), report.sha256)
        self.assertEqual((), report.diagnostics)

    def test_plaintext_sqlite_header_is_rejected(self) -> None:
        payload = verifier.SQLITE_HEADER + b"x" * 512
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "backup.bin"
            path.write_bytes(payload)

            report = verifier.inspect_backup(path)

        self.assertFalse(report.passed)
        self.assertTrue(
            any(item.code == "plaintext_sqlite_header" for item in report.diagnostics)
        )

    def test_all_zero_content_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "backup.bin"
            path.write_bytes(b"\x00" * 512)

            report = verifier.inspect_backup(path)

        self.assertFalse(report.passed)
        self.assertTrue(any(item.code == "all_zero_content" for item in report.diagnostics))

    def test_too_small_artifact_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "backup.bin"
            path.write_bytes(b"encrypted-ish")

            report = verifier.inspect_backup(path, min_size=128)

        self.assertFalse(report.passed)
        self.assertTrue(any(item.code == "too_small" for item in report.diagnostics))

    def test_expected_sha256_match_passes_and_mismatch_fails(self) -> None:
        payload = b"encrypted-backup-fixture" * 32
        expected = hashlib.sha256(payload).hexdigest()
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "backup.bin"
            path.write_bytes(payload)

            matching = verifier.inspect_backup(path, expected_sha256=expected)
            mismatching = verifier.inspect_backup(path, expected_sha256="0" * 64)

        self.assertTrue(matching.passed)
        self.assertFalse(mismatching.passed)
        self.assertTrue(
            any(item.code == "sha256_mismatch" for item in mismatching.diagnostics)
        )

    def test_invalid_digest_and_minimum_are_rejected_before_file_processing(self) -> None:
        with self.assertRaises(ValueError):
            verifier.inspect_backup(Path("unused"), min_size=0)
        with self.assertRaises(ValueError):
            verifier.inspect_backup(Path("unused"), expected_sha256="not-a-digest")

    def test_missing_path_returns_sanitized_error(self) -> None:
        path = Path("/tmp/VERY-PRIVATE-BACKUP-NAME.finora")
        report = verifier.inspect_backup(path)

        self.assertFalse(report.passed)
        serialized = json.dumps(verifier.report_payload(report))
        self.assertNotIn("VERY-PRIVATE-BACKUP-NAME", serialized)
        self.assertTrue(any(item.code == "missing_file" for item in report.diagnostics))

    def test_json_payload_never_contains_backup_path_or_contents(self) -> None:
        sensitive_marker = b"PRIVATE-CONTENT-MARKER"
        payload = sensitive_marker + b"x" * 512
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "PRIVATE-FILENAME.finora"
            path.write_bytes(payload)

            report = verifier.inspect_backup(path)
            serialized = json.dumps(verifier.report_payload(report))

        self.assertNotIn("PRIVATE-FILENAME", serialized)
        self.assertNotIn(sensitive_marker.decode("ascii"), serialized)
        self.assertNotIn("path", serialized.casefold())


if __name__ == "__main__":
    unittest.main()
