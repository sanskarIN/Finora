# Finora Repository File Reference

This document is the repository-level ownership and responsibility map for the current Finora 0.2.0 (build 2), database schema 2 source line.

It complements the feature, architecture, platform, security, privacy, testing, and release manuals by answering a different question: **where does every tracked file belong, what responsibility does that file or narrowly scoped directory have, and which validation boundary applies to it?**

## Coverage contract

Coverage is enforced by `scripts/check_documentation_coverage.py`.

The first column of each inventory table below is a coverage declaration:

- a path without a trailing slash covers exactly that tracked file;
- a path ending in `/` covers every tracked file under that narrow repository area;
- broad one-component prefixes such as `src/`, `docs/`, `tests/`, `.github/`, or `scripts/` are rejected by the checker;
- every path returned by `git ls-files` must be covered by at least one declaration;
- every declaration must cover at least one tracked file, preventing dead/stale inventory entries.

Run:

```bash
python scripts/check_documentation_coverage.py
```

The check documents **repository ownership**, not runtime correctness. Native behavior, store signing, device accessibility, biometric prompts, notification delivery, installed upgrades, and interrupted restore still require the evidence described by the dedicated testing/release documents.

---

## Repository root

| File or area | Responsibility | Change/validation notes |
|---|---|---|
| `.editorconfig` | Shared editor/analyzer formatting conventions. | Keep compatible with C#, XAML, XML, Markdown, YAML, and Python contributors. |
| `.gitattributes` | Git text/binary normalization rules. | Review when adding generated/binary asset families. |
| `.gitignore` | Prevents generated output, local databases/backups, IDE state, signing material, and other non-source artifacts from being tracked. | Release-readiness checks reinforce high-risk exclusions. |
| `CHANGELOG.md` | Human-readable source-line change history. | Record only implemented/merged work; do not claim unexecuted native evidence. |
| `CODE_OF_CONDUCT.md` | Community behavior policy. | Governance document. |
| `CONTRIBUTING.md` | Contributor setup, workflow, quality, privacy, and change expectations. | Keep aligned with CI and documentation update rules. |
| `DECISIONS.md` | Repository-wide product/architecture decisions and boundaries. | Changes that alter local-first/privacy/security/product scope should update this first or in the same workstream. |
| `Directory.Build.props` | Shared .NET compiler/analyzer/deterministic-build and dependency-audit policy. | Affects all .NET projects. |
| `Directory.Packages.props` | Central NuGet package versions. | Dependency changes require build/test/security review. |
| `Finora.sln` | Solution project membership. | Keep source, tests, and tools intentionally represented. |
| `LICENSE` | Apache-2.0 project license. | Legal source of truth for repository licensing. |
| `PRIVACY.md` | Public privacy policy for the local-first source line. | Must match actual data collection/storage/network behavior. |
| `PROJECT_STATUS.md` | Current implementation/evidence status. | Distinguish source implementation from external/native/store evidence. |
| `README.md` | Public project overview, support link, capabilities, build entry points, and status. | First public documentation surface. |
| `SECURITY.md` | Security reporting and supported-version policy. | Security contact is separate from optional project support. |
| `SUPPORT.md` | Product/community support routes and boundaries. | Keep optional contribution links non-entitling. |
| `TERMS.md` | Public terms for the current application/repository scope. | Must remain consistent with privacy/product behavior. |
| `THIRD_PARTY_NOTICES.md` | Human-readable third-party attribution/notices. | Re-audit against release dependencies before distribution. |
| `global.json` | Supported .NET SDK family and roll-forward behavior. | Toolchain compatibility input for local/CI builds. |
| `what_changed.md` | Detailed active continuation ledger. | Append factual work/evidence; archived history lives under `docs/history/`. |

### Root dependency direction

The root project files do not own finance behavior. They define build, governance, legal, release-history, and repository-wide policy. If an application change requires editing a root file, the corresponding source/test/docs change should remain the primary implementation and the root change should explain or support it.

---

## GitHub repository automation and governance

