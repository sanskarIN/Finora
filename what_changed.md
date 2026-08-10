# What Changed — Finora

Last continuation: **2026-08-10**  
Repository: https://github.com/sanskarIN/Finora  
Current branch: **main**  
Current source line: **Finora 0.2.0 (build 2)**  
Current database schema: **2**

This file is intentionally detailed because implementation/status information that would otherwise occupy the chat is recorded here.

---

## 1. Source of truth and current product boundary

The uploaded Finora Personal Finance master prompt remains the implementation source of truth.

Current product rules preserved throughout this continuation:

- product name: **Finora**;
- framework: .NET MAUI;
- language/UI: C# + XAML;
- local persistence: SQLite through EF Core;
- architecture: App / Application / Domain / Infrastructure / Shared plus Unit/Integration/UI-contract tests;
- current release requires no Finora account/login;
- core finance functionality remains offline/local-first;
- no automatic cloud synchronization;
- no automatic backup upload;
- no analytics/advertising telemetry dependency added;
- no background location collection;
- transaction location remains manually entered text;
- money remains signed 64-bit integer minor units;
- user major-unit arithmetic/conversion remains `decimal` based;
- current transfer model remains same-currency only;
- no automatic exchange-rate lookup or invented FX rate;
- Apache-2.0 remains the repository license;
- attribution remains **Made by the Sanskar**.

Intentionally later-version boundaries remain:

- remote Finora account/login;
- cloud synchronization;
- collaboration/shared-finance server flows;
- server/store-backed commercial entitlement validation;
- automatic exchange-rate conversion;
- analytics/advertising telemetry by default.

These are deliberate product boundaries, not silent unfinished claims for the current local-first source line.

---

## 2. Architecture and repository structure

Current solution projects:

- `src/Finora.Shared`
- `src/Finora.Domain`
- `src/Finora.Application`
- `src/Finora.Infrastructure`
- `src/Finora.App`
- `tests/Finora.UnitTests`
- `tests/Finora.IntegrationTests`
- `tests/Finora.UiTests`

Dependency direction remains:

`App -> Application / Infrastructure -> Domain -> Shared`

Repository engineering remains in place:

- `Finora.sln`;
- central package versions;
- warnings-as-errors/latest recommended analysis;
- structural dependency-free verifier;
- staged GitHub Actions CI;
- Dependabot;
- CodeQL;
- dependency review;
- CODEOWNERS;
- issue templates;
- PR template;
- `.gitattributes` / `.gitignore` hardening;
- legal/privacy/security/support/contribution documentation;
- release/test/store-readiness documentation.

---

## 3. Finance model and monetary invariants

Finora continues to use signed `long` minor units for stored/calculated money.

Current hardening includes:

- no floating-point monetary persistence path;
- known currency precision handled during decimal major/minor conversion;
- `long.MinValue` rejected where sign negation/magnitude would be unsafe;
- checked arithmetic for balances, reports, reconciliations, budgets, goal histories and transfer totals;
- account currency must match transaction currency;
- account currency cannot change after transaction or recurrence dependencies exist;
- unrelated currencies are not silently aggregated;
- no automatic FX conversion.

The legacy aggregate dashboard API fails closed for multi-currency callers; the actual Dashboard UI no longer calls that legacy mixed-currency aggregate path.

Dashboard/report/tag totals use explicit reporting currency scope.

---

## 4. Accounts and transfers

Current source includes:

- account create/edit/archive/restore;
- account types including cash, bank, credit card, wallet, savings, investment placeholder and custom;
- opening/current balance;
- account detail and transaction history;
- credit-card limit and billing-day metadata;
- billing day range aligned to 1–31;
- reconciliation history;
- paired same-currency transfers;
- transfer pair zero-sum validation;
- reciprocal counterparty validation;
- linked transfer edit/delete/restore behavior;
- generic single-transaction editing blocked from silently mutating a transfer half.

Additional lifecycle safety:

- active recurring source/destination dependency blocks account archival;
- paused/completed/archived recurrence history may remain linked to archived accounts;
- resume revalidates account availability and currency;
- account currency mutation is blocked once financial/recurring dependencies exist.

---

## 5. Transactions, splits, tags and revisions

Implemented transaction paths include:

- expense;
- income;
- refund;
- adjustment;
- transfer pair workflow;
- decimal-only calculator;
- account/category/date/time;
- merchant/payee;
- payment method;
- manually entered location;
- note;
- advanced filters;
- detailed editing;
- privacy-safe revision snapshots;
- bulk categorization;
- duplicate review without automatic deletion;
- splits;
- tags;
- receipts;
- soft delete/restore;
- selected/all CSV export;
- selected/all PDF export.

