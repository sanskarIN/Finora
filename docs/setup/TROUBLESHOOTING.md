# Finora Troubleshooting

Use synthetic/sample data while diagnosing problems. Never attach a real Finora database, real receipts, PINs, backup passwords, signing keys, or private financial records to a public issue.

## Structural preflight fails

Run:

```bash
python build/scripts/verify_structure.py
```

The preflight reports malformed XML/XAML/RESX/project files, missing project-reference targets, empty source/resource files, unfinished placeholder markers, XAML event handlers without matching C# methods, version/schema drift, selected money-representation violations, and required policy/platform metadata problems.

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

## Warnings/analyzers fail the build

Finora enables nullable reference types, latest-recommended analysis, deterministic builds, and warnings-as-errors through `Directory.Build.props`.

Correct the source warning instead of suppressing analyzers broadly. Narrow suppressions require a documented reason.

## SQLite database is locked

Finora enables WAL, foreign keys, and a busy timeout. Close extra debug instances and ensure tests are not sharing the same database file. Do not manually delete `-wal`/`-shm` files from a live application database.

## Database reports a newer schema

Do not downgrade the schema number manually. Use a Finora build that supports the database version or restore a compatible encrypted backup. Schema-version advancement is controlled by migrations.

## Migration from schema v1 to v2 fails

Keep the original database untouched and work on a copy made from synthetic/test data. Run the migration integration tests. Do not edit production-style databases manually to force `schema.version` forward.

## Developer integrity check reports an error

The hidden developer option now checks SQLite/FK state plus the wider finance graph. Common issue codes include transaction value/account/currency problems, broken transfers, split totals, category cycles, invalid budgets/custom periods, savings contribution links, recurrence dependency/payment state, reconciliation links, and receipt file path/size/checksum problems.

- Export the sanitized integrity report if needed.
- Do not publish the database itself.
- Do not create a new backup over the only known-good external backup until the integrity issue is understood.
- Use `SECURITY.md` for suspected data exposure or security defects.

## Dashboard says totals use one currency

This is intentional. Finora does not silently add unlike currencies or invent exchange rates.

- Dashboard aggregate cards use the configured reporting currency.
- Accounts/transactions/goals/recurrence rows in other currencies retain their own currency.
- Change the reporting/default currency in Settings if you want aggregate reports for a different currency.
- Cross-currency conversion is not part of the current release.

## Tag report does not include another-currency transaction

Tag reports are explicitly currency-scoped. The same tag can exist on INR/USD/etc. transactions without those values being added together. Run the report for the desired currency rather than treating raw minor units as exchange-equivalent.

## Account cannot be archived

Check whether an **Active** recurring rule uses the account as source or destination. Pause, complete, or archive that recurring rule first.

A paused rule may preserve its historical account link after the account is archived, but it cannot resume until its account/category/currency dependencies are valid again.

## Account currency cannot be changed

Finora blocks currency changes after transaction or recurring records reference the account. Changing the currency label would reinterpret historical minor units and is therefore not allowed.

Create a separate account for the other currency or use an explicit migration/exchange workflow if a future release provides one.

## Recurring rule cannot resume

Resume validates current dependencies. Check:

- rule is Paused rather than Completed/Archived;
- configured end date has not already passed;
- source/destination accounts still exist and are not archived;
- account currencies still match the rule;
- referenced category still exists and is active.

Pausing stops new due-occurrence generation but does not delete historical occurrences.

## A paused/archived recurring reminder still appears

Run reminder synchronization from Settings/developer tools after granting notification permission. Current synchronization cancels stale recurrence dedupe keys for non-active rules.

Also remember that an OS may have already displayed a notification before Finora could cancel it; test platform-specific scheduling behavior on a real packaged build.

## Custom budget is not visible for a date

Custom cadence is active only inside an explicit configured period. Finora intentionally does not fabricate a one-day fallback period.

Check that:

- at least one explicit period exists;
- the selected report/dashboard date is inside that period;
- periods do not overlap;
- effective planned amount remains positive after enabled rollover.

## Budget update fails

Explicit period replacement is treated as one logical operation. A failed replacement should leave the prior valid period set intact rather than partially deleting it.

Use synthetic data and the budget rollback integration tests when diagnosing persistence failures. Do not manually delete budget-period rows from a real finance database.

## Backup creation is rejected before a file is produced

Finora validates more than receipt checksums. It also validates the supported financial graph before encrypting it.

Possible causes include:

- transaction/account currency mismatch;
- broken transfer pair;
- invalid split signs/totals/categories;
- category hierarchy problem;
- custom budget without a period or overlapping periods;
- invalid savings contribution history/link;
- active recurrence pointing to an unavailable account/category;
- impossible occurrence payment state;
- reconciliation arithmetic/adjustment mismatch;
- invalid attachment metadata/path/file checksum.

Run the sanitized integrity checker first rather than weakening backup validation.

## Backup preview or restore is rejected

Possible reasons include:

- wrong password;
- tampered/truncated backup;
- unsupported schema version;
- duplicate/invalid object identifiers;
- semantically invalid financial graph even though decryption succeeded;
- invalid attachment path/size/checksum;
- file too large or unreadable.

Finora intentionally fails closed. Do not weaken AES-GCM authentication or graph validation to make a damaged file import.

## App was interrupted during restore

Finora records a private restore journal plus database commit marker and runs recovery before finance navigation.

- If the pending DB marker remains, startup restores the previous receipt tree and removes staged data.
- If the DB commit completed and the marker is gone, startup finalizes the new receipt tree and removes rollback data.
- Do not manually delete `attachments.restore.*` or `attachments.rollback.*` directories before startup recovery has made its decision.

If recovery still fails, preserve the external encrypted backup and work only on synthetic/copied app data during diagnosis.

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

If PIN-enabled state remains but secure-storage verifier material is missing/corrupt, verification intentionally fails closed rather than treating the lock as disabled.

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
