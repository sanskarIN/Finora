# Finora Project Status

Last source review: **2026-08-11**

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

🧪 Safe startup repair normalizes only derived `SavingsGoal.IsCompleted` when underlying contribution history validates. Corrupt histories remain untouched for the integrity checker.

🧪 Passive finance displays use currency-aware formatted money instead of showing raw stored minor units. Account/transaction edit surfaces use the currency's actual supported decimal precision rather than a universal two-decimal assumption.

## Accounts and transfers

🧪 Account create/edit/archive/restore/detail/history/reconciliation are present.

🧪 Same-currency transfers use paired rows with zero-sum/reciprocal linkage; generic mutation of transfer halves is blocked.

🧪 Account currency cannot change after finance/recurrence dependencies exist.

🧪 Active recurrence blocks account archival until paused/completed/archived.

🧪 Account list/detail/history monetary display honors privacy/hide-on-launch; credit/opening edit formatting uses currency precision and billing-day UI/domain range is consistently 1–31.

## Transactions

🧪 Expense/income/refund/adjustment quick-add/edit, calculator, advanced filtering, revision history, bulk categorization, duplicate review, splits, tags, receipts, soft-delete/restore, selected/all export, and linked transfer editing are present.

🧪 Direct transaction persistence rejects zero/`long.MinValue`, invalid signs, transfer linkage, currency shape, and inconsistent deletion metadata.

🧪 Transaction history includes deterministic sort choices and a bounded 50-row incremental display with explicit Load more behavior.

🧪 Transaction/history/tools/detail split displays honor privacy and currency formatting; transaction/tool date filters use shared local-calendar boundaries.

## Categories and tags

🧪 Parent/subcategory create/update, cycle prevention, reorder, archive/restore, merge/reassign, tag management and currency-scoped tag reporting are present.

🧪 Category mutations protect subcategory-budget hierarchy semantics.

## Budgets

🧪 Overall/category/subcategory budgets, weekly/monthly/custom cadence, warning thresholds, rollover and explicit periods are present.

🧪 Shared budget-period policy prevents overlap, treats custom budgets as active only within explicit windows, and uses rollover only when enabled.

🧪 Failed explicit-period replacement is covered for transactional rollback.

🧪 Passive budget planned/actual amounts use currency-aware privacy display.

## Savings goals

🧪 Goals, contributions/withdrawals, optional linked transaction, forecasts/milestones and completion state are present.

🧪 Goal history uses checked arithmetic, cannot fall below zero, and linked transaction currency must match the goal.

🧪 New goals initialize completion from starting progress; startup repairs stale derived completion flags from older source behavior when history is valid.

🧪 Goal cards and monthly contribution forecast no longer reveal monetary values while privacy/hide-on-launch is active.

## Recurring items

🧪 Expense/income/transfer/refund rules, due occurrence persistence, paid/partial/skipped/postponed/reopen workflows, generated transaction linkage and reminders are present.

🧪 Pause/resume/archive rule lifecycle is exposed in UI and persisted.

🧪 Resume revalidates active dependencies.

🧪 Generated recurring transaction/pair drift fails closed.

🧪 Paid occurrence may retain a valid historical postponed date; unpaid states cannot silently contain payment data.

🧪 Rule/occurrence monetary displays use each row's own currency and honor privacy/hide-on-launch.

## Dashboard and reports

🧪 Dashboard is configurable/privacy-aware and does not invoke the legacy mixed-currency aggregate API.

🧪 Aggregate dashboard/report/tag values are currency-scoped. Other-currency rows retain own currency and no implicit FX conversion is performed.

🧪 Dashboard has explicit current financial month, previous financial month, trailing 30-day, trailing 90-day, and year-to-date selection through `DashboardPeriodPolicy`.

🧪 Local-calendar date selections use shared `LocalDateRange` conversion to UTC `[from,toExclusive)` boundaries rather than UTC-midnight assumptions.

🧪 Current balance uses direct current account summaries; period-sensitive cards use the selected Dashboard date range.

🧪 Reports include category spending, income/expense, account trend, budget performance, merchant/payee, monthly comparison, yearly comparison, recurring obligations, savings progress and tag data; category/budget reporting is split-aware and descendant-aware.

🧪 Monthly/yearly comparisons group by local calendar and stop at today, excluding future-dated imported rows until their date arrives.

🧪 Signed chart renderer uses a true zero baseline; negative net values render below zero. Quantitative charts are hidden while privacy mode hides amounts, while textual/list monetary values are masked.

## Import/export

🧪 Mapped CSV import with preview/limits/validation/duplicate protection/transfer validation and transactional persistence is present.

🧪 CSV and dependency-free multipage PDF exports are present.

🧪 Generated share copies live in cache; startup best-effort cleanup removes only known Finora export/backup/integrity files older than 24 hours while preserving fresh, unrelated and diagnostic files.

## Attachments and private filesystem safety

🧪 Receipt storage is app-private with MIME/size limits, generated internal names, SHA-256 metadata, list/open/delete/storage usage/orphan cleanup.

