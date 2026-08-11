# Changelog

All notable Finora changes are documented here. The project follows semantic-versioning intent where practical during pre-release development.

## [Unreleased]

### Changed — 2026-08-11 continuation hardening

- Dashboard reporting-currency notice now binds to the actual `CurrencyScope` ViewModel property instead of a stale property name.
- Dashboard gained explicit current financial month, previous financial month, trailing 30-day, trailing 90-day, and year-to-date period selection through shared domain policy.
- User-selected local calendar dates now resolve through shared timezone-safe `[fromUtc, toExclusiveUtc)` boundaries rather than duplicated UTC-midnight or 23:59:59 calculations.
- Dashboard current balance now uses current account summaries directly instead of rebuilding current balance from an all-history trend query.
- Advanced reports now include yearly comparison, recurring obligations, and savings progress in addition to category, income/expense, account trend, budget, merchant/payee, monthly, and tag reporting.
- Monthly/yearly reports group by local calendar month/year and stop at the current local date so future-dated imported rows do not appear early.
- Signed report charts now draw positive values above and negative values below a true zero baseline rather than using absolute magnitude.
- Report values are masked and quantitative charts hidden while privacy/hide-on-launch is active.
- Passive account, transaction, tools, budget, savings, recurring, reconciliation, and transaction-detail split amounts now use currency-aware privacy formatting instead of raw minor-unit labels.
- Account detail and transaction detail edit formatting uses each currency's actual minor-unit precision rather than hard-coded two-decimal text.
- Savings monthly contribution forecast hides its monetary estimate while privacy mode hides amounts.
- Reconciliation preview/history uses privacy-safe formatted values and shared local end-of-day boundaries.
- Transaction history gained deterministic sort choices and bounded 50-row incremental display with an explicit Load more action.
- Transaction Tools date filters now use shared local-calendar boundaries; duplicate/tool amounts use currency/privacy formatting.
- Onboarding now exposes both Privacy and Terms access with accessibility semantics while preserving revisit behavior.
- Settings full local-finance deletion is explicitly wired to the dedicated complete-reset service handler.
- Settings About exposes repository/profile, business/support contacts, license/notices, contributing/security/support guides, and version/build derived from packaged `AppInfo` metadata.
- Android biometric callback failures no longer surface provider-supplied error strings.
- Android reminder cancellation queries an existing immutable `PendingIntent` with `NoCreate` instead of creating a new cancellation artifact.
- Structural preflight now guards complete-reset wiring, biometric provider-text redaction, and raw minor-unit user-facing XAML labels.

### Added — 2026-08-11 continuation coverage

- `DashboardPeriodPolicy` and unit tests for financial-month/trailing/year-to-date ranges.
- `LocalDateRange` and unit tests for timezone-safe local-date to UTC-boundary conversion.
- Integration coverage for yearly comparison, recurring obligations, savings progress, and future-dated comparison exclusion.
- UI source-contract coverage for Dashboard periods/currency scope, complete report sections, Settings identity/reset/legal/support controls, transaction sorting/paging, onboarding legal links, true-zero chart behavior, and passive amount privacy.
- Reusable `PrivacyMoneyConverter` for currency-aware hidden-money XAML display.

### Changed — 2026-08-10 continuation hardening

