# Finora Release Checklist

Use this checklist for every candidate. Do not mark a box complete from source inspection alone when it requires a compiler, platform SDK, emulator/simulator, physical device, signing service, or store console.

The prioritized execution order is maintained in `docs/NEXT_STEPS.md`. Resolve P0 release blockers and P1 release-candidate requirements before treating P2/P3 expansion as release-critical.

## Source and repository

- [ ] Release commit is on the intended branch/tag.
- [ ] Working tree used for release contains no uncommitted/generated private files.
- [ ] `python build/scripts/verify_structure.py` passes.
- [ ] Structural preflight rejects malformed XML/XAML/project wiring, raw minor-unit display labels, unmasked Settings secret fields, raw exception alerts, complete-reset handler drift, Android backup-rule drift, raw Android biometric-provider text, missing roadmap documentation, and Buy Me a Coffee identity/entitlement-boundary drift.
- [ ] `docs/NEXT_STEPS.md` reflects the current release blockers, release-candidate work, quality backlog, and later-version boundaries.
- [ ] CodeQL/dependency-review findings have been reviewed.
- [ ] Dependabot/security alerts have been reviewed.
- [ ] No API keys, keystores, certificates, passwords, PINs, backup passwords, private keys, real financial databases, or real receipt files are present.
- [ ] `README.md`, `CHANGELOG.md`, `PROJECT_STATUS.md`, `what_changed.md`, privacy/security/support docs, and third-party notices match source.

## Dependencies and licenses

- [ ] Restore exact dependency graph with release SDK/workloads.
- [ ] Review direct and transitive dependency licenses.
- [ ] Review known vulnerabilities and incompatible/deprecated dependencies.
- [ ] Update `THIRD_PARTY_NOTICES.md` when verified graph requires it.
- [ ] Do not add a dependency only to satisfy a cosmetic feature if a maintained platform/API implementation already exists.

## Build and analysis

- [ ] `dotnet workload restore` succeeds on platform build hosts.
- [ ] Core/test projects restore on intended .NET 10 SDK.
- [ ] Release build passes with warnings-as-errors.
- [ ] Unit tests pass.
- [ ] Integration tests pass.
- [ ] UI-contract tests pass.
- [ ] Platform MAUI builds pass on appropriate hosts.
- [ ] CI structural/core/Windows+Android/Apple jobs have actual passing evidence for release commit.
- [ ] Do not treat an empty classic GitHub commit-status response as proof that Actions/check runs passed.

## Database and data integrity

- [ ] Fresh schema creation succeeds.
- [ ] Every previously released schema migrates through real production chain.
- [ ] Current declared schema version matches database documentation.
- [ ] Migration failure/rollback is tested using synthetic copies.
- [ ] WAL/foreign-key/busy-timeout behavior is verified.
- [ ] Hidden developer data-integrity report is healthy on release-candidate sample data.
- [ ] Transaction signs/currencies/extreme values are valid.
- [ ] Linked transfers remain balanced and paired after edits/deletes/restores/imports.
- [ ] Split signs/totals/categories are valid.
- [ ] Category hierarchy is acyclic.
- [ ] Category merge/archive does not convert a subcategory budget into an invalid root-category budget.
- [ ] Custom budgets have non-overlapping explicit periods.
- [ ] Custom budgets are absent outside configured windows.
- [ ] Rollover cannot produce a non-positive/overflowed effective plan.
- [ ] Failed budget-period replacement leaves prior period set intact.
- [ ] Savings contribution history never goes below zero and linked transaction currency is valid.
- [ ] Derived `SavingsGoal.IsCompleted` agrees with validated current progress; safe startup repair does not normalize corrupt history.
- [ ] Active recurrence references available matching-currency accounts/categories.
- [ ] Recurrence occurrence paid/partial/unpaid state matches generated transaction state.
- [ ] Paid-after-postponement history remains valid and useful.
- [ ] Reconciliation differences/adjustment links are internally consistent.
- [ ] Added/modified schema-v2 metadata fails persistence-boundary validation when malformed.

## Currency correctness

