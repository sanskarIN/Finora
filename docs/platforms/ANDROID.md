# Android Platform Guide

This document describes the current Android target in Finora 0.2.0.

## Target

- Target framework: `net10.0-android`
- Minimum supported platform version declared by project: Android API 26.0
- Application ID: `in.sanskar.finora`
- Application title: Finora
- Display version: 0.2.0
- Build version: 2

## Build

On a supported .NET/MAUI Android host:

```bash
dotnet workload restore src/Finora.App/Finora.App.csproj
dotnet restore src/Finora.App/Finora.App.csproj
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-android -c Release --no-restore
```

A release AAB/signing step must use credentials outside the repository.

## Manifest privacy/security

Current Android manifest declares:

- `android:allowBackup="false"`;
- `android:fullBackupContent="@xml/backup_rules"`;
- `android:dataExtractionRules="@xml/data_extraction_rules"`;
- `android:usesCleartextTraffic="false"`;
- `android:supportsRtl="true"`.

Structural preflight verifies the backup-rule links and required data-domain exclusions.

Final merged-manifest/package behavior must still be verified from the built artifact.

## Permissions

Current manifest includes:

- `android.permission.USE_BIOMETRIC`;
- `android.permission.USE_FINGERPRINT`;
- `android.permission.POST_NOTIFICATIONS`.

No background location permission is part of the current design. Transaction location is manually entered text only.

## Automatic backup/device transfer

Finora's supported portable backup mechanism is the explicit password-encrypted Finora backup.

Current Android configuration additionally excludes app-private data from ordinary Android backup/device-transfer paths through:

- `allowBackup=false`;
- legacy `backup_rules.xml`;
- Android 12+ `data_extraction_rules.xml`.

Rules cover root/file/database/shared-preference/external domains.

Release QA must verify the final package and representative device/cloud transfer behavior; source configuration alone is not proof of platform behavior.

## Notifications

Android local reminders use platform alarm/notification APIs through `PlatformNotificationGateway` and are persisted/deduplicated through the platform-neutral local notification service.

Current cancellation behavior queries the existing `PendingIntent` with `PendingIntentFlags.NoCreate`; cancelling a reminder should not create a new PendingIntent as a side effect.

Validate:

- POST_NOTIFICATIONS permission behavior on applicable Android versions;
- schedule/cancel;
- dedupe replacement;
- failed replacement handling;
- reboot/force-stop/doze limitations;
- stale schedule reconciliation;
- generic lock-screen notification text.

## Biometrics

Android biometric integration uses the Android hardware biometrics API namespace and BiometricPrompt path in the current platform service.

The user-facing result must never contain raw `errString` provider text. Provider errors are normalized to stable Finora text and PIN fallback remains available.

Validate on device:

- no enrollment;
- enrollment available;
- success;
- user cancel;
- negative/fallback action;
- temporary error;
- lockout;
- device credential/PIN fallback behavior according to current prompt implementation.

## Sensitive-screen protection

Finora uses Android secure-window behavior for supported sensitive-screen protection.

Verify `FLAG_SECURE` behavior on target devices and document limitations. App-level secure-window behavior cannot prevent a physical camera, privileged/root tooling, or every vendor/OS capture path.

## App-private data

Finora uses MAUI `FileSystem.AppDataDirectory` for durable app-private data such as:

- SQLite database/WAL/SHM;
- receipt attachments;
- restore recovery journal/marker/directories.

Cache share copies use `FileSystem.CacheDirectory` and are not durable finance system-of-record data.

## Receipts/file picker/share

Validate:

- image/PDF selection through system provider;
- copy to app-private storage;
- open selected attachment;
- delete;
- external provider cancellation;
- low-storage failure;
- backup/restore attachments;
- share/save CSV/PDF/encrypted backup;
- path/link confinement on a representative environment where practical.

## Adaptive UI

Phones use bottom tabs. Tablets and device/window idiom can select the desktop/tablet flyout hierarchy. Window width >= 900 also selects the wider navigation mode.

Validate rotation/resizing/foldable/tablet configurations where supported by the release test set.

## Accessibility

Validate:

- TalkBack;
- font/display scaling;
- semantic labels/headings;
- touch target size;
- privacy/lock screen controls;
- report textual equivalents;
- focus order;
- dark/light/system themes;
- reduced motion.

## Privacy-mode test

With privacy/hide-on-launch enabled verify no passive amount leaks from:

- Dashboard;
- Accounts;
- Transactions;
- Transaction Tools;
- account detail;
- budgets;
- savings;
- recurring;
- reconciliation;
- transaction split rows;
- reports/charts.

## Store validation

Before Play release verify:

- final AAB generated/signed securely;
- package ID/version match console;
- adaptive/monochrome icon;
- splash;
- minimum/target SDK policy compatibility;
- permission declarations;
- data-safety answers match local-first behavior;
- backup/device-transfer exclusions match declarations;
- no analytics/advertising SDK introduced unintentionally;
- screenshots contain synthetic data only;
- upgrade/migration from supported prior schema;
- encrypted backup/restore on release build.

See [Store Readiness](../releases/STORE_READINESS.md).