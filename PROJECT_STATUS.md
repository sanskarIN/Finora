# Finora Project Status

Last source review: **2026-08-15**

Current source line: **Finora 0.2.0 (build 2)**  
Current database schema: **2**  
Repository: https://github.com/sanskarIN/Finora

## Status labels

- ✅ **Implemented in source** — concrete code/resources are present.
- 🧪 **Implemented + automated coverage** — source plus unit/integration/UI-contract coverage exists.
- ✅ **Verified automated evidence** — executed GitHub Actions evidence exists for the exact referenced commit.
- ⚠️ **External validation required** — needs emulator/simulator/device, packaging/signing, recovery-failure injection, accessibility, or store-console evidence beyond a source build.
- 🧭 **Later-version scope** — intentionally outside current local-first release.

Source presence is not the same as native release validation. Current commit-specific automated evidence is retained in `docs/testing/CI_EVIDENCE.md`.

## Verified automated validation — 2026-08-15

✅ Strict source-build candidate `f7dbfbb8691edc79cee559101f284ccd90a44cf7` passed Finora CI run `31872362394` and CodeQL run `31872362398`.

✅ Structural preflight passed.

✅ Exact automated test result: **241/241 passed, 0 failed**:

- Unit: 97/97;
- Integration: 109/109;
- UI-contract: 35/35.

✅ Independent Release source builds passed for:

- Windows `net10.0-windows10.0.19041.0` — unpackaged source validation with `WindowsPackageType=None`;
- Android `net10.0-android`;
- iOS `net10.0-ios` on a GitHub macOS runner;
- Mac Catalyst `net10.0-maccatalyst` on a GitHub macOS runner.

✅ CodeQL analysis completed successfully on the same strict candidate.

✅ `XC0022`, `XC0023`, and `XC0025` are promoted to errors in the app project; the successful native builds therefore provide compiler evidence that the migrated compiled-binding contracts do not retain those warning classes on the tested targets.

✅ CI-only commit `6ba519bf69174c68b67f8595872546a259c783dc` updated the primary workflow to Node-24-compatible current action majors (`checkout@v7`, `setup-python@v7`, `setup-dotnet@v6`, `upload-artifact@v7`). Its follow-up run successfully executed the updated structural path before the documentation sequence superseded it through CI concurrency; the completed repository head receives a fresh run after ledger finalization.

⚠️ These source-build results are not evidence of Windows MSIX signing, signed Android AAB production packaging, Apple provisioning/signing/notarization, physical-device behavior, accessibility QA, process-kill recovery testing, or store approval.

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

🧪 About now exposes the canonical optional Buy Me a Coffee support link through `AppConstants.BuyMeACoffeeUrl`; the action uses the system launcher and generic privacy-safe failure handling.

✅ Buy Me a Coffee is explicitly separated from Finora feature entitlement, premium state, and support priority in UI/docs.

⚠️ Final packaged-store policy compatibility for an external contribution/payment link must be checked against each target store's current rules before submission.

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

✅ Structural preflight requires/wires these resources and guards masked secret fields, complete-reset wiring, biometric provider-text redaction, raw exception-alert regressions, raw minor-unit display labels, the roadmap, and canonical Buy Me a Coffee identity/entitlement boundary.

⚠️ Final merged-manifest/package behavior and device backup/transfer behavior still require Android package/device evidence.

## Accessibility/adaptive UI

✅ Phone/tablet/desktop adaptive navigation source is present.

✅ Theme, larger interface, reduced motion and privacy settings are present.

✅ Settings, lock, onboarding, Dashboard period, reports, transaction history/tools and finance pages include additional heading/semantic descriptions.

✅ Accessibility/localization documentation defines chart/text equivalence, privacy-safe screen-reader behavior, keyboard/focus/native test expectations, runtime locale/currency separation, current English-first/localization-ready state, and the initial Hindi resource boundary.

⚠️ TalkBack/VoiceOver/Narrator/keyboard/large-text/high-contrast testing still requires native validation.

## Documentation

✅ A complete documentation hub exists at `docs/README.md`, with a coverage matrix at `docs/DOCUMENTATION_STATUS.md`.

✅ Current dated GitHub Actions evidence exists at `docs/testing/CI_EVIDENCE.md`.

✅ A prioritized execution roadmap exists at `docs/NEXT_STEPS.md`, split into P0 release blockers, P1 release-candidate work, P2 quality/product polish, and P3 later-version architecture.

