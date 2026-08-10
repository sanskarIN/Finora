# Finora Release Checklist

Use this checklist for every candidate. Do not mark a box complete from source inspection alone when it requires a compiler, platform SDK, emulator/simulator, physical device, signing service, or store console.

## Source and repository

- [ ] Release commit is on the intended branch/tag.
- [ ] Working tree used for release contains no uncommitted/generated private files.
- [ ] `python build/scripts/verify_structure.py` passes.
- [ ] CodeQL/dependency-review findings have been reviewed.
- [ ] Dependabot/security alerts have been reviewed.
- [ ] No API keys, keystores, certificates, passwords, PINs, backup passwords, private keys, real financial databases, or real receipt files are present.
- [ ] `README.md`, `CHANGELOG.md`, `PROJECT_STATUS.md`, `what_changed.md`, privacy/security/support docs, and third-party notices match the source.

## Dependencies and licenses

- [ ] Restore exact dependency graph with the release SDK/workloads.
- [ ] Review direct and transitive dependency licenses.
- [ ] Review known vulnerabilities and incompatible/deprecated dependencies.
- [ ] Update `THIRD_PARTY_NOTICES.md` when the verified graph requires it.
- [ ] Do not add a dependency only to satisfy a cosmetic feature if a maintained platform/API implementation already exists.

## Build and analysis

- [ ] `dotnet workload restore` succeeds on platform build hosts.
- [ ] Core/test projects restore on the intended .NET 10 SDK.
- [ ] Release build passes with warnings-as-errors.
- [ ] Unit tests pass.
- [ ] Integration tests pass.
- [ ] UI-contract tests pass.
- [ ] Platform MAUI builds pass on appropriate hosts.
- [ ] CI structural/core/Windows+Android/Apple jobs have actual passing evidence for the release commit.

## Database and data integrity

- [ ] Fresh schema creation succeeds.
- [ ] Every previously released schema migrates through the real production chain.
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
- [ ] Failed budget-period replacement leaves the prior period set intact.
- [ ] Savings contribution history never goes below zero and linked transaction currency is valid.
- [ ] Active recurrence references available matching-currency accounts/categories.
- [ ] Recurrence occurrence paid/partial/unpaid state matches generated transaction state.
- [ ] Reconciliation differences/adjustment links are internally consistent.

## Currency correctness

- [ ] No aggregate adds unlike currencies without explicit conversion.
- [ ] Dashboard aggregate cards are scoped to the configured reporting currency.
- [ ] Other-currency rows/goals/recurrence items retain their own currency labels.
- [ ] Category/merchant/monthly/tag report totals are currency-scoped.
- [ ] JPY-style 0-decimal and KWD-style 3-decimal conversion/import tests pass.
- [ ] Cross-currency transfer remains blocked until an explicit exchange workflow exists.
- [ ] No hidden/automatic exchange-rate lookup was introduced.

## Backup and restore

- [ ] Create encrypted backup with synthetic accounts/transactions/budgets/goals/recurrence/receipts.
- [ ] Preview reports correct schema/counts.
- [ ] Restore succeeds into a clean test profile.
- [ ] Restore succeeds over existing synthetic data without partial replacement.
- [ ] Attachment bytes and checksums round-trip.
- [ ] Wrong password is rejected.
- [ ] Modified ciphertext/tag is rejected.
- [ ] Truncated/oversized file is rejected.
- [ ] Unsupported schema is rejected.
- [ ] Cryptographically valid but semantically invalid finance graph is rejected before destructive replacement.
- [ ] Broken transaction/account currency, transfer, split, category, tag, custom-budget, goal, recurrence, reconciliation, attachment, and settings graphs are rejected.
- [ ] Internal restore markers/settings cannot be imported from the backup snapshot.
- [ ] Interrupted restore before DB commit restores the previous receipt tree on restart.
- [ ] Interrupted restore after DB commit finalizes the new receipt tree on restart.
- [ ] Orphan restore/rollback directories are cleaned only after recovery decision.
- [ ] Failure leaves prior data usable.
- [ ] Backup password/key is never logged or persisted by Finora.
- [ ] Plaintext/receipt buffers are cleared as early as practical on success and failure paths.

## Core functional smoke test