Current invariants include:

- expense negative;
- income/refund positive;
- zero/extreme unsupported amount rejected;
- transfer linkage only on transfer rows;
- deletion state must agree with deletion timestamp;
- split amount nonzero/non-`long.MinValue`;
- split sign must match parent;
- split total must equal parent;
- split categories must be valid/available;
- transaction revision metadata validated before persistence.

---

## 6. Categories and tags

Current functionality includes:

- parent/subcategory create/update;
- hierarchy cycle prevention;
- reorder;
- archive/restore;
- merge/reassign;
- unsafe delete/reassign prevention;
- tag create/update/archive/restore;
- currency-scoped tag reporting.

Hardening added earlier/currently preserved:

- subcategory-budget semantics are protected during category archive/merge/reassignment;
- root-category reassignment cannot silently turn a subcategory budget invalid;
- tag report uses checked arithmetic;
- category/tag metadata now participates in Domain/EF persistence validation.

---

## 7. Reconciliation

Reconciliation source includes:

- preview;
- book balance;
- statement balance;
- explicit difference;
- optional adjustment transaction;
- history;
- note;
- completed timestamp.

Safety rules:

- difference is checked `StatementBalanceMinor - BookBalanceMinor`;
- extreme overflow fails closed;
- adjustment-state flag and adjustment transaction ID must agree;
- linked adjustment transaction must be the correct adjustment/account/amount;
- reconciled opening balance cannot be silently rewritten;
- persistence boundary and backup graph validator both validate reconciliation metadata.

---

## 8. Budgets and custom periods

Implemented budget features:

- overall/category/subcategory;
- weekly/monthly/custom cadence;
- explicit periods;
- warning threshold;
- rollover;
- descendant category accounting;
- split-aware actuals;
- reminder coordination.

`BudgetPeriodPolicy` centralizes interpretation:

- weekly generated windows are Monday–Sunday;
- monthly generated windows use calendar month;
- custom cadence is active only inside explicit periods;
- explicit periods cannot overlap;
- rollover participates only when enabled;
- effective planned amount must remain positive;
- checked arithmetic is used;
- custom cadence requires explicit period data;
- explicit-period replacement is transactional;
- failed replacement is covered by rollback regression tests.

---

## 9. Savings goals

Current goal functionality:

- target amount;
- starting amount;
- target date;
- icon/note;
- contributions;
- withdrawals;
- optional linked transaction;
- forecast/milestone text;
- completion state;
- reduced-motion-friendly celebration text.

Safety rules:

- target positive;
- starting amount between zero and target;
- contribution/withdrawal nonzero and not `long.MinValue`;
- running history uses checked arithmetic;
- running progress cannot fall below zero;
- linked transaction must exist, not be deleted, and match goal currency;
- completion state must match validated progress.

Current continuation fixed a derived-state edge case:

- new goal creation sets `IsCompleted` when starting progress already reaches target;
- startup initialization repairs only stale derived completion flags when the underlying goal/contribution history validates;
- invalid/overflowing/negative histories are not silently repaired and remain visible to the integrity checker.

---

## 10. Recurring items

Implemented recurrence functionality includes:

- expense/income/transfer/refund templates;
- daily/weekly/monthly/yearly/custom interval;
- source account;
- destination account for transfers;
- category;
- amount/currency;
- merchant/payee;
- note;
- start/end;
- grace period;
- reminder lead;
- persisted due occurrence;
- paid;
- partially paid;
- skipped;
- postponed;
- reopened skipped occurrence;
- generated transaction linkage;
- paired recurring transfers;
- pause/resume/archive rule lifecycle.

Safety behavior:

- pending occurrence is persisted before financial transaction creation;
- repeated generation is idempotent through unique rule/due-date constraint;
- paid/partial state requires generated transaction/payment data;
- generated payment must still belong to the rule;
- generated transfer pair must remain complete/balanced/reciprocal;
- pending/skipped occurrence cannot silently contain payment/postponement data;
- postponed state requires a valid later date;
- paid/partial history may retain a valid historical postponed date;
- paused/archived/completed rules stop generation;
- resume revalidates dependencies and end date;
- stale recurring reminders are cancelled during synchronization.

---

## 11. Dashboard and reports

Dashboard remains configurable/privacy-aware with cards for:

- balance;
- income/spending/net;
- remaining budget;
- upcoming recurring;
- top categories;
- savings goals;
- recent transactions;
- six-month cash flow.

Privacy mode/hide amounts remain supported.

Report source includes:

- category spending;
- income vs expense;
- account balance trend;
- budget performance;
- merchant/payee;
- monthly comparison;
- tag report.

Hardening:

- report aggregation is checked;
- category/budget reporting is split-aware;
- category-budget descendants resolve recursively;
- custom-budget windows use the same policy as FinanceStore;
- tag reports require currency scope;
- Dashboard aggregate cards use selected/default reporting currency and explicitly keep other currencies separate.

---

## 12. CSV import and export

CSV import includes:

- mapping;
- preview;
- validation;
- Date/Type/Amount/Account required columns;
- optional Currency/Category/Merchant/Note/Payment Method/Location/Transfer Group/Counterparty/Tags;
- major/minor unit mode;
- currency-aware major-unit decimal conversion;
- fallback account;
- optional category creation;
- duplicate protection including same-batch duplicates;
- transfer group validation;
- UTF-8 validation;
- 50 MB / 100k-row limits;
- transactional persistence.

Export includes:

- CSV;
- dependency-free multipage PDF;
- selected/all transaction modes.

This continuation additionally added bounded stale cache cleanup for user-requested share copies.

---

## 13. Temporary share-artifact cleanup

New Application contract:

- `ITemporaryArtifactCleaner`

New Infrastructure service:

- `TemporaryArtifactCleaner`

Managed cache patterns:

- `Finora-transactions-*.csv`;
- `Finora-transactions-*.pdf`;
- `Finora-*.finora-backup`;
- `Finora-integrity-*.txt`.

Behavior:

- serialized startup runs best-effort cleanup after database initialization and interrupted-restore recovery;
- only managed files older than 24 hours are deleted;
- fresh share copies remain to avoid racing system share sheets;
- unrelated cache files remain;
- diagnostic logs remain;
- file symlink entries are removed as entries rather than recursively following their target;
- cleanup failure cannot block finance startup.

Added regression tests cover:

- stale managed files removed;
- fresh managed files preserved;
- unrelated files preserved;
- diagnostic logs preserved;
- symlink target preservation where link creation is supported.

Once a user explicitly saves/shares a copy outside Finora cache, destination lifecycle is controlled by that destination.

---

## 14. Attachment storage and physical path safety

Receipt/document storage remains app-private.

Existing controls:

- generated internal names;
- MIME allowlist: JPEG/PNG/WebP/HEIC/HEIF/PDF;
- 20 MB/file limit;
- original display filename metadata;
- byte size;
- SHA-256;
- list/open/delete/storage usage/orphan cleanup.

This continuation substantially hardened physical filesystem handling.

`PathSafety` now includes:

- platform-correct comparison;
- canonical descendant resolution;
- `ResolveDescendantWithoutLinks`;
- `EnsureNoLinkTraversal`;
- `EnsureNotLinkIfExists`;
- `IsSymbolicLink`;
- `EnumerateFilesWithoutLinks`.

No-link physical-path policy is now used by:

- attachment open/write/delete/cleanup;
- encrypted backup attachment validation;
- restore staging/rollback paths;
- restore recovery journal;
- crash-safe rollback copying;
- interrupted-restore cleanup;
- data-integrity attachment checks;
- privacy diagnostic log paths.

This protects against a path that is lexically inside `attachments/...` but physically escapes through a symbolic link/reparse point.

Optional regression tests create symlinks where the host permits them and prove fail-closed behavior.

---

## 15. Encrypted backup and restore

Current backup crypto remains:

- user-triggered only;
- PBKDF2-SHA256;
- random salt;
- 210,000 iterations;
- AES-GCM;
- random nonce/tag;
- Finora format magic as authenticated associated data.

Backup snapshot includes schema-v2 supported finance graph plus receipt bytes.

Validation before encryption/preview/restore includes:

- format/magic/length;
- schema;
- unique IDs;
- account/currency relations;
- transfer pairs;
- splits;
- category hierarchy;
- transaction-tag links;
- budgets/periods;
- goals/contributions/completion state;
- recurrence rules/occurrences/generated payments;
- attachment metadata/path/size/hash;
- transaction revisions;
- reconciliation metadata/adjustment link;
- notification metadata;
- settings boundaries;
- internal restore setting exclusion;
- new Domain metadata rules shared with EF persistence.

Crash-safe restore remains layered through:

- `CrashSafeBackupService`;
- `RestoreRecoveryService`;
- `RestoreRecoveryJournal`;
- pre-restore receipt rollback copy;
- database commit marker;
- staged receipt replacement;
- startup recovery before finance navigation.

This continuation added:

- no-link traversal for live receipt root;
- no-link restore staging/rollback/journal paths;
- no-link rollback copying;
- link-aware cleanup;
- accumulated receipt byte buffer clearing on **every** backup-creation exit path, including later-file/query/validation failure;
- decrypted receipt buffer clearing when authenticated graph validation rejects a backup;
- UI-side encrypted backup byte-array clearing after write/share handling;
- masked backup password UI and field clearing.