- [ ] No aggregate adds unlike currencies without explicit conversion.
- [ ] Dashboard aggregate cards are scoped to configured reporting currency.
- [ ] Other-currency rows/goals/recurrence items retain their own currency labels.
- [ ] Category/merchant/monthly/yearly/tag report totals are currency-scoped.
- [ ] Recurring-obligation and savings-progress rows retain their own currencies.
- [ ] JPY-style 0-decimal and KWD-style 3-decimal conversion/import/display tests pass.
- [ ] Account/transaction edit fields use currency-specific decimal precision rather than hard-coded two decimals.
- [ ] Passive finance rows format currency-aware major units rather than showing raw stored minor units.
- [ ] Cross-currency transfer remains blocked until an explicit exchange workflow exists.
- [ ] No hidden/automatic exchange-rate lookup was introduced.

## Local calendar and time-zone correctness

- [ ] User-selected date ranges are interpreted as local calendar dates, not UTC-midnight dates.
- [ ] Shared `LocalDateRange` converts inclusive local dates into UTC `[from,toExclusive)` bounds.
- [ ] Non-UTC fixed-offset regression tests pass.
- [ ] Reversed/invalid ranges fail closed.
- [ ] Native QA covers a DST-observing time zone around invalid/ambiguous local times.
- [ ] Dashboard date periods use local calendar semantics.
- [ ] Transaction advanced filters and Transaction Tools use shared local-date boundaries.
- [ ] Reconciliation statement-date boundary includes the complete local statement day without a `23:59:59` truncation gap.
- [ ] Budget performance/account trend/monthly/yearly report windows use the reviewed local-date conversion where appropriate.
- [ ] Current monthly/yearly comparisons stop at today and exclude future-dated imported rows until their local date arrives.

## Dashboard

- [ ] Reporting-currency notice is visible and bound to current `CurrencyScope` state.
- [ ] Current financial month period resolves using configured financial-month start day.
- [ ] Previous financial month resolves to a complete prior financial window.
- [ ] Last 30 days contains exactly 30 local calendar days including today.
- [ ] Last 90 days contains exactly 90 local calendar days including today.
- [ ] Year-to-date starts January 1 and ends today.
- [ ] Current balance uses current account summaries and is not redefined by selected activity period.
- [ ] Income/spending/net/category/recent date-sensitive cards respond to selected period.
- [ ] Dashboard continues to avoid legacy mixed-currency `GetDashboardAsync` aggregate path.
- [ ] Other-currency account count/explanation is accurate.

## Reports

- [ ] Spending-by-category report is present and split-aware.
- [ ] Income-versus-expense report is present.
- [ ] Account balance trend report is present.
- [ ] Budget performance report is present and follows shared budget-period policy.
- [ ] Merchant/payee report is present.
- [ ] Monthly comparison report is present and local-calendar grouped.
- [ ] Yearly comparison report is present and local-calendar grouped.
- [ ] Recurring-obligation report exposes type/status/amount/currency/next-due/end state without archived-rule noise.
- [ ] Savings-progress report derives current progress from validated contribution history.
- [ ] Tag reporting remains available with explicit currency scope.
- [ ] Signed net-change charts render positive values above zero and negative values below a true zero baseline.
- [ ] Chart renderer does not turn negative values into positive bars with absolute magnitude.
- [ ] Every quantitative chart has equivalent text or tabular values.
- [ ] Quantitative charts are hidden while privacy mode hides monetary values because bar geometry would reveal magnitude.

## Transaction history and tools

- [ ] Search by merchant/note/account/category behaves correctly.
- [ ] Account/category/type/date advanced filters behave correctly.
- [ ] Sort choices include newest, oldest, amount high-to-low, amount low-to-high, and merchant A–Z.
- [ ] Sorting is deterministic for tied values.
- [ ] First displayed history page is bounded to 50 matching rows.
- [ ] `Load more` appends next 50 rows without duplicates or reordering prior rows.
- [ ] History status correctly reports displayed vs matching count.
- [ ] Clear filters restores default period/sort state.
- [ ] Transaction Tools use same local-calendar date boundary policy.
- [ ] Duplicate review never deletes automatically.

## Privacy-mode amount hiding

