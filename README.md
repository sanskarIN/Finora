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

## Current source highlights

### Local-first privacy

- No mandatory login/account creation.
- No automatic cloud synchronization or backup upload.
- No required analytics/advertising telemetry service.
- Manual-only transaction location text; no background location collection.
- Explicit system picker/share/save boundaries for import/export/backup/receipts.
- Uninstall/reset can remove local data, so users should save a verified external encrypted backup when needed.

### Accounts and transfers

- Cash, bank, credit card, digital wallet, savings, investment-placeholder, and custom account types.
- Name/icon/color/currency/opening/current balance.
- Active/hidden/archived states.
- Credit limit and billing day 1–31 metadata.
- Account detail/history, edit/archive/restore.
- Same-currency linked transfer pairs.
- Account reconciliation preview/history with explicit adjustment option.
- Reconciled opening-balance protection.
- Account currency cannot change after financial/recurring dependencies exist.
- Active recurring dependencies block account archival until paused/completed/archived.

### Transactions

- Expense, income, transfer, refund, and adjustment.
- Decimal-safe amount entry and calculator.
- Account/category/date/time/merchant-payee/note/tags/payment method/manual location.
- Split transactions with sign/total/category validation.
- Search and account/category/type/date filters.
- Transaction revision history for critical edits.
- Soft delete/restore.
- Bulk categorization and duplicate review.
- Receipt/document attachment lifecycle.
- Selected/all CSV and PDF export paths.

### Money and currency correctness

Finora stores money as signed 64-bit integer **minor units** plus currency code. User-entered major-unit text is handled with `decimal`; known currency precision supports 0-/2-/3-decimal minor-unit conventions rather than assuming two decimals universally.

- zero and `long.MinValue` persisted amounts are rejected;
- Expense uses negative minor units;
- Income/Refund use positive minor units;
- transfer pairs are equal/opposite and same-currency;
- split sums/signs must match the parent transaction;
- persistence-boundary validation protects tracked EF writes;
- reporting uses checked arithmetic.

Finora does **not** invent exchange rates. Dashboard/report/tag aggregate totals are scoped to an explicit reporting currency. Other-currency rows retain their own currency.

### Categories and tags

- Default categories.
- User categories and subcategories.
- Reorder/archive/restore.
- Safe reassignment/merge.
- Parent-cycle prevention.
- Subcategory-budget-safe category reassignment.
- Tag creation/archive/restore.
- Tag reports require explicit currency scope so INR/USD-style totals cannot be mixed silently.

### Budgets

- Overall/category/subcategory budgets.
- Weekly/monthly/custom cadence.
- Shared `BudgetPeriodPolicy` used by store/report paths.
- Weekly Monday–Sunday windows.
- Calendar-month windows.
- Custom budgets require explicit non-overlapping periods and are inactive outside those periods.
- Rollover applies only when enabled; effective planned value uses checked arithmetic and must remain positive.
- Warning threshold calculation is overflow-safe.
- Recursive category descendants and transaction splits are included correctly.
- Explicit-period replacement has a transactional reliability path/rollback regression coverage.

### Savings goals

- Target/starting amount and optional target date.
- Notes/icon.
- Contributions and withdrawals.
- Optional linked account transaction.
- Running progress cannot fall below zero.
- Linked transaction currency must match the goal.
- Forecast/milestones/completion state.
- Reduced-motion-aware completion messaging.

### Recurring items

- Daily/weekly/monthly/yearly/custom interval.
- Expense/income/transfer/refund template support.
- Start/end date, grace period, reminder lead time.
- Persisted unique occurrence state.
- Paid/partial-paid/skipped/postponed/reopened occurrence actions.
- Idempotent processing so repeated startup/scheduler runs do not create duplicate occurrences.
- Bounded recurrence backlog processing.
- Generated-payment and recurring-transfer link validation.
- Rule Pause / Resume / Archive lifecycle.
- Paused rules stop generation; resume revalidates end date/account/category/currency dependencies.
- Archived rules retain occurrence history but disappear from active rule lists.
- Reminder synchronization cancels stale recurring schedules.

### Dashboard and reports

- Balance, income/spending/net, remaining budget, upcoming recurring items, top categories, goal progress, recent transactions, and cash-flow summaries.
- Configurable cards and privacy mode.
- Dashboard aggregate cards use the configured reporting currency only.
- Currency-scope explanation is displayed when other-currency accounts exist.
- Category spending honors transaction splits.
- Category budgets resolve descendants recursively.
- Income/expense, account balance trend, budget performance, merchant/payee, tag, and monthly comparison report data.
- MAUI-drawn chart surfaces with text/tabular equivalents for accessibility.

### CSV import

- System file picker.
- Detected headers and explicit mapping.
- Required date/type/amount/account fields plus optional currency/category/merchant/note/payment method/location/transfer group/counterparty/tags.
- Currency-specific major-versus-minor-unit handling.
- Decimal-safe conversion.
- UTF-8 validation.
- File/row limits.
- Quoted-field parsing.
- Account/category/tag resolution.
- Duplicate protection including duplicates within one import batch.
- Transfer-group/counterparty validation.
- `long.MinValue` protection.
- Transactional commit and explicit row errors.

### Receipts and attachments

- App-private local storage.
- Sanitized/generated internal filenames.
- Path confinement with platform-correct case sensitivity.
- Image/PDF content-type allow-list.
- Per-file size limit.
- Asynchronous copy.
- SHA-256 checksum and byte-size metadata.
- Open/delete/storage-usage/orphan-cleanup workflow.
- Receipt bytes included in encrypted backups.

