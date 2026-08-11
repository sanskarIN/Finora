#!/usr/bin/env python3
"""Dependency-free structural validation for the Finora repository.

This script intentionally checks only invariants that do not require the .NET SDK.
It complements (and never replaces) restore, build, analyzers, tests, native-device QA,
signing, and store validation.
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
HANDLER_PATTERN = re.compile(r"(?:Clicked|Tapped|CheckedChanged|SelectionChanged|TextChanged|Completed|Unfocused|Focused|Toggled)\s*=\s*\"([A-Za-z_][A-Za-z0-9_]*)\"")
CLASS_PATTERN = re.compile(r'x:Class\s*=\s*"([A-Za-z_][A-Za-z0-9_.]*)"')
MONEY_WORDS = r"(?:Amount|Balance|Limit|Target|Starting|Contribution|Paid|Price|Cost|Income|Expense|Budget|Net|Minor)"
REQUIRED_PATHS = [
    "LICENSE",
    "README.md",
    "CONTRIBUTING.md",
    "CODE_OF_CONDUCT.md",
    "SECURITY.md",
    "SUPPORT.md",
    "PRIVACY.md",
    "TERMS.md",
    "CHANGELOG.md",
    "PROJECT_STATUS.md",
    "DECISIONS.md",
    "THIRD_PARTY_NOTICES.md",
    "what_changed.md",
    "Finora.sln",
    ".github/workflows/ci.yml",
    "docs/TEST_PLAN.md",
    "docs/architecture/OVERVIEW.md",
    "docs/architecture/DATABASE_SCHEMA.md",
    "docs/security/THREAT_MODEL.md",
    "docs/releases/RELEASE_CHECKLIST.md",
    "docs/releases/STORE_READINESS.md",
    "src/Finora.App/Platforms/Android/Resources/xml/backup_rules.xml",
    "src/Finora.App/Platforms/Android/Resources/xml/data_extraction_rules.xml",
]


def files() -> list[Path]:
    result: list[Path] = []
    for path in ROOT.rglob("*"):
        if not path.is_file() or any(part in SKIP_PARTS for part in path.parts):
            continue
        if path.suffix.lower() in SOURCE_EXTENSIONS or path.name == "LICENSE":
            result.append(path)
    return sorted(result)


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def check_required_paths(errors: list[str]) -> None:
    for item in REQUIRED_PATHS:
        if not (ROOT / item).is_file():
            errors.append(f"{item}: required repository file is missing")


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
            text = read(path)
        except UnicodeDecodeError:
            continue
        if not text.strip():
            errors.append(f"{rel(path)}: file is empty")


def check_placeholders(paths: list[Path], errors: list[str]) -> None:
    for path in paths:
        if path.name == "what_changed.md":
            continue
        try:
            text = read(path)
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
                cs_texts[rel(path)] = read(path)
            except UnicodeDecodeError:
                pass

    for path in paths:
        if path.suffix.lower() != ".xaml":
            continue
        text = read(path)
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
    text = read(solution)
    refs = re.findall(r'Project\("\{[^}]+\}"\)\s*=\s*"[^"]+",\s*"([^"]+\.csproj)"', text)
    if not refs:
        errors.append("Finora.sln: no project entries found")
        return
    for item in refs:
        target = (ROOT / item.replace("\\", "/")).resolve()
        if not target.exists():
            errors.append(f"Finora.sln: missing project {item}")


def find_xml_property(path: Path, property_name: str) -> str | None:
    try:
        tree = ET.parse(path)
    except (ET.ParseError, FileNotFoundError):
        return None
    for node in tree.iter():
        if node.tag.split("}")[-1] == property_name and node.text:
            return node.text.strip()
    return None


def check_version_consistency(errors: list[str]) -> None:
    app_project = ROOT / "src/Finora.App/Finora.App.csproj"
    display_version = find_xml_property(app_project, "ApplicationDisplayVersion")
    build_version = find_xml_property(app_project, "ApplicationVersion")
    if not display_version or not build_version:
        errors.append("src/Finora.App/Finora.App.csproj: application version metadata is missing")
        return

    windows_manifest = ROOT / "src/Finora.App/Platforms/Windows/Package.appxmanifest"
    if windows_manifest.exists():
        tree = ET.parse(windows_manifest)
        identity = next((node for node in tree.iter() if node.tag.split("}")[-1] == "Identity"), None)
        manifest_version = identity.attrib.get("Version") if identity is not None else None
        expected = f"{display_version}.0"
        if manifest_version != expected:
            errors.append(f"{rel(windows_manifest)}: package Version {manifest_version!r} does not match application display version {expected!r}")

    readme = ROOT / "README.md"
    if readme.exists() and display_version not in read(readme):
        errors.append(f"README.md: current application version {display_version} is not documented")


def check_schema_consistency(errors: list[str]) -> None:
    constants = ROOT / "src/Finora.Shared/AppConstants.cs"
    schema_doc = ROOT / "docs/architecture/DATABASE_SCHEMA.md"
    if not constants.exists() or not schema_doc.exists():
        return
    match = re.search(r"DatabaseSchemaVersion\s*=\s*(\d+)", read(constants))
    if not match:
        errors.append("src/Finora.Shared/AppConstants.cs: DatabaseSchemaVersion was not found")
        return
    version = match.group(1)
    doc = read(schema_doc)
    if not re.search(rf"\bschema\b[^\n]{{0,80}}(?<!\d){re.escape(version)}(?!\d)", doc, re.IGNORECASE):
        errors.append(f"docs/architecture/DATABASE_SCHEMA.md: schema version {version} is not documented")


def check_money_representation(errors: list[str]) -> None:
    domain_root = ROOT / "src/Finora.Domain"
    if domain_root.exists():
        money_type_pattern = re.compile(rf"\b(?:double|float)\b[^;\n]*\b{MONEY_WORDS}\w*\b|\b{MONEY_WORDS}\w*\b[^;\n]*\b(?:double|float)\b", re.IGNORECASE)
        for path in domain_root.rglob("*.cs"):
            text = read(path)
            for line_number, line in enumerate(text.splitlines(), start=1):
                if money_type_pattern.search(line):
                    errors.append(f"{rel(path)}:{line_number}: floating-point type appears to represent a monetary value")

    app_pages = ROOT / "src/Finora.App/Pages"
    raw_minor_display = re.compile(r"Binding\s+\w*Minor\b[^\n>]{0,220}StringFormat\s*=\s*['\"][^'\"]*\bminor\b", re.IGNORECASE)
    raw_minor_display_reversed = re.compile(r"StringFormat\s*=\s*['\"][^'\"]*\bminor\b[^\n>]{0,220}Binding\s+\w*Minor\b", re.IGNORECASE)
    if app_pages.exists():
        for path in app_pages.rglob("*.xaml"):
            text = read(path)
            if raw_minor_display.search(text) or raw_minor_display_reversed.search(text):
                errors.append(f"{rel(path)}: stored minor-unit values must be converted to currency-aware user-facing money, not labeled as raw minor units")


def _assert_android_rule_domains(path: Path, errors: list[str]) -> None:
    if not path.exists():
        return
    text = read(path)
    for domain in ("root", "file", "database", "sharedpref", "external"):
        if not re.search(rf'<exclude\s+domain="{domain}"\s+path="\."\s*/>', text):
            errors.append(f"{rel(path)}: expected full-domain exclusion for {domain}")


def check_privacy_configuration(errors: list[str]) -> None:
    android_manifest = ROOT / "src/Finora.App/Platforms/Android/AndroidManifest.xml"
    if android_manifest.exists():
        text = read(android_manifest)
        required_fragments = {
            'android:allowBackup="false"': "android:allowBackup must remain false for the local finance store",
            'android:usesCleartextTraffic="false"': "android:usesCleartextTraffic must remain false",
            'android:fullBackupContent="@xml/backup_rules"': "legacy Android full-backup exclusions must remain wired",
            'android:dataExtractionRules="@xml/data_extraction_rules"': "Android 12+ data-extraction exclusions must remain wired",
        }
        for fragment, message in required_fragments.items():
            if fragment not in text:
                errors.append(f"{rel(android_manifest)}: {message}")

    _assert_android_rule_domains(ROOT / "src/Finora.App/Platforms/Android/Resources/xml/backup_rules.xml", errors)
    _assert_android_rule_domains(ROOT / "src/Finora.App/Platforms/Android/Resources/xml/data_extraction_rules.xml", errors)

    settings_xaml = ROOT / "src/Finora.App/Pages/SettingsPage.xaml"
    if settings_xaml.exists():
        text = read(settings_xaml)
        for name in ("BackupPasswordEntry", "NewPinEntry", "ConfirmPinEntry"):
            match = re.search(rf'<Entry\b(?=[^>]*\bx:Name="{name}")[^>]*>', text)
            if match is None or 'IsPassword="True"' not in match.group(0):
                errors.append(f"{rel(settings_xaml)}: {name} must remain a masked password Entry")
        if 'Clicked="OnDeleteAllFinanceDataClicked"' not in text:
            errors.append(f"{rel(settings_xaml)}: complete finance deletion must remain wired to the dedicated reset service handler")

    biometric_service = ROOT / "src/Finora.App/PlatformBiometricService.cs"
    if biometric_service.exists():
        text = read(biometric_service)
        if re.search(r"Result\.Failure\s*\([^)]*errString", text, re.IGNORECASE | re.DOTALL) or re.search(r"errString\s*\?*\.ToString", text):
            errors.append(f"{rel(biometric_service)}: platform biometric provider text must not flow into public failure messages")

    app_root = ROOT / "src/Finora.App"
    secret_prompt = re.compile(r"DisplayPromptAsync\s*\([^;]{0,900}\b(?:password|PIN)\b", re.IGNORECASE | re.DOTALL)
    raw_exception_alert = re.compile(r"DisplayAlertAsync\s*\([^;]{0,900}\b(?:ex|exception)\.Message\b", re.IGNORECASE | re.DOTALL)
    for path in app_root.rglob("*.cs"):
        text = read(path)
        if secret_prompt.search(text):
            errors.append(f"{rel(path)}: secret password/PIN input must use a masked Entry, not DisplayPromptAsync")
        if raw_exception_alert.search(text):
            errors.append(f"{rel(path)}: raw exception messages must not be displayed in user alerts")


def main() -> int:
    paths = files()
    errors: list[str] = []
    check_required_paths(errors)
    check_xml(paths, errors)
    check_empty(paths, errors)
    check_placeholders(paths, errors)
    check_project_references(paths, errors)
    check_xaml_codebehind(paths, errors)
    check_solution_projects(errors)
    check_version_consistency(errors)
    check_schema_consistency(errors)
    check_money_representation(errors)
    check_privacy_configuration(errors)

    if errors:
        print(f"Finora structural preflight FAILED with {len(errors)} issue(s):", file=sys.stderr)
        for issue in errors:
            print(f" - {issue}", file=sys.stderr)
        return 1

    print(f"Finora structural preflight passed: {len(paths)} text/source files checked.")
    print("Validated required files, XML/XAML, project wiring, event handlers, version/schema drift, money representation/display, masked secrets, reset wiring, biometric redaction, and Android privacy/backup rules.")
    print("This is not a compiler, analyzer, test runner, emulator, simulator, signing, or store-validation substitute.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
