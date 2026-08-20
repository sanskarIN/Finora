#!/usr/bin/env python3
"""Validate Finora's cross-platform source/build contract without restoring .NET packages.

This check intentionally proves repository wiring only. It does not replace compiler,
native-device, browser-runtime, packaging, signing, accessibility, or store validation.
"""
from __future__ import annotations

import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
AVALONIA_VERSION = "12.1.1"

REQUIRED_PATHS = (
    "Finora.CrossPlatform.slnx",
    ".github/workflows/cross-platform.yml",
    "docs/platforms/CROSS_PLATFORM.md",
    "docs/platforms/LINUX.md",
    "docs/platforms/WEB.md",
    "docs/platforms/CHROMEOS.md",
    "docs/development/CROSS_PLATFORM_FILE_REFERENCE.md",
    "src/Finora.Universal/Finora.Universal.csproj",
    "src/Finora.Universal/App.axaml",
    "src/Finora.Universal/App.axaml.cs",
    "src/Finora.Universal/UniversalRuntime.cs",
    "src/Finora.Universal/Views/MainView.axaml",
    "src/Finora.Universal.Desktop/Finora.Universal.Desktop.csproj",
    "src/Finora.Universal.Desktop/DesktopUniversalRuntime.cs",
    "src/Finora.Universal.Browser/Finora.Universal.Browser.csproj",
    "src/Finora.Universal.Browser/BrowserUniversalRuntime.cs",
    "src/Finora.Universal.Browser/wwwroot/index.html",
    "src/Finora.Universal.Browser/wwwroot/manifest.webmanifest",
    "src/Finora.Universal.Browser/wwwroot/finora-icon.svg",
    "tools/Finora.Performance/Finora.Performance.csproj",
)

EXPECTED_SOLUTION_PROJECTS = (
    "src/Finora.Shared/Finora.Shared.csproj",
    "src/Finora.Domain/Finora.Domain.csproj",
    "src/Finora.Application/Finora.Application.csproj",
    "src/Finora.Infrastructure/Finora.Infrastructure.csproj",
    "src/Finora.App/Finora.App.csproj",
    "src/Finora.Universal/Finora.Universal.csproj",
    "src/Finora.Universal.Desktop/Finora.Universal.Desktop.csproj",
    "src/Finora.Universal.Browser/Finora.Universal.Browser.csproj",
    "tests/Finora.UnitTests/Finora.UnitTests.csproj",
    "tests/Finora.IntegrationTests/Finora.IntegrationTests.csproj",
    "tests/Finora.UiTests/Finora.UiTests.csproj",
    "tools/Finora.Performance/Finora.Performance.csproj",
)

AVALONIA_PACKAGES = (
    "Avalonia",
    "Avalonia.Desktop",
    "Avalonia.Browser",
    "Avalonia.Themes.Fluent",
    "Avalonia.Fonts.Inter",
)


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8-sig")


def package_versions() -> dict[str, str]:
    tree = ET.parse(ROOT / "Directory.Packages.props")
    versions: dict[str, str] = {}
    for node in tree.findall(".//PackageVersion"):
        name = node.attrib.get("Include")
        version = node.attrib.get("Version")
        if name and version:
            versions[name] = version
    return versions


def project_property(relative: str, name: str) -> str | None:
    tree = ET.parse(ROOT / relative)
    node = tree.find(f".//{name}")
    return node.text.strip() if node is not None and node.text else None


def project_target_framework(relative: str) -> str | None:
    return project_property(relative, "TargetFramework")


def project_sdk(relative: str) -> str | None:
    tree = ET.parse(ROOT / relative)
    return tree.getroot().attrib.get("Sdk")


def solution_projects(relative: str) -> tuple[str, ...]:
    tree = ET.parse(ROOT / relative)
    projects = []
    for node in tree.findall(".//Project"):
        path = node.attrib.get("Path")
        if path:
            projects.append(path.replace("\\", "/"))
    return tuple(projects)


def validate_pwa_manifest(relative: str) -> list[str]:
    errors: list[str] = []
    try:
        manifest = json.loads(read(relative))
    except json.JSONDecodeError as exc:
        return [f"PWA manifest is not valid JSON: {exc.msg}"]

    if not isinstance(manifest, dict):
        return ["PWA manifest root must be a JSON object"]

    if manifest.get("name") != "Finora":
        errors.append("PWA manifest name must be Finora")
    if manifest.get("display") != "standalone":
        errors.append("PWA manifest display mode must be standalone")

    icons = manifest.get("icons")
    if not isinstance(icons, list):
        errors.append("PWA manifest icons must be an array")
        return errors

    icon_sources = {
        item.get("src")
        for item in icons
        if isinstance(item, dict) and isinstance(item.get("src"), str)
    }
    if not any(Path(source).name == "finora-icon.svg" for source in icon_sources):
        errors.append("PWA manifest must reference finora-icon.svg")

    return errors