- [ ] `PrivacyMode` and `HideAmountsOnLaunch` do not modify persisted money.
- [ ] Dashboard monetary values hide appropriately.
- [ ] Account list balances hide appropriately.
- [ ] Account detail current balance/history hide appropriately.
- [ ] Transaction history amounts hide appropriately.
- [ ] Transaction Tools/duplicate amounts hide appropriately.
- [ ] Transaction-detail passive split amounts hide appropriately while explicit edit input remains editable.
- [ ] Budget planned/actual cards hide appropriately.
- [ ] Savings current/target cards hide appropriately.
- [ ] Savings monthly contribution forecast does not reveal estimated amount while hidden.
- [ ] Recurring rule/occurrence scheduled/paid values hide appropriately.
- [ ] Reconciliation preview/history values hide appropriately.
- [ ] Report rows hide money and report charts are suppressed while hidden.
- [ ] No passive XAML surface labels raw `*Minor` values as user-facing minor units.
- [ ] Turning privacy mode off restores correctly formatted money without reload corruption.

## Backup and restore

- [ ] Create encrypted backup with synthetic accounts/transactions/budgets/goals/recurrence/receipts.
- [ ] Preview reports correct schema/counts.
- [ ] Restore succeeds into clean test profile.
- [ ] Restore succeeds over existing synthetic data without partial replacement.
- [ ] Attachment bytes and checksums round-trip.
- [ ] Wrong password is rejected.
- [ ] Modified ciphertext/tag is rejected.
- [ ] Truncated/oversized file is rejected.
- [ ] Unsupported schema is rejected.
- [ ] Cryptographically valid but semantically invalid finance graph is rejected before destructive replacement.
- [ ] Broken transaction/account currency, transfer, split, category, tag, custom-budget, goal, recurrence, reconciliation, attachment, revision, notification, and settings graphs are rejected.
- [ ] Internal restore markers/settings cannot be imported from backup snapshot.
- [ ] Attachment lexical path escapes are rejected.
- [ ] Attachment symbolic-link/reparse traversal is rejected where host supports test links.
- [ ] Interrupted restore before DB commit restores previous receipt tree on restart.
- [ ] Interrupted restore after DB commit finalizes new receipt tree on restart.
- [ ] Linked restore journal/staging/rollback paths are rejected.
- [ ] Orphan restore/rollback directories are cleaned only after recovery decision.
- [ ] Failure leaves prior data usable.
- [ ] Backup password/key is never logged or persisted by Finora.
- [ ] Settings backup-password input is masked and cleared after operation.
- [ ] Plaintext/receipt buffers are cleared as early as practical on success/failure paths.

## Core functional smoke test

- [ ] First-run onboarding with no account/login requirement.
- [ ] Onboarding explains local-first/no-automatic-upload behavior.
- [ ] Onboarding exposes Privacy and Terms access.
- [ ] Onboarding can be revisited from Settings.
- [ ] Revisiting onboarding with existing accounts does not duplicate opening/sample data.
- [ ] Optional sample data is opt-in only.
- [ ] Create/edit/archive/restore account.
- [ ] Active recurrence blocks account archival; paused dependency behavior is understood.
- [ ] Credit-card metadata and billing-day 1–31 behavior.
- [ ] Record expense/income/refund/adjustment.
- [ ] Transfer between accounts.
- [ ] Search/filter/sort transaction history.
- [ ] Incremental Load more transaction history.
- [ ] Split transaction.
- [ ] Tags and categories/subcategories.
- [ ] Duplicate review and bulk categorization.
- [ ] Revision history.
- [ ] Receipt add/open/delete/storage cleanup.
- [ ] Account reconciliation with/without adjustment.
- [ ] Monthly/category/weekly/custom budget and rollover behavior.
- [ ] Savings goal deposit/withdrawal/link/forecast/milestone.
- [ ] Recurring paid/partial/skipped/postponed/reopened behavior.
- [ ] Recurring rule pause/resume/archive lifecycle.
- [ ] Archived rule retains occurrence history but no longer generates.
- [ ] Dashboard reporting-currency scope/privacy/configurable cards and period selector.
- [ ] Complete report matrix and accessible equivalents.
- [ ] Tag reporting with explicit currency scope.
- [ ] CSV mapping/preview/import.
- [ ] CSV and PDF selected/all export.
- [ ] Full local finance-data deletion through dedicated complete reset service.
- [ ] Developer sample reset requires typed confirmation and uses synthetic data only.

