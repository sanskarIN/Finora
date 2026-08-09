# What Changed — Finora

Last implementation/status refresh: **2026-08-09**  
Repository: https://github.com/sanskarIN/Finora  
Development branch: `main`  
Current source version: **0.2.0 (build 2)**  
Current database schema: **2**

This file is intentionally detailed because implementation/status information that would otherwise consume the chat is recorded here instead.

---

## 1. Source specification used

The uploaded `01_Finora_Personal_Finance_Master_Prompt.md` remains the implementation specification for this project.

The project identity and boundaries preserved throughout the work are:

- Product: **Finora**
- Framework: .NET MAUI
- Primary language: C#
- UI: XAML/.NET MAUI
- Persistence: SQLite through EF Core
- Architecture: multi-project, MVVM-oriented, dependency-injected services
- Source model: open source
- License: Apache-2.0
- Repository: https://github.com/sanskarIN/Finora
- Creator profile: https://www.github.com/sanskarIN
- Business/security email: `sanskarin@outlook.in`
- Support email: `supportramsandesh@gmail.com`
- Attribution: **Made by the Sanskar**
- Current release model: local-first, no mandatory account/login/cloud service

The implementation did not silently change Finora into a cloud-required finance application and did not add a mandatory analytics/advertising/account dependency.

---

## 2. Delivery shape

The repository is organized around the requested separation of concerns:

```text
Finora.sln

src/
  Finora.App/
  Finora.Application/
  Finora.Domain/
  Finora.Infrastructure/
  Finora.Shared/

tests/
  Finora.UnitTests/
  Finora.IntegrationTests/
  Finora.UiTests/

docs/
  architecture/
  branding/
  privacy/
  releases/
  security/
  setup/

build/
  scripts/

.github/
  ISSUE_TEMPLATE/
  workflows/
```

The implementation has been delivered in many focused commits rather than a single monolithic commit so feature history remains traceable.

---

## 3. Product implementation completed in source

### 3.1 Local-first onboarding and privacy

Implemented source includes:

- no mandatory email/phone/login/account creation;
- no required internet connection for core finance recording;
- default currency selection;
- locale preference;
- financial-month start day;
- optional opening balance;
- explicit sample-data opt-in;
- privacy explanation and full privacy-document path;
- onboarding revisit from Settings;
- explicit uninstall/backup warning;
- manual-only transaction-location model;
- no background location collection in the current source.

Packaged app privacy/terms resources were later expanded so the installed-app legal summary matches the repository documentation rather than describing an older, smaller feature set.

### 3.2 Accounts

Implemented source includes:

- Cash;
- Bank account;
- Credit card;
- Digital wallet;
- Savings account;
- Investment-placeholder account;
- Custom account type;
- name;
- icon;
- optional color label;
- currency;
- opening balance;
- calculated current balance;
- active/hidden/archived states;
- optional credit limit;
- optional billing day;
- account-specific transaction history;
- account detail/edit surface;
- archive/restore workflow;
- default-account preference.

Ordinary account lifecycle does not silently delete transaction history.

### 3.3 Transfers

Same-currency transfers use a linked double-entry-style representation:

- two finance-transaction rows;
- shared `TransferGroupId`;
- equal magnitude;
- opposite signs;
- same currency;
- reciprocal counterparty account identifiers;
- transactional creation;
- paired edit/delete/restore behavior.

Cross-currency movement is intentionally not faked as a same-currency pair. It requires a future explicit exchange workflow if added.

### 3.4 Account reconciliation

Schema-v2 reconciliation source includes:

- statement date;
- book balance;
- statement balance;
- difference;
- optional note;
- explicit adjustment choice;
- generated adjustment transaction when requested;
- persisted reconciliation history;
- account last-reconciled metadata;
- reconciliation UI/preview/history.

An unresolved difference is not silently accepted as balanced.

### 3.5 Transactions

Implemented transaction types:

- Expense;
- Income;
- Transfer;
- Refund;
- Adjustment.

Implemented transaction fields/workflows include:

- signed integer minor-unit amount;
- account;
- category/subcategory;
- date/time;
- merchant/payee;
- note;
- tags;
- payment method;
- manually entered location;
- recurrence linkage;
- transfer linkage;
- receipt/document attachments;
- soft-delete/restore state.

Implemented transaction UX includes:

- quick-add;
- decimal-safe calculator keypad;
- edit/detail surface;
- advanced free-text/account/category/type/date filtering;
- split editor;
- tags;
- revision history;
- receipt lifecycle;
- bulk categorization;
- duplicate review;
- selected/all CSV/PDF export paths;
- linked-transfer-safe edits/deletes/restores.

