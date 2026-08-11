# Finora Repository Code Map

This map identifies the main source/test/documentation areas in the current Finora 0.2.0 repository.

## Root

- `Finora.sln` — solution project list.
- `Directory.Build.props` — shared compiler/analyzer/deterministic-build defaults.
- `Directory.Packages.props` — central package versions.
- `.editorconfig` — coding/editor conventions.
- `README.md` — public project overview.
- `DECISIONS.md` — architecture decisions that should not silently drift.
- `PROJECT_STATUS.md` — implemented vs externally validated status.
- `CHANGELOG.md` — release/continuation change log.
- `what_changed.md` — detailed implementation ledger.
- `PRIVACY.md`, `TERMS.md`, `SECURITY.md`, `SUPPORT.md`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `THIRD_PARTY_NOTICES.md`, `LICENSE` — project/legal/community policy.

## `src/Finora.Shared`

Cross-cutting primitives with no MAUI/EF ownership.

Important files include:

- `AppConstants.cs` — product identity, DB filename/schema, backup magic, repository/contact/watermark.
- `CultureSettings.cs` — locale normalization/application.
- `LocalDateRange.cs` — local inclusive calendar range → UTC start/end-exclusive policy.
- `PinAttemptPolicy.cs` — bounded PIN failure/lockout policy.

## `src/Finora.Domain`

Finance model and pure rules.

Important areas include:

- entities/enums in domain model files;
- `Money.cs` — integer-minor/decimal-major conversion and formatting;
- `CurrencyMinorUnits.cs` — known currency decimal precision;
- `DomainRules.cs` — account/transaction/budget/goal/recurrence/metadata validation;
- `BudgetPeriodPolicy.cs` — shared budget window/effective plan behavior;
- `DashboardPeriodPolicy.cs` — Dashboard activity period resolution.

New financial invariants should normally begin here when they are pure/domain-wide.

## `src/Finora.Application`

Contracts/DTOs between presentation and infrastructure.

Current contract families include:

- finance store;
- account management;
- transaction maintenance;
- reconciliation;
- category/tag management;
- recurring workflow;
- reporting;
- CSV import;
- export;
- attachments;
- backup/recovery;
- settings/app lock;
- notifications;
- biometrics/sensitive screen;
- diagnostics/integrity;
- reset/sample data.

Application should not own native MAUI controls or EF Core queries.

## `src/Finora.Infrastructure`

Platform-neutral persistence/workflow implementations.

Important files/services include:

- `FinoraDbContext.cs` — EF Core mapping and persistence-boundary validation.
- `DatabaseInitializer.cs` — database setup/seed/repair entry.
- `DatabaseMigrationRunner.cs` — versioned migration chain.
- `FinanceStore.cs` — core finance persistence workflows.
- `TransactionMaintenanceService.cs` — detail/edit/revisions/bulk/duplicates.
- `AccountManagementService.cs` — account lifecycle/details.
- `CategoryTagService.cs` — category/tag lifecycle and tag reports.
- `ReconciliationService.cs` — statement reconciliation.
- `RecurringWorkflowService.cs` — occurrence/payment lifecycle.
- `AdvancedReportService.cs` — report datasets.
- `CsvImportService.cs` — mapped import.
- `ExportService.cs` — CSV/PDF export.
- `AttachmentService.cs` — app-private receipts.
- `BackupService.cs` — encrypted snapshot serializer/restore.
- `CrashSafeBackupService.cs` — registered backup wrapper/recovery coordination.
- `RestoreRecoveryJournal.cs` / `RestoreRecoveryService.cs` — durable cross-resource recovery.
- `DataIntegrityService.cs` — privacy-safe stored-data diagnostics.
- `FinanceDataResetService.cs` — complete finance deletion.
- `SampleDataService.cs` — deterministic synthetic reset.
- `PrivacyLogger.cs` — bounded sanitized logging.
- `TemporaryArtifactCleaner.cs` — managed cache cleanup.
- `LocalNotificationService.cs` — persisted notification dedupe/reconciliation.

## `src/Finora.App`

MAUI presentation and platform integration.

### Composition/startup