## Settings, About, onboarding and destructive controls

- [ ] Full finance deletion button is wired to `OnDeleteAllFinanceDataClicked` dedicated reset workflow.
- [ ] Full finance deletion requires exact typed confirmation and does not use obsolete partial-delete handler.
- [ ] Backup password, new PIN and confirm-PIN fields are masked.
- [ ] Secret fields are cleared after operation.
- [ ] About version/build reflects packaged `AppInfo` metadata, not a stale hard-coded literal.
- [ ] About displays “Made by the Sanskar”.
- [ ] About technology summary includes .NET MAUI, C#, XAML, SQLite and MVVM.
- [ ] Repository and creator profile links work.
- [ ] Buy Me a Coffee button opens the shared canonical `https://buymeacoffee.com/sanskarIN` URL through the system launcher.
- [ ] Buy Me a Coffee open failure produces generic privacy-safe UI text/logging.
- [ ] Buy Me a Coffee is described as optional project support only, not feature unlock, premium entitlement, subscription, or support-priority purchase.
- [ ] Business/security and support contacts are correct.
- [ ] Apache-2.0/license/notices links are correct.
- [ ] Privacy and Terms links work.
- [ ] Contributing, Security and Support guide links work.
- [ ] External-document open failures produce generic privacy-safe UI text.

## Privacy and security

- [ ] No login/internet is required for current release functionality.
- [ ] No analytics, telemetry, advertising identifiers, or automatic cloud upload was introduced.
- [ ] Manual location remains user-entered only; no background location collection.
- [ ] Diagnostic logs are sanitized.
- [ ] Integrity reports are sanitized.
- [ ] Bound infrastructure errors and user alerts do not expose raw filesystem/database/crypto/provider messages.
- [ ] Notification text is generic/privacy-safe.
- [ ] Stale recurring/budget/backup schedules are cancelled after source-state changes.
- [ ] PIN setup/change/removal and rate-limited lockout are tested.
- [ ] Temporary secure-storage provider failure fails closed.
- [ ] Readable missing/corrupt verifier does not leave a permanent stale-marker lock trap.
- [ ] Inactivity lock is tested.
- [ ] Biometric/Windows Hello success/cancel/unavailable/lockout uses PIN fallback.
- [ ] Android biometric provider `errString` is never displayed verbatim.
- [ ] Sensitive-screen protection is tested where supported and limitations documented.
- [ ] Local premium demo flag is still labeled non-tamper-proof and is not represented as commercial licensing.
- [ ] Android manifest/rule source keeps ordinary automatic backup/device transfer excluded.

## Accessibility and adaptive UI

- [ ] Light, dark, and system appearance.
- [ ] Large text / larger interface setting.
- [ ] Screen-reader semantics for changed flows.
- [ ] Dashboard period selector has understandable label/description and live range result.
- [ ] Reports retain text/tabular equivalents independent of charts.
- [ ] Signed chart zero baseline does not depend on color alone to communicate sign; equivalent rows remain available.
- [ ] Recurring lifecycle controls have understandable labels/descriptions/state.
- [ ] Transaction sort/filter/Load more controls are keyboard/screen-reader operable.
- [ ] Onboarding Privacy/Terms controls are reachable.
- [ ] Settings About, Buy Me a Coffee, and destructive controls have understandable focus/labels.
- [ ] Keyboard navigation/focus on desktop.
- [ ] Reduced motion.
- [ ] Sufficient contrast.
- [ ] Phone bottom-tab hierarchy and tablet/desktop flyout hierarchy.
- [ ] Resize between navigation modes preserves usable primary section.
- [ ] Empty/loading/error/permission-denied states remain actionable.

## Notifications

- [ ] Permission not requested before explicit user action/need.
- [ ] Denied permission is handled without blocking finance functionality.
- [ ] Backup reminder can be disabled and stale schedule is cancelled.
- [ ] Budget warning deduplicates and stale/inactive threshold schedules are cancelled.
- [ ] Recurring reminder deduplicates.
- [ ] Paused/completed/archived recurring rules have stale reminders cancelled.
- [ ] Failed deduplicated replacement does not cancel old working native reminder.
- [ ] Successful replacement commits new/disabled-old DB state before stale native cancellation.
- [ ] Cancellation failure does not incorrectly re-enable disabled DB row.
- [ ] Expired disabled rows are retried for best-effort native cancellation during reconciliation.
- [ ] Android cancellation of missing reminder uses `PendingIntentFlags.NoCreate` and does not create a pending broadcast artifact.
- [ ] App restart does not create duplicate scheduled records.
- [ ] OS-specific scheduling limitations are documented/tested.

