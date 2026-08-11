# Finora Service Catalog

This document maps the current application contracts to their implementation responsibilities so contributors can extend Finora without bypassing the intended domain, persistence, privacy, or platform boundaries.

## Registration root

`src/Finora.App/MauiProgram.cs` is the current dependency-injection composition root.

It registers:

- pooled EF Core `FinoraDbContext` factory using app-private `finora.db3`;
- database initialization/migration;
- finance/domain workflow services;
- backup/recovery/file services;
- privacy diagnostics;
- settings/app-lock services;
- native platform notification/biometric/screen-protection adapters;
- reminder coordination.

## `DatabaseInitializer`

Responsibilities:

- open/initialize the local SQLite database;
- apply supported schema migration flow;
- enforce database pragmas/index setup through current infrastructure;
- seed required default/system data where appropriate;
- perform safe derived-state normalization implemented for the current schema.

Do not add UI or platform-specific behavior here.

## `IFinanceStore` / `FinanceStore`

General-purpose finance persistence and workflow surface used by several ViewModels.

Responsibilities include current account/transaction/category/budget/goal/recurrence persistence operations, account summaries, transaction search, same-currency transfer creation, soft delete/restore, and other core store operations defined by the application contract.

Important boundary: linked transfer creation/edit/delete/restore must preserve both halves. Do not use a generic one-row path for a transfer half.

## `IFinanceDataResetService` / `FinanceDataResetService`

Dedicated destructive finance-data reset.

Responsibilities:

- delete supported schema-2 finance data transactionally;
- include user-created categories and finance relationships;
- preserve application-operability metadata/preferences that are intentionally not finance records;
- perform attachment orphan cleanup after the database operation.

UI must use typed destructive confirmation before invoking this service.

## `ISampleDataService` / `SampleDataService`

Developer-only deterministic synthetic dataset reset.

Responsibilities:

- clear finance data through the intended reset path;
- seed predictable synthetic accounts/transactions/transfer/budget/goal/recurrence content;
- restore default/system categories required for sample workflows.

Never treat sample reset as a data-preserving operation.

## `IStorageRecoveryService` / `RestoreRecoveryService`

Startup recovery for interrupted cross-resource restore.

Responsibilities:

- inspect durable restore journal/marker state;
- distinguish pre-database-commit from post-database-commit recovery;
- restore old attachment tree when DB replacement did not commit;
- finalize new attachment tree when DB replacement committed;
- clean stale staging/rollback artifacts only after making the recovery decision;
- enforce no-link/reparse path safety.

Recovery runs before normal finance navigation.

## `ITransactionMaintenanceService` / `TransactionMaintenanceService`

Detailed transaction workflow service.

Responsibilities:

- load transaction detail;
- normal transaction edit with revision history;
- linked transfer edit through pair-safe logic;
- bulk categorization;
- duplicate candidate detection;
- revision history summaries.

Do not log raw revision snapshot JSON.

## `IAccountManagementService` / `AccountManagementService`

Account-detail lifecycle beyond basic store summaries.

Responsibilities include account detail, updates, state/archive/restore operations, balance/history-oriented account behavior, and protection of currency/reconciliation/recurrence invariants defined by the current contracts.

## `ICategoryTagService` / `CategoryTagService`

Category and tag lifecycle.

Responsibilities:

- category hierarchy create/update;
- cycle prevention;
- reorder;
- archive/restore;
- merge/reassign;
- subcategory-budget relationship protection;
- tag lifecycle;
- currency-scoped tag reporting.

## `IReconciliationService` / `ReconciliationService`

Statement reconciliation workflow.

Responsibilities:

- preview book vs statement balance;
- checked difference;
- complete reconciliation;
- optional explicit adjustment transaction;
- reconciliation history.

Reconciliation must never silently hide a difference.

## `IRecurringWorkflowService` / `RecurringWorkflowService`

Occurrence lifecycle for recurring rules.

Responsibilities:

- preview/prepare occurrences;
- pause/resume/archive lifecycle;
- mark paid/partial;
- skip;
- postpone;
- reopen skipped occurrence;
- create exactly one generated finance transaction/pair when payment state requires it;
- revalidate account/category/currency dependencies.

The recurrence model is occurrence-first; scheduler processing alone does not create money movement.

## `ICsvImportService` / `CsvImportService`

Mapped CSV validation/preview/import.

Responsibilities:

- parse bounded UTF-8 CSV input;
- map required/optional columns;
- convert major/minor monetary values safely;
- validate dates/types/currencies/accounts/categories/tags;
- protect against `long.MinValue` and duplicate rows;
- validate transfer counterparties/groups;
- commit accepted rows transactionally.

## `IAdvancedReportService` / `AdvancedReportService`

Currency-aware report engine.

Current report responsibilities include:

- category spending;
- income vs expense;
- account balance trends;
- budget performance;
- merchant/payee;
- monthly comparison;
- yearly comparison;
- recurring obligations;
- savings progress.

Tag reporting remains under category/tag reporting contract.

Use `LocalDateRange` for local-calendar ranges and `BudgetPeriodPolicy` for budget windows. Do not aggregate unlike currencies.

## `IBackupService` / `CrashSafeBackupService`

The registered backup service is the crash-safe wrapper, not the raw crypto serializer alone.

