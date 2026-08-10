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
- location remains manually entered transaction text;
- money remains signed 64-bit integer minor units;
- user major-unit arithmetic/conversion remains `decimal` based;
- same-currency transfer model remains explicit; no invented exchange rates;
- Apache-2.0 remains the repository license;
- attribution remains **Made by the Sanskar**.

Intentionally later-version boundaries remain:

- remote Finora account/login;
- cloud synchronization;
- collaboration/shared remote finance data;
- mobile-number authentication;
- server-backed commercial entitlement;
- automatic exchange-rate conversion.

These are not silently implemented as hidden network dependencies in the current local-first line.

---

## 2. Git commit identity limitation

Requested Git commit email: `sanskarin@outlook.in`.

The GitHub connector available in this ChatGPT session can create/update repository content and commits but does **not** expose an author/committer-email override for connector-created commits. Therefore the connector-created commits in this session cannot truthfully be represented as having been forced to `sanskarin@outlook.in`.

For local Git commits, configure:

```bash
git config user.email "sanskarin@outlook.in"
```

This limitation remains explicitly documented rather than hidden.

---

## 3. Continuation goal

This continuation concentrated on source correctness/reliability rather than cosmetic expansion. The audit walked through persisted money, account dependencies, recurrence state, budget windows, reporting currency boundaries, backup/restore graphs, app-private path confinement, integrity diagnostics, adaptive UI contracts, platform manifests/APIs, test coverage, and release documentation.

Where an invariant could be bypassed by direct EF/import/restore paths, validation was moved toward shared domain/persistence/backup/integrity boundaries instead of relying only on UI validation.

---

## 4. Platform-correct app-private path confinement

### New shared storage-path policy

Added `src/Finora.Infrastructure/PathSafety.cs`.

The path helper centralizes:

- root normalization;
- descendant resolution;
- path traversal rejection;
- platform-correct path comparison behavior.

Windows is treated with case-insensitive path comparison. Unix-style Android/Apple paths remain case-sensitive.

### Attachment service hardening

`AttachmentService` now uses the shared path policy for receipt/document paths rather than treating every platform as case-insensitive.

### Backup/restore path hardening

Backup attachment path validation and staged restore path resolution use the same policy.

### Restore-journal and integrity path hardening

Restore recovery and integrity diagnostics use the same app-private path semantics, preventing one subsystem from accepting a path another subsystem rejects.

### Tests

Added path-safety regression coverage including sibling-prefix/path traversal behavior and platform-aware comparisons.

Representative commits in this area include:

- `fix(storage): add platform-correct path confinement primitive`
- `fix(attachments): enforce platform-correct receipt path confinement`
- backup/restore/integrity path-confinement updates
- attachment path-safety regression tests.

---

## 5. Startup/recovery ordering

Application activation was hardened so database initialization and restore recovery complete before finance navigation becomes reachable.

The startup path now serializes activation work rather than allowing overlapping activation/lifecycle calls to race the database/recovery state.

This is important because a crash-interrupted restore can leave a durable recovery journal/marker requiring deterministic resolution before pages query finance data or receipt files.

Representative commit:

- `fix(app): serialize startup initialization and restore recovery before navigation`

---

## 6. Domain and persistence invariants

### Transaction invariants

Current domain/persistence checks cover:

- amount cannot be zero;
- amount cannot be `long.MinValue`;
- currency must be valid;
- account ID must be present;
- transaction timestamp must be present;
- Expense amount must be negative;
- Income/Refund amount must be positive;
- transfer rows require transfer group + counterparty;
- transfer counterparty cannot equal the row account;
- non-transfer rows cannot carry transfer linkage;
- transfer rows cannot contain category splits;
- split amounts cannot be zero/`long.MinValue`;
- split sign must match the parent;
- checked split sum must equal the parent amount.

### Account invariants

Current checks cover:

- account name/currency;
- billing day 1–31;
- credit limit nonnegative;
- credit-card metadata only on credit-card account types.

