# Build and Run Finora

Finora is a multi-project .NET 10 solution with two presentation families: the established .NET MAUI application for Android, iOS/iPadOS, Mac Catalyst, and Windows, plus an additive Avalonia universal path for Linux/Windows/macOS desktop and WebAssembly/browser delivery. Use a .NET 10 toolchain compatible with the declared target frameworks.

Complete documentation index: [`docs/README.md`](../README.md)  
Cross-platform matrix: [`docs/platforms/CROSS_PLATFORM.md`](../platforms/CROSS_PLATFORM.md)  
Prioritized next steps: [`docs/NEXT_STEPS.md`](../NEXT_STEPS.md)  
Exhaustive tracked-file ownership: [`docs/development/REPOSITORY_FILE_REFERENCE.md`](../development/REPOSITORY_FILE_REFERENCE.md) and [`docs/development/CROSS_PLATFORM_FILE_REFERENCE.md`](../development/CROSS_PLATFORM_FILE_REFERENCE.md)

## Required development tools

Common:

- Git.
- Python 3 for dependency-free structural/repository QA.
- .NET 10 SDK compatible with the declared target frameworks.
- .NET MAUI workload for native MAUI app builds.
- .NET WebAssembly workload (`wasm-tools`) for the browser host.

Platform tooling:

- Android: Android SDK/emulator or physical-device tooling.
- Windows: Windows 10/11 development host with required Windows SDK/App SDK support.
- iOS and Mac Catalyst: supported macOS host with Xcode and Apple platform tooling.
- Linux universal desktop: a supported .NET 10 Linux environment and the desktop libraries required by Avalonia/runtime packaging.
- Web/WASM: a modern browser for runtime validation after the WebAssembly build.

Apple archive/signing work requires a compatible Mac/Xcode host. Keep all signing certificates, provisioning profiles, passwords, keystores, and private keys out of the repository.

## Clone

```bash
git clone https://github.com/sanskarIN/Finora.git
cd Finora
```

## Dependency-free preflight and repository QA

Run these first:

```bash
python build/scripts/verify_structure.py
python scripts/run_repo_qa.py
```

### Structural preflight

The current structural preflight checks:

- required repository/legal/community files;
- the complete required documentation tree, including `docs/NEXT_STEPS.md`;
- repository-relative Markdown file links;
- canonical product/support identity including `https://buymeacoffee.com/sanskarIN`;
- Settings/About Buy Me a Coffee handler/shared-constant wiring and the no-feature-unlock boundary;
- XML/XAML/RESX/project parsing;
- project/solution references;
- empty files;
- unfinished placeholder markers;
- XAML event handlers;
- app/package version consistency;
- schema-document consistency;
- suspicious floating-point monetary representation;
- raw minor-unit passive XAML display patterns;
- masked Settings backup/PIN secret fields;
- password/PIN prompt regressions;
- complete finance-reset handler wiring;
- biometric provider-text redaction;
- raw exception-message alert regressions;
- Android local-data privacy/backup rules.

The Markdown check validates repository-relative file targets only. It does not make network requests and does not attempt to prove external URLs or section anchors are reachable. Likewise, the preflight verifies the configured Buy Me a Coffee URL string but does not verify that the external service is reachable or allowed by a target app store.

Structural preflight does **not** compile C#, restore NuGet packages, execute analyzers, run .NET tests, build native targets, sign packages, or validate devices/stores.

### Repository QA runner

`scripts/run_repo_qa.py` executes the dependency-free developer QA suite:

1. Python developer-tool unit tests from `scripts/tests/`;
2. tracked-file documentation coverage through `scripts/check_documentation_coverage.py`; and
3. localization validation through `scripts/validate_localization.py`.

The documentation coverage check reads the exact tracked set from `git ls-files` and compares it with the canonical repository reference plus approved narrow companion inventories such as `docs/development/CROSS_PLATFORM_FILE_REFERENCE.md`. Every tracked file must be covered by an exact path or a meaningful narrow directory responsibility. Stale entries and broad one-component catch-all prefixes such as `src/`, `docs/`, or `tests/` fail the check.

Run only the coverage check with:

```bash
python scripts/check_documentation_coverage.py
```

Run the repository QA and continue into the .NET test suite when the SDK is available with:

```bash
python scripts/run_repo_qa.py --include-dotnet
```

A passing repository QA run proves those dependency-free repository contracts for the checked source tree. It does not prove native runtime, signed packaging, accessibility, biometric behavior, notification delivery, store compliance, installed upgrades, browser-storage durability, or interrupted recovery.

## Recommended repository wrappers

Windows PowerShell:

```powershell
./build/scripts/verify.ps1
```

macOS/Linux shell:

```bash
./build/scripts/verify.sh
```