### 3.6 Decimal-only money/calculator handling

Money is persisted/calculated as signed 64-bit integer minor units with currency code.

User-entered major-unit conversion uses `decimal`. A decimal expression parser/calculator supports parentheses and `+`, `-`, `*`, `/` with precedence and rejects invalid expressions/divide-by-zero.

Binary floating-point values are not used for stored/calculated monetary amounts. Non-money UI ratios can still use floating-point representation where appropriate.

### 3.7 Transaction revision history

Schema v2 adds `TransactionRevision` records.

Critical pre-change state is recorded before operations such as edits, bulk category changes, linked-transfer edits, delete, and restore. User-facing history uses a sanitized summary path; raw private revision snapshot data is not sent through diagnostic logging.

### 3.8 Categories, subcategories, and tags

Implemented category/tag management includes:

- default categories;
- custom categories;
- custom subcategories;
- icons;
- ordering;
- archive/restore;
- parent assignment;
- parent-cycle prevention;
- safe reassignment;
- merge into another category;
- child reparenting as required by workflow;
- tag create/update/archive/restore;
- usage/report linkage.

### 3.9 Receipt and attachment lifecycle

`AttachmentService` implements app-private receipt/document storage with:

- supported image/PDF MIME allow-list;
- maximum per-file size;
- user filename sanitization;
- generated internal filename;
- safe-path confinement under app data;
- asynchronous file copy;
- byte-count metadata;
- SHA-256 checksum;
- attachment DB metadata;
- list/open/delete;
- storage usage calculation;
- orphan-file cleanup;
- audit events without receipt contents.

The transaction detail surface can select, store, open, and explicitly delete local receipts/documents.

### 3.10 Budgets

Implemented budget source includes:

- overall budget;
- category budget;
- subcategory budget;
- weekly cadence;
- monthly cadence;
- custom period;
- rollover option;
- warning threshold;
- explicit budget periods;
- planned/actual/variance calculations;
- category-descendant handling;
- split-transaction accounting;
- budget UI;
- local reminder coordination when notifications are enabled and authorized.

### 3.11 Savings goals

Implemented savings-goal source includes:

- name;
- icon;
- target amount;
- starting amount;
- currency;
- target date;
- notes;
- deposits;
- withdrawals;
- optional linked account transaction;
- progress;
- forecast;
- milestone messaging;
- completion state;
- reduced-motion-aware completion behavior.

### 3.12 Recurring items

Implemented recurrence setup includes:

- recurring expense;
- recurring income;
- recurring transfer;
- recurring refund;
- daily;
- weekly;
- monthly;
- yearly;
- custom intervals;
- start date;
- optional end date;
- grace period;
- reminder lead time;
- account;
- destination account for transfer;
- category;
- amount;
- merchant/payee;
- note.

Recurrence processing is occurrence-first and idempotent:

- a persisted `RecurrenceOccurrence` represents the due obligation;
- `(RecurrenceRuleId, DueOn)` is unique;
- repeated processing does not create duplicate occurrences;
- the finance transaction is created only through paid/partial-paid workflow;
- paid, partially paid, skipped, and postponed states are implemented;
- recurring transfers create a balanced linked pair.

This replaced the earlier simpler behavior where recurrence processing could immediately generate a finance transaction.

### 3.13 Dashboard

Implemented configurable dashboard data includes:

- current total balance;
- selected-period income;
- selected-period spending;
- net change;
- remaining budget;
- upcoming recurring obligations;
- top categories;
- savings-goal progress;
- recent transactions;
- cash-flow summaries;
- financial-month-start handling;
- configurable card visibility;
- privacy mode that hides displayed amounts.

### 3.14 Reports

Implemented report datasets/surfaces include:

- category spending;
- income versus expense;
- account balance trend;
- cash-flow-oriented/monthly comparison data;
- budget performance;
- merchant/payee reporting;
- tag reporting;
- recurring obligation data through recurring workflow/dashboard;
- savings progress.

A dependency-free MAUI `GraphicsView` chart surface was added with textual/tabular equivalents so charts are not the only representation of financial meaning.

### 3.15 CSV import

Implemented CSV import includes:

- system file picker;
- header detection;
- explicit user mapping;
- mapping preview;
- Date/Type/Amount/Account required mapping;
- optional Currency/Category/Merchant/Note/Payment Method/Location/Transfer Group/Counterparty/Tags mapping;
- major-unit versus minor-unit choice;
- decimal-safe amount conversion;
- UTF-8 validation;
- maximum file size;
- maximum row count;
- quoted/escaped CSV parsing;
- date/type/currency/amount validation;
- account resolution/fallback account;
- optional missing-category creation;
- tag linking;
- duplicate protection;
- transfer-group pair validation;
- transactional commit;
- explicit error reporting.

