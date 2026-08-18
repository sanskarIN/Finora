# Changelog

All notable Finora changes are documented here. The project follows semantic-versioning intent where practical during pre-release development.

## [Unreleased]

### Added — 2026-08-18 large-dataset performance tooling

- Added standalone `tools/Finora.Performance` net10.0 tooling that consumes Finora's real Application/Infrastructure services without becoming part of the packaged app runtime.
- Added batched synthetic finance-data seeding for four INR accounts, default categories, configurable transaction volumes, budgets, goals, recurrence rules, and bounded SHA-256-verified synthetic receipt files.
- Added observational measurements for populated startup, first/deep database-backed history paging, broad/selective history search, amount sorting, income/expense/category/merchant/account/budget/recurring/savings reports, CSV export/import, PDF export, encrypted backup create/restore, integrity checking, managed heap and process working set.
- Added correctness gates so benchmark failures remain actionable: history count/deep-page checks, non-empty export/backup output, exact isolated CSV import counts with no skips/invalid rows, expected encrypted-restore graph counts, and healthy `DataIntegrityService` output.
- Added machine-readable JSON results with runtime/OS/architecture/processor/dataset/operation/iteration metadata and explicit timing/data/paging evidence-policy notes.
- Added a bounded `Performance smoke (10k)` CI job that compiles the complete harness under warnings-as-errors and executes startup/history/reports/integrity using synthetic data.
- Added an on-demand `.github/workflows/performance.yml` workflow for 10k/50k/100k datasets with selectable operations and iterations plus retained JSON artifacts.
- Added `docs/testing/PERFORMANCE_BENCHMARKING.md` covering dataset shape, execution, interpretation, correctness checks, memory caveats, comparison hygiene, and release boundaries.
- Updated documentation index/status, project status, roadmap, CI evidence, changelog and cumulative ledger so performance claims remain evidence-based.

### Verified — 2026-08-18 performance-tooling candidate

- Exact source candidate `8a8e7e51a2bacecdc58405d3d5301e79f3d78c8b` passed Finora CI run `32127759802`, CodeQL run `32127759687`, and Dependency Review run `32127759673`.
- Structural preflight passed.
- Unit tests passed 102/102.
- Integration tests passed 179/179.
- UI-contract tests passed 38/38.
- Total automated tests passed: **319/319**, with zero failures/skips.
- The performance project built in Release with **0 warnings and 0 errors**.
- The bounded 10k synthetic CI smoke seeded the dataset successfully and executed startup, history, reports, and full integrity checking.
- Retained performance artifact: `9321290557`, SHA-256 `97eb07bf963491e8d89d45798b21aa99d0da312b931c3ea25b17e2dae5accb46`.
- Release source builds passed independently for Windows, Android, iOS, and Mac Catalyst on the same exact source candidate.
- The recorded 10k smoke is observational evidence only; it does not claim runtime execution of CSV/PDF/backup performance operations, the complete `--operations all` profile, 50k/100k profiles, signed packages, device responsiveness, accessibility, recovery failure injection, installed prior-version upgrades, or store approval.

### Changed — 2026-08-18 database-backed transaction history paging

- Replaced interactive all-results in-memory transaction history slicing with SQLite/EF Core database paging through a dedicated `ITransactionHistoryStore`.
- Added a typed paged query/result contract carrying search text, account/category/type filters, exclusive UTC date boundaries, sort order, offset, page size, total count and `HasMore`.
- Preserved all existing transaction-history sort choices and free-text fields while applying filters/count/sort before `Skip`/`Take`.
- Added deterministic secondary ordering across page boundaries for a fixed result set and case-insensitive SQLite merchant sorting.
- Kept the last applied query stable for **Load more** requests so un-applied filter-control edits cannot mix query states.
- Added validation for negative offsets, invalid page sizes and invalid date ranges; maximum store page size is 200 and UI page size remains 50.
- Expanded integration coverage for 120-row paging boundaries, filters, sorts, soft-delete exclusion and payment/location/account/category search; updated UI source-contract coverage so regressions back to `_allMatches` are rejected.
- Updated transaction feature/service documentation and marked the previous P2 paging roadmap item implemented.

### Verified — 2026-08-18 database paging candidate

