# Finora

**Finora** is an open-source, local-first personal finance application built with .NET MAUI, C#, XAML, SQLite/Entity Framework Core, and MVVM-oriented presentation architecture.

> **Made by the Sanskar**

Current source version: **0.2.0 (build 2)**  
Current database schema: **2**

Finora's current product model requires no Finora account, login, email address, phone number, subscription account, or internet connection for core finance functionality. Financial records remain on the user's device unless the user explicitly imports, exports, shares, or saves an encrypted backup.

## Product goals

Finora is designed to help users:

- record income, expenses, refunds, adjustments, and transfers;
- understand balances, cash flow, spending, and budget performance;
- manage categories, subcategories, and tags;
- create budgets and warning thresholds;
- track savings goals and contributions;
- manage recurring obligations and reminders;
- reconcile account statements;
- attach local receipt/document files;
- import CSV files through mapping/preview/validation;
- export finance records to CSV/PDF;
- create encrypted backups and restore them safely;
- keep working fully offline without an account requirement.

## Implemented source areas

### Onboarding and local-first privacy

- No mandatory login/account creation.
- Default currency and locale preference.
- Financial-month start day.
- Optional opening balance.
- Explicit sample-data opt-in.
- Privacy summary/full privacy path.
- Onboarding can be revisited from Settings.

### Accounts

- Cash, bank, credit card, digital wallet, savings, investment-placeholder, and custom types.
- Name/icon/color/currency/opening balance.
- Active/hidden/archived state.
- Credit limit and billing day metadata.
- Account-specific transaction history.
- Same-currency linked transfer pair.
- Account editing/archive/restore.
- Reconciliation preview/history with explicit adjustment option.

### Transactions

- Expense, income, transfer, refund, and adjustment.
- Decimal-safe amount entry and calculator.
- Account/category/date/time/merchant-payee/note/tags/payment method/manual location.
- Manual-only location: no background location collection.
- Split transactions.
- Search and account/category/type/date filters.
- Transaction revision history for critical edits.
- Soft delete/restore.
- Bulk categorization.
- Duplicate review.
- Receipt/document attachment lifecycle.
- Selected/all CSV and PDF export paths.

### Categories and tags

- Default categories.
- User categories and subcategories.
- Reorder/archive/restore.
- Safe reassignment/merge.
- Parent-cycle prevention.
- Tag creation/archive/restore and reporting linkage.

### Budgets

- Overall/category/subcategory budgets.
- Weekly/monthly/custom periods.
- Rollover option.
- Planned versus actual/variance reporting.
- Warning threshold.
- Split-aware category spending.
- Reminder coordination when permission/settings allow.

### Savings goals

- Target/starting amount and optional target date.
- Notes/icon.
- Contributions and withdrawals.
- Optional linked account transaction.
- Forecast/milestones/completion state.
- Reduced-motion-aware completion messaging.

### Recurring items

- Daily/weekly/monthly/yearly/custom interval.
- Expense/income/transfer/refund template support.
- Start/end date, grace period, reminder lead time.
- Persisted unique occurrence state.
- Paid/partial-paid/skipped/postponed actions.
- Idempotent processing so repeated startup/scheduler runs do not create duplicate occurrences.
- Linked transfer creation for recurring transfer payments.

### Dashboard and reports

- Balance, income/spending/net, remaining budget, upcoming recurring items, top categories, goal progress, recent transactions, and cash-flow summaries.
- Configurable cards.
- Privacy mode that hides displayed amounts.
- Category spending, income/expense, account balance trend, budget performance, merchant/payee, tag, and monthly comparison report data.
- MAUI-drawn chart surfaces with text/tabular equivalents for accessibility.

### CSV import

- System file picker.
- Detected headers and explicit mapping.
- Required date/type/amount/account fields plus optional currency/category/merchant/note/payment method/location/transfer group/counterparty/tags.
- Major-versus-minor-unit handling.
- Decimal-safe conversion.
- UTF-8 validation.
- File/row limits.
- Quoted-field parsing.
- Account/category/tag resolution.
- Duplicate protection.
- Transfer-group validation.
- Transactional commit and explicit row errors.

### Receipts and attachments

- App-private local storage.
- Sanitized/generate internal filenames.
- Path confinement.
- Image/PDF content-type allow-list.
- Per-file size limit.
- Asynchronous copy.
- SHA-256 checksum and byte-size metadata.
- Open/delete/storage-usage/orphan-cleanup workflow.
- Receipt bytes included in encrypted backups.

### Encrypted backup and restore

- Explicit user-created backup only; no automatic upload.
- PBKDF2-SHA256 password-derived key.
- Random salt.
- AES-GCM authenticated encryption with random nonce/tag.
- Schema metadata/preview.
- Receipt path/size/checksum verification.
- Staged attachment restore.
- Transactional database replacement with rollback handling.
- Wrong/tampered/incompatible backup rejection.

Finora cannot recover a forgotten backup password.

### App lock and privacy controls

- Optional 4–12 digit PIN.
- Random-salt password-based verifier.
- OS secure storage for small verifier/security values.
- Escalating local lockout.
- Configurable inactivity auto-lock.
- Optional biometric/Windows Hello with PIN fallback.
- Privacy mode/hide amounts.
- Platform sensitive-screen protection where supported.
- Platform limitations are documented rather than represented as universal screenshot blocking.

### Local reminders

- Local notification scheduling only after permission where applicable.
- Persisted schedule/dedupe state.
- Backup, budget, and recurring-item reminder coordination.
- Android, Apple, and Windows platform source paths.
- Generic privacy-safe notification text.

### Diagnostics and integrity

