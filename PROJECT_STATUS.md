# Finora Project Status

Last source review: **2026-08-18**

Current source line: **Finora 0.2.0 (build 2)**  
Current database schema: **2**  
Repository: https://github.com/sanskarIN/Finora

## Status labels

- ✅ **Implemented in source** — concrete code/resources are present.
- 🧪 **Implemented + automated coverage** — source plus unit/integration/UI-contract coverage exists.
- ✅ **Verified automated evidence** — executed GitHub Actions evidence exists for the exact referenced commit.
- ⚠️ **External validation required** — needs emulator/simulator/device, packaging/signing, recovery-failure injection, accessibility, store-console, or explicitly unexecuted large-performance-profile evidence beyond a source build.
- 🧭 **Later-version scope** — intentionally outside current local-first release.

Source presence is not the same as native release validation. Current commit-specific automated evidence is retained in `docs/testing/CI_EVIDENCE.md`; performance methodology/evidence boundaries are retained in `docs/testing/PERFORMANCE_BENCHMARKING.md`.

## Verified automated validation — 2026-08-18

✅ Current release-hardening source candidate `8a8e7e51a2bacecdc58405d3d5301e79f3d78c8b` passed Finora CI run `32127759802`, CodeQL run `32127759687`, and Dependency Review run `32127759673`.

✅ Structural preflight passed.

✅ Exact automated test result: **319/319 passed, 0 failed, 0 skipped**:

- Unit: 102/102;
- Integration: 179/179;
- UI-contract: 38/38.

✅ Independent Release source builds passed for:

- Windows `net10.0-windows10.0.19041.0` — unpackaged source validation with `WindowsPackageType=None`;
- Android `net10.0-android`;
- iOS `net10.0-ios` on a GitHub macOS runner;
- Mac Catalyst `net10.0-maccatalyst` on a GitHub macOS runner.

✅ CodeQL and Dependency Review completed successfully on the same exact source candidate.

✅ The new `tools/Finora.Performance` project compiled in Release with **0 warnings and 0 errors** under the repository warnings-as-errors policy.

✅ The normal CI performance smoke seeded **10,000 synthetic transactions** and executed startup, database-backed history paging/search/sort, long-range reports, and a full integrity scan successfully. Retained JSON evidence is artifact `9321290557`, SHA-256 `97eb07bf963491e8d89d45798b21aa99d0da312b931c3ea25b17e2dae5accb46`.

✅ The harness also implements CSV export/import round-trip measurement, PDF export, encrypted backup creation/restoration, managed/process memory observations, configurable iterations, and an on-demand 10k/50k/100k workflow. Those heavier paths compile in the exact verified candidate.

⚠️ The recorded normal CI smoke deliberately did **not** execute the complete `--operations all` profile, CSV/PDF/backup runtime measurements, or 50k/100k profiles. Those remain explicit on-demand evidence tasks; compile-only support is not represented as executed benchmark evidence.

✅ Interactive transaction history continues to use database-backed 50-row paging through `ITransactionHistoryStore`, with search/filter/sort/count applied in SQLite/EF Core before materialization, soft-deleted rows excluded before count/page, deterministic page boundaries for a fixed result set, total match count/`HasMore`, and a stable last-applied-query snapshot for **Load more**.

✅ Paging integration coverage proves a 120-row history is returned as 50/50/20 pages without duplicate/missing IDs for a fixed result set, preserves all supported filters/sorts/search fields, rejects invalid offsets/page sizes/date ranges, and excludes soft-deleted rows. UI-contract coverage rejects regression back to `_allMatches` in-memory history slicing.

✅ The current candidate retains the earlier strict XAML compiled-binding, migration-safety, hostile-backup, receipt-checksum, deliberate-integrity-corruption, privacy-log synchronization, reset-safety, linked restore-recovery, currency-precision, and local-calendar correctness coverage.

✅ Representative 0-, 2-, 3-, and 4-decimal currency classes are exercised with JPY, INR, KWD, and CLF through conversion/import/export/report/account/budget/savings/recurring/reconciliation/encrypted-backup workflows with exact minor-unit assertions.

✅ `FinanceStore` budget and legacy Dashboard date windows use the shared local-calendar `[from,toExclusive)` conversion instead of UTC-midnight assumptions. Automated store coverage includes UTC+05:30, UTC-07:00, and deterministic DST boundaries, while the shared `LocalDateRange` unit suite includes UTC, positive/negative offsets, DST start/end, multi-day and reversed-range behavior.