Managed `string` password values cannot be deterministically zeroed by C#, but Finora no longer persists the backup password and clears the UI field after the operation.

---

## 16. Local notifications and reminder consistency

Current local notification system remains permission-gated and platform-specific:

- Android alarms/BroadcastReceiver;
- Apple UserNotifications;
- Windows scheduled toasts.

Reminder coordinator covers:

- weekly backup;
- budget threshold;
- recurring rules;
- stale schedule reconciliation;
- generic privacy-safe notification text.

This continuation fixed deduplicated replacement ordering:

1. schedule replacement with OS first;
2. if OS scheduling fails, preserve old enabled reminder;
3. if OS scheduling succeeds, persist replacement and disable old row inside DB transaction;
4. commit DB state;
5. best-effort cancel stale OS reminders;
6. if DB write fails after OS scheduling, best-effort cancel the newly scheduled OS reminder.

Additional reconciliation behavior:

- expired enabled rows are disabled;
- disabled/expired IDs receive best-effort OS cancellation retry;
- pending enabled rows are rescheduled;
- cancellation failure no longer restores a DB row to enabled state.

New integration coverage tests:

- failed dedupe replacement;
- successful dedupe replacement;
- cancellation failure;
- expired reminder cleanup;
- reconciliation behavior.

---

## 17. App lock, PIN and biometric security

Current app-lock model remains:

- optional PIN;
- PBKDF2-SHA256 verifier;
- random salt;
- OS secure storage;
- fixed-time comparison;
- escalating local lockout;
- inactivity auto-lock;
- biometric/Windows Hello where supported;
- PIN fallback required.

This continuation hardened PIN handling:

- direct PIN inputs must be 4–12 ASCII digits before PBKDF2;
- secure verifier/salt/derived byte arrays are cleared where managed APIs permit;
- an explicit lock-enabled marker remains for fail-closed behavior;
- if secure-storage provider temporarily throws and marker says lock enabled, `HasPinAsync` fails closed;
- if secure storage is readable but verifier is truly missing/corrupt, stale marker/failure state is cleared so app cannot become permanently trapped on LockPage;
- PIN removal only reports success after secure verifier removal succeeds;
- PIN removal failure is logged privacy-safely and reports generic failure;
- biometric failure no longer surfaces raw provider error text;
- PIN fallback remains available.

Settings secret entry changed from ordinary prompts to masked fields:

- `BackupPasswordEntry`;
- `NewPinEntry`;
- `ConfirmPinEntry`.

Lock PIN entry remains masked.

Fields are cleared after attempts.

---

## 18. Android automatic backup / device-transfer exclusion

Android manifest continues to include:

- `android:allowBackup="false"`;
- `android:usesCleartextTraffic="false"`.

This continuation added explicit backup resources:

- `Platforms/Android/Resources/xml/backup_rules.xml`;
- `Platforms/Android/Resources/xml/data_extraction_rules.xml`.

Legacy full-backup exclusions cover:

- root;
- file;
- database;
- shared preferences;
- external domain.

Android 12+ cloud-backup/device-transfer exclusions cover the same domains.

Structural preflight now requires these files and manifest wiring.

Important validation boundary:

- source/configuration is present;
- final merged-manifest/AAB behavior and actual device/cloud-transfer behavior still require native Android build/device evidence.

---

## 19. Privacy logger and user-visible error handling

Privacy logger already recorded only bounded event/type tokens and ignored arbitrary properties.

This continuation added:

- no-link/reparse protection for diagnostic directory/current/previous log paths;
- regression tests proving caller properties are not serialized;
- regression tests proving exception messages are not serialized;
- rotation tests;
- optional linked-log rejection test.

`ViewModelBase` now maps infrastructure failures safely:

- storage I/O;
- unauthorized access;
- cryptographic errors;
- JSON/provider/database errors;
- path-like/stack-like technical text.

Short deliberate validation messages remain user-visible when safe.

`AsyncCommand` now:

- prevents parallel execution as before;
- contains unexpected non-fatal failures;
- invokes a privacy-safe failure hook;
- is wired in `MauiProgram` to `IPrivacyLogger`;
- does not use `ConfigureAwait(false)` for UI command continuations.

Primary Reports/Settings infrastructure alerts now use generic user-facing text while exception type/event is logged separately.

---

## 20. Data integrity diagnostics

A major correctness gap was found and fixed in this continuation:

- regression tests had been added for budgets/goals/recurrence/reconciliation corruption;
- the service implementation had not actually gained all corresponding checks.

`DataIntegrityService` now implements aggregate checks for:

