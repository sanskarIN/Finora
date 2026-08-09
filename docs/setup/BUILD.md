# Build and Run Finora

Finora is a multi-project .NET MAUI solution. The current source targets .NET 10 TFMs for Android, iOS, Mac Catalyst, and Windows. Use a .NET/MAUI toolchain that supports the target frameworks declared in `src/Finora.App/Finora.App.csproj`.

## Required development tools

Common:

- Git.
- Python 3 for the dependency-free structural preflight.
- .NET 10 SDK compatible with the declared target frameworks.
- .NET MAUI workload.

Platform tooling:

- Android: Android SDK/emulator or physical device tooling.
- Windows: Windows 10/11 development host with required Windows SDK/App SDK support.
- iOS and Mac Catalyst: supported macOS host with Xcode and Apple platform tooling.

Apple archive/signing work requires a compatible Mac/Xcode host. Keep all signing certificates, provisioning profiles, passwords, keystores, and private keys out of the repository.

## Clone

```bash
git clone https://github.com/sanskarIN/Finora.git
cd Finora
```

## Dependency-free preflight

Run this first. It checks XML/XAML/project structure, project-reference targets, empty files, unfinished placeholder markers, and XAML event-handler wiring. It does **not** compile C#.

```bash
python build/scripts/verify_structure.py
```

## Restore and quality gate

```bash
dotnet --info
dotnet workload restore
dotnet restore Finora.sln
dotnet format Finora.sln --verify-no-changes --no-restore
dotnet build Finora.sln -c Release --no-restore
dotnet test Finora.sln -c Release --no-build
```

On Windows/PowerShell the repository wrapper performs the same sequence:

```powershell
./build/scripts/verify.ps1
```

## Platform builds

Android:

```bash
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-android -c Release
```

Windows:

```powershell
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-windows10.0.19041.0 -c Release
```

iOS on a supported Mac/Xcode host:

```bash
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-ios -c Release
```

Mac Catalyst on a supported Mac/Xcode host:

```bash
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-maccatalyst -c Release
```

## Local data locations

Finora uses MAUI `FileSystem.AppDataDirectory` for the SQLite database and app-private receipt/attachment data. Cache exports/diagnostics are placed under `FileSystem.CacheDirectory` before the user explicitly shares/saves them through system UI.

Do not point development builds at a directory containing real finance data. Use synthetic/sample data for development and tests.

## Database schema

The source currently declares schema version 2 in `Finora.Shared.AppConstants`. Existing schema-v1 local databases are upgraded by `DatabaseMigrationRunner`. Migration tests live under `tests/Finora.IntegrationTests`.

Before releasing a build, test:

1. fresh schema creation;
2. upgrade from every previously released schema;
3. force-close/restart around ordinary writes;
4. encrypted backup before upgrade;
5. encrypted backup preview/restore after upgrade;
6. local receipt preservation and checksum verification;
7. the hidden developer-option data-integrity check.

## CI

`.github/workflows/ci.yml` separates structural validation, core tests, Windows/Android MAUI builds, and Apple MAUI builds. CodeQL and dependency-review workflows provide additional repository security gates.

A source file existing in the repository is not proof that a platform feature works on a device. Native notification, biometric, capture-protection, file-picker/share, packaging, and signing behavior must be validated on the corresponding platform.

## Release preparation

Use:

- `docs/releases/RELEASE_CHECKLIST.md`
- `docs/releases/STORE_READINESS.md`
- `docs/TEST_PLAN.md`
- `docs/security/THREAT_MODEL.md`

Never commit API keys, signing secrets, backup passwords, PINs, certificates, private keys, real finance databases, or real receipt images.