- User-facing infrastructure failures in Reports/Settings and bound ViewModel errors no longer expose raw exception messages, provider details, filesystem paths, or stack-like text. Deliberate short validation messages remain actionable.
- Unexpected non-fatal `AsyncCommand` failures are contained and routed to the privacy-safe logger instead of escaping the `async void` event boundary.
- Privacy logger storage now rejects symbolic-link/reparse traversal and has regression coverage proving caller properties/exception messages are not serialized and current log rotation stays bounded.
- Backup password/new PIN/confirm PIN entry in Settings now uses masked controls and clears entered values after operations. Lock-screen PIN remains masked and is cleared after attempts.
- PIN verification validates 4–12 ASCII digits before hashing, zeroes verifier buffers where possible, fails closed during temporary secure-storage provider failure, and self-heals a stale enabled marker when readable verifier material is actually missing/corrupt.
- PIN removal now reports secure-storage failure without falsely claiming the app lock was removed.
- Biometric/Windows Hello lock-screen failure messaging is stable/generic and keeps PIN fallback.
- Local notification dedupe replacement now schedules the new OS reminder before database replacement, disables old rows inside the database transaction, then cancels stale OS reminders after commit. Failed replacement preserves the prior enabled reminder.
- Notification reconciliation disables expired rows and retries best-effort cancellation for disabled/expired OS IDs.
- App-private path safety now rejects symbolic-link/reparse-point traversal in receipt open/write/cleanup, backup validation/staging, restore journal/rollback paths, crash-safe rollback copy, integrity checks, and privacy logs.
- Encrypted backup creation now clears every accumulated receipt buffer on every exit path, including later-file/query/graph-validation failures; decrypted attachment buffers are also cleared when authenticated validation rejects a snapshot.
- Android now ships explicit legacy full-backup and Android 12+ cloud/device-transfer exclusion rules in addition to `allowBackup=false`; all private data domains are excluded from ordinary automatic backup/transfer.
- Startup best-effort cleanup removes only known Finora CSV/PDF/backup/integrity-report cache share copies older than 24 hours, preserving fresh/unrelated/diagnostic files.
- Structural preflight now guards Android backup-rule resources/wiring, masked Settings secret fields, secret prompt regressions, and raw exception-message alerts.
- Domain rules and EF `SaveChanges` now validate schema-v2 metadata for splits, categories/tags/links, attachment metadata, recurrence occurrences, transaction revisions, reconciliations, notification schedules, app settings, audit entries, and backup metadata in addition to existing finance aggregates.
- Authenticated backup graph validation reuses those Domain metadata rules so preview/restore and persistence no longer diverge on basic entity shape.
- Data-integrity diagnostics now actually implement the aggregate checks that existing regression tests expected: budgets/periods, savings histories/completion state, recurrence relations/payment state, reconciliations, and attachment parent/path/hash data.
- Earlier stale `IntegrityIssue.Count` test assertions were corrected to the current `AffectedRecords` contract.
- New goal creation initializes `IsCompleted` from starting progress; database initialization repairs only a stale derived goal-completion flag when underlying contribution history validates and leaves corrupt history untouched for integrity diagnostics.
- Settings delete handler naming was aligned between XAML and code-behind; structural preflight continues to guard all event-handler references.
- Lock and Settings surfaces gained additional accessibility heading/semantic descriptions for security controls.

### Added — 2026-08-10 continuation coverage

- Notification consistency integration tests for failed/successful dedupe replacement, cancellation failures, expired-row cleanup and reconciliation.
- Symbolic-link receipt path regression covering attachment open, integrity check and encrypted-backup rejection where host link creation is supported.
- Privacy logger tests for property/message redaction, rotation and linked-file refusal.
- Temporary share-artifact cleaner tests covering managed/fresh/unrelated/diagnostic files and file-link target preservation.
- ViewModel/AsyncCommand tests for safe error mapping, cancellation, concurrency and failure containment.
- Direct schema-v2 metadata persistence tests for invalid attachment path, notification, recurrence occurrence, reconciliation, category, revision and deletion-state rows plus valid paid-after-postponement history.
- Derived savings goal completion repair tests, including corrupt negative-running-history non-repair.
- Android `backup_rules.xml` and `data_extraction_rules.xml` privacy resources.
- `ITemporaryArtifactCleaner` and bounded cache cleanup implementation.

### Changed — earlier 2026-08-10 hardening

- Added platform-correct canonical path confinement for receipt/restore/recovery paths.
- Fixed startup/lifecycle initialization serialization so activation work does not race database migration/recovery.
- Centralized account/transaction/budget/goal/recurrence monetary/domain rules and extended checked arithmetic.
- Enforced transaction/account currency consistency, account-currency immutability after finance/recurrence use, same-currency recurrence destinations, split-category validity, linked-goal transaction currency, and fail-closed legacy dashboard aggregation.
- Hardened generic transaction editing/duplicate review and recurring generated-payment updates against transfer-pair/rule/account/currency drift.
- Reconciliation/account detail use checked arithmetic and safe error handling; reconciled opening balance is not silently rewritten.
- Advanced reports are split-aware, recursive for category descendants, currency-scoped, rollover-consistent and overflow-safe.
- Category/tag mutation/report workflows protect subcategory-budget semantics and currency-scoped tag totals.
- Encrypted backup creation/preview/restore validates full authenticated financial graph before destructive replacement and clears receipt/plaintext buffers as early as practical.
- Active recurring dependencies block account archival; archived accounts remain valid for inactive recurrence history.
- Recurring rules gained pause/resume/archive lifecycle, accessible controls, reminder synchronization and lifecycle tests.
- Budget-period semantics are centralized: explicit periods cannot overlap, custom cadence is active only inside explicit periods, and effective rollover plan must remain positive.
- Data-integrity diagnostics expanded to aggregate finance graph checks.
- Dashboard removed legacy mixed-currency aggregate dependency.
- Budget explicit-period update is transactional and has rollback regression coverage.
- Security/threat/test/release/privacy/build/public docs were aligned with current source.

