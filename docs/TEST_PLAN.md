# Finora Test Plan

Finora handles private financial records and local backups. Tests must use synthetic data only. Passing source/unit tests does not replace native device validation.

## 1. Structural preflight

Run on every pull request and release candidate:

```bash
python build/scripts/verify_structure.py
```

Expected: no malformed XML/XAML/RESX/project files, missing project references, empty implementation/resource files, unfinished placeholder markers, missing XAML event handlers, version/schema drift, missing required repository/policy files, forbidden floating-point money fields, raw minor-unit user-facing money bindings, Android automatic-backup/data-transfer regressions, unmasked Settings backup/PIN fields, password/PIN `DisplayPromptAsync` regressions, biometric provider-text regressions, or raw exception messages passed into user alerts.

## 2. Build/static analysis

Use the platform-appropriate SDK/workloads and repository verification wrappers. Warnings are errors. Do not ship a release that succeeds only after broad analyzer suppression.

Core/non-MAUI projects can be restored/built/tested on a general .NET host. MAUI target builds require corresponding workloads/platform hosts.

## 3. Unit tests

Cover pure/domain behavior:

- currency-aware major/minor money conversion including 0-, 2-, and 3-decimal currencies;
- rounding boundaries and integer overflow;
- currency normalization/validation;
- transaction sign/domain/deletion-state validation;
- transfer invariants;
- split sign/total invariants;
- category/tag metadata rules;
- account credit-card field invariants;
- budget kind/category/threshold/period rules;
- explicit budget-period overlap rejection;
- custom-budget active/inactive windows;
- Monday–Sunday weekly period resolution;
- rollover enabled/disabled behavior;
- non-positive/overflowing effective rollover plan rejection;
- savings goal/contribution rules;
- recurrence next-occurrence calculations across month/year boundaries;
- recurrence occurrence structural state rules;
- attachment/revision/reconciliation/notification metadata rules;
- decimal calculator precedence/parentheses/division/error cases;
- locale normalization/application helpers;
- dashboard current/previous financial-month, trailing 30/90-day, and year-to-date period policy;
- local calendar date range conversion to exclusive UTC bounds, including non-UTC offsets and invalid ranges;
- PIN attempt escalation/lockout policy;
- ViewModel busy/error/command behavior;
- safe error mapper preserves short validation text but redacts path/database/crypto/provider details;
- unexpected `AsyncCommand` failures are contained and routed through the privacy hook.

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
- persistence-boundary invalid sign/currency/deletion-state writes are rejected;
- zero/`long.MinValue` money is rejected;
- critical edits create revision history;
- bulk categorization creates revisions;
- split totals equal parent amount;
- split signs equal parent sign;
- split categories must exist and remain active;
- invalid split totals/categories are rejected/detected;
- tag linking and removal;
- duplicate detection does not delete data automatically;
- search/filter by account/category/type/date/text;
- local-calendar filter dates use the shared UTC boundary policy;
- transaction history sort choices remain deterministic;
- first history page is bounded to 50 rows and Load more appends the next page without duplicating rows.

### Schema-v2 metadata persistence boundary

Direct `FinoraDbContext.SaveChanges` tests must prove that Added/Modified rows cannot bypass structural rules:

- attachment traversal/unsupported content/invalid size/hash metadata rejected;
- empty/oversized notification fields rejected;
- recurrence occurrence paid/postponed state contradictions rejected;
- a paid occurrence may retain a valid historical postponed date;
- reconciliation difference/adjustment-state mismatch rejected;
- invalid category/tag metadata rejected;
- invalid transaction revision metadata rejected;
- invalid app setting/audit/backup metadata rejected;
- invalid transaction deletion timestamp state rejected.

These tests complement relational services, foreign keys, backup graph validation, and the integrity checker rather than replacing them.

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
- failed explicit-period replacement rolls back prior persisted period set.

### Savings goals

- goal create/load;
- deposit/withdrawal;
- withdrawal never drives running progress below zero;
- optional linked contribution transaction exists and uses goal currency;
- target/milestone/completion behavior;
- checked contribution aggregation;
- savings-progress report derives current amount/completion from validated history;
- savings forecast does not expose estimated money while privacy mode hides amounts.

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
- paid-after-postponement preserves useful history without violating state validation;
- paused rule creates no due occurrences;
- paused → resume re-enables generation;
- resume revalidates end date/account/category/currency dependencies;
- archived rule is removed from active rule list while occurrence history remains;
- completed/archived rule cannot be resumed;
- backlog guard prevents unbounded occurrence generation;
- end date and custom interval behavior;
- recurring-obligation report retains rule type/status/currency/next-due information.