### Budget invariants

Current checks cover:

- positive budget limit;
- valid currency;
- warning threshold 1–100;
- correct category requirement for Overall/Category/Subcategory kinds;
- explicit period validity;
- non-overlapping explicit periods.

### Savings invariants

Current checks cover:

- positive target;
- starting amount in 0..target;
- valid currency;
- contribution/withdrawal nonzero/non-`long.MinValue`;
- valid contribution timestamp;
- running progress cannot fall below zero.

### Recurrence invariants

Current checks cover:

- rule name/interval/positive amount/currency;
- valid source account;
- start/end date relationship;
- day-of-month bounds;
- grace/reminder bounds;
- no recurring Adjustment template;
- transfer destination must exist/differ from source;
- transfer recurrence cannot carry category;
- non-transfer recurrence cannot carry destination account;
- next-occurrence movement must advance.

### EF persistence boundary

`FinoraDbContext` validation now acts as an additional protection layer so invalid account/transaction state cannot be committed through tracked EF writes merely because it bypassed the normal ViewModel/service path.

Direct-EF integration tests intentionally exercise this boundary.

---

## 7. Account lifecycle, recurrence dependencies, and reconciliation

### Account currency immutability after dependencies

Account currency change is rejected after either:

- transactions reference the account; or
- recurrence rules reference it as source/destination.

This prevents historical minor-unit values or recurring templates from being silently reinterpreted under a new currency label.

### Account archival and recurrence

An account used by an **Active** recurring rule cannot be archived until that rule is paused/completed/archived.

Both the account-management service and the direct FinanceStore archive path enforce this boundary.

Paused historical recurrence can remain linked to an archived account, but a future Resume operation must revalidate current dependencies.

### Account editor

Credit-card billing-day editing now consistently supports the domain range 1–31 rather than truncating valid days at 28.

### Reconciliation hardening

Reconciliation now fails closed for:

- invalid dates;
- arithmetic overflow;
- unresolved difference without explicit adjustment handling;
- inconsistent adjustment state.

Reconciled opening-balance changes are protected from silently rewriting already-reconciled history.

### Tests

Added account lifecycle dependency tests covering:

- active recurrence blocks Archive action;
- state-picker archival is also blocked;
- paused recurrence permits archival;
- reconciliation overflow/date behavior;
- reconciled opening-balance protection.

Representative commits include:

- `fix(accounts): block archival while active recurring rules depend on account`
- `fix(accounts): allow full domain billing-day range in account editor`
- `test(accounts): cover recurring dependencies during account archival`
- reconciliation hardening/tests.

---

## 8. Recurrence occurrence/payment integrity

Recurring payment mutation now validates the rule and related account/category state before changing money.

### Generated ordinary payment checks

Existing generated payment rows must still:

- exist;
- remain non-deleted;
- belong to the same recurrence rule;
- match rule transaction type;
- match source account;
- match currency;
- not unexpectedly carry transfer linkage.

### Generated recurring transfer checks

Existing transfer payments must still form a complete transfer pair and match the rule's configured source/destination/currency relationships.

### State behavior

- repeated full payment is idempotent when the existing completed state already matches;
- incompatible mutation of a fully paid occurrence is rejected;
- a skipped occurrence must be reopened before payment/postponement;
- full-paid occurrence cannot be skipped/postponed;
- skipped occurrence has an explicit Reopen transition;
- payment links are not silently recreated around an inconsistent generated row.

### Tests

State-transition tests cover skip → reopen → paid, repeated full-payment idempotency, generated-link checks, and account availability.

---

## 9. Recurring-rule lifecycle completed

The recurrence contract/service/UI now exposes rule lifecycle rather than only occurrence lifecycle.

### Application contract

Added:

- `PauseRuleAsync`;
- `ResumeRuleAsync`;
- `ArchiveRuleAsync`.

### Pause

