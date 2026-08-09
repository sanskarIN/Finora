#!/usr/bin/env python3
"""Dependency-free structural validation for the Finora repository.

This script is intentionally limited to checks that do not require the .NET SDK.
It complements (and never replaces) restore, build, analyzers, tests, and device QA.
"""
from __future__ import annotations

import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE_EXTENSIONS = {".cs", ".xaml", ".xml", ".resx", ".csproj", ".props", ".targets", ".md", ".ps1", ".py", ".yml", ".yaml", ".json"}
XML_EXTENSIONS = {".xaml", ".xml", ".resx", ".csproj", ".props", ".targets"}
SKIP_PARTS = {".git", "bin", "obj", ".vs", ".idea", ".vscode"}
PLACEHOLDER_PATTERNS = [
    re.compile(r"\bTODO\b", re.IGNORECASE),
    re.compile(r"\bFIXME\b", re.IGNORECASE),
    re.compile(r"NotImplementedException"),
    re.compile(r"throw\s+new\s+NotSupportedException\s*\(\s*\)"),
    re.compile(r"placeholder implementation", re.IGNORECASE),
]
HANDLER_PATTERN = re.compile(r"(?:Clicked|Tapped|CheckedChanged|SelectionChanged|TextChanged|Completed|Unfocused|Focused)\s*=\s*\"([A-Za-z_][A-Za-z0-9_]*)\"")
CLASS_PATTERN = re.compile(r'x:Class\s*=\s*"([A-Za-z_][A-Za-z0-9_.]*)"')


def files() -> list[Path]:
    result: list[Path] = []
    for path in ROOT.rglob("*"):
        if not path.is_file() or any(part in SKIP_PARTS for part in path.parts):
            continue
        if path.suffix.lower() in SOURCE_EXTENSIONS:
            result.append(path)
    return sorted(result)


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def check_xml(paths: list[Path], errors: list[str]) -> None:
    for path in paths:
        if path.suffix.lower() not in XML_EXTENSIONS:
            continue
        try:
            ET.parse(path)
        except ET.ParseError as exc:
            errors.append(f"{rel(path)}: malformed XML/XAML: {exc}")


def check_empty(paths: list[Path], errors: list[str]) -> None:
    for path in paths:
        try:
            text = path.read_text(encoding="utf-8-sig")
        except UnicodeDecodeError:
            continue
        if not text.strip():
            errors.append(f"{rel(path)}: file is empty")


def check_placeholders(paths: list[Path], errors: list[str]) -> None:
    for path in paths:
        if path.name == "what_changed.md":
            continue
        try:
            text = path.read_text(encoding="utf-8-sig")
        except UnicodeDecodeError:
            continue
        for pattern in PLACEHOLDER_PATTERNS:
            if pattern.search(text):
                errors.append(f"{rel(path)}: placeholder marker matched {pattern.pattern!r}")
                break


def check_project_references(paths: list[Path], errors: list[str]) -> None:
    for path in paths:
        if path.suffix.lower() != ".csproj":
            continue
        try:
            tree = ET.parse(path)
        except ET.ParseError:
            continue
        for node in tree.iter():
            if node.tag.split("}")[-1] != "ProjectReference":
                continue
            include = node.attrib.get("Include")
            if not include:
                continue
            target = (path.parent / include.replace("\\", "/")).resolve()
            if not target.exists():
                errors.append(f"{rel(path)}: missing ProjectReference target {include}")


def check_xaml_codebehind(paths: list[Path], errors: list[str]) -> None:
    cs_texts: dict[str, str] = {}
    for path in paths:
        if path.suffix.lower() == ".cs":
            try:
                cs_texts[rel(path)] = path.read_text(encoding="utf-8-sig")
            except UnicodeDecodeError:
                pass

    for path in paths:
        if path.suffix.lower() != ".xaml":
            continue
        text = path.read_text(encoding="utf-8-sig")
        class_match = CLASS_PATTERN.search(text)
        if not class_match:
            continue
        class_name = class_match.group(1).split(".")[-1]
        candidates = [content for content in cs_texts.values() if re.search(rf"\bpartial\s+class\s+{re.escape(class_name)}\b", content)]
        if not candidates:
            errors.append(f"{rel(path)}: no matching partial class for {class_match.group(1)}")
            continue
        merged = "\n".join(candidates)
        for handler in sorted(set(HANDLER_PATTERN.findall(text))):
            if not re.search(rf"\b{re.escape(handler)}\s*\(", merged):
                errors.append(f"{rel(path)}: XAML handler {handler} was not found in matching C# partial class")


def check_solution_projects(errors: list[str]) -> None:
    solution = ROOT / "Finora.sln"
    if not solution.exists():
        errors.append("Finora.sln: missing solution file")
        return
    text = solution.read_text(encoding="utf-8-sig")
    refs = re.findall(r'Project\("\{[^}]+\}"\)\s*=\s*"[^"]+",\s*"([^"]+\.csproj)"', text)
    if not refs:
        errors.append("Finora.sln: no project entries found")
        return
    for item in refs:
        target = (ROOT / item.replace("\\", "/")).resolve()
        if not target.exists():
            errors.append(f"Finora.sln: missing project {item}")


def main() -> int:
    paths = files()
    errors: list[str] = []
    check_xml(paths, errors)
    check_empty(paths, errors)
    check_placeholders(paths, errors)
    check_project_references(paths, errors)
    check_xaml_codebehind(paths, errors)
    check_solution_projects(errors)

    if errors:
        print(f"Finora structural preflight FAILED with {len(errors)} issue(s):", file=sys.stderr)
        for issue in errors:
            print(f" - {issue}", file=sys.stderr)
        return 1

    print(f"Finora structural preflight passed: {len(paths)} text/source files checked.")
    print("This is not a compiler, analyzer, test runner, emulator, simulator, signing, or store-validation substitute.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