### 3.16 CSV/PDF export

CSV export was expanded to include useful transaction fields and linkage metadata, including transaction ID, date/type/amount/currency, account/category, merchant/payee, note, payment method, manual location, transfer group, counterparty account, and tags.

PDF export supports multi-page transaction reporting through a dependency-free implementation behind `IExportService`.

Exports are generated locally and handed to the system share/save surface only after explicit user action.

### 3.17 Encrypted backup creation

Current backup creation:

1. validates backup password requirements;
2. reads schema-v2 supported local data;
3. validates every receipt path;
4. verifies receipt existence;
5. verifies receipt byte count;
6. verifies SHA-256 metadata where available;
7. serializes finance data plus receipt bytes;
8. derives a key using PBKDF2-SHA256 with random salt and 210,000 iterations;
9. encrypts/authenticates with AES-GCM using a random nonce and authentication tag;
10. uses Finora backup magic as authenticated associated data;
11. zeroes derived key memory after use;
12. records privacy-safe backup metadata/audit state;
13. returns encrypted bytes for explicit system save/share.

There is no automatic backup upload path in the current source.

### 3.18 Encrypted backup restore

Current restore flow:

- rejects unreadable/oversized files;
- validates Finora backup magic/length;
- authenticates/decrypts with AES-GCM;
- validates schema;
- validates attachment metadata/blob count;
- validates receipt paths;
- validates receipt size/checksum;
- shows a backup preview before replacement;
- stages receipt files in a temporary private directory;
- replaces supported relational data inside a DB transaction;
- swaps attachment directories;
- includes rollback/cleanup handling if commit/swap fails;
- rejects wrong-password/tampered/incompatible backups rather than silently accepting them.

Finora cannot recover a forgotten backup password.

### 3.19 Local notifications

Schema v2 adds persisted local notification schedule records containing:

- kind;
- generic title/body;
- trigger time;
- optional dedupe key;
- enabled/delivered state.

Implemented platform source paths include:

- Android scheduling/notification code;
- iOS/Mac Catalyst UserNotifications code;
- Windows scheduled-toast code.

Reminder coordination covers backup reminders, budget thresholds, and recurring obligations. Permission is explicit where required. Notification text is intentionally generic because a notification can appear outside Finora's app lock.

### 3.20 App PIN lock

Implemented app-lock source includes:

- 4–12 digit PIN validation;
- random salt;
- PBKDF2-SHA256-based PIN verifier;
- fixed-time comparison;
- OS secure storage for small verifier/security values;
- failed-attempt counter;
- escalating local lockout;
- configurable inactivity auto-lock;
- set/change/remove PIN UI.

### 3.21 Biometrics and Windows Hello

Implemented platform adapters cover Android biometrics, Apple LocalAuthentication, and Windows Hello availability/authentication paths.

Biometric/Hello unlock requires a configured Finora PIN fallback. Cancellation, unavailability, or authentication failure does not intentionally bypass the lock.

### 3.22 Sensitive-screen protection

Implemented source uses platform-supported controls where available, including Android secure-window behavior and supported Windows display-affinity behavior.

Unsupported/partial platforms return/report a limitation instead of falsely claiming universal screenshot blocking.

### 3.23 Settings and developer options

Settings source now covers:

- theme;
- default currency;
- locale preference;
- financial month start;
- privacy mode;
- hide amounts on launch;
- reduced motion;
- larger interface;
- default account;
- default transaction type;
- notification preference;
- backup reminders;
- receipt image quality/storage controls;
- inactivity auto-lock;
- biometric preference;
- sensitive-screen preference;
- local premium demo state;
- configurable dashboard cards;
- last backup timestamp.

Settings actions include:

- request/sync local notifications;
- cleanup receipt files;
- create encrypted backup;
- preview/restore encrypted backup;
- export sanitized diagnostics;
- set/change/remove PIN;
- revisit onboarding;
- manage categories/tags;
- delete local finance data;
- open repository/profile;
- compose business/support email;
- privacy/terms/notices surfaces.

Hidden developer options behind repeated version taps include:

- schema version;
- feature flags;
- reminder sync;
- local premium demo flag;
- local data-integrity check.

### 3.24 Local premium state

The local premium flag is explicitly treated as a development/demo capability, not secure commercial entitlement.

The source/documentation does not pretend a local boolean is tamper-proof licensing. Future commercial entitlement requires a store/server-backed architecture.

### 3.25 Branding assets

Included branding source/guidance includes:

- primary SVG icon;
- Android adaptive foreground source;
- monochrome system icon source;
- light splash source;
- dark splash source;
- store/safe-zone guidance;
- wordmark guidance;
- attribution placement guidance.

The small launcher glyph intentionally does not contain tiny text.

### 3.26 Localization readiness

Included source includes:

- English baseline resource file;
- initial Hindi common-string resource file/structure;
- locale preference.

The app is currently English-first/localization-ready. Full screen-by-screen Hindi localization is **not** claimed as complete.

### 3.27 Accessibility source

Implemented architecture/preferences include:

- light/dark/system theme;
- reduced-motion preference;
- larger-interface preference;
- textual/tabular equivalents for financial charts;
- semantic/adaptive design work in MAUI surfaces;
- documented keyboard/screen-reader/large-text/contrast/reduced-motion release gates.

Native accessibility validation is still a platform/device release requirement.

---

## 4. Database schema work

### 4.1 Current schema version

`AppConstants.DatabaseSchemaVersion` is **2**.

### 4.2 Schema-v2 additions

Schema v2 includes/mapped:

- `TransactionRevision`;
- `AccountReconciliation`;
- `NotificationSchedule`;
- `Attachment.OriginalFileName`.

Indexes were added for revision history, reconciliation lookups, notification trigger/dedupe state, plus the existing finance indexes.

### 4.3 Transactional v1 → v2 migration

`DatabaseMigrationRunner`:

- reads `schema.version` from existing DB;
- rejects invalid/missing version metadata for an existing versioned DB path;
- rejects DBs newer than the build supports;
- executes one registered migration step at a time;
- runs v1 → v2 in a SQLite transaction;
- advances `schema.version` only after migration SQL and the settings update succeed;
- retains an explicit default failure for an unregistered future migration.

Migration integration coverage was added.

### 4.4 Database initialization

Initialization creates a clean schema when required, enables WAL/foreign keys/busy timeout, migrates existing supported DBs, and preserves/creates default categories.

---

## 5. New privacy-safe data-integrity diagnostics

A new `IDataIntegrityService` / `DataIntegrityService` was added in the latest continuation.

The checker verifies locally:

1. SQLite `PRAGMA integrity_check`;
2. SQLite `PRAGMA foreign_key_check`;
3. transaction account existence;
4. transaction/account currency consistency;
5. transfer pair count;
6. equal/opposite transfer amount relationship;
7. transfer same-currency relationship;
8. reciprocal counterparty account relationship;
9. linked transfer delete-state consistency;
10. transaction split totals;
11. category parent cycles;
12. duplicate recurrence occurrences;
13. recurrence references to generated transactions;
14. receipt path confinement;
15. receipt file existence;
16. receipt byte-size metadata;
17. receipt SHA-256 checksum metadata.

The exported integrity report contains health codes/counts only. It explicitly avoids account names, merchant/payee names, notes, transaction amounts, receipt filenames/contents, PINs, and backup passwords.

The check is exposed through hidden developer options and can optionally export a sanitized text report.

Integration tests were added for a healthy DB and a deliberately broken transfer pair.

---

## 6. Centralized exception/diagnostic hardening

The continuation added `AppExceptionCoordinator` and wired it through DI/App startup.

It observes:

- `AppDomain.CurrentDomain.UnhandledException`;
- `TaskScheduler.UnobservedTaskException`;
- startup initialization failures;
- activation/lifecycle failures.

Only privacy-safe event tokens and exception **types** are written through `IPrivacyLogger`. Exception messages and stack traces are not written to this local privacy log.

`PrivacyLogger` was also hardened:

- caller-supplied property dictionaries are intentionally not serialized;
- event tokens are restricted to safe characters/length;
- exception message/stack is not recorded;
- local diagnostic file is bounded;
- rotation keeps a previous bounded file;
- logger failures are best-effort and must not crash core finance workflows;
- sanitized log export remains explicit user action.

---

## 7. Repository/CI/security automation added in the continuation

### 7.1 Structural preflight

Added `build/scripts/verify_structure.py`.

Without requiring .NET, it checks repository text/source structure for:

- malformed XML/XAML/RESX/project files;
- empty source/config/resource files;
- unfinished placeholder markers;
- missing `ProjectReference` targets;
- XAML `x:Class` without matching partial class;
- selected XAML event-handler names without matching C# method;
- solution project entries pointing to missing projects.

The script explicitly states that it does **not** replace a compiler, analyzer, test runner, emulator/simulator, signing validation, or store validation.

### 7.2 PowerShell verification wrapper

`build/scripts/verify.ps1` now runs:

1. structural preflight;
2. `dotnet --info`;
3. `dotnet workload restore`;
4. NuGet restore;
5. format verification;
6. Release build;
7. Release tests with TRX output.