- SQLite `PRAGMA integrity_check`;
- foreign keys;
- accounts;
- transaction amounts/signs/currencies/dates/linkage;
- transaction-account currency;
- transaction categories;
- transfer pairs;
- splits/signs/totals/categories;
- category parent/cycle validity;
- budgets/periods/category semantics/effective plan;
- goals/contributions/linked transaction currency/running progress/completion state;
- recurrence rule account/destination/category/currency relations;
- recurrence occurrence duplicate/payment/postponement/generated transaction state;
- reconciliation arithmetic/adjustment link;
- attachment parent/path/no-link/existence/size/hash metadata.

Sanitized report remains count/code based and avoids:

- account names;
- merchant/payee names;
- notes;
- amounts;
- receipt filenames;
- transaction contents.

Existing tests incorrectly referenced `IntegrityIssue.Count`; those stale assertions were corrected to the actual `AffectedRecords` contract.

---

## 21. Expanded Domain and EF persistence-boundary validation

This continuation added schema-v2 metadata validation to `DomainRules`.

New/expanded validators cover:

- transaction split;
- category;
- tag;
- transaction-tag link;
- budget period;
- savings goal icon/metadata;
- recurrence occurrence state;
- attachment metadata;
- transaction revision;
- reconciliation;
- notification schedule;
- app setting;
- audit entry;
- backup metadata;
- transaction deletion-state/timestamp agreement.

`FinoraDbContext.SaveChanges` / `SaveChangesAsync` now validates Added/Modified tracked entities before SQLite persistence.

Covered entity types:

- Account;
- FinanceTransaction;
- TransactionSplit;
- Category;
- Tag;
- TransactionTag;
- Budget;
- BudgetPeriod;
- SavingsGoal;
- GoalContribution;
- RecurrenceRule;
- RecurrenceOccurrence;
- Attachment;
- TransactionRevision;
- AccountReconciliation;
- NotificationSchedule;
- AppSetting;
- AuditEntry;
- BackupMetadata.

Model max-length declarations were aligned for metadata fields.

This is deliberately layered with, not substituted for:

- foreign keys;
- unique indexes;
- service-level relation validation;
- backup graph validation;
- data integrity diagnostics.

Direct-DbContext integration tests now prove malformed schema-v2 metadata is rejected before persistence.

A positive regression case confirms paid recurrence history may retain a valid postponed date.

---

## 22. Safe derived savings-goal state repair

Earlier source behavior could create/persist a goal whose starting amount already met target while `IsCompleted` remained false.

Current fixes:

- Savings ViewModel sets `IsCompleted` from starting progress on creation;
- `DatabaseInitializer` performs safe normalization after schema initialization;
- it validates goal/contribution history first;
- it uses checked running arithmetic;
- it repairs only the derived completion flag;
- corrupt/overflowing/negative-running histories are not modified;
- such invalid history remains detectable by `DataIntegrityService`.

Added tests cover:

- valid stale completion flag repaired;
- invalid negative-running history left untouched;
- integrity checker still reports invalid contribution history.

---

## 23. Accessibility improvements

Current continuation improved security-surface semantics:

Settings:

- heading levels;
- security switch descriptions;
- masked backup password description;
- masked PIN setup/confirm descriptions.

Lock screen:

- Level1 heading;
- live status description;
- biometric/Windows Hello description with PIN fallback;
- masked PIN semantic guidance;
- PIN unlock button description.

Native TalkBack/VoiceOver/Narrator/large-text/keyboard/high-contrast validation remains an external release gate.

---

## 24. Structural preflight expansion

`build/scripts/verify_structure.py` continues to validate:

- required files;
- XML/XAML/RESX/project parseability;
- non-empty source/resource files;
- TODO/FIXME/NotImplemented/placeholder markers;
- project references;
- solution project references;
- XAML partial classes;
- XAML event handler presence;
- application version consistency;
- database schema documentation consistency;
- floating-point monetary-field patterns.

This continuation added privacy/security configuration gates:

- Android `allowBackup=false`;
- Android no-cleartext setting;
- legacy backup-rule wiring;
- Android 12+ data-extraction-rule wiring;
- full-domain excludes for root/file/database/sharedpref/external;
- required backup-rule resources;
- `BackupPasswordEntry` must remain masked;
- `NewPinEntry` must remain masked;
- `ConfirmPinEntry` must remain masked;
- secret password/PIN flows must not regress to ordinary `DisplayPromptAsync`;
- raw `ex.Message`/`exception.Message` must not be passed into user alerts.

A Settings XAML/code-behind handler mismatch discovered during this continuation was corrected (`OnDeleteAllClicked`).

---

## 25. Tests added or expanded in this continuation

### Notification tests

