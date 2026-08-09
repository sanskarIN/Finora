# What Changed — Finora

This file is the detailed implementation log requested instead of a long chat response.

## Source prompt

The uploaded `01_Finora_Personal_Finance_Master_Prompt.md` was treated as the implementation source. The GitHub repository initially contained only the Apache-2.0 `LICENSE`.

## Delivery shape

The build is organized as a multi-project .NET MAUI solution so the application UI, domain rules, use-case contracts, SQLite infrastructure, and shared primitives remain separated. The staged implementation is divided conceptually into:

1. **Foundation:** repository policy, architecture, local-first/privacy decisions, design resources, build/release documentation.
2. **Finance core:** normalized entities, integer minor-unit money, SQLite persistence, transactions/transfers, budgets, goals, recurrence, backup/export, privacy logging.
3. **MAUI application:** onboarding, app lock, dashboard, accounts, transactions, budgets, goals, reports, recurring items, settings/developer options, platform bootstraps, localization resources.
4. **Quality gate:** unit/integration/UI-contract tests, GitHub Actions jobs, verification scripts, release checklist and explicit deferred store-release work.

## Added

- `Finora.sln` with Shared, Domain, Application, Infrastructure, MAUI App, UnitTests, IntegrationTests and UiTests projects.
- Domain entities for Account, FinanceTransaction, TransactionSplit, Category, Tag, TransactionTag, Budget, BudgetPeriod, SavingsGoal, GoalContribution, RecurrenceRule, RecurrenceOccurrence, Attachment, AppSetting, AuditEntry and BackupMetadata.
- Signed 64-bit integer minor-unit money model. Floating point is not used to store or calculate monetary values. The only `double` in the finance layer is a non-monetary 0–1 savings progress ratio for UI rendering.
- EF Core SQLite persistence with WAL, foreign keys, busy timeout and relational indexes.
- Accounts, transaction search/persistence, paired same-currency transfers, account archiving, transaction soft-delete/restore and audit entries.
- Categories, budgets, savings goals, recurring rules and idempotent recurrence processing.
- Dashboard/report summaries with textual report output for accessibility.
- CSV export/import preview and dependency-free PDF transaction export.
- AES-GCM encrypted backups with PBKDF2-SHA256 password-derived keys, authenticated metadata and transactional restore.
- Privacy-aware diagnostic logger that redacts sensitive fields.
- PIN hashing, fixed-time comparison, escalating local lockout and configurable inactivity auto-lock, with verifier material in OS secure storage.
- MAUI onboarding, dashboard, transactions, accounts, budgets, goals, reports, recurring and settings screens wired to application services.
- Explicit system file-picker/share-sheet interaction for restores/backups/exports; no automatic upload path exists.
- Local premium demo flag clearly documented as non-secure and not suitable for commercial entitlement enforcement.
- Hidden developer panel unlocked by repeated version taps, including database schema and feature-flag visibility.
- Original editable SVG app icon, foreground icon and splash source.
- English resource file plus initial Hindi localization resource structure.
- Android, iOS, Mac Catalyst and Windows platform bootstrap files.
- README, privacy/security/support/terms/contributing/code-of-conduct, architecture docs, ADRs, database schema docs, threat model, test plan, release checklist and troubleshooting/setup guides.
- GitHub issue templates, pull-request template and Windows/macOS CI jobs.
- Unit tests for decimal-safe money/domain rules/transaction signs.
- SQLite integration tests for transfer conservation and recurrence idempotency.
- UI route/privacy/recovery contract test scaffold.

## Local structural verification performed in this ChatGPT environment

The following checks were executed against the staged repository:

- XML/XAML/project/RESX parsing: **passed**.
- Every `ProjectReference` points to an existing project: **passed**.
- Every XAML `x:Class` has a matching C# partial class: **passed**.
- Every XAML `Clicked`/`Tapped` handler has a matching C# method: **passed**.
- Empty implementation file scan: **passed**.
- `TODO`, `FIXME`, `NotImplementedException` and `NotSupportedException` scan: **passed**.

## GitHub commit identity limitation

Requested commit email: `sanskarin@outlook.in`.

The GitHub connector available to ChatGPT does not expose author/committer-name or author/committer-email parameters for `create_commit`, so this session cannot force that email into connector-created commit metadata. For local commits, configure it with:

```bash
git config user.email "sanskarin@outlook.in"
```

## Build/test limitation

The execution container reports `dotnet: command not found`. Therefore restore, C# compilation, MAUI workload compilation and `dotnet test` could not be executed locally in this session. The repository includes GitHub Actions and verification scripts so compiler/platform failures are visible instead of being hidden. A successful CI/device run is required before calling this a release candidate.

## Compatibility limitation

Live web access is disabled in this session. Additional chart, notification and biometric third-party packages were intentionally not guessed. This avoids pinning unverified package versions. `PROJECT_STATUS.md` records the platform/store work that remains before a production store release.

## Commit message

- `feat: implement Finora local-first personal finance application`