### 7.3 GitHub CI

`.github/workflows/ci.yml` now separates:

- structural preflight;
- core unit/integration/UI-contract tests;
- formatting verification;
- Windows + Android MAUI builds;
- iOS + Mac Catalyst MAUI builds;
- uploaded core TRX artifacts.

### 7.4 CodeQL

Added `.github/workflows/codeql.yml` for C# analysis with a manual MAUI/Android build path.

### 7.5 Dependency review

Added `.github/workflows/dependency-review.yml` for pull requests with high-severity dependency-change rejection configuration.

### 7.6 Dependabot

Added `.github/dependabot.yml` for weekly NuGet and GitHub Actions update proposals.

### 7.7 CODEOWNERS

Added `.github/CODEOWNERS` with default ownership plus explicit sensitive-area ownership for security/privacy/backup/app-lock/migration/platform/workflow files.

### 7.8 Privacy-aware repository templates

Expanded/added:

- bug report template requiring synthetic data and excluding security reports;
- feature request template with privacy check;
- issue config routing security/support away from blank public issues;
- pull-request template covering privacy/data impact, migration/backup compatibility, analysis/tests/platform QA, secrets, docs, and synthetic screenshots.

### 7.9 Repository private-artifact hygiene

`.gitignore` was expanded to exclude:

- build/test outputs;
- platform packages;
- certificate/signing/provisioning material;
- databases/WAL files;
- Finora encrypted backups;
- app-private attachments;
- diagnostic logs;
- integrity exports;
- generated finance CSV/PDF exports;
- local secret/config files.

A `.gitattributes` file was added for consistent cross-platform line endings and binary file classification.

---

## 8. Tests added/expanded

### 8.1 Unit

Current unit coverage includes money/domain behavior and decimal calculator behavior such as:

- precedence;
- parentheses;
- decimal arithmetic;
- division;
- negative values;
- divide-by-zero failure.

### 8.2 SQLite integration

Current integration coverage includes at least:

- linked transfer conservation;
- recurrence idempotency;
- no automatic finance transaction before recurring occurrence is paid;
- transaction revision creation;
- bulk-categorization revision creation;
- reconciliation explicit-adjustment behavior;
- user-selected CSV column mapping;
- decimal amount conversion;
- encrypted receipt backup round trip;
- v1 → v2 migration;
- privacy-safe data-integrity healthy path;
- broken transfer-pair detection.

### 8.3 UI contracts

The UI-contract project tracks key route/privacy/recovery/workflow contracts. It is not represented as real native-device UI automation.

### 8.4 Test-plan expansion

`docs/TEST_PLAN.md` now documents detailed structural, build, unit, integration, migration, integrity, UI-contract, Android, Windows, iOS, Mac Catalyst, reliability/failure-injection, privacy/security, and release-evidence requirements.

---

## 9. Documentation expanded/realigned

The continuation materially updated:

- `README.md`;
- `CHANGELOG.md`;
- `PROJECT_STATUS.md`;
- `DECISIONS.md`;
- `SECURITY.md`;
- `PRIVACY.md`;
- `SUPPORT.md`;
- `TERMS.md`;
- `CONTRIBUTING.md`;
- `CODE_OF_CONDUCT.md`;
- `.github/pull_request_template.md`;
- `.github/ISSUE_TEMPLATE/bug_report.yml`;
- `.github/ISSUE_TEMPLATE/feature_request.yml`;
- `.github/ISSUE_TEMPLATE/config.yml`;
- `docs/architecture/OVERVIEW.md`;
- `docs/architecture/DATABASE_SCHEMA.md`;
- `docs/privacy/DATA_LIFECYCLE.md`;
- `docs/security/THREAT_MODEL.md`;
- `docs/setup/BUILD.md`;
- `docs/setup/TROUBLESHOOTING.md`;
- `docs/TEST_PLAN.md`;
- `docs/releases/RELEASE_CHECKLIST.md`;
- `docs/releases/STORE_READINESS.md`;
- packaged `Resources/Raw/privacy.txt`;
- packaged `Resources/Raw/terms.txt`;
- this `what_changed.md`.

Documentation now consistently distinguishes implemented source from required native release validation.

---

## 10. Commit trail — main implementation expansion before this continuation

The feature implementation was intentionally divided into many focused commits. Important commit messages created during the previous expansion include:

### Application/domain contracts and primitives