Responsibilities:

- serialize access to backup/restore/recovery operations;
- invoke encrypted backup/restore implementation;
- create durable cross-resource restore journal state;
- snapshot existing attachment tree before destructive restore;
- write/read pending DB marker;
- coordinate startup recovery behavior.

The underlying encrypted format uses PBKDF2-SHA256 and AES-GCM through the existing backup implementation. Do not bypass the crash-safe wrapper from normal app flows.

## `IExportService` / `ExportService`

Local user-triggered export.

Responsibilities:

- CSV generation;
- dependency-free PDF generation;
- supported selected/all transaction export data.

Sharing/saving is an App/OS responsibility after the bytes/file are produced.

## `IAttachmentService` / `AttachmentService`

App-private receipt/document lifecycle.

Responsibilities:

- validate selected receipt/document;
- copy to generated internal path;
- compute/persist size/hash metadata;
- list/open/delete;
- storage usage;
- orphan cleanup;
- path/no-link safety.

## `IDataIntegrityService` / `DataIntegrityService`

Privacy-safe local integrity diagnostics.

Checks include SQLite/foreign-key health plus finance graph relations such as transaction/account currency, transfers, splits, category cycles, budgets/periods, goals/contributions, recurrence, reconciliation, and attachment path/size/hash state.

Output must remain sanitized codes/counts rather than private finance contents.

## `ITemporaryArtifactCleaner` / `TemporaryArtifactCleaner`

Best-effort cache hygiene.

Responsibilities:

- remove only known Finora CSV/PDF/backup/integrity share-copy patterns after grace period;
- preserve fresh files;
- preserve unrelated cache files;
- preserve diagnostic logs;
- avoid recursively following symlink targets.

Failure must not block finance startup.

## `IPrivacyLogger` / `PrivacyLogger`

Bounded privacy-safe diagnostics.

Responsibilities:

- record event token and exception type only for errors;
- omit exception message/stack and arbitrary caller finance properties;
- sanitize/bound event tokens;
- rotate bounded log files;
- reject linked/reparse log paths.

`AsyncCommand.UnexpectedFailureHandler` is wired to this logger in `MauiProgram`.

## `AppExceptionCoordinator`

Coordinates application-level unhandled/unobserved exception reporting through privacy-safe paths. Unobserved task exceptions are marked observed after reporting so a handled diagnostic path does not leave them unobserved.

## `IAppSettingsService` / `MauiAppSettingsService`

MAUI Preferences-backed application settings.

Current settings include default currency/locale, financial month start, privacy/hide amounts, theme, reduced motion, backup reminders, onboarding state, auto-lock, local premium demo, notifications, biometric preference, sensitive-screen protection, receipt quality, larger interface, default account/type, Dashboard card settings, and last-backup timestamp.

Validation/normalization happens on read/write where implemented.

## `IAppLockService` / `MauiAppLockService`

Local PIN verifier lifecycle.

Responsibilities:

- validate 4–12 ASCII-digit PIN;
- PBKDF2-SHA256 verifier with random salt;
- OS SecureStorage read/write/remove;
- fixed-time verification;
- bounded escalating lockout through `PinAttemptPolicy`;
- fail-closed behavior when secure-storage provider is temporarily unavailable and enabled marker exists;
- stale-marker cleanup when secure storage is readable but verifier is absent/corrupt.

## `IPlatformNotificationGateway` / `PlatformNotificationGateway`

Native scheduling abstraction implemented with target-specific APIs/conditional source.

Responsibilities:

- request/check notification capability;
- schedule local reminder;
- cancel existing reminder;
- keep user-visible payload generic.

Native delivery behavior must be validated on each target.

## `ILocalNotificationService` / `LocalNotificationService`

Database-backed local notification lifecycle and deduplication.

Responsibilities:

- persist schedule state;
- schedule replacement safely;
- disable stale DB rows transactionally;
- cancel stale OS schedule after commit;
- reconcile disabled/expired/pending state.

## `IBiometricService` / `PlatformBiometricService`

Optional platform biometric/Windows Hello factor. It is not a replacement for PIN fallback.

Platform/provider error text must be normalized before ordinary user-facing display.

## `ISensitiveScreenService` / `SensitiveScreenService`

Applies platform-supported sensitive-screen/capture protection where available. This is capability-based and does not claim universal screenshot prevention.

## `ReminderCoordinator`

Coordinates higher-level reminder sources such as backup reminders, budget thresholds, and recurring items with local notification persistence/gateway behavior.

## Cross-cutting policies

### Money

Use signed 64-bit integer minor units for stored/calculated money and `decimal` for major-unit parsing/conversion. Use `Money`/`CurrencyMinorUnits`; do not introduce `float`/`double` monetary arithmetic.

### Local dates

Use `LocalDateRange` for inclusive local calendar selections converted to UTC query boundaries.

### Budgets

Use `BudgetPeriodPolicy` instead of creating a second budget-window implementation.

### Errors

Keep deliberate validation messages actionable, but map unexpected storage/database/crypto/provider/path details to generic UI errors and privacy-safe diagnostics.

### Platform code

Keep OS APIs in App/platform adapters. Platform-neutral domain/application/infrastructure should remain testable without MAUI APIs where practical.