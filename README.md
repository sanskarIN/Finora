# Finora

> A local-first personal finance application built with .NET MAUI, C#, XAML, SQLite/EF Core, and MVVM.

**Made by the Sanskar**

Current source line: **Finora 0.2.0 (build 2)**  
Current database schema: **2**

## Product principles

Finora's current release is intentionally local-first:

- no Finora account/login required;
- core finance workflows work without internet;
- no automatic cloud sync;
- no automatic backup upload;
- no analytics/advertising telemetry dependency;
- no background location collection;
- manually entered transaction location only;
- user-triggered encrypted backups;
- Android ordinary automatic backup/device-transfer paths are explicitly excluded;
- money stored/calculated as signed integer minor units with decimal major-unit conversion;
- no implicit FX conversion or exchange-rate lookup.

## Current target platforms

The MAUI app source targets:

- Android;
- iOS;
- Mac Catalyst;
- Windows.

Native release validation still requires the corresponding .NET/MAUI workloads, Apple Xcode host for Apple targets, signing/provisioning, emulators/simulators/physical devices and store checks. Source presence is not a claim that those release gates have passed.

## Architecture

```text
Finora.App
  ↓
Finora.Application + Finora.Infrastructure
  ↓
Finora.Domain
  ↓
Finora.Shared
```

Projects:

```text
src/Finora.Shared
src/Finora.Domain
src/Finora.Application
src/Finora.Infrastructure
src/Finora.App

tests/Finora.UnitTests
tests/Finora.IntegrationTests
tests/Finora.UiTests
```

## Finance capabilities implemented in source

### Accounts and transfers

- Cash, bank, credit-card, wallet, savings, investment-placeholder and custom account types.
- Opening/current balances and account state.
- Credit-card limit/billing day metadata.
- Account archive/restore and detail history.
- Same-currency atomic paired transfers.
- Explicit prevention of unsupported implicit cross-currency transfers.
- Account currency change blocked after financial/recurring dependencies exist.
- Active recurrence dependencies block account archival until paused/completed/archived.

### Transactions

- Expense, income, refund and adjustment quick-add.
- Decimal-only calculator.
- Account/category/date/time/merchant/payee/payment-method/manual-location/note fields.
- Advanced text/account/category/type/date filtering.
- Detailed edits with privacy-safe revision snapshots.
- Bulk categorization.
- Duplicate-candidate review without automatic deletion.
- Splits and tags.
- Receipt/document attachments.
- Soft delete/restore.
- Selected/all CSV and PDF export.
- Linked-transfer editing preserves both sides.

### Categories and tags

- Category/subcategory hierarchy.
- Cycle prevention.
- Reorder/archive/restore/merge/reassign workflows.
- Subcategory-budget hierarchy protection during mutation.
- Tag create/update/archive/restore.
- Currency-scoped tag reporting.

### Reconciliation

- Statement/book preview.
- Explicit difference.
- Optional adjustment transaction.
- Persisted reconciliation history.
- Checked reconciliation arithmetic.
- Opening balance protected after reconciliation history exists.

### Budgets

- Overall/category/subcategory budgets.
- Weekly/monthly/custom cadence.
- Explicit custom periods.
- Warning threshold and optional rollover.
- Recursive descendant-category and split-aware actuals.
- Central period policy prevents overlap and makes custom budgets inactive outside explicit windows.
- Effective rollover plan must remain positive.
- Explicit-period replacement is transactional.

### Savings goals

- Target/starting values, target date, icon/note.
- Contributions/withdrawals.
- Optional linked transaction.
- Running progress cannot fall below zero.
- Linked transaction must use goal currency.
- Forecast/milestone/completion state.
- New goals initialize completion from starting progress; startup safely repairs stale derived completion flags from earlier source behavior when history validates.

### Recurring items

- Expense/income/transfer/refund templates.
- Daily/weekly/monthly/yearly/custom intervals.
- Source/destination accounts, category, amount, merchant/payee, note, date range, grace/reminder lead.
- Persisted pending occurrences before financial transaction creation.
- Paid, partially paid, skipped, postponed and reopened states.
- Generated transaction linkage.
- Recurring transfers create linked transfer pairs.
- Pause/resume/archive lifecycle.
- Resume revalidates account/category/currency/end-date dependencies.
- Paid history can retain a valid historical postponed date.