✅ End-user documentation covers onboarding through destructive reset, backup/restore, platform limitations, privacy, reports, import/export, notifications, Settings, and support.

✅ Architecture documentation covers solution design, schema, service ownership, end-to-end data flow, adaptive navigation/UI contracts, engineering decisions, and repository code map.

✅ Feature documentation covers accounts/transactions/reconciliation, budgets/goals/recurring, reports/import/export, and Settings.

✅ Security/operations documentation covers threat model, app lock/privacy, encrypted backup/crash recovery, data lifecycle, diagnostics/integrity, and reset/sample data.

✅ Developer/testing documentation covers build/run, troubleshooting, developer workflow, safe feature changes, test-layer selection, native platform validation matrix, and exact CI evidence.

✅ Platform documentation covers Android, Windows, iOS, and Mac Catalyst target frameworks, minimum platform metadata, native APIs, privacy boundaries, accessibility, packaging, and release QA.

✅ Release documentation covers release checklist, store readiness, versioning/migrations/backup compatibility, store metadata preparation, changelog, and project status.

✅ Buy Me a Coffee is documented consistently in the docs hub, Settings reference, support guide, store metadata, roadmap, README and shared source identity.

✅ Dependency-free structural preflight treats every core documentation reference as required and validates repository-relative Markdown file links without network access.

⚠️ External URLs, Markdown anchors, live store policy requirements, package/signing, and native behavior described by platform docs still require external/current validation.

## Repository engineering

✅ Structural verifier, staged CI workflow, Dependabot, CodeQL, dependency review, CODEOWNERS, issue/PR templates and release/security documentation are present.

✅ Structural preflight guards required documentation, local Markdown file links, product/support identity, Android backup exclusions, masked secret inputs, complete-reset wiring, raw minor-unit display, biometric provider text and raw exception-message alerts.

✅ Actual GitHub Actions evidence now exists for structural preflight, 241 automated tests, all four MAUI Release source builds, and CodeQL on strict source candidate `f7dbfbb8691edc79cee559101f284ccd90a44cf7`.

✅ Primary CI action majors were updated to Node-24-compatible releases after the strict source-validation run.

## Current next milestone

The preferred next milestone remains a reproducible Finora 0.2.0 release candidate, but automated source validation is no longer the leading unknown. The next evidence work is the platform/release layer: migration/restore failure injection, native privacy/security behavior, accessibility, packaging/signing, dependency/license review, and store-policy validation.

P0/P1 release evidence should be completed before major P2/P3 feature expansion.

## Native/release validation still required

The 2026-08-15 GitHub Actions evidence closes structural, automated-test, CodeQL, and four-target Release **source-build** uncertainty for the strict candidate. Before store-ready status, still execute and retain evidence for:

1. fresh-install plus schema migration/upgrade using synthetic prior-version data;
2. integrity checks on migrated/restored release-candidate datasets;
3. encrypted backup create/preview/restore plus wrong-password, tamper, invalid-graph, linked-path, and process-interruption recovery paths on target environments;
4. privacy-mode passive display/chart behavior on real native UI;
5. 0/2/3/4-decimal currency precision QA where supported;
6. non-UTC and DST-observing local-calendar/report validation;
7. native notification permission/replacement/lifecycle/cancellation behavior;
8. PIN/app-lock/biometric or Windows Hello/capture behavior on actual supported platform states;
9. Android merged-manifest backup/data-transfer exclusion validation and actual backup/device-transfer behavior;
10. file picker/share/import/export/receipt confinement and restart flows;
11. TalkBack/VoiceOver/Narrator/keyboard/large-text/high-contrast/reduced-motion validation;
12. complete finance-data deletion validation including receipt files and persisted settings boundary;
13. Android signed AAB production packaging;
14. Windows MSIX identity/publisher/signing validation;
15. iOS provisioning/signing/archive validation;
16. Mac Catalyst signing/notarization/distribution validation;
17. exact dependency-license/vulnerability review;
18. current external Buy Me a Coffee contribution-link policy review for each target distribution channel;
19. final privacy/data-safety/store metadata and submission review.

## Intentionally later-version scope

🧭 Finora remote account/login.

🧭 Cloud sync/server API.

🧭 Collaboration/shared finance spaces.

🧭 Server/store-backed commercial entitlement verification.

🧭 Automatic FX/exchange-rate workflow.

🧭 Analytics/advertising telemetry by default.

These are product-boundary decisions, not incomplete source claims for current local-first release.
