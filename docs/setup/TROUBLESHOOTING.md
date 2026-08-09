# Finora Troubleshooting

Use synthetic/sample data while diagnosing problems. Never attach a real Finora database, real receipts, PINs, backup passwords, signing keys, or private financial records to a public issue.

## Structural preflight fails

Run:

```bash
python build/scripts/verify_structure.py
```

The preflight reports malformed XML/XAML/RESX/project files, missing project-reference targets, empty source/resource files, unfinished placeholder markers, and XAML event handlers without matching C# methods. Fix the reported file before attempting a release build.

The script is not a C# compiler and cannot replace `dotnet build` or tests.

## `dotnet` is not found

Install a .NET SDK compatible with the target frameworks in `src/Finora.App/Finora.App.csproj`, then verify:

```bash
dotnet --info
```

Do not change Finora target frameworks merely to make an unrelated older SDK accept the solution.

## MAUI workload is missing

Run:

```bash
dotnet workload restore
```

If the workload manifest or platform SDK is unavailable, update/install the supported .NET/MAUI/Android/Apple/Windows toolchain on the build host rather than committing generated workload files.

## NuGet restore fails

- Confirm the SDK version expected by the source.
- Clear only NuGet caches that are safe to rehydrate; do not delete Finora app data.
- Confirm `Directory.Packages.props` remains the central package-version source.
- Do not bypass dependency/security warnings by disabling them globally.

## Formatting or warnings fail the build

Finora enables nullable reference types, latest-recommended analysis, deterministic builds, and warnings-as-errors through `Directory.Build.props`.

Run:

```bash
dotnet format Finora.sln --verify-no-changes --no-restore
dotnet build Finora.sln -c Release --no-restore
```

Correct the source warning instead of suppressing analyzers broadly. Narrow suppressions require a documented reason.

## SQLite database is locked

Finora enables WAL, foreign keys, and a busy timeout. Close extra debug instances and ensure tests are not sharing the same database file. Do not manually delete `-wal`/`-shm` files from a live application database.

## Database reports a newer schema

Do not downgrade the schema number manually. Use a Finora build that supports the database version or restore a compatible encrypted backup. Schema-version advancement is controlled by migrations.

## Migration from schema v1 to v2 fails

Keep the original database untouched and work on a copy made from synthetic/test data. Run the migration integration tests. Do not edit production-style databases manually to force `schema.version` forward.

## Developer integrity check reports an error

The hidden developer option checks SQLite integrity, foreign keys, transfer pairing, split totals, category cycles, recurrence links, and receipt file size/checksum/path safety.

- Export the sanitized integrity report if needed.
- Do not publish the database itself.
- Do not create a new backup over the only known-good external backup until the integrity issue is understood.
- Use `SECURITY.md` for suspected data exposure or security defects.

## Backup preview or restore is rejected

Possible reasons include:

- wrong password;
- tampered/truncated backup;
- unsupported schema version;
- invalid attachment path/size/checksum;
- file too large or unreadable.

Finora intentionally fails closed. Do not weaken AES-GCM authentication or backup validation to make a damaged file import.

## Receipt/attachment file is missing

The transaction record may still contain attachment metadata. Use the integrity checker and orphan-file cleanup. A missing receipt file cannot be reconstructed from metadata unless an encrypted backup contains the receipt bytes.

## Local notification does not appear

- Confirm notification permission is granted.
- Confirm notifications and the relevant reminder are enabled in Settings.
- Use the developer reminder-sync action.
- Test OS power-management/reboot/force-stop behavior on the target platform; operating systems can impose scheduling restrictions.
- Notification text intentionally avoids private transaction details.

## Biometrics / Windows Hello unavailable

Finora requires a configured PIN fallback before biometric unlock can be enabled. Confirm biometric/Hello enrollment and platform availability. Cancellation or lockout should return to the PIN path rather than bypassing app lock.

## Sensitive-screen capture protection unavailable

Android and supported Windows paths have platform-specific protection. Other platform paths may not provide a universal screenshot-blocking API. Finora should report the limitation rather than claiming protection that the OS cannot guarantee.

## Apple build attempted on Windows

Use a supported Mac with compatible Xcode for iOS/Mac Catalyst archive/signing/device validation. Source-level compilation of other projects on Windows does not replace an Apple platform build.

## Windows packaging identity/signing fails

Release packaging must use the final package identity/publisher and signing material supplied outside the repository. Never commit a signing certificate password or private key.

## Android signing fails

Configure release keystore/signing through secure external build/release configuration. Never add the keystore or password to source control.

## File picker/share sheet behaves differently by platform

Backup, restore, import, export, and attachment workflows intentionally use system pickers/share surfaces. Test packaged/signed builds because sandbox/identity behavior may differ from debug deployment.

## Public bug report

Use `.github/ISSUE_TEMPLATE/bug_report.yml` with synthetic data only. Security vulnerabilities and possible private-data exposure must be reported privately according to `SECURITY.md`.
