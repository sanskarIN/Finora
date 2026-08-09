# Changelog

All notable source changes to Finora are tracked here. The repository is currently on the 0.2.0 development line; a tagged store release requires the external gates documented in `docs/releases/RELEASE_CHECKLIST.md`.

## [Unreleased]

### Reliability, correctness and recovery hardening

- Added currency-aware minor-unit precision for zero-, two- and three-decimal currencies while preserving signed `long` money storage and `decimal` conversion.
- Hardened account/transaction/split validation and enforced core Account/FinanceTransaction invariants at every EF save boundary.
- Rejected zero/`long.MinValue`/invalid-sign/invalid-currency financial states and extended privacy-safe integrity diagnostics to detect legacy/raw corruption.
- Fixed CSV import to use currency precision, reject overflow/extreme minor units safely, validate transfer counterparties, count invalid rows exactly once, apply transfer tags, and prevent duplicate rows within one import batch.
- Added multi-currency isolation: dashboard/report aggregates use one explicit reporting currency; unlike currencies are retained/displayed separately and never silently converted/added.
- Added fail-closed app-lock behavior when PIN secure-storage verifier state is missing/malformed plus tested bounded lockout escalation policy.
- Added centralized unhandled/unobserved exception capture with privacy-safe event-only diagnostics.
- Added complete transactional finance-data reset preserving schema/app preferences/PIN state and removing finance-domain/audit/backup/reminder data plus receipt files after DB commit.
- Added deterministic synthetic developer sample reset with typed destructive confirmation.
- Added skipped recurring-occurrence reopen workflow, repeated full-payment idempotency and account/currency drift guards.
- Added production crash-safe encrypted restore wrapper, durable private recovery journal, transient pending DB marker, verified pre-restore receipt rollback copy, startup recovery and orphan recovery-directory cleanup.
- Serialized backup/preview/restore operations to prevent recovery-state races.

### Adaptive UI, accessibility and localization

- Added phone bottom-tab and tablet/desktop flyout primary navigation with resize/idiom switching and equivalent-route preservation.
- Startup, onboarding and PIN/biometric unlock now route to the correct adaptive dashboard root.
- Added scalable global control sizing/minimum touch targets and semantic heading levels.
- Added validated runtime culture coordinator; saved locale applies before normal UI navigation and can update live from Settings/onboarding.
- Added live currency/number/date format preview.
- Reports now expose locale-aware formatted money rows rather than raw minor-unit integers where the user reads financial values.
- Dashboard/reporting UI explains the selected reporting currency and separated other currencies.

### Platform/package hardening

- Added required iOS/Mac Catalyst biometric purpose strings.
- Aligned Windows package source version with Finora 0.2.0 and included desktop target-family metadata.
- Kept Android local-data backup disabled and cleartext traffic disabled; structural preflight enforces both flags.

### Tests and CI

- Added/expanded unit tests for domain sign/currency/split rules, JPY/INR/KWD precision, culture normalization, PIN lockout policy and production `ViewModelBase`/`AsyncCommand` behavior.
- Added integration tests for direct-EF persistence invariants, complete finance reset, synthetic sample reset, restore crash-recovery decisions, integrity regression, recurring state transitions, currency-aware CSV import and report currency isolation.
- Expanded UI-contract tests for phone/desktop adaptive routes, resize preservation, large-text, keyboard-focus and screen-reader obligations.
- Strengthened structural preflight with required-file, XML/XAML/project, reference/handler, app/package version, schema-document, monetary representation and Android privacy checks.
- Corrected CI topology so core tests run without MAUI workload assumptions and native MAUI builds run on supported Windows/macOS runners.
- Updated local verification scripts to be host-aware and core-first.

### Added earlier in 0.2.0

#### Finance core and database

