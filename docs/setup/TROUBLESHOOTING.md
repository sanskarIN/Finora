# Finora Troubleshooting

Use synthetic/test data while diagnosing. Never attach a real Finora database, receipt, PIN, backup password, recovery journal, signing credential, or private financial screenshot to a public issue.

## `dotnet` or MAUI workload is missing

Check:

```bash
dotnet --info
dotnet workload list
```

Then restore the app workload/project on a supported host:

```bash
dotnet workload restore src/Finora.App/Finora.App.csproj
dotnet restore src/Finora.App/Finora.App.csproj
```

Linux is suitable for structural/core checks but not the repository's Windows/Android/Apple release matrix. Use Windows for Windows/Android and a supported Mac/Xcode host for Apple targets.

## Structural preflight fails

Run:

```bash
python build/scripts/verify_structure.py
```

Fix the reported path rather than disabling the check. It now validates required repository files, XML/XAML/project parsing, project/solution references, XAML handlers, version/schema-document consistency, money-representation signals, and Android privacy flags.

A passing preflight is not evidence that C# compiles.

## SQLite is locked

Finora enables WAL and a busy timeout. Close other app/debug/test processes using the same database and retry. Do not copy a live DB/WAL/SHM set selectively and then treat it as a supported backup.

## Database will not open after update

Check the release/schema pair and migration logs using synthetic copies. Finora rejects databases whose declared schema is newer than the build understands. Current source declares schema 2 and includes v1 → v2 migration.

Do not manually change `schema.version` to force an incompatible database open.

## Data-integrity check reports a problem

The hidden developer integrity check returns privacy-safe issue codes/counts. Treat errors as real until investigated. It checks SQLite/foreign keys, transaction values/sign/currency, transfer pairs, split totals, category cycles, recurrence references, and receipt path/size/hash state.

Create a separate encrypted backup only if the database/receipt state is appropriate for recovery; do not overwrite the only known-good backup.

## Encrypted backup preview/restore is rejected

Possible causes include wrong password, altered/truncated file, unsupported schema, oversized payload, unsafe attachment metadata, missing receipt bytes, or checksum mismatch.

Finora cannot recover a forgotten backup password.

## Finora reports interrupted-restore recovery failure

Production restore uses a private journal plus a transient `internal.restore.commit` marker to keep SQLite and receipt files consistent across process interruption.

At startup:

- a matching pending marker means the DB replacement did not commit, so the previous verified receipt tree is restored;
- an absent matching marker means the DB committed, so the new receipt tree is finalized;
- stale staging/rollback directories are cleaned only after that decision.

If safe automatic recovery cannot complete, Finora intentionally blocks normal initialization. Do not delete `finora-restore-recovery.json` or `attachments.rollback.*` manually before preserving a diagnostic copy in a private test/support environment and understanding which DB state committed. Never publish recovery artifacts because they are still local application metadata.

## PIN is enabled but unlock fails after secure-storage damage

Finora is deliberately fail-closed. A persistent non-secret “PIN enabled” marker prevents missing/malformed secure-storage verifier material from being treated as “no PIN.” The app will not bypass the lock simply because the verifier is unavailable.

Use supported device/app recovery procedures rather than weakening the verifier check. A future product recovery mechanism must be designed explicitly; do not add a hidden bypass.

## Dashboard total seems to omit an account

Check the account currency. Dashboard aggregate totals use the configured default/reporting currency only. Accounts in other currencies remain separate and are not converted or added using an invented exchange rate. The dashboard displays a notice listing separated currencies.

Change the default/reporting currency if you want to view aggregates in another existing currency context. Finora does not currently perform automatic exchange-rate conversion.

## Imported JPY/KWD-style amount looks unexpected

Major-unit import uses currency-specific minor-unit precision. Zero-decimal currencies such as JPY and supported three-decimal currencies such as KWD are not forced through two decimals. Confirm:

- the mapped Currency column/default currency;
- whether the Amount column is marked major units or already minor units;
- the release's verified currency-precision metadata;
- the CSV uses invariant-style numeric input expected by the importer.

## Navigation differs between phone and desktop/tablet

This is intentional. Phones use bottom primary tabs. Tablet/desktop or sufficiently wide layouts expose the equivalent primary routes through a flyout/sidebar hierarchy. Resizing should preserve the equivalent primary section.

If resize routing is wrong, record the current Shell route, device idiom, window width and synthetic reproduction steps. Test onboarding/unlock/startup separately because they use adaptive root routing too.

## Notifications do not appear

Check OS permission, Finora notification preference, platform scheduling support, current trigger time and device background/power policy. Reminder text is intentionally generic.

Do not treat notification delivery as a financial source of truth; persisted recurrence/budget/backup state remains in Finora.

## Biometrics/Windows Hello cannot be enabled

Set a Finora PIN first. Biometric/Hello unlock requires PIN fallback. Then verify platform enrollment/capability/permission state. On Apple targets ensure the packaged `NSFaceIDUsageDescription` is present.

## Screenshot/capture protection differs by platform

Protection is platform-capability-based. Android uses secure-window behavior; supported Windows configurations use display-affinity behavior. Apple/platform limitations must be communicated honestly. An external camera/rooted or compromised OS is outside app-level control.

## Receipt will not open or integrity check reports it missing

Receipts must remain beneath app-private `attachments` storage. Finora validates stored path/size/checksum where required. Moving/deleting app-private files outside Finora can break metadata/file consistency.

Use attachment cleanup only for true orphan files; do not manually point attachment metadata at arbitrary paths.

## Full finance reset fails

Reset deletes schema-v2 finance records in dependency-safe order. Category hierarchies are removed leaves-first; a cycle causes rollback rather than partial deletion. App preferences, schema marker and PIN configuration are intentionally retained.

If reset fails, run the integrity checker and investigate the underlying relational/category issue using synthetic data.

## Developer sample reset warning

“Reset to synthetic sample data” is destructive by design. It requires exact typed confirmation, clears current finance data, reseeds system categories and creates deterministic synthetic records. Do not use it on data you need unless you have already saved a verified encrypted backup elsewhere.

## Windows package build/signing issue

Repository package metadata is development source, not production signing evidence. Configure final package identity/publisher/signing securely in release infrastructure and verify source/package version alignment.

## Apple build fails on Windows/Linux

Build/archive Apple targets on a supported Mac/Xcode host. Do not interpret source inspection or a non-Apple build as evidence that LocalAuthentication/UserNotifications/archive/signing works.

## Need more help

Read `SUPPORT.md`, `SECURITY.md`, `PROJECT_STATUS.md`, `docs/TEST_PLAN.md` and `docs/releases/STORE_READINESS.md` before opening a public bug. Use synthetic data only. Report vulnerabilities privately as described in `SECURITY.md`.
