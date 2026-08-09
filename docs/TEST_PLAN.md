# Finora Test Plan

Finora handles private financial records and local backups. Tests must use synthetic data only. Passing source/unit tests does not replace native device validation.

## 1. Structural preflight

Run on every pull request and release candidate:

```bash
python build/scripts/verify_structure.py
```

Expected: no malformed XML/XAML/RESX/project files, missing project references, empty implementation/resource files, unfinished placeholder markers, or missing XAML event handlers.

## 2. Build/static analysis

```bash
dotnet workload restore
dotnet restore Finora.sln
dotnet format Finora.sln --verify-no-changes --no-restore
dotnet build Finora.sln -c Release --no-restore
dotnet test Finora.sln -c Release --no-build
```

Warnings are errors. Do not ship a release that succeeds only after broad analyzer suppression.

## 3. Unit tests

Cover pure/domain behavior:

- major/minor money conversion and rounding boundaries;
- integer overflow behavior;
- currency normalization/validation;
- transaction sign/domain validation;
- transfer invariants;
- recurrence next-occurrence calculations across month/year boundaries;
- decimal calculator precedence/parentheses/division/error cases;
- import parsing helpers where pure;
- PIN/security helper behavior where injectable/testable;
- progress/forecast calculations that do not require platform UI.

## 4. Database integration tests

Use isolated SQLite databases per test.

### Accounts and transfers

- create/edit/archive/restore account;
- opening balance/current balance calculation;
- same-currency paired transfer is atomic and net-zero across accounts;
- transfer edit updates both halves;
- transfer delete/restore affects both halves;
- cross-currency paired transfer is rejected until an explicit exchange workflow exists;
- reconciliation with no difference;
- reconciliation with explicit adjustment;
- reconciliation with unresolved difference is rejected;
- reconciliation history persists.

### Transactions

- create/edit/delete/restore expense/income/refund/adjustment;
- critical edits create revision history;
- bulk categorization creates revisions;
- split totals equal parent amount;
- invalid split totals are rejected/detected;
- tag linking and removal;
- duplicate detection does not delete data automatically;
- search/filter by account/category/type/date/text.

### Categories/tags

- create parent/subcategory;
- prevent hierarchy cycle;
- reorder;
- archive/restore;
- merge/reassign references safely;
- tag archive/restore and report linkage.

### Budgets/goals

- monthly/weekly/custom period boundaries;
- overall/category/subcategory actuals;
- rollover calculation;
- warning threshold behavior;
- split transaction contribution to category budgets;
- savings deposit/withdrawal;
- linked contribution transaction;
- target/milestone/completion behavior;
- withdrawal never drives goal below zero when prohibited by domain rules.

### Recurrence

- processing is idempotent across repeated calls/restarts;
- unique `(RecurrenceRuleId, DueOn)` invariant;
- pending occurrence does not create a transaction before paid/partial-paid action;
- paid/partial paid creates exactly one transaction;
- recurring transfer creates a balanced pair;
- skip/postpone state transitions;
- end date and custom interval behavior.

### CSV import

- quoted commas/escaped quotes/newlines if supported by parser;
- UTF-8 validation;
- file/row limits;
- explicit user-selected column mapping;
- major-unit decimal-safe conversion;
- minor-unit import;
- invalid date/type/currency/amount rejection;
- missing account/fallback behavior;
- optional category creation;
- tag linking;
- duplicate skipping;
- transfer-group pair validation;
- transactional failure/rollback.

### Backup/restore

- create/preview/restore current schema;
- wrong password rejected;
- changed authentication tag/ciphertext rejected;
- truncated/oversized backup rejected;
- future schema rejected;
- attachment bytes round-trip;
- attachment path escape rejected;
- attachment size/hash mismatch rejected;
- database replacement and attachment-directory swap are consistent;
- failed restore leaves prior data usable;
- backup metadata/audit entries do not expose finance contents.

### Migrations

For every released schema version:

1. create a representative synthetic database at that schema;
2. migrate through the actual production path;
3. verify schema version advances only on successful migration;
4. verify all entities/relationships/data remain correct;
5. verify a failed migration rolls back;
6. run the data-integrity checker after migration.

Current required migration coverage includes v1 → v2.

### Data integrity diagnostics

- healthy SQLite database returns healthy sanitized report;
- broken transfer half is detected;
- foreign-key violation is detected;
- split mismatch is detected;
- category cycle is detected;
- recurrence duplicate/missing generated transaction is detected;
- missing/changed receipt file is detected;
- unsafe attachment path is detected;
- sanitized report contains counts/codes only, not account names, merchant/payee names, notes, amounts, or receipt filenames.

## 5. ViewModel/UI-contract tests

Cover navigation contracts and state behavior without pretending this is native UI automation:

- onboarding → dashboard;
- privacy mode/hidden amounts;
- transaction quick-add and detail routes;
- accounts/detail/reconciliation routes;
- import and transaction-tool routes;
- category/tag management route;
- budget/goal/recurring/report routes;
- backup/restore/settings/legal routes;
- app-lock fallback state;
- destructive actions require explicit confirmation.

## 6. Android device/emulator tests

At minimum test a current emulator and a physical device when available:

- fresh install/onboarding;
- app restart/force-stop persistence;
- account/transaction/transfer/budget/goal/recurrence core flows;
- receipt picker/open/delete;
- CSV import and CSV/PDF export;
- encrypted backup/share/save/restore;
- notification permission and scheduled reminders;
- reboot/doze/force-stop reminder limitations;
- biometric success/cancel/unavailable/lockout with PIN fallback;
- `FLAG_SECURE` behavior;
- dark/light/system theme;
- TalkBack, large font/display scaling, reduced motion;
- Android back/navigation behavior;
- upgrade from prior release/schema.

## 7. Windows packaged tests

- install/upgrade/uninstall MSIX/package;
- resizable window/minimum usable size/high DPI;
- keyboard focus/navigation;
- Windows Hello success/cancel/unavailable with PIN fallback;
- scheduled toast behavior with packaged identity;
- display-affinity capture protection where supported;
- file picker/share/export/backup/restore;
- Narrator and high-contrast/large text behavior;
- database/receipt preservation across package upgrade.

## 8. iOS device/simulator tests

- archive/build on supported Xcode host;
- onboarding/core finance flows;
- LocalAuthentication states with PIN fallback;
- UserNotifications permission/scheduling;
- document picker/share/export/backup/restore/receipt flows;
- VoiceOver, Dynamic Type, reduced motion, dark mode;
- platform screenshot-protection limitation is correctly communicated;
- upgrade/migration behavior.

## 9. Mac Catalyst tests

- archive/build/signing prerequisites;
- resizable windows and keyboard/mouse focus;
- LocalAuthentication/UserNotifications;
- file picker/share flows;
- VoiceOver/accessibility and dark/light modes;
- database/receipt persistence and migration.

## 10. Reliability/failure injection

Exercise:

- force-close after transaction save returns but before next navigation;
- low disk space during attachment copy/export/backup;
- cancelled picker/share flow;
- permission denial/revocation;
- corrupted local receipt file;
- locked/unavailable file;
- wrong/tampered backup;
- repeated recurrence processing after restart;
- database lock contention;
- migration interruption using a copied synthetic database.

Never intentionally corrupt a user's real finance database during testing.

## 11. Privacy/security regression

Verify:

- no network/account requirement for current release;
- no analytics/telemetry/advertising SDK introduced;
- logs do not include amounts, account names, merchant/payee names, notes, locations, receipt names/contents, PINs, backup passwords, or encryption material;
- local notification text remains generic/privacy-safe;
- backup key material is not persisted;
- app-lock verifier uses secure storage for small secrets only;
- local premium remains explicitly non-tamper-proof demo state;
- no repository/build artifact contains signing credentials or real finance data.

## 12. Release evidence

For a release candidate, retain CI run links, test result artifacts, platform build logs, migration-test results, and device-smoke-test checklist results. Do not mark a platform gate complete based only on source inspection.