- Active → Paused;
- Paused is idempotent;
- Completed/Archived cannot be paused;
- audit metadata recorded;
- paused rules do not generate new occurrences.

### Resume

Only Paused rules resume.

Before activation, Resume revalidates:

- domain rule validity;
- configured end date has not already passed;
- source account exists/is available;
- destination account exists/is available where required;
- account currencies match the rule;
- referenced category remains active.

### Archive

- transitions rule to Archived;
- preserves occurrence history;
- archived rule disappears from active rule listing;
- repeated archive is idempotent;
- archived rule cannot be resumed.

### UI

Recurring page now includes:

- selected-rule picker;
- selected rule status/type/frequency/amount/next-due summary;
- Pause button;
- Resume button;
- Archive button;
- state-sensitive enablement;
- accessibility descriptions;
- existing skipped-occurrence Reopen button.

### Reminder integration

Create/process/pause/resume/archive paths synchronize reminders when notifications are enabled.

### Tests

Added recurring-rule lifecycle integration tests for:

- pause prevents due generation;
- resume restores generation;
- archive hides active rule while keeping occurrence history;
- resume fails after source account becomes archived;
- resume fails after rule end date;
- completed/archived rules cannot resume.

Representative commits:

- `feat(recurring): add rule pause resume and archive contracts`
- `feat(recurring): implement pause resume and archive rule lifecycle`
- `feat(recurring): add pause resume archive lifecycle commands to viewmodel`
- `fix(recurring): restore occurrence binding and expose rule lifecycle state`
- `feat(recurring): add accessible pause resume and archive controls`
- `test(recurring): cover pause resume archive and due-generation lifecycle`.

---

## 10. Reminder synchronization and privacy

Reminder synchronization now treats native schedules as derived state, not append-only state.

### Backup reminder

If backup reminders are disabled, the stale `backup:weekly` schedule is cancelled.

### Budget reminders

- threshold arithmetic avoids multiplying a potentially large minor-unit amount before division;
- threshold calculation uses checked/overflow-safe decomposition;
- notification content is generic/privacy-safe;
- inactive/stale `budget:` dedupe keys are cancelled.

### Recurrence reminders

- only Active rules with a future trigger are scheduled;
- Paused/Completed/Archived rules cancel stale dedupe schedules;
- stale recurrence keys no longer present in the active rule set are removed;
- notification content stays generic and does not contain private merchant/amount/note data.

Representative commit:

- `fix(reminders): synchronize recurring lifecycle and stale local schedules`.

---

## 11. Category mutation safety

Category archive/merge workflows were hardened around all dependent finance records.

### Reassignment protections

When a `BudgetKind.Subcategory` budget uses the source category, archive/merge cannot reassign that budget to a root category and silently invalidate budget semantics.

### Existing protections retained

Category archive/merge continues to handle:

- transactions;
- transaction splits;
- budgets;
- recurrence rules;
- children;
- cycle prevention;
- active/archived state.

### Tests

Added mutation-safety tests for subcategory-budget reassignment/merge and corrected a `Result<Guid>.Value` access mistake caught during the compile-readiness review.

Representative commits:

- category reassignment safety changes;
- `test(categories): fix Result Guid access in mutation safety suite`.

---

## 12. Tag reporting now has explicit currency scope

The prior tag-report contract returned money totals without a currency dimension and could therefore combine unlike currencies for the same tag.

### Contract change

`TagSpendSummary` now includes `Currency`.

`GetTagReportAsync` now requires an explicit currency argument.

### Infrastructure change

Tag report queries now:

- normalize/validate requested currency;
- filter linked transactions to that currency;
- ignore transfer rows for income/expense aggregation;
- use checked arithmetic;
- reject `long.MinValue` stored amounts.

### Tests

Added regression coverage showing the same tag on INR and USD transactions returns separate currency-scoped totals rather than adding raw minor units together.

Representative commits:

- `fix(reports): add explicit currency scope to tag reporting contract`
- `fix(reports): filter tag totals by explicit reporting currency`
- `test(reports): cover explicit currency isolation in tag reports`.

