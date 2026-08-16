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
PLACEHOLDER_SCAN_EXCLUSIONS = {"what_changed.md", "build/scripts/verify_structure.py"}
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
BUY_ME_A_COFFEE_URL = "https://buymeacoffee.com/sanskarIN"
DOCUMENTATION_PATHS = [
    "docs/README.md",
    "docs/DOCUMENTATION_STATUS.md",
    "docs/NEXT_STEPS.md",
    "docs/USER_GUIDE.md",
    "docs/TEST_PLAN.md",
    "docs/accessibility/ACCESSIBILITY_AND_LOCALIZATION.md",
    "docs/architecture/OVERVIEW.md",
    "docs/architecture/DATABASE_SCHEMA.md",
    "docs/architecture/SERVICE_CATALOG.md",
    "docs/architecture/DATA_FLOW.md",
    "docs/architecture/NAVIGATION_AND_UI.md",
    "docs/features/ACCOUNTS_AND_TRANSACTIONS.md",
    "docs/features/BUDGETS_GOALS_RECURRING.md",
    "docs/features/REPORTS_IMPORT_EXPORT.md",
    "docs/features/SETTINGS_REFERENCE.md",
    "docs/features/PROJECT_SUPPORT.md",
    "docs/security/THREAT_MODEL.md",
    "docs/security/APP_LOCK_AND_PRIVACY.md",
    "docs/security/BACKUP_AND_RECOVERY.md",
    "docs/privacy/DATA_LIFECYCLE.md",
    "docs/operations/DIAGNOSTICS_AND_INTEGRITY.md",
    "docs/operations/DATA_RESET_AND_SAMPLE_DATA.md",
    "docs/setup/BUILD.md",
    "docs/setup/TROUBLESHOOTING.md",
    "docs/development/DEVELOPER_GUIDE.md",
    "docs/development/CODE_MAP.md",
    "docs/development/ADDING_A_FEATURE.md",
    "docs/testing/TESTING_GUIDE.md",
    "docs/testing/NATIVE_VALIDATION_MATRIX.md",
    "docs/platforms/ANDROID.md",
    "docs/platforms/WINDOWS.md",
    "docs/platforms/APPLE.md",
    "docs/releases/RELEASE_CHECKLIST.md",
    "docs/releases/STORE_READINESS.md",
    "docs/releases/VERSIONING_AND_MIGRATIONS.md",
    "docs/releases/STORE_METADATA_TEMPLATE.md",
]
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
    *DOCUMENTATION_PATHS,
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


def check_markdown_links(paths: list[Path], errors: list[str]) -> None:
    link_pattern = re.compile(r"!?\[[^\]]*\]\(([^)]+)\)")
    for path in paths:
        if path.suffix.lower() != ".md":
            continue
        text = read(path)
        for match in link_pattern.finditer(text):
            raw_target = match.group(1).strip()
            if not raw_target:
                continue
            if raw_target.startswith("<") and raw_target.endswith(">"):
                raw_target = raw_target[1:-1].strip()
            target = raw_target.split(maxsplit=1)[0]
            lowered = target.lower()
            if lowered.startswith(("http://", "https://", "mailto:", "tel:", "data:")) or target.startswith("#"):
                continue
            target = target.split("#", 1)[0].split("?", 1)[0]
            if not target:
                continue
            candidate = (path.parent / target).resolve()
            try:
                candidate.relative_to(ROOT.resolve())
            except ValueError:
                errors.append(f"{rel(path)}: Markdown link escapes repository root: {raw_target}")
                continue
            if not candidate.exists():
                errors.append(f"{rel(path)}: broken repository-relative Markdown link: {raw_target}")


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
        if rel(path) in PLACEHOLDER_SCAN_EXCLUSIONS:
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