### Reports and local-calendar boundaries

- category spending remains split-aware and currency-scoped;
- income-versus-expense remains currency-scoped;
- account balance trend uses local-calendar boundaries;
- budget performance resolves budget windows through `BudgetPeriodPolicy` and local-calendar UTC boundaries;
- monthly comparison groups by local calendar month rather than UTC month;
- yearly comparison groups by local calendar year;
- current monthly/yearly comparisons exclude future-dated imported rows;
- yearly comparison returns the requested trailing-year range with checked income/expense/net values;
- savings-progress report uses checked contribution history;
- recurring-obligation report excludes archived rules but preserves active/paused/completed report state as applicable;
- chart source can represent negative net values without applying absolute magnitude.

### Local notifications

- first schedule persists after OS acceptance;
- failed deduplicated replacement preserves old enabled reminder and does not cancel it;
- successful deduplicated replacement disables old DB row only after new OS acceptance;
- old OS reminder cancellation runs after database commit;
- platform cancellation failure does not revert DB disabled state;
- expired enabled rows become disabled during reconciliation;
- disabled/expired IDs are retried for OS cancellation;
- pending enabled reminders are rescheduled after reconciliation;
- generic privacy-safe content remains free of amount/account/merchant/note details;
- Android cancellation queries an existing `PendingIntent` with `NoCreate` rather than creating a cancellation artifact.

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
- duplicate skipping including duplicates within same import batch;
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
- attachment lexical path escape rejected with platform-correct path semantics;
- attachment symbolic-link/reparse traversal rejected when host supports test link creation;
- crash-safe rollback copy refuses linked entries;
- restore journal/staging/rollback paths refuse linked traversal;
- attachment size/hash mismatch rejected;
- account/transaction currency graph drift rejects backup creation/preview/restore;
- broken transfer/split/category/tag/budget/goal/recurrence/reconciliation graph is rejected;
- custom budget without periods/overlapping periods is rejected;
- active recurrence on archived account is rejected;
- paused historical recurrence on archived account remains compatible;
- internal restore settings/markers are not imported;
- every accumulated receipt buffer is cleared on backup creation success/failure as far as managed-memory APIs permit;
- decrypted receipt buffers are cleared on authenticated graph-validation failure;
- UI-side encrypted backup byte array is cleared after write/share;
- database replacement and attachment-directory swap are consistent;
- durable restore journal/commit marker recovers interruption;
- pending marker restores previous attachment tree;
- committed database marker finalizes new attachment tree;
- incomplete rollback snapshots do not delete untouched live attachments;
- stale restore staging/rollback directories are cleaned after recovery decision;
- failed restore leaves prior data usable;
- backup metadata/audit entries do not expose finance contents.

### Diagnostics and temporary artifacts

- privacy logger ignores caller property dictionaries;
- privacy logger records exception type/event token but never exception message/stack;
- event tokens are bounded/sanitized;
- diagnostic log rotates at bounded size;
- diagnostic log refuses linked/reparse storage paths;
- stale managed CSV/PDF/backup/integrity-report cache copies older than grace period are removed;
- fresh managed share copies remain;
- unrelated cache files and diagnostic logs remain;
- file symlink share entry deletion does not delete target when host supports links;
- cleanup failure is best-effort and does not block startup.

### Migrations

For every released schema version:

1. create representative synthetic database at that schema;
2. migrate through actual production path;
3. verify schema version advances only on successful migration;
4. verify all entities/relationships/data remain correct;
5. verify failed migration rolls back;
6. run data-integrity checker after migration.

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
- invalid savings contribution/link/currency/running-progress/completion state is detected;
- active recurrence on archived/mismatched account is detected;
- recurrence duplicate/invalid payment/generated-transaction state is detected;
- reconciliation arithmetic/adjustment-link drift is detected;
- missing/changed receipt file is detected;
- unsafe lexical or linked attachment path is detected;
- sanitized report contains counts/codes only, not account names, merchant/payee names, notes, amounts, or receipt filenames.

## 5. ViewModel/UI-contract tests

