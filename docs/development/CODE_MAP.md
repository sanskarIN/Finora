# Finora Repository Code Map

This map identifies the main source/test/documentation areas in the current Finora 0.2.0 repository.

For the exhaustive tracked-file ownership/responsibility inventory, see [Repository File Reference](REPOSITORY_FILE_REFERENCE.md). The code map is intentionally concise for navigation; the file reference is mechanically checked against `git ls-files` so no tracked file can silently sit outside the documented repository map.

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
- `global.json`, `.gitattributes`, `.gitignore` — toolchain and repository hygiene policy.

## `src/Finora.Shared`

Cross-cutting primitives with no MAUI/EF ownership.

Important files include:

- `AppConstants.cs` — product identity, DB filename/schema, backup magic, repository/contact/watermark.
- `CultureSettings.cs` — locale normalization/application.
- `LocalDateRange.cs` — local inclusive calendar range → UTC start/end-exclusive policy.
- `PinAttemptPolicy.cs` — bounded PIN failure/lockout policy.
- `Result.cs` — shared result primitive.

## `src/Finora.Domain`

Finance model and pure rules.

Important areas include:

- `Entities.cs` / `Enums.cs` — persisted/domain finance model and enumerations;
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
- reset/sample data;
- temporary artifacts.

Application should not own native MAUI controls or EF Core queries.

## `src/Finora.Infrastructure`

Platform-neutral persistence/workflow implementations.

Important files/services include:

- `FinoraDbContext.cs` — EF Core mapping and persistence-boundary validation.
- `DatabaseInitializer.cs` — database setup/seed/repair entry.
- `DatabaseMigrationRunner.cs` — versioned migration chain.
- `FinanceStore.cs` — core finance persistence workflows.
- `TransactionHistoryStore.cs` — database-backed history filtering/sorting/paging.
- `TransactionMaintenanceService.cs` — detail/edit/revisions/bulk/duplicates.
- `AccountManagementService.cs` — account lifecycle/details.
- `CategoryTagService.cs` — category/tag lifecycle and tag reports.
- `ReconciliationService.cs` — statement reconciliation.
- `RecurringWorkflowService.cs` — occurrence/payment lifecycle.
- `AdvancedReportService.cs` — report datasets.
- `CsvImportService.cs` — mapped import.
- `ExportService.cs` — CSV/PDF export.
- `AttachmentService.cs` — app-private receipts.
- `PathSafety.cs` — filesystem-path validation for app-private artifacts.
- `BackupService.cs` / `BackupGraphValidator.cs` — encrypted snapshot serializer/restore and graph validation.
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

`Pages/` contains XAML/code-behind for primary and secondary workflows. Some Settings behavior is split into partial files such as reset/sample/security/about/integrity helpers to keep destructive/platform actions isolated.

### Controls/converters

- `Controls/ReportBarChartView.cs` — dependency-free signed bar chart.
- `Converters/PrivacyMoneyConverter.cs` — currency-aware passive amount display/hiding.
- `InverseBoolConverter` — common visibility/enable inversion where used.

### Platform services

- `Services.cs` — MAUI settings + app lock service implementations.
- `PlatformNotificationGateway.cs` — target-specific local notification APIs.
- `PlatformBiometricService.cs` — biometric/Windows Hello integration.
- `SensitiveScreenService.cs` — platform-supported capture protection.
- `ReminderCoordinator.cs` — application-level reminder reconciliation.
- `Services/AppExceptionCoordinator.cs` — privacy-safe global exception coordination.

### Resources

`Resources/` contains colors/styles, paired neutral/Hindi localization resources, icons, optional project-support artwork, privacy/terms/notices raw text, and splash assets used by MAUI.

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

Current contracts cover navigation, Dashboard/reports, Settings, onboarding, transaction paging/sort, signed charts, privacy amount surfaces, localization-sensitive wiring, and security-sensitive app-lock/biometric/capture behavior where source contracts can prove it.

## `tools/Finora.Performance`

Synthetic performance/correctness harness for deterministic large-dataset measurements. It consumes real Application/Infrastructure services, writes machine-readable evidence, and remains outside the packaged app runtime.

## `build/scripts`

- `verify_structure.py` — dependency-free structural/privacy/repository preflight.
- `verify.ps1` — host-aware PowerShell verification.
- `verify.sh` — host-aware shell verification.

## `scripts`

Dependency-light developer/QA tools cover repository QA orchestration, tracked-file documentation coverage, localization validation, synthetic CSV generation, CSV diagnostics, export/backup artifact verification, release readiness, and native Android/Windows smoke helpers. Tool unit tests live under `scripts/tests/`.

The exact tool inventory and usage examples are in `scripts/README.md` and `docs/testing/REPOSITORY_QA.md`.

## `.github`

Repository automation includes staged CI, CodeQL, dependency review, Dependabot, CODEOWNERS, issue templates, PR template/policies, performance tooling, localization/artifact/native harness workflows, sample-data verification, and release readiness.

The primary CI structural preflight runs both `build/scripts/verify_structure.py` and `scripts/run_repo_qa.py`, so dependency-free developer-tool, tracked-file documentation, and localization failures block later CI stages.

## `docs`

- `docs/README.md` — documentation index.
- `docs/USER_GUIDE.md` — end-user guide.
- `docs/DOCUMENTATION_STATUS.md` — documentation status/update policy.
- `docs/development/REPOSITORY_FILE_REFERENCE.md` — exhaustive mechanically checked tracked-file responsibility map.
- `docs/architecture/` — design, schema, services, data flow, navigation/UI.
- `docs/features/` — feature manuals.
- `docs/security/` — threat model/app lock/backup.
- `docs/privacy/` — data lifecycle.
- `docs/operations/` — diagnostics/reset/sample operations.
- `docs/setup/` — build/troubleshooting.
- `docs/development/` — contributor code/change/file-ownership guides.
- `docs/testing/` — QA, CI evidence, performance, security acceptance, native validation guidance.
- `docs/releases/` — release/store/versioning/metadata guidance.
- `docs/platforms/` — target-specific engineering/QA notes.
- remaining focused areas document accessibility, localization, import/export/backup artifact checks, branding, and historical ledger material.

## File-level coverage invariant

Run:

```bash
python scripts/check_documentation_coverage.py
```

The checker compares the tracked repository with `docs/development/REPOSITORY_FILE_REFERENCE.md`. It requires every tracked file to be covered by an exact path or a meaningful narrow directory area and rejects stale or overly broad coverage declarations.

This code map and the exhaustive file reference serve different purposes: this document gets a developer to the right layer quickly; the reference makes repository-wide file ownership complete and mechanically enforceable.

## Where to make a change

- money/domain invariant → Domain + unit tests + persistence/integration coverage if stored;
- new service workflow → Application contract + Infrastructure implementation + DI + tests;
- new persisted entity/field → Domain + DbContext + migration + backup/integrity/reset/docs/tests;
- new page/state → App ViewModel/Page + route if needed + UI-contract tests;
- native API → App platform service + target metadata + native validation docs;
- new user finance data → backup/restore + reset + integrity + privacy lifecycle must be reviewed;
- new passive amount → privacy formatting/hiding must be reviewed;
- new local date filter → `LocalDateRange` policy must be reviewed;
- new budget window → `BudgetPeriodPolicy` must be reviewed;
- new tracked file → place it in the narrowest responsibility area, update the file reference if that area's description is no longer accurate, then run repository QA.