## [0.2.0] - Development

### Architecture and persistence

- Introduced layered .NET MAUI solution: Shared, Domain, Application, Infrastructure, App, unit/integration/UI tests.
- Implemented SQLite/EF Core local-first persistence with foreign keys, WAL, busy timeout and indexes.
- Implemented schema version 2 and transactional v1→v2 migration.
- Added transaction revisions, reconciliations and notification schedules in schema v2.
- Centralized deterministic local data reset and synthetic sample-data services.

### Finance core

- Signed `long` minor-unit money storage/calculation with decimal major-unit conversion.
- Accounts with state, opening/current balance, credit-card fields and history.
- Same-currency atomic paired transfers.
- Expense/income/refund/adjustment transactions, splits, tags, receipts, search/filter, soft-delete/restore, bulk categorization and duplicate review.
- Category/subcategory hierarchy, archive/restore/merge/reorder and tag management.
- Account reconciliation with preview/history/explicit adjustments.
- Overall/category/subcategory budgets with weekly/monthly/custom periods, warning thresholds and rollover.
- Savings goals with contributions/withdrawals, optional linked transactions, forecasts and milestones.
- Recurring expense/income/transfer/refund templates with persisted occurrences and paid/partial/skipped/postponed/reopen workflows.

### Dashboard and reports

- Configurable privacy-aware dashboard cards for balance, income/expense/net, remaining budget, upcoming recurrence, categories, goals, recent transactions and six-month cash flow.
- Advanced category/income-expense/account-balance/budget/merchant/monthly/tag report datasets.
- Accessible dependency-free `GraphicsView` bar chart and textual/tabular equivalent.

### Import/export

- Mapped/previewed CSV import with validation, limits, duplicate protection and transactional persistence.
- CSV export and dependency-free multipage PDF export for selected/all transactions.

### Backup and privacy

- Password-encrypted local backup using PBKDF2-SHA256 + AES-GCM.
- Receipt bytes and schema-v2 data included in backup/restore.
- Backup preview, attachment size/hash/path validation, transactional DB replacement and attachment rollback.
- Local-first/no-login/no-automatic-upload product boundary.
- Privacy-safe bounded diagnostic logging.

### Security

- Local PIN verifier with random salt, PBKDF2, secure storage and escalating lockout.
- Biometric/Windows Hello where supported with PIN fallback.
- Sensitive screen protection on supported Android/Windows paths.
- Privacy mode and hide-amounts behavior.

### Notifications

- Permission-gated local notification persistence/deduplication.
- Android alarms/BroadcastReceiver, Apple UserNotifications and Windows scheduled toasts.
- Weekly backup, budget threshold and recurring reminder coordination using generic privacy-safe text.

### UI, settings and onboarding

- Responsive phone/tablet/desktop navigation.
- Theme, currency/locale, month-start, privacy, accessibility, security, notification, receipt, dashboard and developer settings.
- Hidden developer panel with schema/feature/integrity/sample-data controls.
- Onboarding explains local-first/no-login/no-auto-upload/uninstall/backup/manual-location/sample-data behavior.
- English localization baseline plus Hindi common strings.
- App icon/splash/monochrome branding resources and attribution.

### Repository engineering

- Apache-2.0, privacy/terms/security/support/contributing/conduct/notices docs.
- Structural preflight, staged multi-platform CI, Dependabot, CodeQL, dependency review, CODEOWNERS, issue/PR templates.
- Release/store-readiness/test/threat/database/data-lifecycle documentation.
