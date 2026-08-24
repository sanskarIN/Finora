# Finora

[![Support Finora on Buy Me a Coffee](src/Finora.App/Resources/Images/bmc_support.svg)](https://buymeacoffee.com/sanskarIN)

> **☕ Support Finora development:** [Buy Me a Coffee — sanskarIN](https://buymeacoffee.com/sanskarIN). Support is optional and never unlocks features, changes support priority, or replaces store/server-backed entitlement validation.

**Finora** is an open-source, local-first personal finance application built on .NET 10 and C#. Its established application path uses .NET MAUI, XAML, SQLite/Entity Framework Core, and MVVM-oriented presentation architecture for Android, iOS/iPadOS, Mac Catalyst, and Windows. An additive Avalonia universal path extends the source toward Linux desktop, additional Windows/macOS desktop hosting, and WebAssembly/PWA delivery without moving finance rules into platform-specific UI code.

> **Made by the Sanskar**

Current source version: **0.2.0 (build 2)**  
Current database schema: **2**

Finora's current product model requires no Finora account, login, email address, phone number, subscription account, or internet connection for core native finance functionality. Financial records remain on the user's device unless the user explicitly imports, exports, shares, or saves an encrypted backup.

The universal browser host is intentionally more restrictive than the native application: it exposes the WebAssembly/PWA presentation foundation but keeps finance persistence disabled until a browser-local encrypted storage adapter passes the repository's parity, migration, recovery, integrity, privacy, quota/eviction, backup/restore, and offline validation requirements. A successful Web build is therefore not represented as full browser finance parity.

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
- keep working fully offline without an account requirement on supported native finance paths.

## Current source highlights

### Local-first privacy

- No mandatory login/account creation.
- No automatic cloud synchronization or backup upload.
- No required analytics/advertising telemetry service.
- Manual-only transaction location text; no background location collection.
- Explicit system picker/share/save boundaries for import/export/backup/receipts.
- Android source explicitly disables ordinary app backup and ships cloud/device-transfer exclusion rules for private app data.
- Native universal desktop storage remains OS-local through the existing SQLite/EF Core infrastructure.
- Browser finance persistence is disabled rather than silently downgrading native durability/privacy semantics.
- Uninstall/reset can remove local data, so users should save a verified external encrypted backup when needed.

### Privacy-mode amount hiding

`PrivacyMode` and `HideAmountsOnLaunch` are display controls; they do not rewrite stored finance data.

Passive monetary surfaces use currency-aware hidden-money behavior across:

- Dashboard;
- account list/detail/history;
- transaction history and Transaction Tools;
- transaction-detail split display;
- budget cards;
- savings cards and contribution forecast;
- recurring rules and occurrences;
- reconciliation preview/history;
- Reports rows.

When amounts are hidden, quantitative report charts are also hidden because bar height would otherwise reveal monetary magnitude. Explicit amount input/edit controls remain editable when the user intentionally edits money.

### Accounts and transfers

- Cash, bank, credit card, digital wallet, savings, investment-placeholder, and custom account types.
- Name/icon/color/currency/opening/current balance.
- Active/hidden/archived states.
- Credit limit and billing day 1–31 metadata.
- Account detail/history, edit/archive/restore.
- Currency-aware account amount formatting, including non-two-decimal currencies.
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
- Search and account/category/type/date/text filters.
- Deterministic sort choices: newest, oldest, amount high-to-low, amount low-to-high, merchant A–Z.
- Bounded 50-row history display with explicit **Load more** behavior.
- Shared local-calendar date-range policy for advanced filters and Transaction Tools.
- Transaction detail/edit workflow with currency-specific edit precision.
- Transaction revision history for critical edits.
- Soft delete/restore.
- Bulk categorization and duplicate review without automatic deletion.
- Receipt/document attachment lifecycle.
- Selected/all CSV and PDF export paths.

### Money and currency correctness

Finora stores money as signed 64-bit integer **minor units** plus currency code. User-entered major-unit text is handled with `decimal`; known currency precision supports 0-/2-/3-/4-decimal conventions where configured rather than assuming two decimals universally.

- zero and `long.MinValue` persisted transaction amounts are rejected;
- Expense uses negative minor units;
- Income/Refund use positive minor units;
- transfer pairs are equal/opposite and same-currency;
- split sums/signs must match parent transaction;
- persistence-boundary validation protects tracked EF writes;
- reporting uses checked arithmetic;
- passive UI money is formatted through currency-aware `Money` semantics instead of labeling stored minor units directly.

Finora does **not** invent exchange rates. Dashboard/report/tag aggregate totals are scoped to an explicit reporting currency. Other-currency rows retain their own currency.

### Local calendar and UTC persistence

Transactions persist UTC timestamps, but user-selected dates mean local calendar days.

`LocalDateRange` converts inclusive local dates into UTC `[fromUtc, toExclusiveUtc)` boundaries and centralizes invalid/ambiguous midnight handling. The policy is reused by Dashboard periods, Reports, transaction filters/tools, reconciliation statement-date boundaries, budget report windows, and account trend calculations where local calendar meaning matters.

This avoids common UTC-midnight and `23:59:59` boundary mistakes for users outside UTC and around daylight-saving transitions.

### Categories and tags

- Default categories.
- User categories and subcategories.
- Reorder/archive/restore.
- Safe reassignment/merge.
- Parent-cycle prevention.
- Subcategory-budget-safe category reassignment.
- Tag creation/archive/restore.
- Tag reports require explicit currency scope so unlike currencies cannot be mixed silently.

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
- Explicit-period replacement has a transactional rollback path/regression coverage.
- Passive planned/actual values honor privacy and currency formatting.

### Savings goals

- Target/starting amount and optional target date.
- Notes/icon.
- Contributions and withdrawals.
- Optional linked account transaction.
- Running progress cannot fall below zero.
- Linked transaction currency must match goal.
- Forecast/milestones/completion state.
- Reduced-motion-aware completion messaging.
- Goal cards and forecast amount respect privacy/hide-on-launch.
- Startup can repair a stale derived completion flag only when underlying goal history validates.

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
- Recurring monetary rows retain their own currency and respect privacy mode.

### Dashboard

Dashboard cards are configurable and currency-scoped. The current source includes:

- current balance;
- income/spending/net change;
- remaining budget;
- upcoming recurring items;
- top spending categories;
- savings goals;
- recent transactions;
- cash-flow trend;
- privacy mode;
- explicit reporting-currency explanation.

Dashboard now supports selectable activity periods through `DashboardPeriodPolicy`:

- current financial month;
- previous financial month;
- last 30 days;
- last 90 days;
- year to date.

The financial-month start is configurable from day 1–28. Current balance remains a current account-state value; activity cards use the selected period. The Dashboard does not call the legacy mixed-currency aggregate API.

### Reports

Current report source includes:

- spending by category;
- income versus expense;
- account balance trends;
- budget performance;
- merchant/payee report;
- 12-month comparison;
- 5-year/yearly comparison;
- recurring-obligation report;
- savings-progress report;
- tag reporting through category/tag services with explicit currency scope.

Monthly/yearly current comparisons use local calendar grouping and stop at today, so future-dated imported rows are not included before their local date arrives.

Charts use dependency-free MAUI `GraphicsView` rendering and always have text/tabular equivalents. Signed net-change charts use a true zero baseline: positive values render above zero and negative values below zero. Quantitative charts are suppressed while amounts are hidden by privacy settings.

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
- Canonical path confinement with platform-correct case sensitivity.
- Symbolic-link/reparse-point rejection across attachment/backup/recovery/integrity paths.
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
- Receipt path/size/checksum/link verification.
- Complete financial-graph validation before encryption and after authenticated decryption.
- Validation covers IDs, account/currency links, transfers, splits, categories/tags, budgets/periods, goals/contributions, recurrence, attachments, revisions, reconciliation, notification metadata, and settings boundaries.
- Internal restore markers/settings are not imported from snapshot settings.
- Sensitive plaintext/receipt buffers are cleared as early as practical after use/failure.
- Crash-safe restore operation gate, durable journal, database commit marker, startup recovery, attachment rollback/finalization, and orphan staging cleanup.
- Wrong/tampered/truncated/incompatible/semantically-invalid backups are rejected.
- Settings backup-password entry is masked and cleared from UI after operation.

Finora cannot recover a forgotten backup password.

### App lock and privacy controls

- Optional 4–12 digit PIN.
- Random-salt PBKDF2-SHA256 verifier.
- OS secure storage for small verifier/security values.
- Explicit PIN-enabled marker distinguishes temporary provider failure from readable missing/corrupt verifier state.
- Temporary secure-storage provider failure fails closed.
- Readable missing/corrupt verifier can clear stale marker rather than permanently trapping app behind nonexistent verifier.
- Escalating bounded local lockout.
- Configurable inactivity auto-lock.
- Optional biometric/Windows Hello with PIN fallback.
- Android biometric provider error text is not forwarded verbatim to user-visible Results.
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
- Deduplicated replacement schedules the new native reminder before committing replacement state and only then cancels stale native ID.
- Failed replacement preserves prior enabled reminder.
- Android cancellation looks up an existing immutable `PendingIntent` with `NoCreate` rather than creating a cancellation artifact.

### Diagnostics and integrity

- Privacy logger stores sanitized event/type tokens rather than private finance payloads.
- Bounded/rotated local diagnostic log and sanitized export.
- Linked/reparse diagnostic paths are rejected.
- Bound infrastructure errors and user alerts avoid raw filesystem/database/crypto/provider messages.
- Hidden developer integrity checker covers:
  - SQLite integrity and foreign keys;
  - transaction values/account/currency state;
  - transfer pairing;
  - split signs/totals;
  - category cycles;
  - budgets/custom periods/category relations;
  - savings goal/contribution/link/completion state;
  - recurrence dependencies/occurrence payment state;
  - reconciliation arithmetic/adjustment links;
  - receipt path/presence/size/SHA-256/link state.
- Integrity export contains health codes/counts rather than account names, merchants, notes, amounts, or receipt contents.

### Adaptive UI, localization and accessibility

- Mobile bottom-tab navigation.
- Tablet/desktop flyout/sidebar navigation.
- Runtime adaptive switching with primary-section preservation.
- Onboarding/unlock route through adaptive root.
- Runtime locale normalization/application and number/date format preview.
- English resource baseline plus initial Hindi common-string resource structure.
- Light/dark/system theme.
- Reduced-motion and larger-interface settings.
- Accessible report text equivalents.
- Accessible recurring-rule lifecycle controls.
- Accessible Dashboard period selection, transaction sort/load-more, Settings security/destructive/About controls, and Onboarding Privacy/Terms controls in source.

Full screen-by-screen Hindi localization and final native accessibility validation are not represented as complete. The current Avalonia universal landing surface is also a platform foundation, not a claim that every MAUI screen/localization/accessibility behavior has already reached universal-host parity.

### Settings, onboarding and developer tools

Settings includes:

- default currency/locale/financial month start;
- privacy/hide amounts;
- theme, reduced motion, larger interface;
- default account/transaction type;
- notifications/backup reminders;
- receipt quality/storage;
- auto lock/biometric/sensitive-screen preferences;
- Dashboard cards;
- local premium demo flag (explicitly non-tamper-proof);
- hidden developer panel;
- reminder sync;
- expanded local integrity checker;
- typed destructive full-finance reset through dedicated complete reset service;
- typed deterministic synthetic sample-data reset;
- masked backup password/PIN fields;
- Revisit onboarding control.

Onboarding explains local-first/no-account/no-automatic-upload behavior, currency/locale/financial month, optional opening balance and explicit sample-data opt-in. It exposes Privacy and Terms access, highlights optional Buy Me a Coffee project support with the shared branded artwork, and can be revisited safely when accounts already exist.

About uses packaged `AppInfo` version/build metadata and exposes:

- **Made by the Sanskar**;
- .NET MAUI / C# / XAML / SQLite / MVVM technology summary;
- repository and creator GitHub profile;
- optional Buy Me a Coffee project-support link;
- business/security and support contacts;
- Apache-2.0 license/notices;
- Privacy and Terms;
- Contributing, Security and Support guides.

The Buy Me a Coffee link opens `https://buymeacoffee.com/sanskarIN` as an optional external contribution page. It does not unlock Finora features, create premium entitlement, change support priority, or replace store/server-backed licensing. Packaged releases must verify the target store's current policy for external contribution/payment links. See [`docs/features/PROJECT_SUPPORT.md`](docs/features/PROJECT_SUPPORT.md) for the support UX and trust boundary.

### Temporary share artifacts

Generated CSV/PDF/backup/integrity-report share copies are cache artifacts, not durable records. Startup best-effort cleanup targets only known Finora share-copy names older than a 24-hour grace period while preserving fresh, unrelated, and diagnostic files. File links are removed as links rather than traversed to targets.

## Architecture

```text
src/
  Finora.App/                 # Established .NET MAUI UI, resources, platform integrations
  Finora.Universal/           # Shared Avalonia UI/runtime capability boundary
  Finora.Universal.Desktop/   # Linux/Windows/macOS Avalonia desktop host
  Finora.Universal.Browser/   # WebAssembly/PWA host; finance persistence currently disabled
  Finora.Domain/              # Entities, money/domain rules, dashboard/budget policies
  Finora.Application/         # Use-case/report contracts and DTOs
  Finora.Infrastructure/      # Native SQLite, files, reports, backup, import/export, diagnostics
  Finora.Shared/              # Shared constants/primitives/local-date policy

tests/
  Finora.UnitTests/
  Finora.IntegrationTests/
  Finora.UiTests/

tools/
  Finora.Performance/         # Synthetic performance/correctness harness

docs/
  accessibility/
  architecture/
  development/
  features/
  operations/
  platforms/
  privacy/
  releases/
  security/
  setup/
  testing/

build/
  scripts/
```

Documentation hub: [`docs/README.md`](docs/README.md)  
Cross-platform matrix: [`docs/platforms/CROSS_PLATFORM.md`](docs/platforms/CROSS_PLATFORM.md)  
Next steps: [`docs/NEXT_STEPS.md`](docs/NEXT_STEPS.md)  
Architecture details: [`docs/architecture/OVERVIEW.md`](docs/architecture/OVERVIEW.md)  
Database schema: [`docs/architecture/DATABASE_SCHEMA.md`](docs/architecture/DATABASE_SCHEMA.md)  
Engineering decisions: [`DECISIONS.md`](DECISIONS.md)

## Build and validation

Dependency-free repository structural check:

```bash
python build/scripts/verify_structure.py
```

Dependency-free cross-platform contract check:

```bash
python scripts/check_cross_platform.py
```

The structural verifier checks repository/documentation structure and local links, product/support identity including BMC discovery surfaces, XML/XAML/project wiring, version/schema drift, floating-point monetary fields, raw minor-unit user-facing labels, selected Android privacy/backup rules, masked secret fields, complete-reset handler wiring, biometric provider-text redaction, and raw exception-alert regressions.

The cross-platform checker validates the universal solution/project inventory, centrally pinned Avalonia packages, target frameworks, compiled-binding contract, native desktop runtime wiring, browser persistence safety boundary, PWA manifest semantics, workflow matrix, and platform documentation before more expensive .NET builds run.

Representative universal builds:

```bash
dotnet restore src/Finora.Universal.Desktop/Finora.Universal.Desktop.csproj
dotnet build src/Finora.Universal.Desktop/Finora.Universal.Desktop.csproj -c Release --no-restore

dotnet workload install wasm-tools
dotnet restore src/Finora.Universal.Browser/Finora.Universal.Browser.csproj
dotnet build src/Finora.Universal.Browser/Finora.Universal.Browser.csproj -c Release --no-restore
```

Full compiler/test verification requires a compatible .NET 10 environment plus the target-specific MAUI/WebAssembly/platform SDKs and workloads described in [`docs/setup/BUILD.md`](docs/setup/BUILD.md).

Commit-specific validation claims belong in the CI/release evidence documentation and must name the exact candidate/run that executed. Source presence, a queued workflow, or an older successful candidate is not reused as proof for a newer head.

## Target platforms

Finora currently has these delivery/source paths:

- Android — established MAUI target `net10.0-android`;
- iOS / iPadOS — established MAUI target `net10.0-ios`;
- Windows 10/11 — established MAUI target `net10.0-windows10.0.19041.0` plus the Avalonia desktop host;
- macOS — established Mac Catalyst target `net10.0-maccatalyst` plus the Avalonia desktop host;
- Linux — Avalonia desktop host targeting `net10.0`; full MAUI-screen feature parity and native packaging/runtime validation remain explicit work;
- Web / modern browsers — Avalonia WebAssembly host targeting `net10.0-browser`; finance persistence is intentionally disabled in this phase;
- ChromeOS — Android delivery where supported and/or the Web/PWA path; dedicated ChromeOS-path validation remains required.

For Linux, the current host follows the stable Avalonia X11 baseline through normal platform detection. Native Wayland is not represented as validated by this branch unless a later candidate explicitly enables and validates that separate path.

Platform source presence is **not** proof of a successful release. Final notification/biometric/screen-protection/file-picker behavior, local-calendar/time-zone behavior, privacy display, chart rendering, storage/recovery behavior, packaging, signing where applicable, accessibility, upgrade, browser/PWA behavior, and store/browser policy must be tested with the appropriate SDK/host/device/runtime.

Use [`docs/platforms/CROSS_PLATFORM.md`](docs/platforms/CROSS_PLATFORM.md), [`docs/releases/STORE_READINESS.md`](docs/releases/STORE_READINESS.md), and [`docs/releases/RELEASE_CHECKLIST.md`](docs/releases/RELEASE_CHECKLIST.md).

## Current source validation status

See [`PROJECT_STATUS.md`](PROJECT_STATUS.md) for distinction between implemented source and external compiler/device/store gates, and [`what_changed.md`](what_changed.md) for the latest continuation-specific evidence boundary.

No claim is made that Finora is bug-free.

## Next steps

The prioritized execution roadmap is [`docs/NEXT_STEPS.md`](docs/NEXT_STEPS.md).

The strongest next milestone is a reproducible Finora 0.2.0 release candidate with evidence for structural verification, dependency/workload restore, automated tests, all native builds, universal desktop/WebAssembly builds, schema migration, backup/restore and interruption recovery, finance-data integrity, privacy mode, currency/date correctness, notifications, app lock/biometrics, accessibility, complete local data deletion, packaging/signing, and store/browser-policy review.

For the universal path, the next engineering milestones are full Linux finance-screen parity and native integration validation, plus a deliberately designed/tested encrypted browser-local persistence layer before browser finance workflows are enabled.

Large later-version features such as remote accounts, cloud sync, collaboration, secure commercial entitlement, automatic FX, or telemetry should not displace unresolved correctness/release/cross-platform parity blockers.

## Testing

See [`docs/TEST_PLAN.md`](docs/TEST_PLAN.md). Current automated/source-contract areas include money/currency, domain/persistence metadata, linked transfers, split/category behavior, recurrence payment/rule lifecycle, account/reconciliation dependencies, custom budget periods, Dashboard periods, local-date UTC conversion, complete report matrix, future-date comparison exclusion, currency-aware import/reporting, encrypted backup graph validation, crash-safe restore recovery, schema migration, expanded integrity diagnostics, privacy-safe amount surfaces, signed chart baseline, transaction sorting/incremental display, adaptive navigation, reset safety, onboarding/About controls, BMC support visibility/trust boundaries, platform privacy source contracts, and universal-host repository/build contracts.

## Security

Private vulnerability reporting: [`SECURITY.md`](SECURITY.md)  
Threat model: [`docs/security/THREAT_MODEL.md`](docs/security/THREAT_MODEL.md)

Do not attach real finance databases, receipts, PINs, backup passwords, or other secrets to public issues.

## Local premium/demo state

The local premium flag is a development/demo capability only. It is **not** tamper-proof commercial entitlement validation. A future paid build would require store/server-backed entitlement design.

Buy Me a Coffee is also **not** an entitlement mechanism. Supporting the project externally must remain separate from app feature access and licensing state.

## Later-version boundaries

Not part of current local-first release:

- cloud synchronization;
- remote Finora account/login;
- collaboration;
- mobile-number authentication;
- server-backed commercial entitlement;
- automatic exchange-rate conversion;
- analytics/advertising telemetry by default.

These require new architecture, privacy, security, retention, and migration decisions before implementation.

## Repository and contacts

- Repository: https://github.com/sanskarIN/Finora
- Creator/open-source profile: https://www.github.com/sanskarIN
- **☕ Support development: https://buymeacoffee.com/sanskarIN**
- Business/security: `sanskarin@outlook.in`
- Support: `supportramsandesh@gmail.com`
- Attribution: **Made by the Sanskar**

## License

Finora is licensed under Apache License 2.0. See [`LICENSE`](LICENSE).

Third-party components retain their own licenses. Exact direct/transitive package license metadata must be reviewed with release toolchain before publishing binaries. See [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).