`LocalNotificationConsistencyTests.cs`

Covers:

- failed replacement preserves old reminder;
- successful dedupe replacement;
- cancellation failure persistence;
- expired-row disable;
- reconciliation retry/reschedule behavior.

### Receipt/path tests

`AttachmentPathSafetyTests.cs`

Expanded to cover:

- lexical traversal rejection;
- platform case behavior;
- symbolic-link receipt directory rejection when supported;
- attachment open fails closed;
- integrity report detects unsafe path;
- encrypted backup refuses linked receipt path.

### Integrity tests

Existing integrity assertion property names fixed.

Aggregate integrity regression tests now correspond to actual service implementation.

### ViewModel/command tests

`ViewModelBaseTests.cs` expanded for:

- busy/error lifecycle;
- validation message preservation;
- path/database technical text redaction;
- cryptographic error redaction;
- cancellation text;
- concurrent invocation guard;
- AsyncCommand parallel-execution guard;
- AsyncCommand unexpected failure containment/privacy hook;
- property notification behavior.

### Privacy logger tests

`PrivacyLoggerTests.cs`

Covers:

- caller property redaction;
- exception message redaction;
- event token sanitization;
- bounded log rotation;
- linked diagnostic file refusal where supported.

### Temporary cache tests

`TemporaryArtifactCleanerTests.cs`

Covers:

- stale managed cleanup;
- fresh managed preservation;
- unrelated file preservation;
- diagnostic file preservation;
- negative age validation;
- symlink-entry target preservation where supported.

### Metadata persistence tests

`MetadataPersistenceInvariantTests.cs`

Covers direct EF rejection of:

- attachment traversal metadata;
- empty notification title;
- paid occurrence without generated transaction;
- incorrect reconciliation difference;
- negative category sort order;
- missing revision snapshot;
- deleted transaction without deletion timestamp.

Also covers a valid paid-after-postponement history case.

### Derived goal-state tests

`DerivedGoalStateRepairTests.cs`

Covers:

- safe completion flag repair;
- corrupt negative-running contribution history left untouched;
- integrity detection remains active.

Previously-added/currently-retained test suites also cover:

- account lifecycle dependencies;
- reconciliation safety;
- aggregate integrity;
- backup graph validation;
- budget rollback;
- category mutation safety;
- custom-budget persistence;
- finance relation invariants;
- recurring lifecycle/state transitions;
- report consistency;
- budget effective plan;
- budget period policy;
- Domain rules;
- migration paths;
- backup/attachment round trips;
- UI route/contracts.

---

## 26. Documentation updated in this continuation

Updated documentation includes:

- `README.md`
- `CHANGELOG.md`
- `PROJECT_STATUS.md`
- `docs/security/THREAT_MODEL.md`
- `docs/TEST_PLAN.md`
- `docs/releases/RELEASE_CHECKLIST.md`
- `docs/releases/STORE_READINESS.md`
- `docs/privacy/DATA_LIFECYCLE.md`
- `docs/architecture/DATABASE_SCHEMA.md`
- this `what_changed.md`

Documentation now explicitly covers:

- symlink/reparse threat model;
- masked secret entry;
- PIN secure-storage consistency;
- generic provider error behavior;
- notification replacement consistency;
- Android backup/device-transfer exclusion;
- stale share cache cleanup;
- EF metadata persistence boundary;
- expanded integrity diagnostics;
- safe derived goal completion repair;
- backup receipt-buffer memory hygiene;
- external/native validation boundaries.

---

## 27. Focused commit ledger — current continuation

The continuation intentionally used many focused commits. Known commit IDs/messages include:

- `0f3a2b29271eb4ddfb78190df83341f236a96c74` — `fix(privacy): prevent raw exception details from reaching UI alerts`
- `9678ae1515f7da36cacf8a25b999474d01c7faa5` — `fix(notifications): make dedupe replacement failure-safe across database and OS`
- `9ce9e7dfad5077303905dc6dff4e47e8087aa87e` — `test(notifications): cover dedupe failure cancellation and reconciliation consistency`
- `fb266f4c8e04755a459eb1acbaf49557377093c5` — `test(notifications): fix result value handling in consistency suite`
- `12f39d8838b71141339f88c12f3aa1186b19e160` — `fix(storage): reject symbolic-link traversal in app-private paths`
- `563878f6ebf9300186193680be7a851dc0c0eac0` — `fix(attachments): reject linked receipt paths and safe-walk cleanup`
- `0bb81100b95ff0c5bf661d12450a3a278742b0f6` — `fix(backup): clear receipt buffers on validation failure and reject linked paths`
- `f210c6ab6b886f9d11c087a99d5d55ea5a3c86bb` — `fix(recovery): reject linked restore journals and recovery directories`
- `999bb3486bf018fe6a24189ece287bca45f8f4b9` — `fix(recovery): make receipt rollback copies symlink-safe`
- `b15c24fad552017a11bc94d9116cf1779eeaa037` — `fix(recovery): make interrupted-restore cleanup link-aware`
- `6c8d1693f40d5bb3ffa42e65c670cd4a80ff86f5` — `test(integrity): fix issue-count assertions to current contract`
- `46e0b7d27b99062ea9c06342c287f65e6ee8d4dc` — `fix(integrity): implement missing aggregate graph checks and linked-path detection`
- `1a5ea99c0f157dbd500ae4e12d027a7586a825d3` — `test(storage): cover symbolic-link receipt traversal and fix integrity assertions`
- `df26a2a06994b385bc5d1efcb77a1c9aef98abc4` — `fix(ui): sanitize bound errors and contain async command failures`
- `4efe78963f7267d3d1e1c852fab5bcf03e46e8b1` — `fix(ui): preserve UI synchronization context for viewmodel commands`
- `57cde60e25d5b8853ee821da08b8f9356acb997e` — `feat(diagnostics): route async command failures to privacy-safe logger`
- `2956a185ace5059a956f4bfa8d0ab068015c2fee` — `test(ui): cover safe error mapping and async command failure containment`
- `9ec975582ff481f2e22146cf478afd125f4323b4` — `fix(backup): zero all accumulated receipt buffers on every creation exit`
- `cdc120c75cc0c8b07735c523cac5263302dbf10e` — `fix(diagnostics): harden privacy log storage against linked paths`
- `e428c39e4cc9b2aec94e0f6f1492b37237641b9e` — `test(diagnostics): verify privacy logger redaction rotation and linked-path refusal`
- `728089c89564157bc630a4eba4ded4a252f30493` — `test(diagnostics): avoid framework-specific timeout assertion helper`
- `75b91e023371632faf335329dbc9360ffb042ef7` — `feat(storage): add stale temporary artifact cleanup contract`
- `24e31c45487b9ee2096657a8a95e7e980133f866` — `feat(storage): implement bounded cleanup for stale shared Finora artifacts`
- focused DI registration + immediate typo-correction commits for the temporary artifact cleaner;
- focused startup integration commit for stale share cleanup;
- focused integration test commit for stale share cleanup behavior;
- `9511a9494b0ae461d2998f956e571d84ae451194` — `feat(android): exclude all Finora private domains from legacy full backup`
- `427eca35bcb5d9965d6b890c0da106f5af86489c` — `feat(android): exclude private finance data from cloud and device transfer`
- `695929ee9e5880be5980ae386c4917dd15ee2b7c` — `feat(android): wire explicit no-backup and no-transfer rules`
- `4874cb946a4624e2720c08f25577be45470322c5` — `feat(security): use masked settings fields for backup passwords and PIN setup`
- `a6a722c89c788b2bb0c1d945e87b2304d2b403d6` — `fix(security): consume and clear masked backup password and PIN fields`
- `5d358a93c1febe4137ec681e0993c94c44034ead` — `fix(ui): align Settings delete handler with code-behind`
- `b857f19904867052b7f561553ba3968b3adb8832` — `ci: guard Android backup rules secret prompts and raw exception alerts`
- `431ea67efb8a5ebd2c38fd80482c65022c42a72d` — `fix(security): harden PIN verifier consistency and input bounds`
- `2ac1daafefdece1089420f7d27a966aae7a6ad7f` — `fix(security): handle PIN removal failures without false success`
- `c00cd2df71ababb8a0317f3681123a2541666b77` — `fix(security): route PIN removal through failure-safe handler`
- focused Domain commit adding schema-v2 metadata validation rules;
- focused Domain correction preserving paid postponement history/legacy hash compatibility;
- focused EF persistence-boundary validation commit;
- focused EF change-tracking import correction;
- focused metadata persistence integration tests;
- focused lock-screen biometric provider-text sanitization;
- focused lock-screen accessibility semantics;
- focused threat-model update;
- focused test-plan update;
- focused release-checklist update;
- focused data-lifecycle update;
- focused store-readiness update;
- focused backup graph/domain metadata alignment;
- focused new-goal completion initialization;
- focused startup derived-goal-state repair;
- focused derived-goal-state repair tests;
- focused database-schema documentation update;
- focused public project-status update;
- focused changelog update;
- focused README update;
- final `what_changed.md` ledger commit.

The GitHub connector used for this work does not expose a supported author/committer-email override on its file-commit actions, so it cannot force `sanskarin@outlook.in` into Git commit metadata. The requested email remains documented as Finora business/security contact.

