# Finora Test Plan

Finora handles private financial records and local backups. Tests must use synthetic data only. Passing source/unit tests does not replace native device validation.

## 1. Structural preflight

Run on every pull request and release candidate:

```bash
python build/scripts/verify_structure.py
```

Expected: no malformed XML/XAML/RESX/project files, missing required repository files, missing project references, empty implementation/resource files, unfinished placeholder markers, missing XAML event handlers, app/package version drift, schema-document drift, suspicious floating-point monetary fields, or weakened Android local-data privacy flags.

## 2. Compiler/static-analysis gates

The repository treats warnings as errors and enables recommended analyzers. Core test projects build on a normal .NET 10 host. Native MAUI target frameworks build on their supported CI hosts.

Use the repository wrappers:

```bash
./build/scripts/verify.sh
```

or on Windows:

```powershell
./build/scripts/verify.ps1
```

Set `FINORA_SKIP_MAUI=1` only when intentionally running core verification on a host that cannot build the native targets. Native platform builds remain mandatory before release.

Formatting cleanup is encouraged, but source formatting alone is not used as a release correctness substitute. Compiler/analyzer/test/platform gates are authoritative.

## 3. Unit tests

Cover pure/domain behavior:

- integer minor-unit money conversion and rounding boundaries;
- zero-, two-, and three-decimal currency precision (for example JPY/INR/KWD);
- explicit custom precision where a non-standard accounting unit is intentionally used;
- integer overflow and `long.MinValue` rejection;
- currency normalization/validation;
- expense/income/refund sign rules;
- split sign and checked total invariants;
- account credit-card constraints;
- recurrence next-occurrence calculations across month/year boundaries;
- decimal calculator precedence/parentheses/division/error cases;
- runtime locale normalization/fallback/application;
- PIN failure-count bounds and escalating lockout cap;
- ViewModel base busy/error/property-notification behavior;
- async command parallel-execution suppression.

Tests that mutate process culture run in a non-parallel collection and restore the previous culture afterward.

## 4. Database integration tests

Use isolated SQLite databases per test.

### Persistence-boundary invariants

- direct EF writes normalize valid account/transaction currencies;
- direct EF writes reject invalid currency codes;
- positive Expense and negative Income/Refund writes are rejected;
- zero/`long.MinValue` amounts are rejected;
- service/import/restore-style code cannot bypass Account/FinanceTransaction validation.

### Accounts and transfers

- create/edit/archive/restore account;
- opening/current balance calculation;
- same-currency paired transfer is atomic and net-zero across accounts;
- transfer edit updates both halves;
- transfer delete/restore affects both halves;
- cross-currency paired transfer is rejected until an explicit exchange workflow exists;
- reconciliation no-difference and explicit-adjustment paths;
- unresolved reconciliation difference rejected;
- reconciliation history persists.

### Transactions/categories/tags

- create/edit/delete/restore Expense/Income/Refund/Adjustment;
- critical edits create revision history;
- bulk categorization creates revisions;
- split totals equal parent amount;
- invalid split totals/signs are rejected/detected;
- tag linking/removal;
- duplicate review never deletes automatically;
- search/filter by account/category/type/date/text;
- category create/subcategory/cycle-prevention/reorder/archive/restore/merge/reassign;
- tag archive/restore and report linkage.

### Budgets/goals

- weekly/monthly/custom period boundaries;
- overall/category/subcategory actuals;
- rollover and warning thresholds;
- split transaction contribution to category budgets;
- savings deposits/withdrawals and optional linked transactions;
- target/milestone/completion behavior;
- withdrawal constraints.

### Recurrence

- processing is idempotent across repeated calls/restarts;
- unique `(RecurrenceRuleId, DueOn)` invariant;
- pending occurrence creates no transaction before paid/partial-paid;
- paid/partial-paid creates exactly one transaction;
- repeated full-payment action is idempotent;
- recurring transfer creates a balanced pair;
- skip → reopen → pay path;
- skipped item must reopen before postpone/payment;
- fully paid item cannot be postponed;
- archived/unavailable account blocks payment generation;
- account/rule currency drift blocks payment generation;
- end-date/custom interval behavior.

### CSV import

- quoted commas/escaped quotes/multiline quoted fields;
- UTF-8 validation;
- file/row limits;
- explicit user-selected column mapping;
- currency-specific major-unit conversion (including zero/three-decimal currencies);
- minor-unit import;
- `long.MinValue`/overflow rejection without crashing;
- invalid date/type/currency/amount rejection;
- parse errors counted exactly once;
- missing account/fallback behavior;
- optional category creation;
- tag linking;
- duplicate skipping including duplicates inside one import batch;
- transfer-group pair/counterparty validation;
- transactional failure/rollback.

### Reports/multi-currency

- aggregated reports include only the selected reporting currency;
- unlike currencies are never silently added;
- account/budget rows retain their own currencies;
- JPY/KWD/etc. formatting uses currency-specific precision;
- dashboard/report summary calculations use checked integer arithmetic.

### Backup/restore/recovery