- Multi-project .NET MAUI solution with Shared, Domain, Application, Infrastructure, App, UnitTests, IntegrationTests and UiTests projects.
- Signed 64-bit integer minor-unit money model and decimal-safe major/minor conversion.
- SQLite/EF Core persistence with WAL, foreign keys, busy timeout and relational indexes.
- Database schema v2 with transactional v1 → v2 migration.
- Schema-v2 transaction revision, account reconciliation, local notification schedule and attachment-original-filename records.
- Accounts, transaction search/persistence, paired same-currency transfers, account archiving and transaction soft-delete/restore.
- Category/subcategory/tag persistence plus archive/restore/merge/reassignment/reordering workflows.
- Budget, budget period/rollover, savings goal/contribution and recurrence persistence/workflows.
- Recurrence occurrence processing redesigned to persist an idempotent pending occurrence before any payment transaction is generated.
- Transaction revision history for critical edits/deletes/restores/bulk categorization/transfer edits.
- Reconciliation workflow with explicit adjustment transaction and history.
- Privacy-safe local database/foreign-key/transfer/split/category/recurrence/attachment integrity checker.

#### Transactions, accounts, planning and reports

- Advanced transaction entry with date/time, payment method, manual-only location, notes, filters and decimal calculator.
- Transaction detail editing, tags, split editor, receipt lifecycle and revision-history UI.
- Multi-select transaction tools, bulk categorization, duplicate review and selected export.
- Account detail/edit/archive/restore, transfer UI and reconciliation UI.
- Category/subcategory/tag management UI.
- Overall/category/subcategory weekly/monthly/custom budgets and reminder-aware UI.
- Savings-goal milestones, deposits/withdrawals, linked contributions and forecast UI.
- Recurring rule setup and paid/partial/skipped/postponed occurrence workflow.
- Configurable privacy-aware dashboard with balance/income/spending/net/budget/upcoming/category/goal/recent/cash-flow cards.
- Category, income/expense, account-balance, budget-performance, merchant/payee, monthly-comparison and tag report datasets.
- Dependency-free MAUI `GraphicsView` bar chart with zero baseline and equivalent text/tabular data.

#### Import, export, receipt storage and backup

- Full CSV mapping/preview/import pipeline with UTF-8 validation, file/row limits, quoted-field parsing, duplicate protection, optional category creation, tag linking and transfer-group validation.
- Richer transaction CSV export and multipage dependency-free PDF export.
- Receipt/document attachment management in app-private storage with file/type/size/path/hash controls and orphan cleanup.
- AES-GCM encrypted backups with PBKDF2-SHA256 password-derived key, authenticated metadata, attachment byte inclusion, preview and transactional restore.

#### Privacy and security

- Local-first/no-login current release and explicit system picker/share-sheet export boundary.
- Privacy-aware settings and bounded diagnostics.
- PIN hashing, rate limiting and inactivity auto-lock.
- Android/iOS/Mac Catalyst biometric and Windows Hello unlock source with PIN fallback.
- Android and supported-Windows sensitive-screen protection source.
- Persisted/deduplicated local reminders with Android, Apple and Windows scheduling source.
- Generic reminder text to reduce lock-screen finance leakage.
- Local premium demo flag remains explicitly non-secure and not commercial entitlement validation.

#### MAUI UI, branding and documentation

- Onboarding, dashboard, transactions, accounts, budgets, goals, reports, recurring items and settings surfaces wired to services.
- Encrypted backup/restore, diagnostics export, PIN management, notification permission, attachment cleanup, categories/tags, developer controls and local finance-data deletion actions.
- Original editable Finora app-icon/adaptive foreground/monochrome and light/dark splash vector sources.
- Packaged privacy, terms and third-party-notices documents with in-app legal viewer.
- English-first resource baseline and Hindi localization-ready resource structure.
- README, privacy/security/support/terms/contributing/code-of-conduct, architecture, threat model, test plan, release/store-readiness and troubleshooting/build documentation.

#### Repository quality

- Unit tests for money, transaction signs, domain rules and decimal calculator.
- SQLite integration tests for transfers, recurrence idempotency, revision history, reconciliation, CSV import, attachment backup and schema migration.
- UI route/privacy/recovery contract tests.
- Cross-platform GitHub Actions build/test workflow, Dependabot, CodeQL, dependency review and CODEOWNERS.
