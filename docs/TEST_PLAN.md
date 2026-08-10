# Finora Test Plan

Finora handles private financial records and local backups. Tests must use synthetic data only. Passing source/unit tests does not replace native device validation.

## 1. Structural preflight

Run on every pull request and release candidate:

```bash
python build/scripts/verify_structure.py
```

Expected: no malformed XML/XAML/RESX/project files, missing project references, empty implementation/resource files, unfinished placeholder markers, missing XAML event handlers, version/schema drift, missing required repository/policy files, forbidden floating-point money fields, or selected Android privacy-manifest regressions.

## 2. Build/static analysis

Use the platform-appropriate SDK/workloads and the repository verification wrappers. Warnings are errors. Do not ship a release that succeeds only after broad analyzer suppression.

Core/non-MAUI projects can be restored/built/tested on a general .NET host. MAUI target builds require their corresponding workloads/platform hosts.

## 3. Unit tests

Cover pure/domain behavior:

- currency-aware major/minor money conversion including 0-, 2-, and 3-decimal currencies;
- rounding boundaries and integer overflow;
- currency normalization/validation;
- transaction sign/domain validation;
- transfer invariants;
- split sign/total invariants;
- account credit-card field invariants;
- budget kind/category/threshold/period rules;
- explicit budget-period overlap rejection;
- custom-budget active/inactive windows;
- Monday–Sunday weekly period resolution;
- rollover enabled/disabled behavior;
- non-positive/overflowing effective rollover plan rejection;
- savings goal/contribution rules;
- recurrence next-occurrence calculations across month/year boundaries;
- decimal calculator precedence/parentheses/division/error cases;
- locale normalization/application helpers;
- PIN attempt escalation/lockout policy;
- ViewModel base busy/error/command behavior where platform-neutral.

## 4. Database integration tests

Use isolated SQLite databases per test.

### Accounts and transfers

- create/edit/archive/restore account;
- opening balance/current balance checked calculation;
- same-currency paired transfer is atomic and net-zero across accounts;
- transfer edit updates both halves;
- transfer delete/restore affects both halves;
- cross-currency paired transfer is rejected until an explicit exchange workflow exists;
- account currency change is rejected after transaction references;
- account currency change is rejected after recurrence references;
- active recurring source/destination blocks account archival;
- paused recurring dependency permits archival but cannot resume until dependencies are available;
- reconciliation with no difference;
- reconciliation with explicit adjustment;
- reconciliation with unresolved difference is rejected;
- reconciliation arithmetic overflow fails closed;
- reconciled opening balance cannot be silently changed;
- reconciliation history persists.

### Transactions

- create/edit/delete/restore expense/income/refund/adjustment;
- persistence-boundary invalid sign/currency writes are rejected;
- zero/`long.MinValue` money is rejected;
- critical edits create revision history;
- bulk categorization creates revisions;
- split totals equal parent amount;
- split signs equal parent sign;
- split categories must exist and remain active;
- invalid split totals/categories are rejected/detected;
- tag linking and removal;
- duplicate detection does not delete data automatically;
- search/filter by account/category/type/date/text.

### Categories/tags

- create parent/subcategory;
- prevent hierarchy cycle;
- reorder;
- archive/restore;
- merge/reassign references safely;
- cannot reassign a `Subcategory` budget to a root category through archive/merge;
- tag archive/restore;
- tag report requires explicit currency;
- same tag across INR/USD does not aggregate the two currencies;
- tag report uses checked arithmetic and rejects unsupported extreme stored amounts.

### Budgets

- monthly/weekly/custom period boundaries;
- custom cadence requires explicit period;
- overlapping explicit periods are rejected;
- custom budget is absent outside configured window;
- overall/category/subcategory actuals;
- recursive descendant category accounting;
- split transaction contribution to category budget;
- rollover applies only when enabled;
- effective planned amount remains positive;
- warning-threshold arithmetic cannot overflow;
- failed explicit-period replacement rolls back the prior persisted period set.

### Savings goals

- goal create/load;
- deposit/withdrawal;
- withdrawal never drives running progress below zero;
- optional linked contribution transaction exists and uses goal currency;
- target/milestone/completion behavior;
- checked contribution aggregation.

### Recurrence

- processing is idempotent across repeated calls/restarts;
- unique `(RecurrenceRuleId, DueOn)` invariant;
- pending occurrence does not create a transaction before paid/partial-paid action;
- paid/partial paid creates exactly one transaction;
- repeated full payment is idempotent;
- generated transaction must still belong to the rule;
- generated recurring transfer pair must remain complete/balanced/account-correct;
- recurring transfer creates a balanced pair;
- skip/postpone/reopen state transitions;
- paused rule creates no due occurrences;
- paused → resume re-enables generation;
- resume revalidates end date/account/category/currency dependencies;
- archived rule is removed from active rule list while occurrence history remains;
- completed/archived rule cannot be resumed;
- backlog guard prevents unbounded occurrence generation;
- end date and custom interval behavior.

### CSV import

- quoted commas/escaped quotes/newlines where parser supports them;
- UTF-8 validation;
- file/row limits;
- explicit user-selected column mapping;
- currency-specific major-unit decimal conversion (including JPY/KWD-style precision);
- minor-unit import;
- `long.MinValue` rejection before sign normalization;
- invalid date/type/currency/amount rejection;
- missing account/fallback behavior;
- optional category creation;
- tag linking;
- duplicate skipping including duplicates within the same import batch;
- transfer-group/counterparty pair validation;
- parse errors counted exactly once;
- transactional failure/rollback.