| File or area | Responsibility | Change/validation notes |
|---|---|---|
| `.github/CODEOWNERS` | Review ownership hints for sensitive repository areas. | Update when ownership-sensitive paths change. |
| `.github/FUNDING.yml` | Canonical optional Buy Me a Coffee funding metadata. | Funding is optional and must not imply entitlement or support priority. |
| `.github/dependabot.yml` | Automated dependency update policy. | Keep ecosystems/directories consistent with actual manifests. |
| `.github/pull_request_template.md` | PR quality/privacy/testing/documentation checklist. | Should reflect current required validation. |
| `.github/ISSUE_TEMPLATE/` | Bug, feature, and issue-template configuration. | Must avoid prompting users to publish sensitive finance data. |
| `.github/workflows/` | CI, CodeQL, dependency review, performance, localization, artifact verification, native-harness, sample-data, and release-readiness workflows. | Workflow action majors and permissions are guarded by structural/release checks. |

### Workflow evidence rule

A workflow file describes automation. It is not proof that a specific commit passed. Commit/run-specific evidence belongs in `docs/testing/CI_EVIDENCE.md` and must name the exact source candidate and observed conclusion.

---

## Build preflight

| File or area | Responsibility | Change/validation notes |
|---|---|---|
| `build/scripts/` | Dependency-free repository structure/privacy/XAML/XML/project-reference preflight plus host wrapper scripts. | Runs before expensive .NET/native jobs and must remain safe on clean clones. |

`verify_structure.py` owns repository-shape invariants such as required documents, Markdown relative links, XML/XAML parseability, placeholder/debt checks, project references, XAML handler wiring, selected finance/privacy invariants, and other source-contract preflight. `verify.ps1` and `verify.sh` provide host-friendly entry points without replacing CI.

---

## Documentation tree

### Top-level manuals

| File or area | Responsibility | Change/validation notes |
|---|---|---|
| `docs/DOCUMENTATION_STATUS.md` | Documentation coverage matrix and update policy. | Update when a new canonical manual/coverage class is introduced. |
| `docs/FINAL_HARDENING_2026-08-19.md` | Post-closure correctness, restore-safety, receipt-consistency, regression, evidence-boundary, and branch-governance audit. | Keep exact completed work separate from queued/native/store evidence. |
| `docs/FINAL_REPOSITORY_CLOSURE.md` | Repository-engineering closure boundary versus external release evidence. | Never reinterpret external/native work as already complete. |
| `docs/NEXT_STEPS.md` | Prioritized release-evidence and later-version roadmap. | Current source backlog and external gates must stay distinct. |
| `docs/README.md` | Documentation entry point/index. | New canonical manuals should be linked here. |
| `docs/TEST_PLAN.md` | End-to-end test strategy and release validation expectations. | Keep aligned with actual suites/workflows/native gates. |
| `docs/USER_GUIDE.md` | End-user workflows for implemented features. | Do not document speculative features as available. |

### Focused documentation areas