### Encrypted backup and restore

- Explicit user-created backup only; no automatic upload.
- PBKDF2-SHA256 password-derived key with random salt.
- AES-GCM authenticated encryption with random nonce/tag.
- Schema metadata/preview.
- Receipt path/size/checksum verification.
- Complete financial-graph validation before encryption and after authenticated decryption.
- Validation covers IDs, account/currency links, transfers, splits, categories/tags, budgets/periods, goals/contributions, recurrence, attachments, revisions, reconciliation, notification metadata, and settings boundaries.
- Internal restore markers/settings are not imported from snapshot settings.
- Sensitive plaintext/receipt buffers are cleared as early as practical after use.
- Crash-safe restore operation gate, durable journal, database commit marker, startup recovery, attachment rollback/finalization, and orphan staging cleanup.
- Wrong/tampered/truncated/incompatible/semantically-invalid backups are rejected.

Finora cannot recover a forgotten backup password.

### App lock and privacy controls

- Optional 4–12 digit PIN.
- Random-salt PBKDF2-SHA256 verifier.
- OS secure storage for small verifier/security values.
- Persistent PIN-enabled marker; missing/corrupt verifier fails closed.
- Escalating bounded local lockout.
- Configurable inactivity auto-lock.
- Optional biometric/Windows Hello with PIN fallback.
- Privacy mode/hide amounts.
- Platform sensitive-screen protection where supported.
- Apple Face ID purpose text included where required.
- Platform limitations documented rather than represented as universal screenshot blocking.

### Local reminders

- Local notification scheduling only after permission where applicable.
- Persisted schedule/dedupe state.
- Backup, budget, and recurring-item reminder coordination.
- Android, Apple, and Windows platform source paths.
- Generic privacy-safe notification text.
- Stale backup/budget/recurrence schedules removed when source state no longer needs them.

### Diagnostics and integrity

- Privacy logger stores sanitized event/type tokens rather than private finance payloads.
- Bounded/rotated local diagnostic log and sanitized export.
- Hidden developer integrity checker covers:
  - SQLite integrity and foreign keys;
  - transaction values/account/currency state;
  - transfer pairing;
  - split signs/totals;
  - category cycles;
  - budgets/custom periods/category relations;
  - savings goal/contribution/link state;
  - recurrence dependencies/occurrence payment state;
  - reconciliation arithmetic/adjustment links;
  - receipt path/presence/size/SHA-256 state.
- Integrity export contains health codes/counts rather than account names, merchants, notes, amounts, or receipt contents.

### Adaptive UI, localization and accessibility

- Mobile bottom-tab navigation.
- Tablet/desktop flyout/sidebar navigation.
- Runtime adaptive switching with primary-section preservation.
- Onboarding/unlock route through the adaptive root.
- Runtime locale normalization/application and number/date format preview.
- English resource baseline plus initial Hindi common-string resource structure.
- Light/dark/system theme.
- Reduced-motion and larger-interface settings.
- Accessible report text equivalents.
- Accessible recurring-rule lifecycle controls.

Full screen-by-screen Hindi localization and final native accessibility validation are not represented as complete.

### Settings and developer tools

- Default currency/locale/financial month start.
- Privacy/hide amounts.
- Theme, reduced motion, larger interface.
- Default account/transaction type.
- Notifications/backup reminders.
- Receipt quality/storage.
- Auto lock/biometric/sensitive-screen preferences.
- Dashboard cards.
- Local premium demo flag (explicitly non-tamper-proof).
- Hidden developer panel.
- Reminder sync.
- Expanded local integrity checker.
- Typed destructive full-finance reset.
- Typed deterministic synthetic sample-data reset.

## Architecture

```text
src/
  Finora.App/                 # .NET MAUI UI, resources, platform integrations
  Finora.Domain/              # Entities, money/domain rules, period policies
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

## Build and validation

Dependency-free structural check:

```bash
python build/scripts/verify_structure.py
```

Full compiler/test verification requires a compatible .NET 10/MAUI environment and the platform SDK/workloads described in [`docs/setup/BUILD.md`](docs/setup/BUILD.md).

The implementation environment used in the current ChatGPT continuation does **not** provide a local `dotnet` executable, so this repository does not claim a local `dotnet build` or `dotnet test` pass from this session.

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

See [`docs/TEST_PLAN.md`](docs/TEST_PLAN.md). Current test areas include money/currency, domain and persistence invariants, linked transfers, split/category behavior, recurrence payment/rule lifecycle, account/reconciliation dependencies, custom budget-period policies, currency-aware import/reporting, encrypted backup graph validation, crash-safe restore recovery, schema migration, expanded integrity diagnostics, adaptive navigation, reset safety, and UI source contracts.

## Security

Private vulnerability reporting: [`SECURITY.md`](SECURITY.md)  
Threat model: [`docs/security/THREAT_MODEL.md`](docs/security/THREAT_MODEL.md)

Do not attach real finance databases, receipts, PINs, or backup passwords to public issues.

## Local premium/demo state

The local premium flag is a development/demo capability only. It is **not** tamper-proof commercial entitlement validation. A future paid build would require store/server-backed entitlement design.

## Later-version boundaries

Not part of the current local-first release:

- cloud synchronization;
- remote Finora account/login;
- collaboration;
- mobile-number authentication;
- server-backed commercial entitlement;
- automatic exchange-rate conversion.

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