Cover navigation contracts and state behavior without pretending this is native UI automation:

- onboarding → adaptive root;
- onboarding exposes privacy and terms access and tells users it can be revisited;
- mobile bottom-tab hierarchy and desktop/tablet flyout hierarchy;
- startup/unlock preserve adaptive destination;
- privacy mode/hidden amounts across dashboard, accounts, transaction history/tools/detail splits, budgets, savings, recurring, reconciliation and reports;
- quantitative report charts hidden while amounts are hidden;
- passive monetary rows use currency-aware formatting rather than raw minor-unit labels;
- account/transaction detail editable amounts use currency-specific decimal precision rather than a hard-coded two-decimal assumption;
- transaction quick-add/detail routes;
- account detail billing-day UI remains 1–31;
- accounts/detail/reconciliation routes;
- import and transaction-tool routes;
- transaction history sort picker and bounded Load more contract;
- category/tag management route;
- budget/goal/recurring/report routes;
- recurring page exposes pause/resume/archive and skipped-occurrence reopen bindings;
- dashboard source does not call legacy mixed-currency aggregate API;
- dashboard displays explicit reporting-currency scope;
- dashboard exposes current/previous financial month, trailing 30/90 days and year-to-date period selection;
- Reports page exposes category, income/expense, monthly, yearly, merchant, budget, recurring, savings and account-trend sections;
- signed report chart source uses a true zero baseline and does not call `Math.Abs(item.ValueMinor)`;
- backup/restore/settings/legal routes;
- Settings can revisit onboarding;
- Settings About version/build comes from packaged `AppInfo` metadata;
- Settings exposes repository/profile/business/support/license/privacy/terms/notices/contributing/security/support-guide information;
- Settings full deletion remains wired to dedicated complete finance reset handler;
- Settings backup password/new PIN/confirm PIN fields remain masked;
- secret fields are cleared after use;
- lock PIN field remains masked and screen-reader described;
- biometric failure uses stable generic text rather than raw provider text;
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
- dedupe replacement failure/success behavior where practical;
- cancelling a nonexistent reminder does not create a new pending alarm artifact;
- reboot/doze/force-stop reminder limitations;
- biometric success/cancel/unavailable/lockout with PIN fallback;
- biometric OS/provider error strings are not surfaced verbatim;
- `FLAG_SECURE` behavior;
- verify app data is excluded from Android automatic backup/cloud backup/device transfer according to manifest/rules;
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
- platform screenshot-protection limitation correctly communicated;
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
- linked/reparse receipt directory/path;
- locked/unavailable file;
- wrong/tampered/semantically-invalid backup;
- interruption before/after restore database commit marker;
- repeated recurrence processing after restart;
- pause/resume/archive around pending occurrence state;
- failed custom-budget period replacement after old period deletion begins;
- notification replacement OS failure and DB failure paths;
- secure-storage unavailable/missing/malformed PIN verifier paths;
- database lock contention;
- migration interruption using a copied synthetic database.

Never intentionally corrupt a user's real finance database during testing.

## 11. Privacy/security regression

Verify:

- no network/account requirement for current release;
- no analytics/telemetry/advertising SDK introduced;
- logs do not include amounts, account names, merchant/payee names, notes, locations, receipt names/contents, PINs, backup passwords, provider exception messages, or encryption material;
- passive money surfaces hide monetary values when privacy/hide-on-launch is active;
- quantitative chart magnitude is not visible while amounts are hidden;
- bound errors and alerts do not expose raw storage/database/crypto/provider paths/messages;
- local notification text remains generic/privacy-safe;
- stale local reminders are cancelled when source state is no longer active;
- backup key material is not persisted;
- app-lock verifier uses secure storage for small secrets only;
- temporary secure-storage provider failure fails closed when lock-enabled marker exists;
- readable missing/corrupt verifier cannot permanently trap app behind stale marker;
- Android app-private data is excluded from ordinary backup/device-transfer paths;
- local premium remains explicitly non-tamper-proof demo state;
- no repository/build artifact contains signing credentials or real finance data.

## 12. Release evidence

For a release candidate, retain CI run links, test result artifacts, platform build logs, migration-test results, backup/recovery failure-path results, Android backup-rule packaging evidence, and device-smoke-test checklist results. Do not mark a platform gate complete based only on source inspection or an empty classic commit-status list.
