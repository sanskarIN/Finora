# Finora Project Status

Last source review: **2026-08-10**

Current source line: **Finora 0.2.0 (build 2)**  
Current database schema: **2**  
Repository: https://github.com/sanskarIN/Finora

## Status labels

- ✅ **Implemented in source** — concrete code/resources are present.
- 🧪 **Implemented + automated coverage** — source plus unit/integration/UI-contract coverage exists.
- ⚠️ **External validation required** — needs .NET/MAUI workloads, CI runner, emulator/simulator/device, signing, or store-console evidence.
- 🧭 **Later-version scope** — intentionally outside current local-first release.

Source presence is not the same as native release validation.

## Architecture

✅ Projects/layers:

- `Finora.Shared`
- `Finora.Domain`
- `Finora.Application`
- `Finora.Infrastructure`
- `Finora.App`
- `Finora.UnitTests`
- `Finora.IntegrationTests`
- `Finora.UiTests`

✅ Dependency direction remains App → Application/Infrastructure → Domain → Shared.

## Persistence and money safety

✅ SQLite/EF Core local persistence with WAL, foreign keys, busy timeout, indexes and schema versioning.

🧪 Money is signed 64-bit minor units; major-unit conversion uses `decimal` with currency-aware precision.

🧪 Domain/EF persistence boundary now validates Added/Modified schema-v2 entities, including:

- accounts;
- transactions and deletion-state/timestamp agreement;
- splits;
- categories/tags/transaction-tag links;
- budgets/periods;
- goals/contributions;
- recurrence rules/occurrences;
- attachment metadata;
- transaction revisions;
- reconciliations;
- notification schedules;
- app settings;
- audit entries;
- backup metadata.

🧪 Safe startup repair normalizes only derived `SavingsGoal.IsCompleted` when the underlying contribution history validates. Corrupt histories remain untouched for the integrity checker.

## Accounts and transfers

🧪 Account create/edit/archive/restore/detail/history/reconciliation are present.

🧪 Same-currency transfers use paired rows with zero-sum/reciprocal linkage; generic mutation of transfer halves is blocked.

🧪 Account currency cannot change after finance/recurrence dependencies exist.

🧪 Active recurrence blocks account archival until paused/completed/archived.

## Transactions

🧪 Expense/income/refund/adjustment quick-add/edit, calculator, advanced filtering, revision history, bulk categorization, duplicate review, splits, tags, receipts, soft-delete/restore, selected/all export, and linked transfer editing are present.

🧪 Direct transaction persistence rejects zero/`long.MinValue`, invalid signs, transfer linkage, currency shape, and inconsistent deletion metadata.

## Categories and tags

🧪 Parent/subcategory create/update, cycle prevention, reorder, archive/restore, merge/reassign, tag management and currency-scoped tag reporting are present.

🧪 Category mutations protect subcategory-budget hierarchy semantics.

## Budgets

🧪 Overall/category/subcategory budgets, weekly/monthly/custom cadence, warning thresholds, rollover and explicit periods are present.

🧪 Shared budget-period policy prevents overlap, treats custom budgets as active only within explicit windows, and uses rollover only when enabled.

🧪 Failed explicit-period replacement is covered for transactional rollback.

## Savings goals

🧪 Goals, contributions/withdrawals, optional linked transaction, forecasts/milestones and completion state are present.

🧪 Goal history uses checked arithmetic, cannot fall below zero, and linked transaction currency must match the goal.

🧪 New goals initialize completion from starting progress; startup repairs stale derived completion flags from older source behavior when history is valid.

## Recurring items

🧪 Expense/income/transfer/refund rules, due occurrence persistence, paid/partial/skipped/postponed/reopen workflows, generated transaction linkage and reminders are present.

🧪 Pause/resume/archive rule lifecycle is exposed in UI and persisted.

🧪 Resume revalidates active dependencies.

🧪 Generated recurring transaction/pair drift fails closed.

🧪 Paid occurrence may retain a valid historical postponed date; unpaid states cannot silently contain payment data.

## Dashboard and reports

🧪 Dashboard is configurable/privacy-aware and does not invoke the legacy mixed-currency aggregate API.

🧪 Aggregate dashboard/report/tag values are currency-scoped. Other-currency rows retain own currency and no implicit FX conversion is performed.

🧪 Reports include category spending, income/expense, account trend, budget performance, merchant/payee, monthly comparison and tag data; category/budget reporting is split-aware and descendant-aware.

## Import/export

🧪 Mapped CSV import with preview/limits/validation/duplicate protection/transfer validation and transactional persistence is present.

🧪 CSV and dependency-free multipage PDF exports are present.

🧪 Generated share copies live in cache; startup best-effort cleanup removes only known Finora export/backup/integrity files older than 24 hours while preserving fresh, unrelated and diagnostic files.

## Attachments and private filesystem safety

🧪 Receipt storage is app-private with MIME/size limits, generated internal names, SHA-256 metadata, list/open/delete/storage usage/orphan cleanup.

🧪 Logical path confinement is supplemented by physical symbolic-link/reparse-point rejection.

🧪 No-link policy is reused by attachment open/write/cleanup, encrypted backup validation/staging, crash-safe restore rollback copy, restore recovery journal/directories and integrity checking.

