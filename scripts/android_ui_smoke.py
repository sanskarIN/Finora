#!/usr/bin/env python3
"""Dependency-free Android native UI smoke harness for Finora.

The harness launches an installed app, asks Android UIAutomator for the current
accessibility hierarchy, and validates expected text/descriptions/resource IDs.
It intentionally does not capture screenshots or persist the full hierarchy.
"""

from __future__ import annotations

import argparse
import json
import re
import shutil
import subprocess
import sys
import time
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Sequence

BOUNDS_RE = re.compile(r"^\[(\d+),(\d+)\]\[(\d+),(\d+)\]$")
REMOTE_DUMP = "/sdcard/finora-ui-smoke.xml"


@dataclass(frozen=True)
class UiNode:
    text: str
    description: str
    resource_id: str
    class_name: str
    clickable: bool
    enabled: bool
    bounds: tuple[int, int, int, int] | None

    @property
    def searchable_text(self) -> str:
        return " ".join(
            part for part in (self.text, self.description, self.resource_id) if part
        )


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Launch an installed Android app and validate its native accessibility hierarchy."
    )
    parser.add_argument("--package", required=True, help="Installed Android package/application ID.")
    parser.add_argument(
        "--activity",
        help="Optional fully qualified activity component. Without this, the launcher intent is used.",
    )
    parser.add_argument(
        "--serial",
        help="Optional ADB device serial. Required when more than one device/emulator is connected.",
    )
    parser.add_argument("--startup-seconds", type=float, default=2.5)
    parser.add_argument("--expect-text", action="append", default=[])
    parser.add_argument("--expect-description", action="append", default=[])
    parser.add_argument("--expect-id", action="append", default=[])
    parser.add_argument(
        "--forbid-text",
        action="append",
        default=[],
        help="Fail when visible/accessibility text contains this case-insensitive value.",
    )
    parser.add_argument(
        "--report",
        type=Path,
        help="Optional path for a small JSON result report. Full UI text is never written.",
    )
    return parser.parse_args()


def parse_bounds(value: str) -> tuple[int, int, int, int] | None:
    match = BOUNDS_RE.match(value.strip())
    if match is None:
        return None
    left, top, right, bottom = (int(part) for part in match.groups())
    if right < left or bottom < top:
        return None
    return left, top, right, bottom


def parse_hierarchy(xml_text: str) -> list[UiNode]:
    root = ET.fromstring(xml_text)
    nodes: list[UiNode] = []
    for element in root.iter("node"):
        attributes = element.attrib
        nodes.append(
            UiNode(
                text=attributes.get("text", ""),
                description=attributes.get("content-desc", ""),
                resource_id=attributes.get("resource-id", ""),
                class_name=attributes.get("class", ""),
                clickable=attributes.get("clickable", "false").lower() == "true",
                enabled=attributes.get("enabled", "true").lower() == "true",
                bounds=parse_bounds(attributes.get("bounds", "")),
            )
        )
    return nodes


def contains_casefold(value: str, expected: str) -> bool:
    return expected.casefold() in value.casefold()


def validate_nodes(
    nodes: Sequence[UiNode],
    *,
    expected_text: Sequence[str],
    expected_descriptions: Sequence[str],
    expected_ids: Sequence[str],
    forbidden_text: Sequence[str],
) -> list[str]:
    errors: list[str] = []

    for expected in expected_text:
        if not any(contains_casefold(node.text, expected) for node in nodes):
            errors.append(f"expected visible text not found: {expected!r}")

    for expected in expected_descriptions:
        if not any(contains_casefold(node.description, expected) for node in nodes):
            errors.append(f"expected accessibility description not found: {expected!r}")

    for expected in expected_ids:
        if not any(contains_casefold(node.resource_id, expected) for node in nodes):
            errors.append(f"expected resource ID not found: {expected!r}")

    for forbidden in forbidden_text:
        if any(contains_casefold(node.searchable_text, forbidden) for node in nodes):
            errors.append(f"forbidden UI/accessibility text was present: {forbidden!r}")

    return errors


def adb_command(serial: str | None, *parts: str) -> list[str]:
    command = ["adb"]
    if serial:
        command.extend(("-s", serial))
    command.extend(parts)
    return command


def run_adb(
    serial: str | None,
    *parts: str,
    check: bool = True,
    timeout: float = 20,
) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        adb_command(serial, *parts),
        check=check,
        capture_output=True,
        text=True,
        timeout=timeout,
    )


def require_device(serial: str | None) -> None:
    if shutil.which("adb") is None:
        raise RuntimeError("adb was not found on PATH. Install Android platform-tools first.")

    result = run_adb(serial, "get-state")
    if result.stdout.strip() != "device":
        raise RuntimeError("ADB target is not in the ready 'device' state.")


def launch_app(package: str, activity: str | None, serial: str | None) -> None:
    if activity:
        component = activity if "/" in activity else f"{package}/{activity}"
        run_adb(serial, "shell", "am", "start", "-W", "-n", component)
        return

    run_adb(
        serial,
        "shell",
        "monkey",
        "-p",
        package,
        "-c",
        "android.intent.category.LAUNCHER",
        "1",
    )


def dump_hierarchy(serial: str | None) -> list[UiNode]:
    run_adb(serial, "shell", "uiautomator", "dump", REMOTE_DUMP)
    result = run_adb(serial, "exec-out", "cat", REMOTE_DUMP)
    try:
        return parse_hierarchy(result.stdout)
    finally:
        run_adb(serial, "shell", "rm", "-f", REMOTE_DUMP, check=False)


def write_report(path: Path, *, node_count: int, errors: Sequence[str]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    payload = {
        "passed": not errors,
        "nodeCount": node_count,
        "errorCount": len(errors),
        "errors": list(errors),
    }
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


def main() -> int:
    args = parse_args()
    if args.startup_seconds < 0 or args.startup_seconds > 60:
        raise SystemExit("--startup-seconds must be between 0 and 60")

    try:
        require_device(args.serial)
        launch_app(args.package, args.activity, args.serial)
        time.sleep(args.startup_seconds)
        nodes = dump_hierarchy(args.serial)
    except (RuntimeError, subprocess.CalledProcessError, subprocess.TimeoutExpired, ET.ParseError) as exc:
        print(f"Android UI smoke setup failed: {exc}", file=sys.stderr)
        return 2

    errors = validate_nodes(
        nodes,
        expected_text=args.expect_text,
        expected_descriptions=args.expect_description,
        expected_ids=args.expect_id,
        forbidden_text=args.forbid_text,
    )

    if args.report:
        write_report(args.report, node_count=len(nodes), errors=errors)

    if errors:
        print("Android UI smoke validation failed:", file=sys.stderr)
        for error in errors:
            print(f"  - {error}", file=sys.stderr)
        return 1

    print(
        f"Android UI smoke validation passed across {len(nodes)} accessibility node(s)."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