---

## 13. Currency-aware money/import/reporting hardening retained and extended

Earlier 0.2.0 hardening introduced `CurrencyMinorUnits` and currency-aware `Money` helpers.

This continuation audited all aggregate/report paths against that model.

Current behavior includes:

- 0-decimal known currencies such as JPY-style data;
- 2-decimal default/common currencies;
- 3-decimal known currencies such as KWD-style data;
- decimal-safe conversion;
- no binary floating-point persistence;
- checked integer minor-unit aggregation;
- no implicit exchange-rate conversion.

CSV import uses the row currency's minor-unit precision for major-unit input.

---

## 14. CSV import correctness retained

The importer now/continues to cover:

- explicit mapping;
- UTF-8/file-size/row-count validation;
- quoted CSV fields;
- currency-aware major-unit conversion;
- exact minor-unit import;
- account resolution/fallback;
- optional category creation;
- tag linking;
- duplicate detection;
- duplicate detection within the same import batch;
- transfer group and counterparty validation;
- `long.MinValue` rejection before sign normalization;
- transaction/account currency validation;
- parse errors counted exactly once;
- transactional persistence.

Currency-aware import tests include 0-/3-decimal currency cases and malformed extreme values.

---

## 15. Split-aware reports and recursive category budgets

Category spending and budget reporting were previously hardened so splits are the allocation source when present.

This continuation preserved those semantics while adding shared budget-window policy.

Current reporting rules:

- transaction with splits: use split amounts by split category;
- no split: use parent transaction category;
- do not count both parent and splits;
- category-budget descendant resolution is recursive, not only one child level;
- all monetary aggregation is checked;
- `long.MinValue` is rejected instead of passed through absolute-value arithmetic.

---

## 16. Dashboard mixed-currency regression removed

The Dashboard had already introduced currency-safe calculations, but the audit found it still called the legacy `IFinanceStore.GetDashboardAsync` first. That legacy API intentionally fails closed when multiple currencies exist, so merely having another-currency account could break the page before the safe calculations ran.

### Fix

Dashboard no longer calls the legacy mixed-currency aggregate API.

Aggregate cards now derive from:

- currency-scoped income/expense report;
- currency-scoped category report;
- direct account summaries filtered to reporting currency;
- budget performance filtered to reporting currency;
- currency-scoped monthly comparison.

Other-currency rows continue to display their own currency.

`CurrencyScope` explicitly tells the user when other-currency accounts are excluded from aggregate cards because Finora does not invent exchange rates.

### Performance follow-up

Dashboard balance calculation uses direct account summaries rather than loading all-history balance trend series merely to obtain the latest balance.

### UI contract

UI source-contract tests now explicitly fail if Dashboard regresses to a `GetDashboardAsync(` call.

Representative commits:

- `fix(dashboard): remove legacy mixed-currency aggregate dependency`
- `perf(dashboard): use direct account balances instead of all-history trend queries`
- `test(ui): lock recurring lifecycle and currency-safe dashboard contracts`.

---

## 17. Custom budget-period policy centralized

Added `src/Finora.Domain/BudgetPeriodPolicy.cs`.

### Shared window semantics

- explicit periods take precedence;
- weekly generated windows are Monday through Sunday;
- monthly generated windows are calendar months;
- custom cadence has **no fabricated fallback window**;
- custom budget is active only when the selected date falls inside an explicit period;
- rollover contributes only if `RolloverEnabled`;
- checked effective planned amount must remain positive.

### Overlap prevention

`DomainRules.ValidateBudget` rejects overlapping explicit periods.

Inclusive date boundaries mean one period starting on the previous period's end date is considered an overlap.

### Store behavior

`GetBudgetsAsync` uses the shared policy.

Custom budgets outside the selected date are omitted rather than reported as a synthetic one-day budget.

`SaveBudgetAsync` requires at least one explicit period for Custom cadence.