- `feat(accounts): add account management contracts`
- `feat(attachments): add receipt storage contracts`
- `feat(categories): add category and tag management contracts`
- `feat(import): add mapped CSV import contracts`
- `feat(notifications): add local reminder contracts`
- `feat(accounts): add reconciliation contracts`
- `feat(recurring): add occurrence workflow contracts`
- `feat(reports): add advanced reporting contracts`
- `feat(security): add biometric and capture protection contracts`
- `feat(transactions): add maintenance and revision contracts`
- `feat(transactions): add decimal-only calculator engine`
- `feat(database): advance Finora schema version to 2`
- `feat(database): add schema v2 revision reconciliation and reminder entities`
- `feat(settings): expand application settings and service contracts`
- `fix(money): expose decimal-safe minor unit conversion`

### Database/infrastructure

- `feat(database): add transactional v1 to v2 migration`
- `feat(database): map schema v2 records and indexes`
- `feat(database): run schema migrations during initialization`
- `feat(accounts): implement account detail and lifecycle service`
- `feat(attachments): implement private receipt storage service`
- `feat(transactions): add privacy-safe revision snapshots`
- `feat(accounts): implement reconciliation workflow`
- `feat(categories): implement category and tag workflows`
- `feat(recurring): implement payment skip and postpone workflows`
- `feat(transactions): implement editing bulk categorization and duplicate review`
- `feat(reports): implement accessible financial report datasets`
- `feat(notifications): persist and deduplicate local reminders`
- `feat(import): implement mapped validated CSV import pipeline`
- `feat(finance): harden core store invariants budgets goals and recurrence`
- `feat(backup): include schema v2 data and receipt bytes in encrypted restore`
- `feat(export): expand CSV and multipage PDF transaction exports`

### MAUI/platform integration

- `feat(reminders): coordinate backup and budget notifications`
- `feat(security): add platform sensitive-screen protection`
- `feat(ui): add reusable inverse boolean converter`
- `feat(security): add biometric and Windows Hello authentication`
- `feat(notifications): add Android Apple and Windows local scheduling`
- `feat(app): wire schema v2 services security reminders and accessibility`
- `feat(settings): persist privacy security defaults and dashboard preferences`

### User-facing workflows

- `feat(transactions): add advanced filters calculator and complete quick add`
- `feat(transactions): complete detail receipts splits tags and review tools`
- `feat(accounts): complete account editing transfers and detail history`
- `feat(categories): complete category subcategory merge reorder and tag UI`
- `feat(accounts): complete reconciliation preview adjustment and history UI`
- `feat(budgets): complete overall category custom and reminder-aware budget UI`
- `feat(goals): complete savings forecasts milestones and linked contributions`
- `feat(recurring): complete recurrence setup due-item workflow and reminders`
- `feat(reports): add accessible charts merchant budgets and balance trends`
- `feat(dashboard): add configurable privacy-aware finance overview`
- `feat(settings): complete security privacy accessibility lock and onboarding UI`
- `feat(import): complete CSV mapping preview import and legal-document surfaces`
- `feat(ui): wire complete page navigation backup security and settings handlers`

### Assets/tests/legal

- `docs(legal): package privacy terms notices and branding variants`
- `test: expand finance migration recurrence import backup and UI contracts`

---

## 11. Commit trail — this continuation

The latest continuation also used many focused commits. Commit messages include:

### CI/repository automation

- `ci: add dependency-free repository structural preflight`
- `ci: run structural preflight before full verification`
- `ci: stage structural core and cross-platform MAUI quality gates`
- `chore(deps): add scheduled NuGet and Actions dependency updates`
- `chore(repo): add code ownership rules`
- `ci(security): add CodeQL analysis workflow`
- `ci(security): add pull request dependency review`

### Integrity diagnostics

- `feat(diagnostics): add privacy-safe data integrity contracts`
- `feat(diagnostics): implement SQLite finance and receipt integrity checks`
- `refactor(diagnostics): make integrity checks strongly typed`
- `feat(diagnostics): register local data integrity service`
- `feat(diagnostics): expose local integrity check in developer options`
- `feat(diagnostics): add sanitized integrity report workflow`
- `test(diagnostics): cover healthy database and broken transfer detection`

### Repository/community/privacy gates

- `chore(repo): harden privacy-aware feature request template`
- `chore(repo): expand privacy-safe bug report template`
- `chore(repo): route security reports away from public issues`
- `chore(repo): strengthen pull request quality gate`
- `chore(security): expand private artifact and signing secret exclusions`
- `chore(repo): normalize text and platform line endings`

### Release/build/test/security documentation

- `docs(release): add cross-platform store readiness matrix`
- `docs(build): document schema-v2 verification and platform build flow`
- `docs(build): expand Finora troubleshooting guide`
- `docs(test): expand release-candidate test matrix`
- `docs(release): expand final Finora release checklist`
- `docs(security): expand Finora local-first threat model`
- `docs(architecture): document Finora schema-v2 service boundaries`
- `docs(database): document Finora schema version 2 and migration invariants`
- `docs(architecture): record current Finora engineering decisions`