## Dashboard and reports

Dashboard cards can include:

- balance;
- income/spending/net;
- remaining budget;
- upcoming recurring;
- top categories;
- savings goals;
- recent transactions;
- six-month cash flow.

Privacy mode can hide amounts. Aggregate dashboard/report values are currency-scoped; other-currency rows retain their own currency rather than being silently converted or added together.

Advanced reports include:

- category spending;
- income vs expense;
- account balance trend;
- budget performance;
- merchant/payee;
- monthly comparison;
- tag report.

The chart surface includes a textual/tabular equivalent.

## CSV import and export

Mapped CSV import provides:

- column mapping/preview;
- required Date/Type/Amount/Account fields;
- optional Currency/Category/Merchant/Note/Payment Method/Location/Transfer Group/Counterparty/Tags;
- major/minor amount-unit modes;
- currency-aware decimal conversion;
- fallback account;
- optional category creation;
- duplicate protection including same-batch duplicates;
- transfer-pair/counterparty validation;
- 50 MB / 100k row limits;
- UTF-8 validation;
- transactional import.

Export supports CSV and dependency-free multipage PDF.

## Receipt and private-file safety

Receipt files are stored under Finora app-private storage with:

- safe generated internal filenames;
- JPEG/PNG/WebP/HEIC/HEIF/PDF allowlist;
- 20 MB/file limit;
- byte-size + SHA-256 metadata;
- list/open/delete/storage-usage/orphan cleanup;
- canonical path confinement using platform-correct comparison;
- symbolic-link/reparse-point traversal rejection;
- no-link traversal shared by attachment cleanup, backup/restore/recovery and integrity diagnostics.

## Backup and restore

Encrypted local backups are user-triggered only.

Crypto:

- PBKDF2-SHA256 with random salt;
- 210,000 iterations;
- AES-GCM with random nonce/tag;
- authenticated Finora format magic.

Backup/restore validates:

- size/magic/schema;
- authenticated decryption;
- unique IDs;
- full supported finance graph relationships;
- schema-v2 metadata Domain rules;
- attachment path/size/hash;
- symbolic-link/reparse-point confinement;
- custom-budget periods;
- account/transaction currency relations;
- transfers/splits/categories/tags;
- goals/contributions;
- recurrence state;
- reconciliation links;
- notification/settings boundaries.

Restore uses staged receipt files, database transaction, filesystem rollback, and a durable crash-recovery journal/commit marker. Startup recovers interrupted restore state before finance navigation.

Plaintext/receipt byte buffers are cleared as early as managed APIs permit, including accumulated receipt buffers on later-file/query/validation failure. UI-side encrypted backup bytes are also cleared after write/share handling.

## Privacy and security

### App lock

- 4–12 ASCII-digit PIN.
- PBKDF2 verifier + random salt.
- OS secure storage.
- Fixed-time verification.
- Escalating lockout.
- Inactivity auto-lock.
- Biometric/Windows Hello where supported, with PIN fallback.
- Secure-storage provider failure fails closed when explicit lock marker exists.
- Readable missing/corrupt verifier clears stale marker to avoid permanent lock-screen trap.

Backup password/new PIN/confirm PIN Settings fields are masked and cleared after use; lock-screen PIN is masked and cleared after attempts.

### Screen privacy

- Privacy mode / hide amounts.
- Android secure-window protection.
- Windows display-affinity protection where supported.
- Platform limitations are documented rather than claiming universal screenshot prevention.

### Android automatic backup/device transfer

Android source keeps:

- `android:allowBackup="false"`;
- legacy `backup_rules.xml` excluding root/file/database/shared preferences/external domains;
- Android 12+ `data_extraction_rules.xml` excluding the same domains from cloud backup and device transfer;
- `android:usesCleartextTraffic="false"`.

Final packaged/physical-device behavior remains a release validation gate.

### Diagnostics

Privacy logger stores only sanitized event/type tokens. It ignores arbitrary caller properties and does not serialize exception messages/stacks. Diagnostic paths reject link/reparse traversal and the log is bounded/rotated.

Bound ViewModel infrastructure errors and primary Settings/Reports alerts use generic messages rather than raw filesystem/database/crypto/provider details. Unexpected `AsyncCommand` failures are contained and routed to the privacy logger.

The developer integrity report contains counts/codes rather than private finance contents.

## Local notifications