✅ Migration production code validates the target schema and SQLite foreign-key/integrity state before advancing `schema.version`; automated coverage includes fresh initialization/reopen, schema-version guards, v1→v2 data preservation/idempotence, malformed-target rollback, and legacy foreign-key corruption rejection.

✅ Backup validation requires valid 32-byte receipt SHA-256 metadata rather than accepting missing checksum metadata; authenticated hostile-payload tests cover unsupported schema, semantic relationship corruption, receipt path escape, receipt size/hash drift, wrong/tampered/truncated encrypted inputs, and exact multi-precision money restoration.

✅ Integrity regression coverage directly injects split-total drift, account/transaction currency mismatch, missing/changed receipts, invalid checksum metadata, category cycles, and foreign-key violations in addition to the pre-existing integrity families.

✅ Restore-recovery tests directly prove fail-closed behavior for linked recovery journals and linked rollback copies while preserving live receipt state/recovery evidence.

⚠️ These source-build results are not evidence of Windows MSIX signing, signed Android AAB production packaging, Apple provisioning/signing/notarization, physical-device behavior, accessibility QA, actual process-kill/low-disk recovery testing, installed prior-version upgrades on every target, full 10k/50k/100k `all` benchmark execution, or store approval.

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

✅ Developer/performance tooling:

- `Finora.Performance` — standalone synthetic large-dataset performance/correctness harness in `tools/`.

✅ Dependency direction remains App → Application/Infrastructure → Domain → Shared. The performance tool consumes production Application/Infrastructure contracts but is not part of the packaged app runtime.

## Persistence and money safety

✅ SQLite/EF Core local persistence with WAL, foreign keys, busy timeout, indexes and schema versioning.

🧪 Money is signed 64-bit minor units; major-unit conversion uses `decimal` with currency-aware precision.

🧪 Automated money coverage spans representative 0-decimal JPY, 2-decimal INR, 3-decimal KWD, and 4-decimal CLF behavior, including half-unit rounding and exact minor-unit preservation across finance workflows and portable data paths.

🧪 Domain/EF persistence boundary validates Added/Modified schema-v2 entities, including:

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

🧪 Account balance and reconciliation regression tests carry exact JPY/INR/KWD/CLF minor units through opening balances, adjustments, previews, and final balances.

## Transactions

🧪 Expense/income/refund/adjustment quick-add/edit, calculator, advanced filtering, revision history, bulk categorization, duplicate review, splits, tags, receipts, soft-delete/restore, selected/all export, and linked transfer editing are present.

🧪 Direct transaction persistence rejects zero/`long.MinValue`, invalid signs, transfer linkage, currency shape, and inconsistent deletion metadata.

✅ Interactive transaction history uses database-backed 50-row paging through `ITransactionHistoryStore`, with search/filter/sort/count applied in SQLite, deterministic page boundaries for a fixed result set, soft-delete exclusion, total match count/`HasMore`, and a stable last-applied-query snapshot for **Load more**.

🧪 Paging coverage verifies 120-row 50/50/20 boundaries with no duplicate/missing IDs for a fixed result set, filter-before-count/page behavior, all five sort modes, invalid boundary rejection, soft-delete exclusion, and free-text matching across merchant/note/payment method/location/account/category fields.

🧪 Transaction/history/tools/detail split displays honor privacy and currency formatting; transaction/tool date filters use shared local-calendar boundaries.

✅ The synthetic 10k performance smoke exercises first-page, deep-page, broad/selective search, and amount-sort history queries against the production `ITransactionHistoryStore` without materializing the entire matching set in the ViewModel.

## Categories and tags

🧪 Parent/subcategory create/update, cycle prevention, reorder, archive/restore, merge/reassign, tag management and currency-scoped tag reporting are present.

🧪 Category mutations protect subcategory-budget hierarchy semantics.

## Budgets

🧪 Overall/category/subcategory budgets, weekly/monthly/custom cadence, warning thresholds, rollover and explicit periods are present.

🧪 Shared budget-period policy prevents overlap, treats custom budgets as active only within explicit windows, and uses rollover only when enabled.

