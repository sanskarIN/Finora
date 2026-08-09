# Build and Run Finora

Finora is a multi-project .NET MAUI solution. The current source targets .NET 10 TFMs for Android, iOS, Mac Catalyst, and Windows. Use a .NET/MAUI toolchain that supports the target frameworks declared in `src/Finora.App/Finora.App.csproj`.

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

The current preflight checks required repository files, XML/XAML/RESX/project parsing, project/solution references, empty files, unfinished placeholder markers, XAML event handlers, app/package version consistency, schema-document consistency, suspicious floating-point monetary representation, and Android local-data privacy flags.

It does **not** compile C#, restore NuGet packages, execute analyzers, run tests, build native targets, sign packages, or validate devices/stores.

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

- `docs/releases/RELEASE_CHECKLIST.md`
- `docs/releases/STORE_READINESS.md`
- `docs/TEST_PLAN.md`
- `docs/security/THREAT_MODEL.md`

Never commit API keys, signing secrets, backup passwords, PINs, certificates, private keys, real finance databases, or real receipt images.