### Report behavior

`AdvancedReportService.GetBudgetPerformanceAsync` uses the same shared policy, removing duplicated period interpretation.

### Backup/integrity behavior

Backup graph validation and on-device integrity diagnostics validate custom-period presence/overlap.

### Tests

Added:

- explicit overlap rejection;
- custom in-window/out-of-window resolution;
- rollover enabled/disabled behavior;
- Monday–Sunday weekly window;
- non-positive effective rollover rejection;
- effective-plan overflow rejection;
- custom budget persistence;
- out-of-window store/report omission;
- explicit-period replacement coverage;
- failure-path rollback regression coverage.

Representative commits:

- `fix(budgets): reject overlapping explicit budget periods`
- `feat(budgets): centralize explicit weekly monthly and custom period resolution`
- `fix(budgets): reject non-positive effective rollover plans`
- `fix(reports): share custom budget period policy with finance store`
- `test(budgets): cover explicit overlap rollover and custom period activation`
- `test(budgets): cover custom period persistence and inactive-window behavior`
- `test(budgets): prove failed explicit-period replacement rolls back prior periods`
- `test(budgets): cover effective-plan rollover boundaries`.

---

## 18. FinanceStore relationship hardening

The FinanceStore path now/continues to enforce:

- account currency immutability after transactions/recurrence reference it;
- account archival blocked by active recurrence;
- transaction category validity;
- every split category must exist and be active;
- transaction/account currency match;
- transfers through paired atomic transfer workflow;
- savings linked transaction exists/is nondeleted/currency matches goal;
- recurrence source/destination accounts exist and match currency;
- recurrence category exists/is active;
- custom budget explicit-period requirements;
- shared budget-window resolution;
- checked sums and overflow protection;
- bounded recurrence backlog generation.

---

## 19. Backup graph validation expanded

Cryptographic authentication is necessary but not sufficient for a financial restore. A backup can be correctly encrypted and still contain semantically invalid data produced by an old defect, external manipulation before backup creation, or future compatibility mistake.

`BackupGraphValidator` validates the supported schema-v2 graph before encryption and after authenticated decryption.

### IDs/settings

- unique/non-empty entity IDs;
- unique setting keys;
- reject snapshot-provided `schema.version`;
- reject internal restore-marker settings.

### Accounts/transactions

- account domain validation;
- transaction domain validation;
- transaction account exists;
- transaction currency matches account;
- category/recurrence links exist;
- transfer pairs are complete/balanced/reciprocal/same currency/deletion-consistent.

### Splits/tags/categories

- split parent/category exists;
- split sign/total valid;
- transfer cannot have splits;
- transaction-tag links reference existing rows and are unique;
- category parent exists;
- category hierarchy is acyclic.

### Budgets

- budget domain rules;
- category relationship valid;
- Subcategory budget targets a child category;
- period parent exists;
- duplicate/overlapping periods rejected;
- Custom budget requires explicit period.

### Goals

- savings goal domain state;
- contribution domain state;
- goal parent exists;
- linked transaction exists and matches goal currency;
- chronological running progress never falls below zero.

### Recurrence

- recurrence domain state;
- source/destination account exists and currency matches;
- Active rule cannot point to archived account;
- category exists/is active;
- occurrence uniqueness;
- due/postpone relationship;
- generated transaction exists and belongs to rule;
- Paid/PartiallyPaid states require valid payment values/generated transaction;
- Pending/Skipped/Postponed must not carry hidden payment/generated state.

Historical Paused/Completed/Archived recurrence is allowed to retain an account link after that account is later archived; Active recurrence is not.

### Reconciliation

- account exists;
- timestamps valid;
- checked `statement - book == difference`;
- adjustment flag/link consistency;
- adjustment transaction is the correct account/type/amount.

### Attachments/notifications

- attachment parent/size/path metadata;
- notification metadata shape.

### Tests