🧪 `FinanceStore.GetBudgetsAsync` resolves each local budget period through shared `LocalDateRange` and filters UTC persistence using an exclusive end boundary; regression coverage proves the positive non-hour UTC+05:30 boundary instead of assuming UTC midnight.

🧪 Exact planned/actual values are covered across JPY/INR/KWD/CLF representative precision classes.

🧪 Failed explicit-period replacement is covered for transactional rollback.

🧪 Passive budget planned/actual amounts use currency-aware privacy display.

✅ The 10k performance smoke exercises budget-performance reporting as an observational workload while retaining all finance-correctness tests as the source of truth.

## Savings goals

🧪 Goals, contributions/withdrawals, optional linked transaction, forecasts/milestones and completion state are present.

🧪 Goal history uses checked arithmetic, cannot fall below zero, and linked transaction currency must match the goal.

🧪 New goals initialize completion from starting progress; startup repairs stale derived completion flags from older source behavior when history is valid.

🧪 JPY/INR/KWD/CLF savings regressions assert target, starting/contribution/current values and progress without decimal-to-minor drift.

🧪 Goal cards and monthly contribution forecast no longer reveal monetary values while privacy/hide-on-launch is active.

## Recurring items

🧪 Expense/income/transfer/refund rules, due occurrence persistence, paid/partial/skipped/postponed/reopen workflows, generated transaction linkage and reminders are present.

🧪 Pause/resume/archive rule lifecycle is exposed in UI and persisted.

🧪 Resume revalidates active dependencies.

🧪 Generated recurring transaction/pair drift fails closed.

🧪 JPY/INR/KWD/CLF recurring regressions preserve exact rule/occurrence amounts and generated paid-transaction minor units.

🧪 Paid occurrence may retain a valid historical postponed date; unpaid states cannot silently contain payment data.

🧪 Rule/occurrence monetary displays use each row's own currency and honor privacy/hide-on-launch.

## Dashboard and reports

🧪 Dashboard is configurable/privacy-aware and does not invoke the legacy mixed-currency aggregate API.

🧪 Aggregate dashboard/report/tag values are currency-scoped. Other-currency rows retain own currency and no implicit FX conversion is performed.

🧪 Dashboard has explicit current financial month, previous financial month, trailing 30-day, trailing 90-day, and year-to-date selection through `DashboardPeriodPolicy`.

🧪 Local-calendar date selections use shared `LocalDateRange` conversion to UTC `[from,toExclusive)` boundaries rather than UTC-midnight assumptions.

🧪 The legacy `FinanceStore.GetDashboardAsync` path also uses that shared local boundary, with deterministic UTC+05:30, UTC-07:00, and DST-start integration regressions.

🧪 Current balance uses direct current account summaries; period-sensitive cards use the selected Dashboard date range.

🧪 Reports include category spending, income/expense, account trend, budget performance, merchant/payee, monthly comparison, yearly comparison, recurring obligations, savings progress and tag data; category/budget reporting is split-aware and descendant-aware.

🧪 Income/expense reporting has explicit exact-minor-unit regression coverage for JPY/INR/KWD/CLF precision classes.

🧪 Monthly/yearly comparisons group by local calendar and stop at today, excluding future-dated imported rows until their date arrives.

🧪 Signed chart renderer uses a true zero baseline; negative net values render below zero. Quantitative charts are hidden while privacy mode hides amounts, while textual/list monetary values are masked.

✅ The 10k synthetic smoke executes income/expense, category, merchant, account-trend, budget, recurring, and savings report families and retains the observed result artifact.

## Import/export

🧪 Mapped CSV import with preview/limits/validation/duplicate protection/transfer validation and transactional persistence is present.

🧪 Major-unit CSV import has explicit JPY/INR/KWD/CLF precision regression coverage.

🧪 CSV export is verified to preserve exact stored `AmountMinor`; exported data is previewed and imported into a second SQLite database with exact minor-unit equality across the four precision classes.

🧪 CSV and dependency-free multipage PDF exports are present.

🧪 Generated share copies live in cache; startup best-effort cleanup removes only known Finora export/backup/integrity files older than 24 hours while preserving fresh, unrelated and diagnostic files.

✅ Performance tooling includes full CSV export, isolated CSV import, and PDF export measurements. CSV benchmark selection refuses datasets above the production 100,000-row import ceiling.