- Source candidate `d841efb8c392860b221f331b4ced9119020b849e` passed Finora CI run `32120115922`, CodeQL run `32120115965`, and Dependency Review run `32120115912`.
- Structural preflight passed.
- Unit tests passed 102/102.
- Integration tests passed 179/179.
- UI-contract tests passed 38/38.
- Total automated tests passed: 319/319, with zero failures/skips.
- Release source builds passed independently for Windows, Android, iOS, and Mac Catalyst.
- Intermediate candidate `6617a0b6b07b4cd4befcd48ae22c476ab0b917d1` was blocked by strict analyzer `CA1861` in a new test assertion; the assertion was corrected without weakening analyzers or warnings-as-errors.
- Signed packaging, installed prior-version migration, native failure-injection/device/accessibility QA, dependency-license acceptance, and store approval remain separate release gates.

### Changed — 2026-08-15 migration, backup, integrity and recovery hardening

- Hardened schema migration so the version marker advances only after required target-column validation plus SQLite foreign-key/integrity validation succeeds inside the migration transaction.
- Added schema-version guard, fresh-initialization/reopen, v1→v2 data-preservation/idempotence, malformed-target rollback, and legacy foreign-key corruption regression coverage.
- Expanded encrypted-backup hostile-input testing for wrong password, ciphertext tampering, truncation, authenticated unsupported schema, authenticated relationship corruption, and authenticated receipt path/size/hash corruption.
- Tightened portable receipt validation so backup creation/preview/restore require valid 32-byte SHA-256 metadata instead of accepting missing checksum metadata.
- Expanded deliberate data-integrity corruption tests for split totals, transaction/account currency drift, missing/changed receipt files, invalid receipt checksum metadata, category parent cycles, and SQLite foreign-key violations.
- Corrected privacy-log rotation test synchronization so assertions observe completed asynchronous writes instead of racing a newly created rotated file.
- Added recovery regression coverage proving linked recovery journals and linked rollback copies fail closed without following unsafe filesystem targets or discarding live receipt/recovery state.

### Verified — 2026-08-15 data-safety candidate

- Source candidate `f80b29d44a225a6d745529519e6c59cadbc152a8` passed Finora CI run `31875164890` and CodeQL run `31875164864`.
- Structural preflight passed.
- Unit tests passed 97/97.
- Integration tests passed 141/141.
- UI-contract tests passed 35/35.
- Total automated tests passed: 273/273, with zero failures.
- Release source builds passed independently for Windows, Android, iOS, and Mac Catalyst with warnings-as-errors and the strict XAML compiled-binding diagnostics still active.
- Retained core and native diagnostic artifacts are recorded with job/artifact IDs and digests in `docs/testing/CI_EVIDENCE.md`.
- Installed prior-version upgrade testing, real process-kill/low-disk recovery injection, signed packaging, physical-device QA, accessibility, dependency acceptance, and store submission remain separate release gates.

### Changed — 2026-08-15 cross-platform build and XAML stabilization

- Split native CI validation into independent Windows, Android, iOS, and Mac Catalyst jobs so one platform failure no longer cancels another platform before diagnostics are collected.
- Fixed Android platform-version analysis by using runtime-recognized API guards for biometric APIs and notification permission.
- Resolved the Apple `AppDelegate` naming-analyzer conflict with a narrow required-entry-point suppression rather than weakening analyzers globally.
- Updated Microsoft.EntityFrameworkCore and Microsoft.EntityFrameworkCore.Sqlite to the 10.0.10 servicing line, clearing the linker metadata failure observed during native Release builds.
- Separated Windows source compilation from MSIX packaging in CI by validating Windows with `WindowsPackageType=None`; signed/package identity validation remains a separate release gate.
- Audited native diagnostics and found a large compiled-binding warning set (`XC0022`) across the app's XAML surfaces.
- Added explicit `x:DataType` contracts to the affected pages, templates, and picker item bindings instead of suppressing compiled-binding diagnostics.
- Promoted `XC0022`, `XC0023`, and `XC0025` to build errors so missing/incorrect compiled-binding context cannot silently return.
- Updated the primary Finora CI workflow to Node-24-compatible current GitHub Action majors: checkout v7, setup-python v7, setup-dotnet v6, and upload-artifact v7.

