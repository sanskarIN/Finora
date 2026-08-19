#!/usr/bin/env python3
"""Validate Finora .resx localization bundles using only the Python standard library."""

from __future__ import annotations

import argparse
import re
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

PLACEHOLDER_RE = re.compile(r"(?<!\{)\{(\d+)(?:[^}]*)\}(?!\})")
NEUTRAL_SUFFIX = "Resources.resx"
HINDI_SUFFIX = "Resources.hi.resx"


@dataclass(frozen=True)
class ResourceValue:
    key: str
    value: str
    file: Path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Validate Finora localization resource bundles.")
    parser.add_argument(
        "--root",
        type=Path,
        default=Path("src/Finora.App/Resources/Strings"),
        help="Directory containing Finora .resx bundles.",
    )
    parser.add_argument(
        "--allow-empty",
        action="store_true",
        help="Allow empty resource values (disabled by default).",
    )
    return parser.parse_args()


def parse_resx(path: Path) -> dict[str, ResourceValue]:
    try:
        root = ET.parse(path).getroot()
    except ET.ParseError as exc:
        raise ValueError(f"{path}: malformed XML: {exc}") from exc

    values: dict[str, ResourceValue] = {}
    for node in root.findall("data"):
        key = (node.get("name") or "").strip()
        if not key:
            raise ValueError(f"{path}: resource entry has no name")
        if key in values:
            raise ValueError(f"{path}: duplicate key '{key}'")

        value_node = node.find("value")
        value = "" if value_node is None or value_node.text is None else value_node.text
        values[key] = ResourceValue(key=key, value=value, file=path)

    if not values:
        raise ValueError(f"{path}: contains no <data> resource entries")
    return values


def neutral_bundles(root: Path) -> list[Path]:
    return sorted(
        path
        for path in root.glob(f"*{NEUTRAL_SUFFIX}")
        if not path.name.endswith(HINDI_SUFFIX)
    )


def hindi_for(neutral: Path) -> Path:
    return neutral.with_name(neutral.name[: -len(".resx")] + ".hi.resx")


def placeholders(value: str) -> tuple[str, ...]:
    return tuple(sorted(PLACEHOLDER_RE.findall(value)))


def validate_pair(
    neutral_path: Path,
    hindi_path: Path,
    *,
    allow_empty: bool,
) -> list[str]:
    errors: list[str] = []
    if not hindi_path.exists():
        return [f"{neutral_path.name}: missing Hindi bundle {hindi_path.name}"]

    try:
        neutral = parse_resx(neutral_path)
    except ValueError as exc:
        return [str(exc)]

    try:
        hindi = parse_resx(hindi_path)
    except ValueError as exc:
        return [str(exc)]

    neutral_keys = set(neutral)
    hindi_keys = set(hindi)

    for key in sorted(neutral_keys - hindi_keys):
        errors.append(f"{hindi_path.name}: missing key '{key}'")
    for key in sorted(hindi_keys - neutral_keys):
        errors.append(f"{hindi_path.name}: unexpected key '{key}'")

    for key in sorted(neutral_keys & hindi_keys):
        neutral_value = neutral[key].value
        hindi_value = hindi[key].value
        if not allow_empty:
            if not neutral_value.strip():
                errors.append(f"{neutral_path.name}: key '{key}' has an empty value")
            if not hindi_value.strip():
                errors.append(f"{hindi_path.name}: key '{key}' has an empty value")

        neutral_placeholders = placeholders(neutral_value)
        hindi_placeholders = placeholders(hindi_value)
        if neutral_placeholders != hindi_placeholders:
            errors.append(
                f"{hindi_path.name}: key '{key}' placeholder mismatch: "
                f"neutral={neutral_placeholders} hindi={hindi_placeholders}"
            )

    return errors


def validate_global_key_uniqueness(paths: Iterable[Path]) -> list[str]:
    errors: list[str] = []
    owners: dict[str, Path] = {}
    for path in paths:
        try:
            values = parse_resx(path)
        except ValueError as exc:
            errors.append(str(exc))
            continue
        for key in values:
            existing = owners.get(key)
            if existing is not None:
                errors.append(
                    f"global duplicate key '{key}' appears in both "
                    f"{existing.name} and {path.name}"
                )
            else:
                owners[key] = path
    return errors


def validate(root: Path, *, allow_empty: bool = False) -> list[str]:
    if not root.is_dir():
        return [f"localization root does not exist or is not a directory: {root}"]

    neutral = neutral_bundles(root)
    if not neutral:
        return [f"no neutral *Resources.resx bundles found under {root}"]

    errors: list[str] = []
    for neutral_path in neutral:
        errors.extend(
            validate_pair(
                neutral_path,
                hindi_for(neutral_path),
                allow_empty=allow_empty,
            )
        )

    errors.extend(validate_global_key_uniqueness(neutral))

    expected_hindi = {hindi_for(path).resolve() for path in neutral}
    for hindi_path in sorted(root.glob(f"*{HINDI_SUFFIX}")):
        if hindi_path.resolve() not in expected_hindi:
            errors.append(
                f"{hindi_path.name}: Hindi bundle has no matching neutral resource bundle"
            )

    return errors


def main() -> int:
    args = parse_args()
    errors = validate(args.root, allow_empty=args.allow_empty)
    if errors:
        print("Finora localization validation failed:", file=sys.stderr)
        for error in errors:
            print(f"  - {error}", file=sys.stderr)
        return 1

    bundles = neutral_bundles(args.root)
    print(
        f"Finora localization validation passed for {len(bundles)} neutral/Hindi bundle pair(s)."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