⚠️ The exact verified normal CI smoke compiled those benchmark paths but did not execute them; use the on-demand `all` workflow for runtime evidence.

## Attachments and private filesystem safety

🧪 Receipt storage is app-private with MIME/size limits, generated internal names, required SHA-256 metadata, list/open/delete/storage usage/orphan cleanup.

🧪 Logical path confinement is supplemented by physical symbolic-link/reparse-point rejection.

🧪 No-link policy is reused by attachment open/write/cleanup, encrypted backup validation/staging, crash-safe restore rollback copy, restore recovery journal/directories and integrity checking.

🧪 Optional symlink regression tests run where host permits link creation; linked recovery journal and rollback-copy failure paths are directly covered.

✅ Performance fixtures can create bounded synthetic receipt files with matching SHA-256 metadata; no user receipt files are read by the harness.

## Backup and restore

🧪 User-triggered encrypted backup uses PBKDF2-SHA256 + AES-GCM and current schema snapshot/receipt bytes.

🧪 Backup creation and authenticated preview/restore validate financial graph plus schema-v2 metadata before destructive replacement.

🧪 Receipt checksum metadata is mandatory for portable backup state; creation, preview, and restore reject missing/invalid metadata and verify receipt bytes against SHA-256.

🧪 Hostile-input regression coverage includes wrong password, ciphertext tamper, truncation, authenticated unsupported schema, authenticated relation corruption, receipt path escape, receipt size drift, and receipt hash drift.

🧪 Encrypted backup precision regression writes JPY/INR/KWD/CLF finance rows, previews the encrypted archive, completely resets finance data, restores the archive, checks exact restored minor values/account relations, and completes a healthy integrity check.

🧪 Receipt/plaintext buffers are cleared as early as managed-memory APIs permit on success and failure paths, including accumulated receipt buffers if a later file/query/validation step fails.

🧪 Crash-safe wrapper persists recovery journal/commit marker and can restore/finalize receipt tree after interrupted restore.

🧪 Recovery fails closed on linked journal/rollback state rather than following unsafe filesystem targets.

🧪 Internal restore settings are not imported from backup snapshots.

✅ Performance tooling includes encrypted backup creation and restoration with expected transaction/attachment count verification.

⚠️ The exact current CI smoke compiles but does not execute that performance operation. Real process termination, low-disk, locked-file, native filesystem recovery injection, and the on-demand full benchmark remain separate evidence tasks.

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

🧪 Reset regression coverage proves finance data is removed while unrelated app settings are preserved.

🧪 About version/build comes from packaged `AppInfo`; attribution, technology summary, repository/profile, business/support contacts, Apache-2.0, notices, privacy/terms, contributing, security and support guide links are exposed.

🧪 About exposes the canonical optional Buy Me a Coffee support link through `AppConstants.BuyMeACoffeeUrl`; the action uses the system launcher and generic privacy-safe failure handling.

✅ Buy Me a Coffee is explicitly separated from Finora feature entitlement, premium state, and support priority in UI/docs.

⚠️ Final packaged-store policy compatibility for an external contribution/payment link must be checked against each target store's current rules before submission.

## Diagnostics and integrity

🧪 Privacy logger ignores arbitrary caller properties and logs event/type tokens only; exception messages/stacks are not serialized.

🧪 Diagnostic current/previous log paths reject symlink/reparse traversal; log is bounded/rotated. Rotation regression assertions synchronize with completed writes rather than racing the asynchronous logger.

🧪 Bound ViewModel infrastructure errors and primary Settings/Reports alerts avoid raw filesystem/database/crypto/provider text.

🧪 Unexpected `AsyncCommand` failures are contained and routed to privacy logger.

🧪 Integrity checker covers SQLite/foreign keys, transaction/account/currency values, transfers, splits, category hierarchy, budgets, goal histories/completion, recurrence relations/state, reconciliation links and attachment path/size/hash/parent data.

🧪 Deliberate-corruption tests directly verify split-total, account-currency, receipt file/size/hash/checksum, category-cycle, and foreign-key issue detection without changing production data to make diagnostics pass.

✅ The 10k synthetic CI performance smoke executes the full integrity service and fails the benchmark if the synthetic graph is unhealthy.

## Android privacy packaging

✅ Android manifest keeps `android:allowBackup="false"` and `android:usesCleartextTraffic="false"`.