---

## 28. Earlier source baseline retained from previous continuations

The current continuation did not remove the large previously implemented Finora feature set, including:

- schema-v2 migration runner;
- transaction revisions;
- reconciliation records;
- notification schedule persistence;
- account management/detail;
- receipt attachment service;
- category/tag management;
- recurring workflow/payment states;
- advanced reports;
- mapped CSV import;
- finance store/domain invariant hardening;
- encrypted backup/restore;
- CSV/PDF export;
- reminder coordinator;
- biometric/Windows Hello integration;
- sensitive-screen protection;
- configurable Dashboard;
- settings/onboarding/legal surfaces;
- deterministic synthetic sample data;
- English baseline/Hindi common-resource localization readiness;
- branding icon/splash resources;
- privacy-safe exception coordinator;
- repository structural preflight/CI/dependency/security automation;
- comprehensive architecture/privacy/security/release documentation.

---

## 29. Verification state

### What is source-verified in this continuation

The repository was audited/updated through GitHub file reads/writes and focused regression tests were added for identified logic/failure paths.

Structural source checks and invariants were strengthened so CI can detect more classes of regression.

### What is **not** claimed from this execution environment

A usable local `.NET` / MAUI SDK/toolchain is not available in this ChatGPT execution environment.

Therefore this continuation does **not** claim that current head has locally passed:

- `dotnet restore`;
- `dotnet workload restore`;
- C# compilation;
- analyzers;
- unit tests executed by `dotnet test`;
- integration tests executed by `dotnet test`;
- UI-contract tests executed by `dotnet test`;
- Android MAUI build;
- Windows MAUI build;
- iOS build/archive;
- Mac Catalyst build/archive;
- emulator/simulator tests;
- physical-device tests;
- signing;
- MSIX/AAB/IPA/package generation;
- store-console validation.

Web search is disabled in this environment, so live package/runner/toolchain compatibility was not independently web-verified.

The available GitHub connector does not provide a workflow-dispatch action in this session.

GitHub Actions generally report through Check Runs rather than the classic combined-status endpoint. An empty classic `statuses` list must **not** be interpreted as either a pass or proof that Actions never ran.

Release evidence must come from actual workflow/check-run/build/device/store results.

---

## 30. Required external release gates still pending evidence

Before calling Finora store-ready, execute/retain evidence for:

1. dependency-free structural preflight;
2. `.NET 10` restore;
3. MAUI workload restore;
4. format/analyzer verification;
5. Release core build;
6. unit tests;
7. integration tests;
8. UI-contract tests;
9. Android MAUI build;
10. Windows MAUI build;
11. iOS/Mac Catalyst builds on supported macOS/Xcode host;
12. v1→v2 migration tests through actual production path;
13. data-integrity regression suite;
14. notification replacement/reconciliation tests;
15. encrypted backup/restore/tamper/graph/path/failure tests;
16. interrupted-restore recovery tests;
17. symbolic-link/reparse tests on representative filesystems;
18. app-lock secure-storage/provider-failure tests;
19. PIN/biometric/Windows Hello/capture tests;
20. masked-secret-entry behavior on native platforms;
21. Android merged-manifest verification;
22. Android ordinary backup/cloud-backup/device-transfer exclusion behavior;
23. local notification permission/reboot/doze/packaging tests;
24. stale cache share-copy cleanup behavior on packaged/native app;
25. TalkBack/VoiceOver/Narrator/keyboard/large-text/high-contrast tests;
26. package signing/install/upgrade/uninstall tests;
27. exact dependency vulnerability/license review;
28. final privacy/data-safety/store metadata review.

---

## 31. Branding, repository and contact identity

- Product: **Finora**
- Attribution: **Made by the Sanskar**
- Repository: https://github.com/sanskarIN/Finora
- Creator/open-source profile: https://www.github.com/sanskarIN
- Business/security: `sanskarin@outlook.in`
- Support: `supportramsandesh@gmail.com`
- License: Apache-2.0

---

## 32. Current conclusion

The current source line is materially more defensive than the previous `what_changed.md` baseline, particularly around:

- notification replacement ordering;
- physical private-path confinement;
- encrypted backup buffer hygiene;
- restore/recovery symlink safety;
- data-integrity aggregate coverage;
- schema-v2 metadata persistence validation;
- Android automatic-backup/device-transfer exclusion;
- masked secret entry;
- PIN/secure-storage consistency;
- generic user-facing infrastructure errors;
- privacy-safe diagnostics;
- stale share-cache retention;
- safe derived savings-goal state repair;
- regression-test coverage;
- release documentation.

Native compilation/test/device/signing/store evidence remains a separate release gate and is intentionally not overstated here.