| File or area | Responsibility | Change/validation notes |
|---|---|---|
| `docs/accessibility/` | Accessibility/localization behavior and native accessibility QA. | Source contracts do not replace TalkBack/VoiceOver/Narrator/manual evidence. |
| `docs/architecture/` | Layering, schema, services, data flow, navigation/UI, and ADRs. | Persisted-model/service-boundary changes must update this area. |
| `docs/backup/` | Encrypted backup artifact verification procedure. | Artifact structure checks do not decrypt or expose user data. |
| `docs/branding/` | Asset identity/source/usage guidance. | Keep app icon/splash/support assets traceable. |
| `docs/development/` | Developer guide, code map, feature-change procedure, and this tracked-file reference. | This area owns repository navigation and change-impact guidance. |
| `docs/export/` | CSV/PDF export artifact verification. | Verification should avoid echoing private exported contents. |
| `docs/features/` | Feature manuals for finance workflows, settings, reports/import/export, and project support. | Follow implemented behavior only. |
| `docs/history/` | Immutable/archived continuation ledger history. | Preserve historical text; append current work to active ledger instead. |
| `docs/import/` | CSV diagnostics and import-specific operational guidance. | Keep parsing/diagnostic behavior privacy-safe. |
| `docs/localization/` | Resource-bundle implementation and validation guidance. | Neutral/Hindi key and placeholder parity are automated. |
| `docs/operations/` | Integrity diagnostics, reset, and deterministic sample-data operations. | Destructive workflows require explicit confirmation/recovery boundaries. |
| `docs/platforms/` | Android, Windows, iOS, and Mac Catalyst platform notes. | Package/signing/device claims require target-platform evidence. |
| `docs/privacy/` | Data lifecycle and retention/deletion boundaries. | New user-data classes must be added here before release. |
| `docs/releases/` | Release checklist, store readiness, metadata, versioning, and migrations. | Store policy/signing evidence must be current for the target release. |
| `docs/security/` | Threat model, app lock/privacy, and backup/recovery security. | Security-sensitive source changes require tests and native validation impact review. |
| `docs/setup/` | Build instructions and troubleshooting. | Keep commands/toolchain versions aligned with repository manifests. |
| `docs/testing/` | CI evidence, repository QA, performance, native UI/accessibility validation, security acceptance, sample data, and release readiness. | Clearly label automated, structural, synthetic, native, and external evidence classes. |

---

## Developer/QA scripts

| File or area | Responsibility | Change/validation notes |
|---|---|---|
| `scripts/README.md` | Entry point for developer and QA tooling. | Add every supported script and privacy boundary. |
| `scripts/android_ui_smoke.py` | Android ADB/UIAutomator accessibility/launch smoke helper. | Avoid persistent screenshots/full hierarchy dumps by default. |
| `scripts/check_documentation_coverage.py` | Ensures all `git ls-files` paths remain represented by this reference. | Runs without third-party Python packages. |
| `scripts/check_release_readiness.py` | Structural release-readiness guard for required files, workflows, artifacts, debt markers, and repository hygiene. | Not a substitute for signed/native/store evidence. |
| `scripts/diagnose_finora_csv.py` | Privacy-safe CSV structural diagnostics. | Do not print transaction values or write to production DBs. |
| `scripts/generate_sample_finance_csv.py` | Deterministic synthetic finance fixture generation. | Synthetic/disposable data only. |
| `scripts/run_repo_qa.py` | One-command dependency-free QA orchestrator with optional .NET suite. | Should include repository-wide Python validation gates. |
| `scripts/validate_localization.py` | Localization bundle/reference/key/placeholder parity validation. | Neutral/Hindi resources are kept structurally synchronized. |
| `scripts/verify_backup_artifact.py` | Encrypted-backup envelope/metadata/digest verification. | Never requests password/decrypts financial contents. |
| `scripts/verify_export_artifact.py` | CSV/PDF export structure verification. | Reports structure rather than private finance content. |
| `scripts/windows_ui_smoke.ps1` | Windows UI Automation smoke helper. | Native runtime evidence remains platform-specific. |
| `scripts/tests/` | Unit tests for every dependency-light developer/QA tool, including documentation coverage. | Executed through Python `unittest discover`. |

---

## `src/Finora.Shared`

| File or area | Responsibility | Change/validation notes |
|---|---|---|
| `src/Finora.Shared/` | Cross-cutting primitives with no MAUI/EF ownership: product constants, culture policy, local date ranges, PIN-attempt policy, and generic result value. | Keep dependencies minimal and cover pure policy with unit tests. |

Important files:

- `AppConstants.cs` — product identity, repository/support metadata, database/schema identifiers, backup magic, and attribution constants.
- `CultureSettings.cs` — supported culture normalization/application.
- `LocalDateRange.cs` — local inclusive calendar selection converted to UTC start/end-exclusive boundaries.
- `PinAttemptPolicy.cs` — bounded local PIN failure/lockout policy.
- `Result.cs` — small shared success/failure result primitive.
- `Finora.Shared.csproj` — project definition.

---

## `src/Finora.Domain`