✅ Legacy `backup_rules.xml` excludes root/file/database/sharedpref/external domains.

✅ Android 12+ `data_extraction_rules.xml` excludes same domains from cloud backup and device transfer.

✅ Structural preflight requires/wires these resources and guards masked secret fields, complete-reset wiring, biometric provider-text redaction, raw exception-alert regressions, raw minor-unit display labels, the roadmap, and canonical Buy Me a Coffee identity/entitlement boundary.

⚠️ Final merged-manifest/package behavior and device backup/transfer behavior still require Android package/device evidence.

## Accessibility/adaptive UI

✅ Phone/tablet/desktop adaptive navigation source is present.

✅ Theme, larger interface, reduced motion and privacy settings are present.

✅ Settings, lock, onboarding, Dashboard period, reports, transaction history/tools and finance pages include additional heading/semantic descriptions.

✅ Accessibility/localization documentation defines chart/text equivalence, privacy-safe screen-reader behavior, keyboard/focus/native test expectations, runtime locale/currency separation, current English-first/localization-ready state, and the initial Hindi resource boundary.

⚠️ TalkBack/VoiceOver/Narrator/keyboard/large-text/high-contrast testing still requires native validation.

## Performance and large-dataset tooling

✅ `tools/Finora.Performance` is wired into the solution as a standalone non-packaged developer tool.

✅ Synthetic dataset seeding is batched and supports the documented 10k/50k/100k comparison sizes without reading real user finance data.

✅ Supported operations include startup, history, reports, CSV export/import, PDF export, encrypted backup create/restore, and integrity checking.

✅ JSON output records dataset/runtime/runner metadata, elapsed times, memory observations, output sizes/item counts, and evidence-policy notes.

✅ `.github/workflows/ci.yml` includes a bounded 10k correctness/performance smoke.

✅ `.github/workflows/performance.yml` provides an on-demand 10k/50k/100k workflow with selectable operations/iterations and retained JSON artifacts.

✅ `docs/testing/PERFORMANCE_BENCHMARKING.md` documents execution, interpretation, correctness checks, benchmark hygiene, and release boundaries.

✅ Exact current 10k bounded smoke evidence is recorded in both the performance guide and `docs/testing/CI_EVIDENCE.md`.

⚠️ Full `all` profile runtime evidence and 50k/100k comparison artifacts are still unexecuted in the recorded current evidence set.

## Documentation

✅ A complete documentation hub exists at `docs/README.md`, with a coverage matrix at `docs/DOCUMENTATION_STATUS.md`.

✅ Current dated GitHub Actions evidence exists at `docs/testing/CI_EVIDENCE.md` and records the **319-test + 10k bounded performance-smoke** source candidate while retaining earlier paging, precision/calendar, migration/backup/integrity/recovery history.

✅ Performance methodology and exact bounded-smoke evidence are documented at `docs/testing/PERFORMANCE_BENCHMARKING.md`.

✅ A prioritized execution roadmap exists at `docs/NEXT_STEPS.md`, split into P0 release blockers, P1 release-candidate work, P2 quality/product polish, and P3 later-version architecture.

✅ End-user documentation covers onboarding through destructive reset, backup/restore, platform limitations, privacy, reports, import/export, notifications, Settings, and support.

✅ Architecture documentation covers solution design, schema, service ownership, end-to-end data flow, adaptive navigation/UI contracts, engineering decisions, and repository code map.

✅ Feature documentation covers accounts/transactions/reconciliation, budgets/goals/recurring, reports/import/export, and Settings.

✅ Security/operations documentation covers threat model, app lock/privacy, encrypted backup/crash recovery, data lifecycle, diagnostics/integrity, and reset/sample data.

✅ Developer/testing documentation covers build/run, troubleshooting, developer workflow, safe feature changes, test-layer selection, native platform validation matrix, exact CI evidence, and performance benchmarking.

✅ Platform documentation covers Android, Windows, iOS, and Mac Catalyst target frameworks, minimum platform metadata, native APIs, privacy boundaries, accessibility, packaging, and release QA.

✅ Release documentation covers release checklist, store readiness, versioning/migrations/backup compatibility, store metadata preparation, changelog, and project status.

✅ Buy Me a Coffee is documented consistently in the docs hub, Settings reference, support guide, store metadata, roadmap, README and shared source identity.