🧪 Optional symlink regression tests run where the host permits link creation.

## Backup and restore

🧪 User-triggered encrypted backup uses PBKDF2-SHA256 + AES-GCM and current schema snapshot/receipt bytes.

🧪 Backup creation and authenticated preview/restore validate financial graph plus schema-v2 metadata before destructive replacement.

🧪 Receipt/plaintext buffers are cleared as early as managed-memory APIs permit on success and failure paths, including accumulated receipt buffers if a later file/query/validation step fails.

🧪 Crash-safe wrapper persists recovery journal/commit marker and can restore/finalize receipt tree after interrupted restore.

🧪 Internal restore settings are not imported from backup snapshots.

## Notifications

🧪 Local permission-gated notifications with Android/Apple/Windows platform gateways are present.

🧪 Deduplicated reminder replacement is failure-safe: schedule new OS reminder first, persist replacement/disable-old second, cancel stale OS reminder after DB commit.

🧪 Failed replacement leaves old enabled reminder untouched; cancellation drift and expired schedules are reconciled best-effort.

🧪 Notification text remains privacy-safe/generic.

## App lock and secret entry

🧪 PIN verifier uses PBKDF2, random salt, secure storage, fixed-time comparison and escalating lockout.

🧪 PIN input is bounded to 4–12 ASCII digits before hashing; derived/verifier byte buffers are zeroed where possible.

🧪 Secure-storage provider failure fails closed when the explicit enabled marker exists; readable missing/corrupt verifier clears stale marker to avoid permanent lock-screen trap.

🧪 Settings backup password/new PIN/confirm PIN fields are masked and cleared after use. Lock-screen PIN is masked and cleared after attempts.

🧪 PIN removal failure is handled without falsely reporting success.

🧪 Biometric/Windows Hello failure returns stable generic PIN-fallback text rather than raw provider text.

## Diagnostics and integrity

🧪 Privacy logger ignores arbitrary caller properties and logs event/type tokens only; exception messages/stacks are not serialized.

🧪 Diagnostic current/previous log paths reject symlink/reparse traversal; log is bounded/rotated.

🧪 Bound ViewModel infrastructure errors and primary Settings/Reports alerts avoid raw filesystem/database/crypto/provider text.

🧪 Unexpected `AsyncCommand` failures are contained and routed to the privacy logger.

🧪 Integrity checker now covers SQLite/foreign keys, transaction/account/currency values, transfers, splits, category hierarchy, budgets, goal histories/completion, recurrence relations/state, reconciliation links and attachment path/size/hash/parent data.

## Android privacy packaging

✅ Android manifest keeps `android:allowBackup="false"` and `android:usesCleartextTraffic="false"`.

✅ Legacy `backup_rules.xml` excludes root/file/database/sharedpref/external domains.

✅ Android 12+ `data_extraction_rules.xml` excludes same domains from cloud backup and device transfer.

✅ Structural preflight requires/wires these resources and guards masked secret fields/raw exception-alert regressions.

⚠️ Final merged-manifest/package behavior and device backup/transfer behavior still require Android build/device evidence.

## Accessibility/adaptive UI

✅ Phone/tablet/desktop adaptive navigation source is present.

✅ Theme, larger interface, reduced motion and privacy settings are present.

✅ Settings and lock screen now include additional heading/semantic descriptions; lock/PIN/biometric controls are screen-reader described.

⚠️ TalkBack/VoiceOver/Narrator/keyboard/large-text/high-contrast testing still requires native validation.

## Repository engineering

✅ Structural verifier, staged CI workflow, Dependabot, CodeQL, dependency review, CODEOWNERS, issue/PR templates and release/security documentation are present.

✅ Structural preflight now additionally guards Android backup exclusions, masked secret inputs and raw exception-message alerts.

⚠️ CI/check-run success must be confirmed from actual GitHub Actions evidence. An empty classic combined-status response is not a passing result.

## Native/release validation still required

⚠️ This execution environment does not provide a usable .NET/MAUI toolchain, so no local claim is made that restore/build/test/native compilation passed here.

Before store-ready status, execute and retain evidence for:

1. structural preflight;
2. NuGet/workload restore;
3. formatting/analyzers;
4. Release core build/tests;
5. Android + Windows MAUI builds;
6. iOS + Mac Catalyst builds on macOS/Xcode host;
7. migration/integrity/backup failure-path tests;
8. notification replacement/lifecycle tests;
9. secret-entry/app-lock/biometric/capture tests;
10. Android merged-manifest backup/data-transfer exclusion validation;
11. Android physical/emulator backup-transfer behavior;
12. receipt symlink/reparse tests where platform permits;
13. accessibility/adaptive/native device smoke tests;
14. signing/package/store validation;
15. final privacy/data-safety/store metadata review.

## Intentionally later-version scope

🧭 Finora remote account/login.

🧭 Cloud sync/server API.

🧭 Collaboration/shared finance spaces.

🧭 Server/store-backed commercial entitlement verification.

🧭 Automatic FX/exchange-rate workflow.

🧭 Analytics/advertising telemetry by default.

These are product-boundary decisions, not incomplete source claims for the current local-first release.