- Privacy logger stores sanitized event/type tokens rather than private finance payloads.
- Bounded/rotated local diagnostic log.
- Sanitized export.
- Hidden developer integrity checker for:
  - SQLite integrity;
  - foreign-key state;
  - transaction/account references and currency;
  - transfer pairing;
  - split totals;
  - category hierarchy cycles;
  - recurrence references/duplicates;
  - receipt path/presence/size/SHA-256 state.
- Integrity export contains health codes/counts rather than account names, merchants, notes, amounts, or receipt contents.

### Developer/repository quality gates

- Nullable reference types.
- Warnings-as-errors.
- Latest-recommended analyzers.
- Deterministic builds.
- Central package-version management.
- Dependency-free structural preflight.
- Unit/integration/UI-contract tests.
- Migration tests.
- Cross-platform GitHub Actions build/test workflow.
- CodeQL workflow.
- Pull-request dependency review.
- Dependabot configuration.
- CODEOWNERS.
- Privacy-aware issue and PR templates.

## Architecture

```text
src/
  Finora.App/                 # .NET MAUI UI, resources, platform integrations
  Finora.Domain/              # Entities, money/domain rules
  Finora.Application/         # Use-case contracts and DTOs
  Finora.Infrastructure/      # SQLite, files, backup, import/export, diagnostics
  Finora.Shared/              # Shared constants/primitives

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
```

Architecture details: [`docs/architecture/OVERVIEW.md`](docs/architecture/OVERVIEW.md)

Database schema: [`docs/architecture/DATABASE_SCHEMA.md`](docs/architecture/DATABASE_SCHEMA.md)

Engineering decisions: [`DECISIONS.md`](DECISIONS.md)

## Money correctness

Finora stores money as signed 64-bit integer **minor units** plus a currency code. User-entered major-unit text is handled with `decimal` at conversion boundaries. Binary floating-point values are not used for stored/calculated monetary amounts.

Same-currency transfers are two linked rows with equal/opposite values and reciprocal counterparty accounts.

## Local-first privacy model

Current source intentionally has:

- no required remote Finora account;
- no required cloud synchronization;
- no automatic backup upload;
- no required analytics/advertising telemetry service;
- no background location collection;
- explicit user-controlled import/export/share flows.

Read [`PRIVACY.md`](PRIVACY.md) and [`docs/privacy/DATA_LIFECYCLE.md`](docs/privacy/DATA_LIFECYCLE.md).

## Build and validation

Dependency-free structural check:

```bash
python build/scripts/verify_structure.py
```

Full SDK quality gate:

```bash
dotnet workload restore
dotnet restore Finora.sln
dotnet format Finora.sln --verify-no-changes --no-restore
dotnet build Finora.sln -c Release --no-restore
dotnet test Finora.sln -c Release --no-build
```

PowerShell wrapper:

```powershell
./build/scripts/verify.ps1
```

See [`docs/setup/BUILD.md`](docs/setup/BUILD.md).

## Target platforms

The MAUI application project currently declares:

- Android: `net10.0-android`
- iOS: `net10.0-ios`
- Mac Catalyst: `net10.0-maccatalyst`
- Windows: `net10.0-windows10.0.19041.0`

Platform source presence is **not** proof of a successful native release. Final notification/biometric/screen-protection/file-picker behavior, packaging, signing, accessibility, upgrade, and store compliance must be tested with the appropriate SDK/host/device.

Use [`docs/releases/STORE_READINESS.md`](docs/releases/STORE_READINESS.md) and [`docs/releases/RELEASE_CHECKLIST.md`](docs/releases/RELEASE_CHECKLIST.md).

## Current source validation status

See [`PROJECT_STATUS.md`](PROJECT_STATUS.md) for the distinction between implemented source and external compiler/device/store gates.

No claim is made that Finora is bug-free.

## Testing

See [`docs/TEST_PLAN.md`](docs/TEST_PLAN.md). Major test areas include money/domain rules, linked transfers, recurrence idempotency, transaction revisions, reconciliation, mapped CSV import, schema migration, encrypted attachment backup/restore, local integrity diagnostics, and navigation/privacy contracts.

## Security

Private vulnerability reporting: [`SECURITY.md`](SECURITY.md)

Threat model: [`docs/security/THREAT_MODEL.md`](docs/security/THREAT_MODEL.md)

Do not attach real finance databases, receipts, PINs, or backup passwords to public issues.

## Localization and accessibility

Finora is English-first and includes localization-ready resource structure with initial Hindi common strings. Full screen-by-screen Hindi localization is not represented as complete yet.

UI architecture includes reduced-motion and larger-interface preferences, semantic/text report equivalents, and desktop/adaptive layout considerations. Final screen-reader/keyboard/large-text/device validation remains a native release gate.

## Local premium/demo state

The local premium flag is a development/demo capability only. It is **not** tamper-proof commercial entitlement validation. A future paid build would require store/server-backed entitlement design.

## Later-version boundaries

Not part of the current local-first release:

- cloud synchronization;
- remote Finora account/login;
- collaboration;
- mobile-number authentication;
- server-backed commercial entitlement.

These require new architecture, privacy, security, retention, and migration decisions before implementation.

## Repository and contacts

- Repository: https://github.com/sanskarIN/Finora
- Creator/open-source profile: https://www.github.com/sanskarIN
- Business/security: `sanskarin@outlook.in`
- Support: `supportramsandesh@gmail.com`
- Attribution: **Made by the Sanskar**

## License

Finora is licensed under the Apache License 2.0. See [`LICENSE`](LICENSE).

Third-party components retain their own licenses. Exact direct/transitive package license metadata must be reviewed with the release toolchain before publishing binaries. See [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).
