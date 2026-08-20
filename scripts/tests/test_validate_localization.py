from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT_PATH = Path(__file__).resolve().parents[1] / "validate_localization.py"
SPEC = importlib.util.spec_from_file_location("validate_localization", SCRIPT_PATH)
assert SPEC is not None and SPEC.loader is not None
validate_localization = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = validate_localization
SPEC.loader.exec_module(validate_localization)


def write_resx(path: Path, entries: dict[str, str]) -> None:
    data = "\n".join(
        f'  <data name="{key}" xml:space="preserve"><value>{value}</value></data>'
        for key, value in entries.items()
    )
    path.write_text(
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
        "<root>\n"
        "  <resheader name=\"resmimetype\"><value>text/microsoft-resx</value></resheader>\n"
        "  <resheader name=\"version\"><value>2.0</value></resheader>\n"
        f"{data}\n"
        "</root>\n",
        encoding="utf-8",
    )


class LocalizationValidatorTests(unittest.TestCase):
    def test_matching_bundle_pair_passes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            write_resx(
                root / "FeatureResources.resx",
                {"Greeting": "Hello", "CountFormat": "Count: {0:N0}"},
            )
            write_resx(
                root / "FeatureResources.hi.resx",
                {"Greeting": "नमस्ते", "CountFormat": "गिनती: {0:N0}"},
            )

            self.assertEqual([], validate_localization.validate(root))

    def test_missing_hindi_bundle_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            write_resx(root / "FeatureResources.resx", {"Greeting": "Hello"})

            errors = validate_localization.validate(root)

            self.assertTrue(any("missing Hindi bundle" in error for error in errors))

    def test_key_mismatch_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            write_resx(root / "FeatureResources.resx", {"Greeting": "Hello"})
            write_resx(root / "FeatureResources.hi.resx", {"Different": "अलग"})

            errors = validate_localization.validate(root)

            self.assertTrue(any("missing key 'Greeting'" in error for error in errors))
            self.assertTrue(any("unexpected key 'Different'" in error for error in errors))

    def test_placeholder_mismatch_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            write_resx(
                root / "FeatureResources.resx",
                {"Summary": "Imported {0:N0}; rejected {1:N0}."},
            )
            write_resx(
                root / "FeatureResources.hi.resx",
                {"Summary": "{0:N0} आयात हुए।"},
            )

            errors = validate_localization.validate(root)

            self.assertTrue(any("placeholder mismatch" in error for error in errors))

    def test_global_duplicate_neutral_key_fails(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            write_resx(root / "FirstResources.resx", {"Shared": "One"})
            write_resx(root / "FirstResources.hi.resx", {"Shared": "एक"})
            write_resx(root / "SecondResources.resx", {"Shared": "Two"})
            write_resx(root / "SecondResources.hi.resx", {"Shared": "दो"})

            errors = validate_localization.validate(root)

            self.assertTrue(any("global duplicate key 'Shared'" in error for error in errors))

    def test_empty_translation_fails_by_default(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            write_resx(root / "FeatureResources.resx", {"Greeting": "Hello"})
            write_resx(root / "FeatureResources.hi.resx", {"Greeting": ""})

            errors = validate_localization.validate(root)

            self.assertTrue(any("empty value" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