Added backup graph validation tests for currency drift, split drift, active recurrence on archived account, and paused-history compatibility.

Representative commits:

- backup graph validator additions;
- `test(backup): cover financial graph validation before encrypted snapshot creation`
- `fix(backup): validate custom budget periods and overlap in snapshot graph`.

---

## 20. Backup sensitive-buffer cleanup

Backup creation now places receipt buffers and serialized plaintext under cleanup logic intended to clear them even when validation/encryption/metadata persistence fails rather than only after the successful validation path.

Receipt bytes read before a later attachment failure are also included in cleanup handling.

Encrypted input buffers and decrypted plaintext are cleared after decrypt/deserialize processing as far as managed-memory APIs permit.

Representative commit:

- `fix(backup): clear receipt buffers on every backup creation failure path`.

Managed-runtime memory cannot provide the same zeroization guarantees as dedicated unmanaged locked memory, so this is documented as best-effort cleanup rather than a false absolute guarantee.

---

## 21. Crash-safe restore retained and integrated with graph validation

The crash-safe restore design from the previous continuation remains:

- serialized backup/restore operation gate;
- pre-restore receipt-directory snapshot;
- durable restore journal;
- pending DB marker;
- underlying authenticated/validated restore;
- marker removal after successful DB commit;
- startup recovery before finance UI;
- pre-commit failure restores old receipt tree;
- post-commit state finalizes new receipt tree;
- stale restore/rollback directory cleanup after recovery decision.

Expanded graph validation occurs before destructive replacement.

---

## 22. Data-integrity checker expanded to aggregate finance state

`DataIntegrityService` now checks substantially more than SQLite/transactions/attachments.

### Existing checks retained

- SQLite `integrity_check`;
- foreign-key check;
- transaction amount/sign/currency/transfer-link state;
- transaction/account currency references;
- transfer pairing;
- split signs/totals;
- category cycles;
- attachment path/presence/size/SHA-256.

### New budget checks

- budget domain state;
- custom budget requires explicit period;
- overlapping periods rejected;
- category exists/is active;
- Subcategory budget points to child category.

Privacy-safe codes include:

- `BUDGET_INVALID`;
- `BUDGET_CATEGORY_INVALID`.

### New savings checks

- goal domain state;
- contribution domain state;
- checked running progress;
- linked transaction exists/is not deleted;
- linked transaction currency matches goal.

Codes include:

- `SAVINGS_GOAL_INVALID`;
- `GOAL_CONTRIBUTION_INVALID`.

### New recurrence checks

- rule domain state;
- source/destination account relationship/currency;
- Active rule cannot depend on archived account;
- category availability;
- duplicate occurrence;
- due/postpone relation;
- generated transaction belongs to rule;
- paid/partial/unpaid state consistency.

Codes include:

- `RECURRENCE_RULE_INVALID`;
- `RECURRENCE_RELATION_INVALID`;
- `RECURRENCE_DUPLICATE`;
- `RECURRENCE_STATE_INVALID`.

### New reconciliation checks

- reconciliation account exists;
- timestamps valid;
- checked difference arithmetic;
- adjustment flag/link/type/account/amount consistency.

Code:

- `RECONCILIATION_INVALID`.

### Tests

Added aggregate integrity regression tests injecting synthetic corruption for:

- custom budget missing period;
- linked goal transaction currency drift;
- Active recurrence on archived account;
- reconciliation difference drift;
- Paid occurrence missing generated transaction.

Representative commit:

- `feat(integrity): validate budgets goals recurrence and reconciliation relations`
- `test(integrity): detect budget goal recurrence and reconciliation corruption`.

---

## 23. PIN/app-lock hardening retained

The previous hardening remains in source:

- 4–12 digit PIN;
- random salt;
- PBKDF2-SHA256 verifier;
- fixed-time compare;
- OS secure storage;
- persistent PIN-enabled marker;
- missing/corrupt verifier fails closed;
- bounded escalating lockout policy;
- shared tested `PinAttemptPolicy`;
- biometric/Windows Hello requires PIN fallback;
- PIN removal clears biometric preference;
- inactivity lock.