def validate() -> list[str]:
    errors: list[str] = []

    for relative in REQUIRED_PATHS:
        path = ROOT / relative
        if not path.is_file():
            errors.append(f"missing required cross-platform path: {relative}")
        elif path.stat().st_size == 0:
            errors.append(f"cross-platform path is empty: {relative}")

    if errors:
        return errors

    versions = package_versions()
    for package in AVALONIA_PACKAGES:
        actual = versions.get(package)
        if actual != AVALONIA_VERSION:
            errors.append(
                f"{package} must be centrally pinned to {AVALONIA_VERSION}; found {actual!r}"
            )

    solution = solution_projects("Finora.CrossPlatform.slnx")
    if solution != EXPECTED_SOLUTION_PROJECTS:
        missing = [item for item in EXPECTED_SOLUTION_PROJECTS if item not in solution]
        unexpected = [item for item in solution if item not in EXPECTED_SOLUTION_PROJECTS]
        if missing:
            errors.append(f"cross-platform solution is missing projects: {', '.join(missing)}")
        if unexpected:
            errors.append(f"cross-platform solution has unexpected projects: {', '.join(unexpected)}")
        if not missing and not unexpected:
            errors.append("cross-platform solution project order does not match the documented contract")

    universal_tfm = project_target_framework("src/Finora.Universal/Finora.Universal.csproj")
    desktop_tfm = project_target_framework(
        "src/Finora.Universal.Desktop/Finora.Universal.Desktop.csproj"
    )
    browser_tfm = project_target_framework(
        "src/Finora.Universal.Browser/Finora.Universal.Browser.csproj"
    )
    browser_sdk = project_sdk("src/Finora.Universal.Browser/Finora.Universal.Browser.csproj")
    compiled_bindings = project_property(
        "src/Finora.Universal/Finora.Universal.csproj",
        "AvaloniaUseCompiledBindingsByDefault",
    )

    if universal_tfm != "net10.0":
        errors.append(f"universal presentation target must be net10.0; found {universal_tfm!r}")
    if desktop_tfm != "net10.0":
        errors.append(f"universal desktop target must be net10.0; found {desktop_tfm!r}")
    if browser_tfm != "net10.0-browser":
        errors.append(
            f"universal browser target must be net10.0-browser; found {browser_tfm!r}"
        )
    if browser_sdk != "Microsoft.NET.Sdk.WebAssembly":
        errors.append(
            "universal browser project must use Microsoft.NET.Sdk.WebAssembly; "
            f"found {browser_sdk!r}"
        )
    if compiled_bindings != "true":
        errors.append(
            "universal presentation project must explicitly enable Avalonia compiled bindings"
        )

    app_code = read("src/Finora.Universal/App.axaml.cs")
    if "public partial class App : Avalonia.Application" not in app_code:
        errors.append(
            "universal App must explicitly inherit Avalonia.Application to avoid namespace/type ambiguity"
        )

    main_view = read("src/Finora.Universal/Views/MainView.axaml")
    if 'x:DataType="vm:MainViewModel"' not in main_view:
        errors.append("universal main view must declare its compiled-binding MainViewModel type")

    desktop_runtime = read("src/Finora.Universal.Desktop/DesktopUniversalRuntime.cs")
    for required in ("OperatingSystem.IsLinux()", "OperatingSystem.IsWindows()", "OperatingSystem.IsMacOS()"):
        if required not in desktop_runtime:
            errors.append(f"desktop runtime is missing platform detection contract: {required}")
    if "UseSqlite" not in desktop_runtime or "DatabaseInitializer" not in desktop_runtime:
        errors.append("desktop runtime must reuse the native SQLite/database initialization path")

    browser_runtime = read("src/Finora.Universal.Browser/BrowserUniversalRuntime.cs")
    if "false" not in browser_runtime or "Native SQLite" not in browser_runtime:
        errors.append(
            "browser runtime must explicitly keep persistent finance disabled until browser storage parity is validated"
        )

    browser_project = read("src/Finora.Universal.Browser/Finora.Universal.Browser.csproj")
    if "Finora.Infrastructure" in browser_project or "SQLite" in browser_project:
        errors.append("browser host must not directly reference native Finora.Infrastructure/SQLite")

    errors.extend(
        validate_pwa_manifest("src/Finora.Universal.Browser/wwwroot/manifest.webmanifest")
    )

    workflow = read(".github/workflows/cross-platform.yml")
    for token in ("ubuntu-latest", "windows-latest", "macos-latest", "wasm-tools"):
        if token not in workflow:
            errors.append(f"cross-platform workflow is missing required build token: {token}")

    matrix = read("docs/platforms/CROSS_PLATFORM.md")
    for platform in ("Android", "iPhone / iPad", "Windows 10/11", "macOS", "Linux", "Web / modern browsers", "ChromeOS"):
        if platform not in matrix:
            errors.append(f"cross-platform support matrix is missing: {platform}")
    if "Finance persistence is intentionally disabled" not in read("docs/platforms/WEB.md"):
        errors.append("Web documentation must preserve the explicit browser persistence boundary")

    return errors


def main() -> int:
    try:
        errors = validate()
    except (OSError, ET.ParseError) as exc:
        print(f"Cross-platform contract check failed to run: {exc}", file=sys.stderr)
        return 2

    if errors:
        print("Finora cross-platform contract check failed:", file=sys.stderr)
        for error in errors:
            print(f"  - {error}", file=sys.stderr)
        return 1

    print(
        "Finora cross-platform contract OK: complete solution wiring, MAUI preservation, "
        "universal desktop hosts, WebAssembly/PWA wiring, compiled bindings, browser persistence "
        "boundary, package pins, and platform docs are present."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
