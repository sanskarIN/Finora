#!/usr/bin/env python3
"""Run Finora's dependency-free repository QA checks from one command.

This runner intentionally focuses on checks that can execute without restoring the
.NET workload or launching a native target. It complements, rather than replaces,
`dotnet test` and device/platform validation.
"""

from __future__ import annotations

import argparse
import os
import shlex
import subprocess
import sys
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Sequence

REPO_ROOT = Path(__file__).resolve().parents[1]


@dataclass(frozen=True)
class QaStep:
    name: str
    command: tuple[str, ...]


@dataclass(frozen=True)
class QaResult:
    name: str
    command: tuple[str, ...]
    return_code: int
    duration_seconds: float

    @property
    def passed(self) -> bool:
        return self.return_code == 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Run dependency-free Finora repository QA checks."
    )
    parser.add_argument(
        "--include-dotnet",
        action="store_true",
        help="Also run `dotnet test` after dependency-free checks.",
    )
    parser.add_argument(
        "--dotnet-configuration",
        default="Release",
        choices=("Debug", "Release"),
        help="Configuration used when --include-dotnet is enabled.",
    )
    parser.add_argument(
        "--fail-fast",
        action="store_true",
        help="Stop after the first failed QA step.",
    )
    parser.add_argument(
        "--list",
        action="store_true",
        dest="list_only",
        help="List planned steps without executing them.",
    )
    return parser.parse_args()


def dependency_free_steps(python: str | None = None) -> list[QaStep]:
    interpreter = python or sys.executable
    return [
        QaStep(
            "Python developer-tool tests",
            (
                interpreter,
                "-m",
                "unittest",
                "discover",
                "-s",
                "scripts/tests",
                "-p",
                "test_*.py",
                "-v",
            ),
        ),
        QaStep(
            "Localization bundles and source references",
            (interpreter, "scripts/validate_localization.py"),
        ),
    ]


def planned_steps(
    *,
    include_dotnet: bool,
    dotnet_configuration: str,
    python: str | None = None,
) -> list[QaStep]:
    steps = dependency_free_steps(python)
    if include_dotnet:
        steps.append(
            QaStep(
                ".NET test suite",
                ("dotnet", "test", "-c", dotnet_configuration, "--nologo"),
            )
        )
    return steps


def run_step(step: QaStep, *, cwd: Path = REPO_ROOT) -> QaResult:
    started = time.monotonic()
    completed = subprocess.run(step.command, cwd=cwd, check=False)
    return QaResult(
        name=step.name,
        command=step.command,
        return_code=completed.returncode,
        duration_seconds=time.monotonic() - started,
    )


def format_command(command: Sequence[str]) -> str:
    return shlex.join(command)


def print_summary(results: Sequence[QaResult]) -> None:
    print("\nFinora repository QA summary")
    print("=" * 34)
    for result in results:
        status = "PASS" if result.passed else "FAIL"
        print(f"{status:4}  {result.duration_seconds:7.2f}s  {result.name}")
    failed = sum(not result.passed for result in results)
    print(f"\n{len(results) - failed} passed; {failed} failed.")


def main() -> int:
    args = parse_args()
    steps = planned_steps(
        include_dotnet=args.include_dotnet,
        dotnet_configuration=args.dotnet_configuration,
    )

    if args.list_only:
        for step in steps:
            print(f"{step.name}: {format_command(step.command)}")
        return 0

    results: list[QaResult] = []
    for step in steps:
        print(f"\n==> {step.name}")
        print(f"$ {format_command(step.command)}")
        try:
            result = run_step(step)
        except FileNotFoundError as exc:
            print(
                f"Required executable was not found for {step.name}: {exc.filename}",
                file=sys.stderr,
            )
            result = QaResult(step.name, step.command, 127, 0.0)
        results.append(result)
        if not result.passed and args.fail_fast:
            break

    print_summary(results)
    return 0 if results and all(result.passed for result in results) else 1


if __name__ == "__main__":
    raise SystemExit(main())