Apple platform manifests include a Face ID purpose string.

---

## 24. Adaptive navigation and locale work retained

### Navigation

- mobile bottom-tab hierarchy;
- tablet/desktop flyout hierarchy;
- adaptive root switching;
- primary-section preservation across mode changes;
- onboarding/unlock routes use adaptive destination.

### Accessibility/UI scaling

- global scalable control height/font resources;
- larger-interface setting;
- reduced-motion setting;
- report text equivalents;
- recurring lifecycle accessibility descriptions.

### Locale

- saved locale normalized/validated;
- runtime process/current/default thread culture application;
- safe fallback;
- Settings number/date formatting preview;
- onboarding locale application.

---

## 25. Data reset/sample data retained

### Full finance reset

A dedicated transactional finance reset service clears all supported finance/schema-v2 data, including user-created categories, while preserving schema metadata/preferences needed for a valid app profile.

Settings uses typed destructive confirmation and cleans receipt orphans only after database deletion succeeds.

### Developer sample reset

A separate hidden developer flow uses typed confirmation and deterministic synthetic data.

It resets finance data then seeds a coherent demo dataset for development/test use.

---

## 26. Platform source audit

This continuation re-read the declared platform TFMs/minimums and key native implementation source.

### Declared targets

- `net10.0-android`;
- `net10.0-ios`;
- `net10.0-maccatalyst`;
- `net10.0-windows10.0.19041.0`.

### Platform source audited

- Android notification scheduling/channel/runtime permission path;
- Android biometric API guard/imports;
- Android secure-window capture protection;
- Apple LocalAuthentication/UserNotifications;
- Apple Face ID purpose text;
- Windows Hello;
- Windows scheduled toast;
- Windows display-affinity capture protection;
- package version metadata;
- MAUI picker/share/page-handler source.

Source review does not replace native compilation/device validation.

---

## 27. Test additions/changes in this continuation

New/expanded test areas include:

### Path/storage

- attachment/path confinement behavior.

### Domain/persistence

- transaction signs/extreme values;
- split total/sign/category state;
- account/currency relations;
- direct EF persistence rejection.

### Accounts/reconciliation

- active recurrence blocks archival;
- paused recurrence permits archival;
- state-picker archive path;
- reconciliation overflow/date;
- reconciled opening balance.

### Recurrence

- generated payment-link safety;
- skip/reopen/payment transitions;
- repeated full-payment idempotency;
- Pause → no generation;
- Resume → generation;
- Archive history preservation;
- resume blocked by archived account;
- resume blocked by expired end date;
- completed/archived resume rejection.

### Categories/tags

- subcategory-budget mutation safety;
- tag range/extreme amount;
- explicit INR/USD tag-report isolation.

### Budgets

- explicit period overlap;
- custom active/inactive periods;
- rollover enable/disable;
- weekly window;
- custom persistence;
- report/store out-of-window omission;
- explicit period replacement;
- failed replacement rollback;
- non-positive/overflow effective plan.

### Reports/dashboard

- split-aware category spending;
- recursive category budget;
- range boundaries;
- currency isolation;
- Dashboard source contract forbids legacy mixed-currency aggregate call.

### Import

- currency-specific major-unit conversion;
- `long.MinValue` handling;
- error counting;
- duplicate/counterparty behavior.

### Backup/recovery

- graph validation currency/split/recurrence cases;
- paused historical rule compatibility;
- attachment encrypted roundtrip;
- restore journal pre-/post-commit recovery.

### Integrity

- custom-budget corruption;
- goal link currency drift;
- active recurrence/account drift;
- reconciliation drift;
- impossible occurrence payment state.

### UI contracts

- adaptive navigation;
- destructive confirmations;
- locale preview;
- recurrence Reopen;
- recurrence Pause/Resume/Archive;
- currency-safe Dashboard source;
- scalable UI resources.