- `MauiProgram.cs` — dependency injection.
- `App.xaml` / `App.xaml.cs` — global resources/application lifecycle.
- `AppShell.xaml` / `AppShell.xaml.cs` — primary/adaptive Shell hierarchy and secondary route registration.
- `Navigation/AppRoutes.cs` — adaptive root selection.

### ViewModels

`ViewModels/` contains presentation state/commands for Dashboard, accounts, transactions, budgets, savings, recurring, reports, Settings, import/tools/details, and related flows.

`ViewModelBase.cs` contains common property/busy/error/async-command behavior.

### Pages

`Pages/` contains XAML/code-behind for primary and secondary workflows. Some Settings behavior is split into partial files such as reset/sample/security/about helpers to keep destructive/platform actions isolated.

### Controls/converters

- `Controls/ReportBarChartView.cs` — dependency-free signed bar chart.
- `Converters/PrivacyMoneyConverter.cs` — currency-aware passive amount display/hiding.
- `InverseBoolConverter` — common visibility/enable inversion where used.

### Platform services

- `Services.cs` — MAUI settings + app lock service implementations.
- `PlatformNotificationGateway.cs` — target-specific local notification APIs.
- `PlatformBiometricService.cs` — biometric/Windows Hello integration.
- sensitive-screen service implementation — platform-supported capture protection.

### Resources

`Resources/` contains colors/styles, localization resources, icons, splash assets, fonts/branding resources used by MAUI.

### Platform manifests/resources

`Platforms/Android`, `Platforms/iOS`, `Platforms/MacCatalyst`, `Platforms/Windows` contain platform metadata/permissions/package resources.

Android privacy-critical resources include `backup_rules.xml` and `data_extraction_rules.xml`.

## `tests/Finora.UnitTests`

Pure/domain/helper/ViewModel-base tests. Examples include:

- money precision;
- domain rules;
- budget/Dashboard period policies;
- local date ranges;
- culture helpers;
- PIN attempt policy;
- `ViewModelBase`/`AsyncCommand` behavior.

## `tests/Finora.IntegrationTests`

SQLite/service/backup/import/integrity tests using isolated synthetic data.

Current coverage areas include finance store/transfers, reset/sample, persistence invariants, reports, backup/recovery, CSV import, integrity regressions, recurrence transitions, notifications, categories/tags/budgets/goals/reconciliation, diagnostics/temp files, and migrations.

## `tests/Finora.UiTests`

Source/UI contract tests, not full native automation.

Current contracts cover navigation, Dashboard/reports, Settings, onboarding, transaction paging/sort, signed charts, privacy amount surfaces, and XAML/ViewModel wiring.

## `build/scripts`

- `verify_structure.py` — dependency-free structural/privacy/repository preflight.
- `verify.ps1` — host-aware PowerShell verification.
- `verify.sh` — host-aware shell verification.

## `.github`

Repository automation includes staged CI, CodeQL, dependency review, Dependabot, CODEOWNERS, issue templates, and PR template/policies.

## `docs`

- `docs/README.md` — documentation index.
- `docs/USER_GUIDE.md` — end-user guide.
- `docs/architecture/` — design, schema, services, data flow, navigation/UI.
- `docs/features/` — feature manuals.
- `docs/security/` — threat model/app lock/backup.
- `docs/privacy/` — data lifecycle.
- `docs/operations/` — diagnostics/reset/sample operations.
- `docs/setup/` — build/troubleshooting.
- `docs/development/` — contributor code/change guides.
- `docs/testing/` — testing/native validation guidance.
- `docs/releases/` — release/store/versioning/metadata guidance.
- `docs/platforms/` — target-specific engineering/QA notes.

## Where to make a change

- money/domain invariant → Domain + unit tests + persistence/integration coverage if stored;
- new service workflow → Application contract + Infrastructure implementation + DI + tests;
- new persisted entity/field → Domain + DbContext + migration + backup/integrity/reset/docs/tests;
- new page/state → App ViewModel/Page + route if needed + UI-contract tests;
- native API → App platform service + target metadata + native validation docs;
- new user finance data → backup/restore + reset + integrity + privacy lifecycle must be reviewed;
- new passive amount → privacy formatting/hiding must be reviewed;
- new local date filter → `LocalDateRange` policy must be reviewed;
- new budget window → `BudgetPeriodPolicy` must be reviewed.