- [ ] First-run onboarding with no account/login requirement.
- [ ] Optional sample data is opt-in only.
- [ ] Create/edit/archive/restore account.
- [ ] Active recurrence blocks account archival; paused dependency behavior is understood.
- [ ] Credit-card metadata and billing-day 1–31 behavior.
- [ ] Record expense/income/refund/adjustment.
- [ ] Transfer between accounts.
- [ ] Search/filter transactions.
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
- [ ] Dashboard reporting-currency scope/privacy/configurable cards.
- [ ] Accessible reports and textual equivalents.
- [ ] Tag reporting with explicit currency scope.
- [ ] CSV mapping/preview/import.
- [ ] CSV and PDF selected/all export.
- [ ] Full local finance-data deletion.
- [ ] Developer sample reset requires typed confirmation and uses synthetic data only.

## Privacy and security

- [ ] No login/internet is required for current release functionality.
- [ ] No analytics, telemetry, advertising identifiers, or automatic cloud upload was introduced.
- [ ] Manual location remains user-entered only; no background location collection.
- [ ] Diagnostic logs are sanitized.
- [ ] Integrity reports are sanitized.
- [ ] Notification text is generic/privacy-safe.
- [ ] Stale recurring/budget/backup schedules are cancelled after source-state changes.
- [ ] PIN setup/change/removal and rate-limited lockout are tested.
- [ ] PIN-enabled state with missing/corrupt verifier fails closed.
- [ ] Inactivity lock is tested.
- [ ] Biometric/Windows Hello success/cancel/unavailable/lockout uses PIN fallback.
- [ ] Sensitive-screen protection is tested where supported and limitations are documented elsewhere.
- [ ] Local premium demo flag is still labeled non-tamper-proof and is not represented as commercial licensing.

## Accessibility and adaptive UI

- [ ] Light, dark, and system appearance.
- [ ] Large text / larger interface setting.
- [ ] Screen-reader semantics for changed flows.
- [ ] Recurring lifecycle controls have understandable labels/descriptions/state.
- [ ] Keyboard navigation/focus on desktop.
- [ ] Reduced motion.
- [ ] Sufficient contrast.
- [ ] Phone bottom-tab hierarchy and tablet/desktop flyout hierarchy.
- [ ] Resize between navigation modes preserves a usable primary section.
- [ ] Empty/loading/error/permission-denied states remain actionable.

## Notifications

- [ ] Permission not requested before explicit user action/need.
- [ ] Denied permission is handled without blocking finance functionality.
- [ ] Backup reminder can be disabled and stale schedule is cancelled.
- [ ] Budget warning deduplicates and stale/inactive threshold schedules are cancelled.
- [ ] Recurring reminder deduplicates.
- [ ] Paused/completed/archived recurring rules have stale reminders cancelled.
- [ ] App restart does not create duplicate scheduled records.
- [ ] OS-specific scheduling limitations are documented/tested.

## Platform packaging

Use `docs/releases/STORE_READINESS.md` for full platform matrices.

### Android

- [ ] Signed AAB generated externally from repository secrets.
- [ ] Adaptive/monochrome icon and splash validated.
- [ ] Notification/biometric/file/share/capture behavior verified.
- [ ] Upgrade/migration tested.

### Windows

- [ ] Final package identity/publisher/signing configured securely.
- [ ] Windows Hello/toasts/file-share/capture behavior verified.
- [ ] Resizing/keyboard/high-DPI verified.
- [ ] Upgrade/migration tested.

### iOS / Mac Catalyst

- [ ] Supported Xcode archive/build completes.
- [ ] Provisioning/signing/notarization handled securely.
- [ ] LocalAuthentication/UserNotifications/file-share behavior verified.
- [ ] VoiceOver/Dynamic Type or desktop accessibility verified.
- [ ] Upgrade/migration tested.

## Store metadata

- [ ] Version/build number matches source and artifacts.
- [ ] Product name is Finora.
- [ ] Attribution is “Made by the Sanskar” in appropriate product surfaces, not over user content.
- [ ] Business/security email is `sanskarin@outlook.in`.
- [ ] Support email is `supportramsandesh@gmail.com`.
- [ ] Repository/profile links are correct.
- [ ] Store screenshots use synthetic data only.
- [ ] Store copy does not promise returns, financial advice, cloud sync, automatic exchange rates, bug-free operation, or tamper-proof local premium licensing.
- [ ] Store privacy/data-safety declarations match actual app behavior and permissions.

## Release decision

- [ ] All applicable gates above have evidence.
- [ ] Known limitations are recorded in `PROJECT_STATUS.md` and release notes.
- [ ] No unresolved issue can cause silent financial corruption, mixed-currency misreporting, unsafe restore, privacy leakage, app-lock bypass, or incorrect migration.
- [ ] Release tag/artifacts are created only after the candidate passes the required gates.