### Reliability/diagnostics hardening

- `feat(reliability): add centralized privacy-safe exception coordinator`
- `feat(reliability): register centralized exception coordinator`
- `feat(reliability): centralize startup and lifecycle exception reporting`
- `feat(diagnostics): bound and harden privacy-safe local logging`

### Security/privacy/legal/community documentation

- `docs(security): expand private vulnerability reporting policy`
- `docs(privacy): document local-first data lifecycle and permissions`
- `docs(support): expand privacy-safe Finora support guidance`
- `docs(contributing): add complete engineering contribution workflow`
- `docs(legal): expand Finora terms and financial disclaimer`
- `docs(app): align packaged privacy summary with local-first implementation`
- `docs(app): align packaged terms summary with current Finora behavior`
- `docs(privacy): document complete Finora local data lifecycle`
- `docs(community): expand Finora contributor code of conduct`
- `docs(community): correct conduct contact formatting`

### Public repository/status documentation

- `docs(readme): document complete Finora 0.2.0 source capabilities`
- `docs(changelog): record complete Finora 0.2.0 development changes`
- `docs(status): align project status with Finora 0.2.0 schema-v2 source`
- `docs(status): update complete Finora continuation change ledger` (this file)

---

## 12. Git commit email limitation

Requested commit email:

```text
sanskarin@outlook.in
```

The GitHub connector available in this ChatGPT environment does **not** expose author/committer email fields for the connected commit/file-write operations. Therefore this session cannot truthfully force `sanskarin@outlook.in` into connector-created commit metadata.

This limitation is documented here rather than pretending the requested email was applied.

For local Git commits under the user's own Git environment, the intended configuration is:

```bash
git config user.email "sanskarin@outlook.in"
```

The connected GitHub identity controls the connector-created commit attribution.

---

## 13. Validation that was actually performed versus validation that remains external

### 13.1 Historical structural validation from the earlier implementation stage

During the earlier staging implementation, dependency-free structural checks were performed for XML/XAML/project/RESX parsing, project-reference resolution, XAML/code-behind matching, event-handler wiring, empty files, and unfinished placeholder/exception markers before the main source push.

### 13.2 Repository searches during this continuation

GitHub repository searches performed during this continuation found no repository matches for `TODO` or `NotImplementedException` at the time those searches were run.

Because later documentation/preflight files can legitimately mention those words while describing what should be checked, a final release must rely on the committed structural script and actual CI/build gates rather than assuming an old text search remains sufficient forever.

### 13.3 Structural checker committed

`build/scripts/verify_structure.py` is now the repeatable dependency-free structural gate and GitHub CI runs it first.

### 13.4 Local .NET compiler limitation in this ChatGPT execution environment

The active ChatGPT execution container used for this project does **not** provide a local `dotnet` executable/toolchain. Therefore this implementation session cannot truthfully claim that it locally executed:

- NuGet restore;
- MAUI workload restore;
- C# compilation;
- `dotnet format`;
- unit tests through `dotnet test`;
- integration tests through `dotnet test`;
- Android MAUI build;
- Windows MAUI build;
- iOS MAUI build/archive;
- Mac Catalyst MAUI build/archive;
- emulator/simulator tests;
- physical-device tests;
- native signing/package creation;
- store submission validation.

No claim of a successful local .NET build/test is made.

### 13.5 Live web/package verification limitation

Live web search is disabled in this session. Therefore current ecosystem/package/toolchain status beyond the repository source cannot be independently web-verified here.

The exact restored package graph, current advisories, exact transitive licenses, supported SDK/workload compatibility, and current store requirements must be checked by CI/release engineering on the actual supported toolchain.

### 13.6 GitHub Actions/CodeQL/dependency review

The repository now contains workflows for structural/core/platform builds, CodeQL, and pull-request dependency review. Their source configuration being present does **not** equal a passing final release run.

The final release commit must have actual reviewed passing workflow/build/test evidence before Finora is represented as production-ready.

### 13.7 No bug-free claim

No claim is made that Finora is bug-free. The project instead adds preventive controls, tests, integrity diagnostics, privacy-safe diagnostics, CI/security automation, and explicit release gates.

---

## 14. Native/platform/store validation still required before production release

### Android

Required external evidence includes:

- supported .NET/MAUI/Android Release build;
- signed AAB produced with credentials outside Git;
- adaptive/monochrome icon verification;
- splash verification;
- notification permission/scheduling/reboot/doze/force-stop behavior;
- biometric success/cancel/unavailable/lockout with PIN fallback;
- `FLAG_SECURE` behavior/limitations;
- file picker/share/import/export/backup/receipt flows;
- force-close persistence;
- schema upgrade/migration;
- TalkBack/large text/reduced motion/theme/layout;
- Play Console data-safety/store listing checks.

### Windows

Required external evidence includes:

- Release build;
- final package identity/publisher;
- secure signing;
- Windows Hello paths;
- scheduled toast behavior under packaged identity;
- display-affinity capture protection behavior/limitations;
- file picker/share/export/backup/receipt flows;
- keyboard/focus/resizing/high DPI;
- Narrator/accessibility;
- package upgrade/migration.

### iOS

Required external evidence includes:

- supported Mac/Xcode Release/archive build;
- provisioning/signing outside Git;
- LocalAuthentication paths with PIN fallback;
- UserNotifications permission/scheduling;
- document picker/share/import/export/backup/receipt flows;
- VoiceOver/Dynamic Type/reduced motion/dark mode/layout;
- screenshot-protection limitation communication;
- app upgrade/migration;
- App Store privacy declarations.

### Mac Catalyst

Required external evidence includes:

- supported archive/build;
- signing/notarization/distribution configuration;
- LocalAuthentication/UserNotifications;
- keyboard/mouse/resizable window/focus/high-DPI behavior;
- file picker/share/import/export/backup/receipt flows;
- VoiceOver/accessibility;
- migration/upgrade.

### Cross-platform

Still requires actual release QA for:

- low disk space;
- cancelled picker/share;
- permission denial/revocation;
- force-close around critical writes;
- database contention;
- damaged receipt files;
- wrong/tampered/truncated backups;
- every released migration path;
- exact dependency/license review;
- final signing-secret handling;
- final store screenshots using synthetic data only.

Detailed gates are in:

- `docs/TEST_PLAN.md`
- `docs/releases/RELEASE_CHECKLIST.md`
- `docs/releases/STORE_READINESS.md`

---

## 15. Intentionally later-version product boundaries

These are not being represented as unfinished requirements for the current local-first release because the master product specification explicitly places them later unless separately allowed/designed:

- cloud synchronization;
- remote Finora account/login service;
- collaboration/shared finance records;
- mobile-number authentication;
- server/store-backed commercial entitlement validation.

Any future implementation of those capabilities requires additional architecture, privacy, security, authentication, retention/deletion, migration, server/database, consent, and threat-model work before release.

---

## 16. Current status decision

The Finora repository now contains a substantially expanded local-first personal-finance implementation, schema-v2 migration path, advanced finance workflows, encrypted backup/restore, local notification/security integrations, privacy-safe diagnostics/integrity tooling, extensive documentation, tests, and CI/security automation.

However, **Finora 0.2.0 must not be represented as a completed production store release until the documented external compiler/platform/device/store gates have real passing evidence**.

That distinction is deliberate: complete source work and release validation are different engineering stages.

---

## 17. Important current files

Primary repository/status files:

- `README.md`
- `CHANGELOG.md`
- `PROJECT_STATUS.md`
- `DECISIONS.md`
- `SECURITY.md`
- `PRIVACY.md`
- `SUPPORT.md`
- `TERMS.md`
- `CONTRIBUTING.md`
- `CODE_OF_CONDUCT.md`
- `THIRD_PARTY_NOTICES.md`
- `what_changed.md`

Architecture/security/release:

- `docs/architecture/OVERVIEW.md`
- `docs/architecture/DATABASE_SCHEMA.md`
- `docs/privacy/DATA_LIFECYCLE.md`
- `docs/security/THREAT_MODEL.md`
- `docs/setup/BUILD.md`
- `docs/setup/TROUBLESHOOTING.md`
- `docs/TEST_PLAN.md`
- `docs/releases/RELEASE_CHECKLIST.md`
- `docs/releases/STORE_READINESS.md`

Quality automation:

- `build/scripts/verify_structure.py`
- `build/scripts/verify.ps1`
- `.github/workflows/ci.yml`
- `.github/workflows/codeql.yml`
- `.github/workflows/dependency-review.yml`
- `.github/dependabot.yml`
- `.github/CODEOWNERS`

Latest reliability/integrity source:

- `src/Finora.Application/IntegrityContracts.cs`
- `src/Finora.Infrastructure/DataIntegrityService.cs`
- `src/Finora.Infrastructure/PrivacyLogger.cs`
- `src/Finora.App/Services/AppExceptionCoordinator.cs`
- `src/Finora.App/Pages/SettingsPage.Integrity.cs`
- `tests/Finora.IntegrationTests/DataIntegrityTests.cs`