def check_product_identity(errors: list[str]) -> None:
    constants = ROOT / "src/Finora.Shared/AppConstants.cs"
    settings_xaml = ROOT / "src/Finora.App/Pages/SettingsPage.xaml"
    about = ROOT / "src/Finora.App/Pages/SettingsPage.About.cs"
    onboarding_xaml = ROOT / "src/Finora.App/Pages/OnboardingPage.xaml"
    onboarding_links = ROOT / "src/Finora.App/Pages/OnboardingPage.Links.cs"
    app_shell = ROOT / "src/Finora.App/AppShell.xaml"
    support_artwork = ROOT / "src/Finora.App/Resources/Images/bmc_support.svg"
    docs_index = ROOT / "docs/README.md"
    roadmap = ROOT / "docs/NEXT_STEPS.md"
    support_doc = ROOT / "SUPPORT.md"
    support_guide = ROOT / "docs/features/PROJECT_SUPPORT.md"

    if constants.exists():
        text = read(constants)
        expected = f'BuyMeACoffeeUrl = "{BUY_ME_A_COFFEE_URL}"'
        if expected not in text:
            errors.append(f"{rel(constants)}: canonical Buy Me a Coffee URL is missing or changed")

    if settings_xaml.exists():
        text = read(settings_xaml)
        if 'Clicked="OnBuyMeACoffeeClicked"' not in text or "Buy Me a Coffee" not in text:
            errors.append(f"{rel(settings_xaml)}: About must expose the Buy Me a Coffee support action")
        if "bmc_support.svg" not in text:
            errors.append(f"{rel(settings_xaml)}: About must retain the branded Buy Me a Coffee artwork")
        if "does not unlock Finora features" not in text:
            errors.append(f"{rel(settings_xaml)}: Buy Me a Coffee must remain explicitly separate from feature entitlement")

    if about.exists():
        text = read(about)
        if "AppConstants.BuyMeACoffeeUrl" not in text:
            errors.append(f"{rel(about)}: Buy Me a Coffee action must use the shared canonical URL")

    if onboarding_xaml.exists():
        text = read(onboarding_xaml)
        if "bmc_support.svg" not in text or 'Clicked="OnOnboardingBuyMeACoffeeClicked"' not in text:
            errors.append(f"{rel(onboarding_xaml)}: onboarding must retain the branded Buy Me a Coffee support surface")
        if "optional external" not in text or "never unlocks app features" not in text:
            errors.append(f"{rel(onboarding_xaml)}: onboarding must keep Buy Me a Coffee optional and separate from entitlement")

    if onboarding_links.exists() and "AppConstants.BuyMeACoffeeUrl" not in read(onboarding_links):
        errors.append(f"{rel(onboarding_links)}: onboarding Buy Me a Coffee action must use the shared canonical URL")

    if app_shell.exists():
        text = read(app_shell)
        if "Shell.FlyoutFooter" not in text or "bmc_support.svg" not in text:
            errors.append(f"{rel(app_shell)}: adaptive navigation must retain the branded Buy Me a Coffee flyout artwork")

    if support_artwork.exists():
        text = read(support_artwork)
        if "SUPPORT FINORA" not in text or "BUY ME A COFFEE" not in text:
            errors.append(f"{rel(support_artwork)}: branded Buy Me a Coffee artwork text is missing")

    for path in (docs_index, roadmap, support_doc, support_guide):
        if path.exists() and BUY_ME_A_COFFEE_URL not in read(path):
            errors.append(f"{rel(path)}: canonical Buy Me a Coffee URL is not documented")


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
    check_markdown_links(paths, errors)
    check_xml(paths, errors)
    check_empty(paths, errors)
    check_placeholders(paths, errors)
    check_project_references(paths, errors)
    check_xaml_codebehind(paths, errors)
    check_solution_projects(errors)
    check_version_consistency(errors)
    check_schema_consistency(errors)
    check_product_identity(errors)
    check_money_representation(errors)
    check_privacy_configuration(errors)

    if errors:
        print(f"Finora structural preflight FAILED with {len(errors)} issue(s):", file=sys.stderr)
        for issue in errors:
            print(f" - {issue}", file=sys.stderr)
        return 1

    print(f"Finora structural preflight passed: {len(paths)} text/source files checked.")
    print("Validated required documentation/repository files and local Markdown links, product/support identity, XML/XAML, project wiring, event handlers, version/schema drift, money representation/display, masked secrets, reset wiring, biometric redaction, and Android privacy/backup rules.")
    print("This is not a compiler, analyzer, test runner, emulator, simulator, signing, or store-validation substitute.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