🧪 Logical path confinement is supplemented by physical symbolic-link/reparse-point rejection.

🧪 No-link policy is reused by attachment open/write/cleanup, encrypted backup validation/staging, crash-safe restore rollback copy, restore recovery journal/directories and integrity checking.

🧪 Optional symlink regression tests run where host permits link creation.

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

🧪 Android cancellation looks up an existing immutable `PendingIntent` using `NoCreate` instead of creating a pending broadcast only to cancel it.

🧪 Notification text remains privacy-safe/generic.

## App lock and secret entry

🧪 PIN verifier uses PBKDF2, random salt, secure storage, fixed-time comparison and escalating lockout.

🧪 PIN input is bounded to 4–12 ASCII digits before hashing; derived/verifier byte buffers are zeroed where possible.

🧪 Secure-storage provider failure fails closed when explicit enabled marker exists; readable missing/corrupt verifier clears stale marker to avoid permanent lock-screen trap.

🧪 Settings backup password/new PIN/confirm PIN fields are masked and cleared after use. Lock-screen PIN is masked and cleared after attempts.

🧪 PIN removal failure is handled without falsely reporting success.

🧪 Biometric/Windows Hello failure returns stable generic PIN-fallback text rather than raw provider text; Android callback no longer forwards `errString`.

## Settings, onboarding and About

🧪 Onboarding covers local-first/no-account/no-auto-upload behavior, currency, locale, financial-month start, optional opening balance, explicit sample-data opt-in and safe revisit behavior.

🧪 Onboarding exposes Privacy and Terms links with accessible headings/descriptions.

🧪 Settings can revisit onboarding without duplicating opening/sample data when accounts already exist.

🧪 Full local-finance deletion is wired to the dedicated complete reset service and retains typed destructive confirmation.

🧪 About version/build comes from packaged `AppInfo`; attribution, technology summary, repository/profile, business/support contacts, Apache-2.0, notices, privacy/terms, contributing, security and support guide links are exposed.

## Diagnostics and integrity

🧪 Privacy logger ignores arbitrary caller properties and logs event/type tokens only; exception messages/stacks are not serialized.

🧪 Diagnostic current/previous log paths reject symlink/reparse traversal; log is bounded/rotated.

🧪 Bound ViewModel infrastructure errors and primary Settings/Reports alerts avoid raw filesystem/database/crypto/provider text.

🧪 Unexpected `AsyncCommand` failures are contained and routed to privacy logger.

🧪 Integrity checker covers SQLite/foreign keys, transaction/account/currency values, transfers, splits, category hierarchy, budgets, goal histories/completion, recurrence relations/state, reconciliation links and attachment path/size/hash/parent data.

## Android privacy packaging

✅ Android manifest keeps `android:allowBackup="false"` and `android:usesCleartextTraffic="false"`.

✅ Legacy `backup_rules.xml` excludes root/file/database/sharedpref/external domains.

✅ Android 12+ `data_extraction_rules.xml` excludes same domains from cloud backup and device transfer.

✅ Structural preflight requires/wires these resources and guards masked secret fields, complete-reset wiring, biometric provider-text redaction, raw exception-alert regressions, and raw minor-unit display labels.

⚠️ Final merged-manifest/package behavior and device backup/transfer behavior still require Android build/device evidence.

## Accessibility/adaptive UI

✅ Phone/tablet/desktop adaptive navigation source is present.

✅ Theme, larger interface, reduced motion and privacy settings are present.

✅ Settings, lock, onboarding, Dashboard period, reports, transaction history/tools and finance pages include additional heading/semantic descriptions.

⚠️ TalkBack/VoiceOver/Narrator/keyboard/large-text/high-contrast testing still requires native validation.

## Repository engineering

✅ Structural verifier, staged CI workflow, Dependabot, CodeQL, dependency review, CODEOWNERS, issue/PR templates and release/security documentation are present.

✅ Structural preflight guards Android backup exclusions, masked secret inputs, complete-reset wiring, raw minor-unit display, biometric provider text and raw exception-message alerts.

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
8. local-calendar/date/report regression tests;
9. notification replacement/lifecycle/cancellation tests;
10. secret-entry/app-lock/biometric/capture tests;
11. Android merged-manifest backup/data-transfer exclusion validation;
12. Android physical/emulator backup-transfer behavior;
13. receipt symlink/reparse tests where platform permits;
14. privacy-mode passive display/chart tests on native UI;
15. Dashboard period/transaction paging/sort/accessibility smoke tests;
16. signing/package/store validation;
17. final privacy/data-safety/store metadata review.

## Intentionally later-version scope

🧭 Finora remote account/login.

🧭 Cloud sync/server API.

🧭 Collaboration/shared finance spaces.

🧭 Server/store-backed commercial entitlement verification.

🧭 Automatic FX/exchange-rate workflow.

🧭 Analytics/advertising telemetry by default.

These are product-boundary decisions, not incomplete source claims for current local-first release.