| File or area | Responsibility | Change/validation notes |
|---|---|---|
| `src/Finora.Domain/` | Pure finance entities, enums, money/currency precision, validation rules, budget periods, and Dashboard period policy. | Business invariants should start here when they do not require persistence/native APIs. |

File responsibilities:

- `Entities.cs` — persisted/domain finance entities and relationships.
- `Enums.cs` — finance-domain enumerations.
- `Money.cs` — integer-minor-unit/decimal-major conversion and formatting behavior.
- `CurrencyMinorUnits.cs` — known currency decimal precision policy.
- `DomainRules.cs` — account, transaction, budget, goal, recurrence, and metadata validation.
- `BudgetPeriodPolicy.cs` — budget time-window/effective plan policy.
- `DashboardPeriodPolicy.cs` — Dashboard activity-period calculation policy.
- `Finora.Domain.csproj` — project definition.

Changing a persisted field also requires infrastructure mapping/migration, backup graph, reset/integrity, tests, schema docs, privacy lifecycle, and release migration review.

---

## `src/Finora.Application`

| File or area | Responsibility | Change/validation notes |
|---|---|---|
| `src/Finora.Application/` | Contracts/DTOs and platform-neutral helpers between presentation and infrastructure. | Must not own MAUI controls or EF query implementations. |

Current contract files cover account management, attachments, categories/tags, finance store/settings/backup core contracts, data reset, import, integrity, notifications, reconciliation, recovery, recurring workflows, reporting, sample data, security, temporary artifacts, transaction maintenance, and transaction construction. `DecimalCalculator.cs` provides decimal calculation support at the application boundary and `Finora.Application.csproj` defines project dependencies.

When adding a service capability, update its contract here before or with the infrastructure implementation, DI registration, calling ViewModel/page, tests, and docs.

---

## `src/Finora.Infrastructure`

| File or area | Responsibility | Change/validation notes |
|---|---|---|
| `src/Finora.Infrastructure/` | SQLite/EF persistence and platform-neutral implementations for finance, import/export, backup/recovery, integrity, notifications, recurrence, logging, path safety, reset, and sample data. | Integration tests are the primary automated boundary; native file-picker/share APIs remain in App. |

Key implementation ownership:

- `FinoraDbContext.cs` — EF mapping and persistence-boundary validation.
- `DatabaseInitializer.cs` — database setup/seed/repair entry point.
- `DatabaseMigrationRunner.cs` — ordered schema migration/version guards.
- `FinanceStore.cs` — core finance persistence/query workflows.
- `TransactionHistoryStore.cs`, `TransactionMaintenanceService.cs`, `TransactionRevisionSerializer.cs` — history/edit/revision/bulk/duplicate behavior.
- `AccountManagementService.cs`, `CategoryTagService.cs`, `ReconciliationService.cs`, `RecurringWorkflowService.cs` — focused finance workflows.
- `AdvancedReportService.cs` — report datasets.
- `CsvImportService.cs`, `ExportService.cs` — import/export transformation and validation.
- `AttachmentService.cs`, `PathSafety.cs` — app-private receipt lifecycle and safe path handling.
- `BackupService.cs`, `BackupGraphValidator.cs`, `CrashSafeBackupService.cs`, `RestoreRecoveryJournal.cs`, `RestoreRecoveryService.cs` — encrypted backup graph, restore, durable recovery, and failure containment.
- `DataIntegrityService.cs`, `FinanceDataResetService.cs`, `SampleDataService.cs` — diagnostics, complete deletion, deterministic sample/reset behavior.
- `LocalNotificationService.cs` — persisted reminder scheduling/dedupe/reconciliation state.
- `PrivacyLogger.cs`, `TemporaryArtifactCleaner.cs` — sanitized diagnostics and managed temporary-file cleanup.
- `Finora.Infrastructure.csproj` — EF/SQLite and project references.

Infrastructure code must not silently upload finance data, invent FX conversion, bypass domain currency/minor-unit rules, or turn local demo/support state into commercial entitlement.

---

## `src/Finora.App` composition and platform integration

### Root application files