### Verified — 2026-08-15 earlier automated evidence

- Earlier strict source candidate `f7dbfbb8691edc79cee559101f284ccd90a44cf7` passed Finora CI run `31872362394` and CodeQL run `31872362398`.
- Structural preflight passed.
- Unit tests passed 97/97.
- Integration tests passed 109/109.
- UI-contract tests passed 35/35.
- Total automated tests passed: 241/241, with zero failures.
- Release source builds passed independently for Windows, Android, iOS, and Mac Catalyst while the compiled-binding warning classes above were fatal.
- Added `docs/testing/CI_EVIDENCE.md` to retain exact commit/run/job boundaries and explicitly distinguish source-build evidence from signed packaging, device, recovery, accessibility, and store evidence.
- This earlier evidence is retained as stabilization history; the newer `f80b29d…` data-safety candidate is the current automated baseline.

### Added — 2026-08-12 project support and next-step roadmap

- Added canonical `AppConstants.BuyMeACoffeeUrl` for `https://buymeacoffee.com/sanskarIN`.
- Added an accessible **Support development · Buy Me a Coffee** action to Settings/About using the shared canonical URL and system launcher.
- Buy Me a Coffee failures use generic user-facing text and the privacy-safe logger rather than raw platform/browser errors.
- Added UI source-contract coverage for the About button, handler, shared URL usage, and privacy-safe failure event.
- Added `docs/NEXT_STEPS.md` with a risk-prioritized P0–P3 roadmap covering native release blockers, release-candidate/store completion, quality/product polish, and later-version architecture.
- Added explicit product/store boundary: Buy Me a Coffee is optional project support only and does not unlock features, create premium entitlement, change support priority, or replace store/server-backed commercial licensing.
- Updated README, documentation hub, documentation coverage matrix, Settings reference, support guide, store metadata template, and project status with the canonical support link and roadmap.
- Extended structural preflight to require `docs/NEXT_STEPS.md` and verify the canonical Buy Me a Coffee URL, About action, shared constant usage, and no-feature-unlock wording.
- The preferred next milestone is now a reproducible Finora 0.2.0 release candidate backed by actual build/test/migration/backup/native/privacy/accessibility/store evidence before major new feature expansion.

### Documentation — 2026-08-11 complete project documentation pass

- Added `docs/README.md` as the complete documentation index and `docs/DOCUMENTATION_STATUS.md` as the documentation coverage/update-policy matrix.
- Added a complete end-user guide covering onboarding, navigation, Dashboard, accounts, transfers, transactions, categories/tags, splits, receipts, reconciliation, budgets, savings, recurring items, reports, CSV import/export, encrypted backup/restore, privacy/app lock, notifications, Settings, developer tools, data deletion, accessibility, support, and current product limitations.
- Added focused feature references for accounts/transactions/reconciliation, budgets/goals/recurring, reports/import/export, and Settings.
- Added architecture references for service ownership, end-to-end data flow, adaptive navigation/UI contracts, and preserved the existing architecture/schema documentation.
- Added security references for app lock/privacy/screen protection and encrypted backup/crash recovery, aligned with the existing threat model and data lifecycle.
- Added operations references for privacy-safe diagnostics/integrity and destructive finance reset/synthetic sample reset.
- Added contributor documentation: developer guide, repository code map, and safe feature-change workflow covering money/date/schema/backup/privacy/testing/documentation requirements.
- Added practical testing guidance plus a cross-platform native validation matrix that separates source/unit/integration/UI-contract evidence from native build/device/store evidence.
- Added Android, Windows, and iOS/Mac Catalyst platform engineering/QA guides using the current project target frameworks, minimum versions, manifests/plists, permissions, privacy boundaries, and release gates.
- Added versioning/database migration/backup compatibility policy and a store metadata preparation template that explicitly requires live store-policy verification before submission.
- Added accessibility/localization guidance describing current English-first/localization-ready state, initial Hindi resource structure, privacy-safe screen-reader requirements, chart equivalents, and native accessibility validation.
- Extended dependency-free structural preflight so the complete core documentation tree is required and repository-relative Markdown file links are checked without network access.
- Updated the build guide to document the complete documentation/preflight/platform/testing/release references.

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