# Native Validation Matrix

Use this matrix for platform/device validation of Finora release candidates. Source/unit/integration/UI-contract tests do not substitute for these checks.

## Common evidence

For every target retain:

- exact release commit SHA;
- toolchain versions;
- device/emulator/simulator OS version;
- Release build result;
- install/launch evidence;
- smoke-test result;
- accessibility result;
- file picker/share result;
- backup/restore result;
- migration result;
- known limitations.

## Common functional matrix

| Area | Android | Windows | iOS | Mac Catalyst |
|---|---|---|---|---|
| Fresh install/onboarding | Required | Required | Required | Required |
| No-login offline finance flow | Required | Required | Required | Required |
| Adaptive navigation/layout | Required | Required | Required | Required |
| Accounts/transfers | Required | Required | Required | Required |
| Transactions/search/sort/paging | Required | Required | Required | Required |
| Splits/tags/receipts | Required | Required | Required | Required |
| Reconciliation | Required | Required | Required | Required |
| Budgets | Required | Required | Required | Required |
| Savings goals | Required | Required | Required | Required |
| Recurring lifecycle | Required | Required | Required | Required |
| Reports/signed charts/privacy | Required | Required | Required | Required |
| CSV import | Required | Required | Required | Required |
| CSV/PDF export/share | Required | Required | Required | Required |
| Encrypted backup/restore | Required | Required | Required | Required |
| Interrupted restore recovery | Required | Required | Required | Required |
| Full finance reset | Required | Required | Required | Required |
| Upgrade/migration | Required | Required | Required | Required |

## Android-specific matrix

Validate:

- `net10.0-android` Release build;
- package ID matches intended release configuration;
- adaptive + monochrome launcher icons;
- splash light/dark behavior;
- runtime notification permission where required;
- alarm/local notification schedule/cancel;
- dedupe replacement behavior;
- `PendingIntentFlags.NoCreate` cancellation path works as expected;
- force-stop/reboot/doze limitations documented;
- BiometricPrompt available/success/cancel/error/lockout;
- raw provider biometric error text not shown;
- PIN fallback;
- `FLAG_SECURE` behavior;
- final merged manifest contains `allowBackup=false` and `usesCleartextTraffic=false`;
- legacy backup rules packaged;
- Android 12+ data extraction rules packaged;
- ordinary cloud backup/device transfer does not copy private Finora finance data under representative supported test conditions;
- picker/share/receipt open behavior;
- app-private attachment path behavior;
- TalkBack;
- large font/display scaling;
- theme/reduced motion;
- upgrade from synthetic prior schema.

## Windows-specific matrix

Validate:

- `net10.0-windows10.0.19041.0` Release build;
- final package identity/version/publisher;
- signing uses external secure credentials;
- packaged install/update/uninstall;
- Windows Hello available/success/cancel/error with PIN fallback;
- scheduled toast behavior under packaged identity;
- reminder cancel/reconcile;
- supported display-affinity capture protection;
- resizable window at narrow/wide/adaptive threshold;
- keyboard focus/navigation;
- high DPI and multi-monitor behavior;
- file picker/share/export/backup/restore;
- attachment open behavior;
- NTFS reparse-point path tests where practical;
- Narrator;
- high contrast/large text;
- package upgrade preserves/migrates local data.

## iOS-specific matrix

Validate:

- `net10.0-ios` Release/archive build on supported macOS/Xcode;
- bundle/provisioning/signing outside repository;
- launch on simulator/device;
- `NSFaceIDUsageDescription` packaged;
- LocalAuthentication available/success/cancel/error/lockout;
- PIN fallback;
- UserNotifications permission/schedule/cancel;
- picker/share/import/export/receipt flows;
- encrypted backup/restore;
- interrupted restore/relaunch recovery;
- VoiceOver;
- Dynamic Type;
- reduced motion;
- dark/light appearance;
- orientation/layout;
- screenshot/capture limitation is accurately documented;
- upgrade/migration.

## Mac Catalyst-specific matrix

Validate:

- `net10.0-maccatalyst` Release/archive build;
- app signing/notarization/distribution prerequisites;
- Face ID/biometric purpose metadata where applicable;
- LocalAuthentication with PIN fallback;
- UserNotifications;
- reminder lifecycle;
- picker/share/backup/restore;
- resizable windows;
- keyboard/mouse focus;
- high DPI;
- VoiceOver;
- theme/reduced motion;
- representative filesystem link/path behavior;
- upgrade/migration.

## Privacy-mode matrix

On each target enable privacy/hide-on-launch and verify passive values are hidden on:

- Dashboard;
- Accounts list;
- Account detail/history;
- Transactions list;
- Transaction Tools;
- Budgets;
- Savings cards/forecast;
- Recurring rules/occurrences;
- Reconciliation preview/history;
- Transaction split list;
- Reports.

Verify report charts do not reveal hidden magnitude.

Then disable privacy mode and verify currency-aware values display correctly for synthetic 0-, 2-, and 3-decimal currencies where UI workflow allows.

## Local-calendar matrix

Use at least one non-UTC device timezone.

Verify:

- transaction at local day start/end;
- date filters include selected local day;
- Dashboard financial-month boundary;
- Reports local From/Through range;
- monthly/yearly grouping;
- current month/year excludes tomorrow's future-dated synthetic row;
- reconciliation includes the full selected local statement day.

Where practical use a timezone with DST and test around a transition date.

## Backup/recovery matrix

On every target:

- create encrypted backup with attachments;
- preview correct counts;
- restore to clean profile;
- restore over existing profile;
- wrong password;
- tampered backup;
- missing/changed receipt in source before backup;
- force-terminate restore at multiple phases;
- relaunch and verify journal/marker recovery;
- verify DB/attachment consistency;
- verify stale restore directories cleaned only after decision.

## Notification matrix

Test:

- permission denied;
- permission granted;
- permission revoked where applicable;
- schedule;
- replacement with same dedupe key;
- cancellation;
- failed replacement simulation where possible;
- stale DB/OS reconciliation;
- recurring pause/archive removes stale reminder;
- budget condition no longer active removes stale reminder;
- backup reminder disable removes stale reminder;
- generic lock-screen text.

## Accessibility matrix

### Android

TalkBack, font scaling, touch targets, focus, semantic labels.

### Windows

Narrator, keyboard-only operation, focus visibility/order, high contrast, DPI.

### iOS

VoiceOver, Dynamic Type, reduced motion, orientation.

### Mac Catalyst

VoiceOver, keyboard/mouse, resizable window, focus order.

## Release decision

A platform is not marked validated until the applicable rows have actual evidence for the exact release commit. Unsupported capabilities must be documented rather than marked as passing by assumption.