### Backup/restore

- create/preview/restore current schema;
- wrong password rejected;
- changed authentication tag/ciphertext rejected;
- truncated/oversized backup rejected;
- future schema rejected;
- attachment bytes round-trip;
- attachment path escape rejected with platform-correct path semantics;
- attachment size/hash mismatch rejected;
- account/transaction currency graph drift rejects backup creation/preview/restore;
- broken transfer/split/category/tag/budget/goal/recurrence/reconciliation graph is rejected;
- custom budget without periods/overlapping periods is rejected;
- active recurrence on archived account is rejected;
- paused historical recurrence on archived account remains compatible;
- internal restore settings/markers are not imported;
- serialized plaintext/receipt buffers are cleared after use/failure as far as managed-memory APIs permit;
- database replacement and attachment-directory swap are consistent;
- durable restore journal/commit marker recovers interruption;
- pending marker restores previous attachment tree;
- committed database marker finalizes new attachment tree;
- incomplete rollback snapshots do not delete untouched live attachments;
- stale restore staging/rollback directories are cleaned after recovery decision;
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
- transaction sign/currency/extreme amount is detected;
- foreign-key violation is detected;
- split sign/total mismatch is detected;
- category cycle is detected;
- invalid custom/overlapping budget periods are detected;
- budget category/subcategory relation drift is detected;
- invalid savings contribution/link/currency/running-progress state is detected;
- active recurrence on archived/mismatched account is detected;
- recurrence duplicate/invalid payment/generated-transaction state is detected;
- reconciliation arithmetic/adjustment-link drift is detected;
- missing/changed receipt file is detected;
- unsafe attachment path is detected;
- sanitized report contains counts/codes only, not account names, merchant/payee names, notes, amounts, or receipt filenames.

## 5. ViewModel/UI-contract tests

Cover navigation contracts and state behavior without pretending this is native UI automation:

- onboarding → adaptive root;
- mobile bottom-tab hierarchy and desktop/tablet flyout hierarchy;
- startup/unlock preserve adaptive destination;
- privacy mode/hidden amounts;
- transaction quick-add/detail routes;
- accounts/detail/reconciliation routes;
- import and transaction-tool routes;
- category/tag management route;
- budget/goal/recurring/report routes;
- recurring page exposes pause/resume/archive and skipped-occurrence reopen bindings;
- dashboard source does not call legacy mixed-currency aggregate API;
- dashboard displays explicit reporting-currency scope;
- backup/restore/settings/legal routes;
- app-lock fallback state;
- destructive finance/sample reset requires typed confirmation;
- larger-interface resources remain globally scalable.

## 6. Android device/emulator tests

At minimum test a current emulator and a physical device when available:

- fresh install/onboarding;
- app restart/force-stop persistence;
- account/transaction/transfer/budget/goal/recurrence core flows;
- recurring pause/resume/archive and stale-reminder cleanup;
- receipt picker/open/delete;
- CSV import and CSV/PDF export;
- encrypted backup/share/save/restore;
- interrupted restore/relaunch recovery where practical;
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
- stale toast/reminder cleanup after recurring lifecycle change;
- display-affinity capture protection where supported;
- file picker/share/export/backup/restore;
- Narrator and high-contrast/large text behavior;
- database/receipt preservation across package upgrade.

## 8. iOS device/simulator tests

- archive/build on supported Xcode host;
- onboarding/core finance flows;
- LocalAuthentication states with PIN fallback;
- UserNotifications permission/scheduling and lifecycle cleanup;
- document picker/share/export/backup/restore/receipt flows;
- VoiceOver, Dynamic Type, reduced motion, dark mode;
- platform screenshot-protection limitation is correctly communicated;
- upgrade/migration behavior.

## 9. Mac Catalyst tests

- archive/build/signing prerequisites;
- resizable windows and keyboard/mouse focus;
- LocalAuthentication/UserNotifications;
- reminder lifecycle cleanup;
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
- wrong/tampered/semantically-invalid backup;
- interruption before/after restore database commit marker;
- repeated recurrence processing after restart;
- pause/resume/archive around pending occurrence state;
- failed custom-budget period replacement after old period deletion begins;
- database lock contention;
- migration interruption using a copied synthetic database.

Never intentionally corrupt a user's real finance database during testing.

## 11. Privacy/security regression

Verify:

- no network/account requirement for current release;
- no analytics/telemetry/advertising SDK introduced;
- logs do not include amounts, account names, merchant/payee names, notes, locations, receipt names/contents, PINs, backup passwords, or encryption material;
- local notification text remains generic/privacy-safe;
- stale local reminders are cancelled when source state is no longer active;
- backup key material is not persisted;
- app-lock verifier uses secure storage for small secrets only and fails closed on missing verifier material;
- local premium remains explicitly non-tamper-proof demo state;
- no repository/build artifact contains signing credentials or real finance data.

## 12. Release evidence

For a release candidate, retain CI run links, test result artifacts, platform build logs, migration-test results, backup/recovery failure-path results, and device-smoke-test checklist results. Do not mark a platform gate complete based only on source inspection.