Both wrappers run structural preflight plus the three core test projects. Native MAUI work is host-aware:

- Windows wrapper builds Windows + Android.
- macOS shell builds iOS + Mac Catalyst.
- Linux runs core verification and delegates MAUI native builds to CI-supported hosts.
- `FINORA_SKIP_MAUI=1` intentionally skips native MAUI builds after core verification when needed for a core-only check.

The universal desktop and browser hosts are validated separately through `.github/workflows/cross-platform.yml`. Do not use a core-only run as release evidence for a native or browser platform.

## Manual core verification

```bash
dotnet --info
dotnet restore tests/Finora.UnitTests/Finora.UnitTests.csproj
dotnet restore tests/Finora.IntegrationTests/Finora.IntegrationTests.csproj
dotnet restore tests/Finora.UiTests/Finora.UiTests.csproj
dotnet test tests/Finora.UnitTests/Finora.UnitTests.csproj -c Release --no-restore
dotnet test tests/Finora.IntegrationTests/Finora.IntegrationTests.csproj -c Release --no-restore
dotnet test tests/Finora.UiTests/Finora.UiTests.csproj -c Release --no-restore
```

`Directory.Build.props` enables nullable analysis, warnings-as-errors, recommended analyzers, and deterministic builds. Formatting cleanup is encouraged, but `dotnet format --verify-no-changes` is not used as the current correctness/release gate because formatting-only drift must not hide compiler/test results.

## MAUI native platform builds

Install/restore the MAUI workload before the native app build:

```bash
dotnet workload restore src/Finora.App/Finora.App.csproj
dotnet restore src/Finora.App/Finora.App.csproj
```

Android (supported Windows CI/dev host):

```bash
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-android -c Release --no-restore
```

Windows:

```powershell
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-windows10.0.19041.0 -c Release --no-restore
```

iOS on supported Mac/Xcode host:

```bash
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-ios -c Release --no-restore
```

Mac Catalyst on supported Mac/Xcode host:

```bash
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-maccatalyst -c Release --no-restore
```

## Universal desktop builds

The Avalonia desktop host is one project for Linux, Windows, and macOS:

```bash
dotnet restore src/Finora.Universal.Desktop/Finora.Universal.Desktop.csproj
dotnet build src/Finora.Universal.Desktop/Finora.Universal.Desktop.csproj -c Release --no-restore
```

Run it locally with:

```bash
dotnet run --project src/Finora.Universal.Desktop/Finora.Universal.Desktop.csproj
```

The desktop runtime creates/opens the Finora SQLite database beneath the operating system's local application-data location and runs the existing `DatabaseInitializer`. The current universal UI exposes a privacy-safe runtime/account-count surface; it is a foundation for Linux portability and must not be confused with complete MAUI-screen parity.

## WebAssembly / PWA build

Install the WebAssembly workload once for the SDK/environment, then restore and build:

```bash
dotnet workload install wasm-tools
dotnet restore src/Finora.Universal.Browser/Finora.Universal.Browser.csproj
dotnet build src/Finora.Universal.Browser/Finora.Universal.Browser.csproj -c Release --no-restore
```

The WebAssembly host is intentionally separated from native SQLite. It starts the shared universal UI and includes a PWA manifest, but persistent finance workflows remain disabled until a dedicated browser-local encrypted persistence adapter passes the requirements in [`docs/platforms/WEB.md`](../platforms/WEB.md).

## Cross-platform solution

`Finora.CrossPlatform.slnx` groups the shared/core projects, existing MAUI app, universal presentation project, desktop host, browser host, and test projects. It is a convenient IDE/build entry point; platform-specific target/workload requirements still apply.

Platform-specific engineering/QA docs:

- [`docs/platforms/CROSS_PLATFORM.md`](../platforms/CROSS_PLATFORM.md)
- [`docs/platforms/ANDROID.md`](../platforms/ANDROID.md)
- [`docs/platforms/WINDOWS.md`](../platforms/WINDOWS.md)
- [`docs/platforms/APPLE.md`](../platforms/APPLE.md)
- [`docs/platforms/LINUX.md`](../platforms/LINUX.md)
- [`docs/platforms/WEB.md`](../platforms/WEB.md)
- [`docs/platforms/CHROMEOS.md`](../platforms/CHROMEOS.md)
- [`docs/testing/NATIVE_VALIDATION_MATRIX.md`](../testing/NATIVE_VALIDATION_MATRIX.md)

## Local data locations

The MAUI application uses `FileSystem.AppDataDirectory` for the SQLite database, receipt files, and transient crash-recovery metadata. Cache exports/diagnostics are placed under `FileSystem.CacheDirectory` before the user explicitly shares/saves them through system UI.