Local reminders are permission-gated and use generic privacy-safe text.

Implemented gateways cover Android, Apple platforms and Windows. Reminder synchronization handles weekly backup, budget threshold and recurring rules.

Deduplicated replacement is failure-safe: the new OS reminder is accepted before database replacement; old rows are disabled transactionally; stale OS reminders are cancelled afterward. Failed replacement preserves the prior enabled reminder.

## Temporary share files

User-requested CSV/PDF exports, encrypted backup share copies and integrity-report share copies can exist temporarily in Finora cache. On serialized startup, Finora best-effort deletes only known matching share-copy files older than 24 hours. Fresh files, unrelated cache files and diagnostic logs are preserved.

Copies explicitly saved/shared into another application/location are controlled by that destination.

## Data integrity and persistence boundary

Finora validates finance data through several layers:

1. Domain/entity rules.
2. EF `SaveChanges` structural validation for Added/Modified schema-v2 entities.
3. SQLite foreign keys/unique indexes.
4. Application/infrastructure relationship and atomic workflow checks.
5. Authenticated backup graph validation.
6. Local data-integrity diagnostics.
7. Safe startup normalization of derived savings completion state only when underlying history validates.
8. Automated tests and native release QA.

The integrity service checks SQLite/foreign keys, account/currency relations, transactions, transfers, splits, category hierarchy, budgets, goal histories, recurrence state, reconciliations and receipt metadata/files.

## Accessibility and adaptive UI

Current source includes:

- phone bottom tabs and tablet/desktop flyout hierarchy;
- system/light/dark theme;
- larger-interface option;
- reduced-motion preference;
- textual chart equivalent;
- semantic descriptions on key security/recurring/settings surfaces;
- lock-screen heading/status/PIN/biometric accessibility metadata.

Native TalkBack/VoiceOver/Narrator/keyboard/large-text/high-contrast validation is still required before store-ready status.

## Synthetic developer data

Hidden developer controls can reset to deterministic synthetic sample data after typed confirmation. Sample data is opt-in and must never be confused with user finance history.

## Build

Prerequisites:

- supported .NET 10 SDK;
- MAUI workloads for required targets;
- platform SDKs;
- macOS/Xcode for iOS/Mac Catalyst archive/build.

Run repository verification:

```powershell
./build/scripts/verify.ps1
```

Dependency-free structural preflight:

```bash
python build/scripts/verify_structure.py
```

Structural preflight also guards Android backup-rule wiring, masked Settings secret fields, XAML event handlers, and raw exception-alert regressions. It is not a compiler/test/native-device substitute.

See:

- [`docs/setup/BUILD.md`](docs/setup/BUILD.md)
- [`docs/setup/TROUBLESHOOTING.md`](docs/setup/TROUBLESHOOTING.md)
- [`docs/TEST_PLAN.md`](docs/TEST_PLAN.md)
- [`docs/releases/RELEASE_CHECKLIST.md`](docs/releases/RELEASE_CHECKLIST.md)
- [`docs/releases/STORE_READINESS.md`](docs/releases/STORE_READINESS.md)
- [`docs/security/THREAT_MODEL.md`](docs/security/THREAT_MODEL.md)
- [`docs/privacy/DATA_LIFECYCLE.md`](docs/privacy/DATA_LIFECYCLE.md)

## Current validation status

The repository contains source, tests, structural verification, CI definitions and release documentation. In this ChatGPT execution environment, a .NET/MAUI compiler/toolchain was not available, so no claim is made that current head passed restore/build/tests/native compilation here.

GitHub classic commit-status output can be empty even when Actions uses check runs; release evidence must come from actual workflow/check-run results.

Before release, follow `docs/releases/RELEASE_CHECKLIST.md` and retain actual platform build/test/device/signing/store evidence.

## Current intentionally later-version scope

Not represented as complete current-release features:

- Finora remote account/login;
- cloud synchronization;
- collaboration/shared-finance server flows;
- server/store-backed commercial entitlement validation;
- automatic exchange-rate conversion;
- analytics/advertising telemetry by default.

## Repository and contact

- Repository: https://github.com/sanskarIN/Finora
- Creator/open-source projects: https://www.github.com/sanskarIN
- Business/security: `sanskarin@outlook.in`
- Support: `supportramsandesh@gmail.com`
- License: Apache-2.0