- create/preview/restore current schema;
- wrong password rejected;
- changed authentication tag/ciphertext rejected;
- truncated/oversized backup rejected;
- future schema rejected;
- attachment bytes round-trip;
- attachment path escape rejected;
- attachment size/hash mismatch rejected;
- failed restore leaves prior data usable;
- pending recovery marker restores verified previous receipt tree;
- missing pending marker finalizes a database-committed restore;
- incomplete rollback-copy state preserves untouched live receipt tree;
- successful crash-safe round trip leaves no recovery marker/journal/orphan directories;
- concurrent backup/preview/restore operations are serialized;
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
- raw SQL corruption of transaction sign/extreme amount is detected;
- broken transfer half is detected;
- foreign-key violation is detected;
- split mismatch/sign issue is detected;
- category cycle is detected;
- recurrence duplicate/missing generated transaction is detected;
- missing/changed receipt file is detected;
- attachment path outside the private receipt root is detected;
- sanitized report exposes codes/counts only, not names/notes/amounts/receipt filenames.

### Full finance reset and synthetic sample reset

- every finance table is cleared dependency-safely;
- self-referencing categories are deleted leaves-first;
- cyclic category reset rolls back rather than partially deleting;
- `schema.version` remains;
- app preferences/PIN configuration remain;
- receipt files are cleaned only after DB commit;
- developer sample reset replaces finance data with deterministic synthetic records;
- sample transfer conserves total balance;
- system categories are reseeded;
- synthetic reset never preserves pre-existing user finance records.

## 5. ViewModel/UI-contract tests

These are state/route contracts, not native UI automation:

- busy/error/property-notification behavior;
- onboarding → adaptive dashboard root;
- PIN/biometric unlock → adaptive dashboard root;
- mobile bottom-tab route set;
- tablet/desktop flyout route set;
- resize route preservation;
- privacy mode/hidden amounts;
- transaction/account/reconciliation/import/category/budget/goal/recurrence/report/settings/legal routes;
- destructive finance reset confirmation;
- synthetic sample reset confirmation;
- large-text, keyboard-focus, and screen-reader semantic flows remain required native acceptance cases.

## 6. Android device/emulator tests

- fresh install/onboarding and restart/force-stop persistence;
- account/transaction/transfer/budget/goal/recurrence core flows;
- receipt picker/open/delete;
- CSV import/export and encrypted backup/share/save/restore;
- kill the app during restore phases and verify startup recovery behavior;
- notification permission/scheduled reminders/reboot-doze limitations;
- biometric success/cancel/unavailable/lockout with PIN fallback;
- missing/corrupted secure-storage verifier fails closed;
- `FLAG_SECURE` behavior;
- phone bottom tabs and tablet/large-width flyout;
- dark/light/system theme, TalkBack, large font/display scaling, reduced motion;
- upgrade from prior release/schema.

## 7. Windows packaged tests

- install/upgrade/uninstall package;
- package identity/version and signing;
- resizable window, flyout/sidebar, minimum usable size, high DPI;
- keyboard focus/navigation and Narrator;
- Windows Hello success/cancel/unavailable with PIN fallback;
- scheduled toast behavior with packaged identity;
- display-affinity capture behavior where supported;
- file picker/share/export/backup/restore including interrupted-restore recovery;
- database/receipt preservation across package upgrade.

## 8. iOS device/simulator tests

- archive/build on supported Xcode host;
- Face ID/Touch ID purpose text and LocalAuthentication states with PIN fallback;
- UserNotifications permission/scheduling;
- document picker/share/export/backup/restore/receipt flows;
- interrupted-restore recovery;
- phone tabs/iPad adaptive navigation;
- VoiceOver, Dynamic Type, reduced motion, dark mode;
- screenshot-protection limitation is communicated accurately;
- upgrade/migration behavior.

## 9. Mac Catalyst tests

- archive/build/signing prerequisites;
- resizable flyout/sidebar layout and keyboard/mouse focus;
- LocalAuthentication/UserNotifications;
- file picker/share/backup/restore/recovery flows;
- VoiceOver/accessibility and dark/light modes;
- database/receipt persistence and migration.

## 10. Reliability/failure injection

Exercise force-close/kill around transaction save, migration, attachment copy, backup creation, each restore journal/copy/database/swap/finalization phase, low disk space, cancelled picker/share, permission denial/revocation, corrupted receipt, locked file, wrong/tampered backup, repeated recurrence processing, database contention, and malformed persisted preference/security values.

Never intentionally corrupt a user's real finance database during testing.

## 11. Privacy/security regression

Verify no current-release network/account requirement, analytics/telemetry/advertising SDK, private financial log payloads, private notification text, persisted backup key material, repository signing credentials, or real finance test data. Secure-storage verifier loss must fail closed. Local premium remains explicitly non-secure demo state. Recovery journal/marker must contain operation metadata only.

## 12. Release evidence

For a release candidate, retain CI run links, test artifacts, native build logs, migration/recovery test results, device accessibility/navigation checks, and signed-package smoke-test evidence. Do not mark a platform gate complete based only on source inspection.