The universal desktop host uses `Environment.SpecialFolder.LocalApplicationData` (with a user-profile fallback) and a Finora subdirectory for its native SQLite database. Platform migration/shared-data behavior between MAUI and universal desktop packages must be designed and tested explicitly before either host is assumed to share an installed data location.

The browser host does not open the native SQLite database.

Important app-private runtime items in the mature MAUI/native infrastructure include:

- SQLite database/WAL/SHM;
- `attachments/` receipt/document tree;
- transient `finora-restore-recovery.json` journal during crash-safe restore;
- transient `attachments.rollback.*` / `attachments.restore.*` recovery directories.

Recovery artifacts are automatically resolved/cleaned after their operation decision. Do not manually delete them while a restore/recovery is active.

Never point development builds at real finance data. Use synthetic/sample data only.

## Database/schema and recovery validation

The source currently declares schema version 2. Existing schema-v1 databases upgrade through `DatabaseMigrationRunner`.

Before releasing, test:

1. fresh schema creation;
2. v1 → v2 and every released migration path;
3. direct-EF persistence invariant failures;
4. force-close/restart around ordinary writes;
5. encrypted backup creation/preview/restore;
6. wrong/tampered backup rejection;
7. receipt checksum/path validation;
8. process termination during each restore recovery phase;
9. startup recovery before normal navigation;
10. full finance reset preserving schema/preferences/PIN state;
11. deterministic synthetic sample reset;
12. the hidden developer data-integrity check.

References:

- [`docs/releases/VERSIONING_AND_MIGRATIONS.md`](../releases/VERSIONING_AND_MIGRATIONS.md)
- [`docs/security/BACKUP_AND_RECOVERY.md`](../security/BACKUP_AND_RECOVERY.md)
- [`docs/operations/DIAGNOSTICS_AND_INTEGRITY.md`](../operations/DIAGNOSTICS_AND_INTEGRITY.md)

## Currency validation

Finora stores integer minor units and contains built-in zero-/two-/three-decimal precision metadata. Release QA must verify the currency precision metadata needed for targeted release markets. Unlike currencies are not converted/aggregated silently; multi-currency reporting behavior must be tested explicitly.

## CI

`.github/workflows/ci.yml` continues to separate:

- structural preflight plus dependency-free repository QA on Ubuntu;
- unit/integration/UI-contract tests on Ubuntu;
- Windows + Android MAUI builds on Windows;
- iOS + Mac Catalyst MAUI builds on macOS.

`.github/workflows/cross-platform.yml` adds:

- universal desktop builds on Ubuntu, Windows, and macOS;
- WebAssembly workload restore/build validation on Ubuntu.

The primary CI structural-preflight job executes:

```bash
python build/scripts/verify_structure.py
python scripts/run_repo_qa.py
```

This makes tracked-file documentation coverage, Python developer-tool tests, and localization validation prerequisites for downstream CI work.

CodeQL/dependency-review repository workflows provide additional security gates. Current workflow action major versions are intentionally conservative; update them only after compatibility/security review.

A source file existing in the repository—or being listed by the documentation coverage check—is not proof that a platform feature works on a device. Notification, biometric, capture-protection, adaptive navigation, accessibility, file-picker/share, packaging, signing, WebAssembly/browser persistence, and interrupted-restore behavior require validation on the corresponding platform.

## Release preparation

Use:

- [`docs/README.md`](../README.md)
- [`docs/platforms/CROSS_PLATFORM.md`](../platforms/CROSS_PLATFORM.md)
- [`docs/NEXT_STEPS.md`](../NEXT_STEPS.md)
- [`docs/development/REPOSITORY_FILE_REFERENCE.md`](../development/REPOSITORY_FILE_REFERENCE.md)
- [`docs/development/CROSS_PLATFORM_FILE_REFERENCE.md`](../development/CROSS_PLATFORM_FILE_REFERENCE.md)
- [`docs/testing/REPOSITORY_QA.md`](../testing/REPOSITORY_QA.md)
- [`docs/testing/TESTING_GUIDE.md`](../testing/TESTING_GUIDE.md)
- [`docs/TEST_PLAN.md`](../TEST_PLAN.md)
- [`docs/releases/RELEASE_CHECKLIST.md`](../releases/RELEASE_CHECKLIST.md)
- [`docs/releases/STORE_READINESS.md`](../releases/STORE_READINESS.md)
- [`docs/releases/STORE_METADATA_TEMPLATE.md`](../releases/STORE_METADATA_TEMPLATE.md)
- [`docs/security/THREAT_MODEL.md`](../security/THREAT_MODEL.md)

If the packaged build contains the external Buy Me a Coffee support link, verify the target store's current external contribution/payment-link policy before submission. The link must never be treated as Finora feature entitlement or secure commercial licensing.

Never commit API keys, signing secrets, backup passwords, PINs, certificates, private keys, real finance databases, or real receipt images.
