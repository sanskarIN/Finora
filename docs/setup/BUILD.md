# Build and Run Finora

Finora is a multi-project .NET MAUI solution. The current source targets .NET 10 TFMs for Android, iOS, Mac Catalyst, and Windows. Use a .NET/MAUI toolchain that supports the target frameworks declared in `src/Finora.App/Finora.App.csproj`.

Complete documentation index: [`docs/README.md`](../README.md)  
Prioritized next steps: [`docs/NEXT_STEPS.md`](../NEXT_STEPS.md)

## Required development tools

Common:

- Git.
- Python 3 for the dependency-free structural preflight.
- .NET 10 SDK compatible with the declared target frameworks.
- .NET MAUI workload for native app builds.

Platform tooling:

- Android: Android SDK/emulator or physical-device tooling.
- Windows: Windows 10/11 development host with required Windows SDK/App SDK support.
- iOS and Mac Catalyst: supported macOS host with Xcode and Apple platform tooling.

Apple archive/signing work requires a compatible Mac/Xcode host. Keep all signing certificates, provisioning profiles, passwords, keystores, and private keys out of the repository.

## Clone

```bash
git clone https://github.com/sanskarIN/Finora.git
cd Finora
```

## Dependency-free preflight

Run this first:

```bash
python build/scripts/verify_structure.py
```

The current preflight checks:

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

Structural preflight does **not** compile C#, restore NuGet packages, execute analyzers, run tests, build native targets, sign packages, or validate devices/stores.

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
- Linux runs core verification and delegates native builds to CI-supported hosts.
- `FINORA_SKIP_MAUI=1` intentionally skips native MAUI builds after core verification when needed for a core-only check.

Do not use a core-only run as release evidence for a native platform.

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

## Native platform builds

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

Platform-specific engineering/QA docs:

- [`docs/platforms/ANDROID.md`](../platforms/ANDROID.md)
- [`docs/platforms/WINDOWS.md`](../platforms/WINDOWS.md)
- [`docs/platforms/APPLE.md`](../platforms/APPLE.md)
- [`docs/testing/NATIVE_VALIDATION_MATRIX.md`](../testing/NATIVE_VALIDATION_MATRIX.md)

## Local data locations

Finora uses `FileSystem.AppDataDirectory` for the SQLite database, receipt files, and transient crash-recovery metadata. Cache exports/diagnostics are placed under `FileSystem.CacheDirectory` before the user explicitly shares/saves them through system UI.

Important app-private runtime items include:

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

`.github/workflows/ci.yml` currently separates:

- structural preflight on Ubuntu;
- unit/integration/UI-contract tests on Ubuntu;
- Windows + Android MAUI builds on Windows;
- iOS + Mac Catalyst MAUI builds on macOS.

CodeQL/dependency-review repository workflows provide additional security gates. Current workflow action major versions are intentionally conservative; update them only after compatibility/security review.

A source file existing in the repository is not proof that a platform feature works on a device. Notification, biometric, capture-protection, adaptive navigation, accessibility, file-picker/share, packaging, signing, and interrupted-restore behavior require validation on the corresponding platform.

## Release preparation

Use:

- [`docs/README.md`](../README.md)
- [`docs/NEXT_STEPS.md`](../NEXT_STEPS.md)
- [`docs/testing/TESTING_GUIDE.md`](../testing/TESTING_GUIDE.md)
- [`docs/TEST_PLAN.md`](../TEST_PLAN.md)
- [`docs/releases/RELEASE_CHECKLIST.md`](../releases/RELEASE_CHECKLIST.md)
- [`docs/releases/STORE_READINESS.md`](../releases/STORE_READINESS.md)
- [`docs/releases/STORE_METADATA_TEMPLATE.md`](../releases/STORE_METADATA_TEMPLATE.md)
- [`docs/security/THREAT_MODEL.md`](../security/THREAT_MODEL.md)

If the packaged build contains the external Buy Me a Coffee support link, verify the target store's current external contribution/payment-link policy before submission. The link must never be treated as Finora feature entitlement or secure commercial licensing.

Never commit API keys, signing secrets, backup passwords, PINs, certificates, private keys, real finance databases, or real receipt images.