---

## 28. Documentation aligned in this continuation

Updated documentation includes:

- `DECISIONS.md`;
- `docs/architecture/DATABASE_SCHEMA.md`;
- `docs/security/THREAT_MODEL.md`;
- `docs/TEST_PLAN.md`;
- `docs/releases/RELEASE_CHECKLIST.md`;
- `docs/releases/STORE_READINESS.md`;
- `docs/privacy/DATA_LIFECYCLE.md`;
- `docs/setup/TROUBLESHOOTING.md`;
- `PROJECT_STATUS.md`;
- `CHANGELOG.md`;
- `README.md`;
- this `what_changed.md`.

These documents now explicitly cover:

- currency-scoped aggregation;
- no implicit exchange rates;
- custom budget period policy;
- recurrence rule lifecycle;
- stale reminder cleanup;
- account/recurrence dependencies;
- full backup graph validation;
- crash-safe restore recovery;
- expanded integrity diagnostics;
- platform-correct path confinement;
- fail-closed PIN behavior;
- compiler/device/store validation boundaries.

---

## 29. Repository quality/CI state

The repository contains:

- dependency-free structural preflight;
- project/XML/XAML/RESX checks;
- project-reference checks;
- placeholder/unfinished marker checks;
- XAML event-handler checks;
- version/package/schema consistency checks;
- money-representation guard checks;
- selected Android privacy/platform checks;
- unit/integration/UI-contract projects;
- GitHub Actions structural/core/MAUI jobs;
- CodeQL;
- dependency review;
- Dependabot;
- CODEOWNERS;
- release/test/security documentation.

The CI topology was previously adjusted so core Ubuntu jobs do not require a restored MAUI solution merely for a formatting gate, while MAUI builds remain on their appropriate Windows/macOS hosts.

---

## 30. Validation that is NOT claimed complete

The ChatGPT execution environment used for this work does not provide a usable local `dotnet` SDK/toolchain.

Therefore this continuation does **not** claim local success for:

- `dotnet restore`;
- C# compiler/analyzer execution;
- `dotnet test`;
- MAUI workload restore;
- Android Release build/package;
- Windows Release package;
- iOS build/archive;
- Mac Catalyst build/archive;
- emulator/simulator testing;
- physical-device testing;
- signing/notarization;
- Play/App/Microsoft store validation.

Source/tests/docs have been expanded specifically so those external gates have concrete checks to run once a matching toolchain is available.

Do not represent Finora 0.2.0 as a production store release until those gates have passing evidence.

No claim is made that Finora is bug-free.

---

## 31. Current release boundaries still intentionally deferred

The following remain later-version work rather than hidden/incomplete current-release features:

- cloud synchronization;
- remote user account/login;
- collaboration/shared remote finance data;
- mobile-number authentication;
- server-backed entitlement/licensing;
- remote key escrow/recovery;
- automatic exchange-rate conversion.

Any future implementation requires corresponding architecture, threat-model, privacy, retention/deletion, migration, authentication, and server-security work.

---

## 32. Product identity

- Product: **Finora**
- Attribution: **Made by the Sanskar**
- Repository: https://github.com/sanskarIN/Finora
- Creator/open-source profile: https://www.github.com/sanskarIN
- Business/security email: `sanskarin@outlook.in`
- Support email: `supportramsandesh@gmail.com`
- License: Apache-2.0

---

## 33. Final continuation state

The 2026-08-10 continuation substantially hardened Finora's current local-first source around financial graph correctness, recurrence lifecycle, budget periods, mixed-currency reporting, backup/restore validation, storage path safety, integrity diagnostics, notification cleanup, account dependencies, and regression coverage.

The next objective is not to invent cloud/login/server functionality inside this release. The next required release work is to run the repository's compiler/tests/MAUI builds and native-device/store matrices on the appropriate toolchains, fix any failures they reveal, and only then prepare signed production artifacts.
