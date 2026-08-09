# Project Status

Current source line: **Finora 0.2.0 (build 2), database schema 2**.

This document distinguishes **implemented source** from **compiler/device/store validation**. A feature being implemented in source does not mean its native platform behavior has been verified on every target.

## Implemented in repository

### Architecture and local-first model

- Multi-project .NET MAUI architecture: Shared, Domain, Application, Infrastructure, App, UnitTests, IntegrationTests, UiTests.
- Android, iOS, Mac Catalyst and Windows target frameworks.
- No required login/account/cloud synchronization in the current release.
- SQLite/EF Core local system of record with WAL, foreign keys and busy timeout.
- Integer `long` minor-unit money storage and `decimal` major-unit conversion.
- Currency-aware zero-/two-/three-decimal conversion/formatting metadata.
- Persistence-boundary validation for account/transaction sign/currency/value/split invariants.
- Schema-v2 migration from schema v1.
- Privacy-safe local data-integrity diagnostic.

### Accounts and transactions

- Cash, bank, credit-card, wallet, savings, investment-placeholder and custom account records.
- Account metadata editing, state management and per-account history.
- Transaction quick add, decimal calculator, date/time, category, merchant/payee, payment method, manual-only location and notes.
- Search/filter by account/category/type/date/text.
- Paired same-currency transfers with shared group and reciprocal counterparties.
- Soft delete/restore, critical revision history, split editor, tags, bulk categorization and duplicate review.
- Receipt/document attachments in app-private storage with path/size/type/SHA-256 controls.
- Account reconciliation with explicit adjustment and history.

### Categories, budgets and goals

- Category/subcategory create/edit/reorder/archive/restore/merge/reassign and cycle prevention.
- Tag create/edit/archive/restore/report linkage.
- Overall/category/subcategory budgets with weekly/monthly/custom cadence, explicit periods, rollover and thresholds.
- Savings goals with deposits/withdrawals, linked transactions, milestones and forecast text.

### Recurring obligations

- Daily/weekly/monthly/yearly/custom recurrence rules.
- Persisted unique occurrences for restart-safe/idempotent processing.
- Pending-first workflow: no financial transaction until paid/partial-paid.
- Paid, partial-paid, skipped, postponed and explicit skipped→reopen transitions.
- Repeated full-payment action is idempotent.
- Recurring transfer pair creation and account/currency availability guards.
- Local reminder scheduling/deduplication.

### Dashboard and reports

- Configurable privacy-aware dashboard cards.
- Explicit reporting-currency behavior: dashboard totals aggregate only the default reporting currency; other account currencies remain separate and are not silently converted/added.
- Recent transaction/upcoming recurrence/goal rows retain their actual currency.
- Category spending, income/expense, merchant/payee, monthly comparison, budget performance and account balance trend reports.
- Aggregate reports filter one reporting currency; account/budget series retain their own currencies.
- Locale-aware money/date display rows.
- MAUI `GraphicsView` chart with zero baseline and text/table equivalents for accessibility.

### Import/export

- CSV header detection, user mapping, preview and validated transactional import.
- UTF-8/file-size/row-count controls, quoted-field parsing and duplicate-header rejection.
- Currency-aware major-unit conversion including zero-/three-decimal currencies.
- Overflow/`long.MinValue` rejection before sign normalization.
- Account/category/tag/transfer/counterparty validation.
- Within-batch duplicate protection and exact invalid-row counts.
- CSV and multipage PDF export plus explicit system share/save flows.

### Backup, restore and recovery

- Password-encrypted backup with PBKDF2-SHA256 + AES-GCM.
- Current-schema preview/restore and attachment byte inclusion.
- Size/path/checksum/schema/tamper validation.
- Serialized backup/preview/restore operations.
- Production `CrashSafeBackupService` around the validated encrypted restore path.
- Durable app-private restore-recovery journal, pre-restore receipt rollback copy and transient DB pending marker.
- Startup recovery before normal navigation.
- Deterministic rollback/finalize decision for process interruption between DB and receipt-tree changes.
- Orphan recovery staging/rollback cleanup only after journal resolution.
- Initialization fails safely instead of exposing mismatched DB/receipt state when automatic recovery cannot complete.

### Privacy/security/settings

- PIN app lock with PBKDF2 verifier, OS secure storage, persistent enabled marker, fixed-time verification, fail-closed missing/corrupt verifier state and bounded escalating lockout.
- Configurable inactivity auto-lock.
- Optional Android/iOS/Mac Catalyst biometric and Windows Hello source with PIN fallback.
- Android/Windows sensitive-screen protection where platform source supports it; unsupported limitations remain explicit.
- Android local-data backup disabled and cleartext traffic disabled in manifest.
- Apple biometric purpose text present.
- Windows package source metadata aligned to 0.2.0.
- Privacy mode/hide amounts, notification/backup-reminder, receipt-storage, locale, currency, default-account/type, dashboard-card, theme, reduced-motion and larger-interface preferences.
- Runtime locale validation/application and live number/date format preview.
- Privacy-safe bounded diagnostics and centralized unhandled/unobserved exception capture.
- No analytics/advertising/automatic upload introduced.

### Adaptive/accessibility UI