| File or area | Responsibility | Change/validation notes |
|---|---|---|
| `src/Finora.App/App.xaml` | Global MAUI resource composition. | Keep shared resource dictionaries valid. |
| `src/Finora.App/App.xaml.cs` | Application startup/lifecycle, lock/privacy/recovery coordination. | Security lifecycle changes require source-contract and native review. |
| `src/Finora.App/AppShell.xaml` | Primary/adaptive Shell hierarchy. | Navigation/accessibility contract surface. |
| `src/Finora.App/AppShell.xaml.cs` | Route registration and Shell behavior. | Keep routes aligned with `Navigation/AppRoutes.cs`. |
| `src/Finora.App/Finora.App.csproj` | MAUI multi-target project, app metadata/resources/packages. | Changes affect Android/Windows/iOS/Mac Catalyst builds. |
| `src/Finora.App/LocalizationResources.cs` | Runtime localization lookup/bridge. | Resource-key validation protects callers. |
| `src/Finora.App/MauiProgram.cs` | Dependency injection and app/service composition. | New contracts/implementations must be registered intentionally. |
| `src/Finora.App/PlatformBiometricService.cs` | Android biometrics, Apple LocalAuthentication, Windows Hello abstraction. | Availability/auth failures must keep safe PIN fallback; native evidence required. |
| `src/Finora.App/PlatformNotificationGateway.cs` | Target-specific local notification APIs. | Permission/delivery behavior requires native validation. |
| `src/Finora.App/ReminderCoordinator.cs` | App-level reminder reconciliation/coordinator. | Keep idempotent scheduling boundaries. |
| `src/Finora.App/SensitiveScreenService.cs` | Best-effort supported-platform capture protection. | Never claim universal screenshot prevention. |
| `src/Finora.App/ServiceHelper.cs` | Small service-resolution/helper glue. | Keep composition concerns out of domain layers. |
| `src/Finora.App/Services.cs` | MAUI settings/app-lock and related app service implementations. | Secure-storage/security changes require regression tests. |

### App subareas

| File or area | Responsibility | Change/validation notes |
|---|---|---|
| `src/Finora.App/Controls/` | Reusable presentation controls, currently including signed report chart rendering. | Accessibility/privacy amount behavior must remain explicit. |
| `src/Finora.App/Converters/` | XAML converters such as privacy-aware money display and inverse booleans. | Passive amount display must respect privacy/currency precision. |
| `src/Finora.App/Navigation/` | Adaptive route/root-selection policy. | Keep Shell and source-contract tests aligned. |
| `src/Finora.App/Pages/` | XAML pages and code-behind/partial files for Dashboard, accounts, transactions, budgets, savings, recurring, reports, import, reconciliation, settings, onboarding, lock, legal, and tools/detail flows. | UI changes need localization, accessibility/privacy, ViewModel wiring, and UI-contract review. |
| `src/Finora.App/Platforms/` | Android, Windows, iOS, and Mac Catalyst startup metadata/manifests/resources. | Permissions, backup policy, package metadata, signing, and native APIs are release-sensitive. |
| `src/Finora.App/Resources/` | App icons, optional BMC artwork, privacy/terms/notices raw text, splash assets, neutral/Hindi RESX bundles, colors, and styles. | Asset/localization parity is validated structurally; store asset rendering remains release QA. |
| `src/Finora.App/Services/` | Focused application-only coordinators such as global exception coordination. | Avoid logging sensitive finance data. |
| `src/Finora.App/ViewModels/` | Presentation state/commands for every current user workflow plus `ViewModelBase`. | Service calls, cancellation/busy/error behavior, privacy, and source-contract tests should remain synchronized. |

### Settings partial-file ownership

`SettingsPage.xaml` is intentionally split from high-risk actions: About/support/legal behavior, integrity diagnostics, complete reset, sample-data reset, and security configuration live in focused partial C# files under `Pages/`. This keeps destructive/security-sensitive code reviewable without hiding it in one large code-behind file.

### Resource ownership

The app resources include:

- adaptive/foreground/monochrome app icon SVGs;
- `bmc_support.svg` for optional external project support presentation;
- privacy/terms/third-party notices embedded as raw application-readable text;
- light/dark splash SVGs;
- paired neutral/Hindi resource bundles for app shell, accounts/details, budgets, categories/tags, import, recurrence, reports, savings, settings, transactions, and transaction workflows;
- shared colors/styles.

Buy Me a Coffee is an optional contribution link only. It does not create entitlement, premium state, licensing, security response priority, or product-support priority.

---

## Automated test projects

| File or area | Responsibility | Change/validation notes |
|---|---|---|
| `tests/Finora.UnitTests/` | Pure/shared/domain/helper/ViewModel-base tests. | Fast invariant coverage; no real user data/native runtime required. |
| `tests/Finora.IntegrationTests/` | SQLite/service/import/export/backup/recovery/integrity/migration/notification/recurrence and persistence tests using isolated synthetic data. | Primary automated persistence/workflow boundary. |
| `tests/Finora.UiTests/` | Source/XAML contract tests for navigation, privacy, localization-sensitive wiring, accessibility-sensitive surfaces, app lock/biometrics/capture protection, and user workflow structure. | These are not full native device automation. |

### Test-to-source change rule

A feature change should normally update the closest existing suite instead of adding a duplicate test project. Native-only behavior that source contracts cannot prove belongs in the native validation matrix/security acceptance/accessibility QA documents and in actual target-platform evidence.

---

## Performance tool

| File or area | Responsibility | Change/validation notes |
|---|---|---|
| `tools/Finora.Performance/` | Deterministic synthetic performance/correctness harness, options, fixtures, result records, and runner. | Timing is observational; correctness/integrity failure is not replaced by arbitrary time thresholds. |

The performance tool must use synthetic data and must keep dataset shape/operation names/output format aligned with `docs/testing/PERFORMANCE_BENCHMARKING.md`. Bounded CI smoke evidence is distinct from the optional full comparable 10k/50k/100k profile.

---

## Cross-file change impact map

### Persisted finance model change

Review/update Domain entities/rules, `FinoraDbContext`, migration runner, finance/service implementations, backup graph/restore, integrity/reset/sample data, unit/integration tests, schema/data-flow/service docs, privacy/threat model, migration/release docs, project status/changelog/ledger as applicable.

### New UI page or presentation state

Review/update App Shell/routes, page XAML/code-behind, ViewModel, localization resources, accessibility/privacy behavior, UI-contract tests, user/feature guide, navigation/UI architecture, and native validation matrix when the page uses native APIs.

### Security/app-lock change

Review/update Application security contracts, App secure-storage/lock/biometric/capture services, startup/activation lifecycle, lock ViewModel/page, unit/UI source contracts, threat model, app-lock guide, security acceptance, and native validation.

### Import/export/backup file-format change

Review/update Application contracts, infrastructure transformer/service, path safety, artifact validators/scripts/tests, integration tests, user guide, data flow, security/privacy docs, and version/release compatibility policy.

### New user-data class

Review/update backup/restore, complete reset, integrity diagnostics, sample data behavior, privacy lifecycle, threat model, any platform backup/extraction policy, migration/versioning, and release declarations before calling the feature complete.

### New dependency or toolchain major

Review/update central package/build policy, lock/restore/build/test evidence, third-party notices where required, Dependabot/release-readiness constraints, setup docs, CI evidence, and release notes.

---

## Maintainer rule for new files

When adding or deleting a tracked file:

1. place it in the narrowest existing responsibility area;
2. update this reference when its purpose is not already accurately described by that area;
3. run `python scripts/check_documentation_coverage.py`;
4. run `python scripts/run_repo_qa.py`;
5. run `python build/scripts/verify_structure.py`;
6. run relevant .NET/native validation for the affected code;
7. update user/architecture/security/privacy/release documentation in the same workstream when behavior changed;
8. record only evidence actually executed for the exact candidate.

The coverage checker prevents a tracked file from being completely absent from this repository map, while the focused manuals remain the authoritative source for detailed behavioral contracts.