## Temporary artifacts

- [ ] Stale managed CSV/PDF/backup/integrity-report cache copies older than grace period are removed at startup.
- [ ] Fresh managed copies remain long enough for share flow.
- [ ] Unrelated cache files remain.
- [ ] Diagnostic logs remain outside temporary-share cleanup selection.
- [ ] File links are deleted as links and external link target remains untouched.
- [ ] Cleanup failure does not block finance startup.

## Platform packaging

Use `docs/releases/STORE_READINESS.md` for full platform matrices.

### Android

- [ ] Signed AAB generated externally from repository secrets.
- [ ] Adaptive/monochrome icon and splash validated.
- [ ] Notification/biometric/file/share/capture behavior verified.
- [ ] Android biometric failures remain stable/generic and retain PIN fallback.
- [ ] Android reminder cancellation `NoCreate` behavior verified on device/emulator.
- [ ] Merged manifest keeps `allowBackup=false`, `usesCleartextTraffic=false`, and both backup-rule resources wired.
- [ ] Ordinary cloud backup/device transfer does not restore Finora private finance DB/preferences/receipts in test profile.
- [ ] Upgrade/migration tested.

### Windows

- [ ] Final package identity/publisher/signing configured securely.
- [ ] Windows Hello/toasts/file-share/capture behavior verified.
- [ ] Resizing/keyboard/high-DPI verified.
- [ ] Privacy-mode passive finance values and report chart suppression verified.
- [ ] Local-calendar/report behavior verified under non-UTC Windows time zone.
- [ ] Upgrade/migration tested.

### iOS / Mac Catalyst

- [ ] Supported Xcode archive/build completes.
- [ ] Provisioning/signing/notarization handled securely.
- [ ] LocalAuthentication/UserNotifications/file-share behavior verified.
- [ ] VoiceOver/Dynamic Type or desktop accessibility verified.
- [ ] Privacy-mode passive finance values and report chart suppression verified.
- [ ] Local-calendar/report behavior verified in at least one non-UTC and one DST-observing test zone.
- [ ] Upgrade/migration tested.

## Store metadata and external support links

- [ ] Version/build number matches source and packaged `AppInfo` shown in About.
- [ ] Product name is Finora.
- [ ] Attribution is “Made by the Sanskar” in appropriate product surfaces, not over user content.
- [ ] Business/security email is `sanskarin@outlook.in`.
- [ ] Support email is `supportramsandesh@gmail.com`.
- [ ] Repository/profile links are correct.
- [ ] Canonical Buy Me a Coffee URL is `https://buymeacoffee.com/sanskarIN` everywhere it is exposed.
- [ ] Current target-store policy has been reviewed for external contribution/payment links before retaining Buy Me a Coffee in the packaged build.
- [ ] Store copy does not describe Buy Me a Coffee as in-app purchase, subscription, premium entitlement, feature unlock, guaranteed support, or secure licensing.
- [ ] Store screenshots use synthetic data only.
- [ ] Store screenshots do not accidentally defeat privacy mode with another passive amount surface.
- [ ] Store copy does not promise returns, financial advice, cloud sync, automatic exchange rates, bug-free operation, or tamper-proof local premium licensing.
- [ ] Store privacy/data-safety declarations match actual app behavior and permissions.

## Release decision

- [ ] All applicable gates above have evidence.
- [ ] Every unresolved P0 item from `docs/NEXT_STEPS.md` is closed or explicitly blocks release.
- [ ] Known limitations are recorded in `PROJECT_STATUS.md` and release notes.
- [ ] No unresolved issue can cause silent financial corruption, mixed-currency misreporting, local-date misclassification, misleading signed chart direction, unsafe restore, privacy leakage, app-lock bypass, or incorrect migration.
- [ ] Release tag/artifacts are created only after candidate passes required gates.