- Phone bottom-tab primary navigation.
- Tablet/desktop flyout/sidebar-equivalent primary hierarchy.
- Runtime idiom/width switch with equivalent-section route preservation.
- Startup/onboarding/PIN/biometric navigation uses adaptive dashboard root.
- Global scalable control sizing and semantic heading styles.
- Live-region/error semantics on changed flows and chart text equivalents.
- Accessibility/large-text/keyboard/screen-reader native validation remains a release gate.

### Data reset and developer tooling

- Complete transactional finance-data reset removes schema-v2 finance records, user-created categories/tags, audit/backup metadata and receipt metadata while preserving schema marker, app preferences and PIN configuration.
- Self-referencing categories delete leaves-first; cycle causes rollback.
- Receipt files clean after DB reset commits.
- Hidden developer panel includes schema/feature flags, reminder sync, privacy-safe integrity check and deterministic synthetic sample reset.
- Synthetic reset requires exact typed destructive confirmation and never intentionally preserves existing finance data.

### Tests and repository quality

- Unit tests for money/currency precision, domain invariants, calculator, culture, PIN policy and ViewModel base/async-command behavior.
- SQLite integration tests for transfers, recurrence, migration, transaction revisions, reconciliation, CSV import, attachment backup, persistence-boundary validation, complete finance reset, synthetic sample reset, restore recovery, integrity regression, recurrence state transitions, currency-aware import and report currency isolation.
- UI-contract tests for primary/adaptive routes and privacy/recovery/accessibility flow obligations.
- Structural preflight validates required repo files, XML/XAML/project parsing, references, XAML handlers, version/schema drift, money representation and Android privacy flags.
- GitHub Actions split structural preflight, core tests, Windows/Android build and Apple build onto appropriate runners.
- CodeQL, dependency review, Dependabot, CODEOWNERS, issue/PR templates and release/security documentation.

## Verification performed in ChatGPT environment

Earlier in this project, a dependency-free structural pass was run against the then-current local staging tree and passed its XML/XAML/project/reference/handler/empty-file/placeholder checks.

During the current continuation, repository history/file inventory and source-level audits were performed through the connected GitHub repository. The structural preflight itself was strengthened substantially after that earlier local pass.

**The active ChatGPT execution environment has no `dotnet`, `csc`, `mcs`, or `msbuild` executable.** Therefore the latest repository state has not been compiled or test-executed locally by ChatGPT. No local claim is made that the latest C# changes, MAUI target frameworks, or native platform APIs have passed a compiler/device run.

GitHub Actions is configured to perform the actual compiler/platform gates on supported runners. A release candidate must retain successful CI/native/device evidence before publication.

## Required external validation before release

### All platforms

- Restore exact dependency graph and verify compatibility/licenses/vulnerabilities.
- Execute latest unit/integration/UI-contract suite with warnings-as-errors/analyzers.
- Test every released migration path and run post-migration integrity check.
- Failure-inject encrypted restore at every journal/copy/DB/swap/finalization boundary.
- Verify multi-currency isolation and currency precision metadata required by release markets.
- Verify real file-picker/share behavior and safe cancellation/error states.
- Verify privacy/terms/notices against actual packaged permissions/behavior.
- Verify no secrets/PII/private finance data in binaries/logs/test artifacts/store screenshots.
- Verify full reset/sample reset only against synthetic data.
- Accessibility QA: screen reader, large text, contrast, keyboard/focus, reduced motion, resize layouts.

### Android

- Release AAB compilation/signing.
- Adaptive/monochrome icon and splash rendering.
- Notification permission/scheduling/restart behavior.
- Android biometric success/cancel/error/lockout and PIN fallback.
- `FLAG_SECURE` capture behavior.
- Phone/tablet adaptive navigation.
- Backup/restore/recovery/import/export/receipt flows on emulator/device.
- Force-stop/restart persistence and package upgrade/migration.

### Windows

- Release package/MSIX build and production package identity/signing.
- Windows Hello and toast scheduling under packaged identity.
- Display-affinity capture behavior on supported Windows versions.
- File picker/share/export/backup/restore/recovery.
- Keyboard, resize, flyout/sidebar and high-DPI behavior.
- Package upgrade/migration.

### iOS / Mac Catalyst

- Build/archive on supported .NET/Xcode combination.
- Provisioning/signing/notarization as applicable.
- LocalAuthentication behavior with declared biometric purpose text and PIN fallback.
- UserNotifications authorization/scheduling.
- File picker/share/backup/restore/recovery.
- Phone/iPad/desktop adaptive navigation as applicable.
- VoiceOver/Dynamic Type or desktop accessibility.
- Upgrade/migration behavior.

## Intentionally later-version product work

The current product design still reserves these for later architecture/version work:

- cloud synchronization;
- Finora online account/login system;
- collaboration/shared finance data;
- server-backed or store-validated commercial entitlement;
- remote key escrow/recovery;
- automatic exchange-rate conversion/cross-currency reporting total.

The existing local premium flag remains a clearly labeled non-secure development/demo capability.

## Release status

**Source implementation: active 0.2.0 development line.**

**Store release status: not declared release-ready until the external compiler, native-platform, signing, accessibility, migration, recovery and device gates above have passed with evidence.**
