# Finora Release Checklist

Use this checklist for every candidate. Do not mark a box complete from source inspection alone when it requires a compiler, platform SDK, emulator/simulator, physical device, signing service, or store console.

## Source and repository

- [ ] Release commit is on intended branch/tag.
- [ ] Working tree used for release contains no uncommitted/generated private files.
- [ ] `python build/scripts/verify_structure.py` passes.
- [ ] Structural preflight confirms XAML handlers, masked backup/PIN fields, Android backup/data-transfer exclusions, and no raw exception-alert regressions.
- [ ] CodeQL/dependency-review findings have been reviewed.
- [ ] Dependabot/security alerts have been reviewed.
- [ ] No API keys, keystores, certificates, passwords, PINs, backup passwords, private keys, real financial databases, or real receipt files are present.
- [ ] `README.md`, `CHANGELOG.md`, `PROJECT_STATUS.md`, `what_changed.md`, privacy/security/support docs, and third-party notices match source.

## Dependencies and licenses

- [ ] Restore exact dependency graph with release SDK/workloads.
- [ ] Review direct/transitive dependency licenses.
- [ ] Review known vulnerabilities and incompatible/deprecated dependencies.
- [ ] Update `THIRD_PARTY_NOTICES.md` when verified graph requires it.
- [ ] Do not add a dependency only to satisfy a cosmetic feature when a maintained platform/API implementation exists.

## Build and analysis

- [ ] `dotnet workload restore` succeeds on platform build hosts.
- [ ] Core/test projects restore on intended .NET 10 SDK.
- [ ] Release build passes with warnings-as-errors.
- [ ] Unit tests pass.
- [ ] Integration tests pass.
- [ ] UI-contract tests pass.
- [ ] Platform MAUI builds pass on appropriate hosts.
- [ ] CI structural/core/Windows+Android/Apple jobs have actual passing evidence for release commit.
- [ ] Do not infer GitHub Actions success from an empty classic combined-status response; retain actual check/workflow evidence.

## Database and data integrity

- [ ] Fresh schema creation succeeds.
- [ ] Every previously released schema migrates through real production chain.
- [ ] Current declared schema version matches database documentation.
- [ ] Migration failure/rollback is tested with synthetic copies.
- [ ] WAL/foreign-key/busy-timeout behavior is verified.
- [ ] Hidden developer data-integrity report is healthy on release-candidate sample data.
- [ ] Transaction signs/currencies/extreme values/deletion timestamps are valid.
- [ ] Linked transfers remain balanced and paired after edits/deletes/restores/imports.
- [ ] Split signs/totals/categories are valid.
- [ ] Category hierarchy is acyclic.
- [ ] Category merge/archive does not convert a subcategory budget into invalid root-category budget.
- [ ] Custom budgets have non-overlapping explicit periods.
- [ ] Custom budgets are absent outside configured windows.
- [ ] Rollover cannot produce non-positive/overflowed effective plan.
- [ ] Failed budget-period replacement leaves prior period set intact.
- [ ] Savings contribution history never goes below zero and linked transaction currency is valid.
- [ ] Active recurrence references available matching-currency accounts/categories.
- [ ] Recurrence occurrence paid/partial/unpaid/postponed state matches generated transaction state.
- [ ] Paid-after-postponement history remains valid.
- [ ] Reconciliation differences/adjustment links are internally consistent.
- [ ] Direct EF writes reject malformed attachment, notification, recurrence-occurrence, reconciliation, revision, category/tag, setting, audit, and backup metadata.

## Currency correctness

- [ ] No aggregate adds unlike currencies without explicit conversion.
- [ ] Dashboard aggregate cards are scoped to configured reporting currency.
- [ ] Other-currency rows/goals/recurrence items retain own currency labels.
- [ ] Category/merchant/monthly/tag report totals are currency-scoped.
- [ ] JPY-style 0-decimal and KWD-style 3-decimal conversion/import tests pass.
- [ ] Cross-currency transfer remains blocked until explicit exchange workflow exists.
- [ ] No hidden/automatic exchange-rate lookup was introduced.

## Backup and restore

- [ ] Create encrypted backup with synthetic accounts/transactions/budgets/goals/recurrence/receipts.
- [ ] Preview reports correct schema/counts.
- [ ] Restore succeeds into clean test profile.
- [ ] Restore succeeds over existing synthetic data without partial replacement.
- [ ] Attachment bytes/checksums round-trip.
- [ ] Wrong password rejected.
- [ ] Modified ciphertext/tag rejected.
- [ ] Truncated/oversized file rejected.
- [ ] Unsupported schema rejected.
- [ ] Cryptographically valid but semantically invalid graph rejected before destructive replacement.
- [ ] Broken transaction/account currency, transfer, split, category, tag, custom-budget, goal, recurrence, reconciliation, attachment, and settings graphs rejected.
- [ ] Internal restore markers/settings cannot be imported from backup snapshot.
- [ ] Lexical receipt path escape rejected.
- [ ] Symbolic-link/reparse-point traversal rejected for live receipt storage, backup validation, restore staging/rollback, recovery journal, and rollback copy.
- [ ] Interrupted restore before DB commit restores previous receipt tree on restart.
- [ ] Interrupted restore after DB commit finalizes new receipt tree on restart.
- [ ] Orphan restore/rollback directories cleaned only after recovery decision and never recursively follow linked directories.
- [ ] Failure leaves prior data usable.
- [ ] Backup password/key is never logged or persisted by Finora.
- [ ] Backup password entry is masked and cleared after create/restore attempts.
- [ ] Plaintext/receipt buffers are cleared as early as practical on success and every failure path.
- [ ] UI encrypted-backup byte array is cleared after writing/sharing.

## Core functional smoke test

- [ ] First-run onboarding with no account/login requirement.
- [ ] Optional sample data is opt-in only.
- [ ] Create/edit/archive/restore account.
- [ ] Active recurrence blocks account archival; paused dependency behavior understood.
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
- [ ] CSV/PDF selected/all export.
- [ ] Full local finance-data deletion.
- [ ] Developer sample reset requires typed confirmation and synthetic data only.

## Privacy and security

- [ ] No login/internet required for current release functionality.
- [ ] No analytics, telemetry, advertising identifiers, or automatic cloud upload introduced.
- [ ] Manual location remains user-entered only; no background location collection.
- [ ] Android `allowBackup=false` remains packaged.
- [ ] Android legacy full-backup rules exclude root/file/database/sharedpref/external domains.
- [ ] Android 12+ cloud-backup/device-transfer rules exclude root/file/database/sharedpref/external domains.
- [ ] Diagnostic logs are sanitized, bounded, and do not follow linked paths.
- [ ] Exception messages, stacks, arbitrary logger properties, finance contents, secret values, and filesystem/provider details do not appear in diagnostics.
- [ ] ViewModel bound errors and primary alerts use generic infrastructure failures rather than raw exception messages.
- [ ] Integrity reports are sanitized.
- [ ] Notification text is generic/privacy-safe.
- [ ] Stale recurring/budget/backup schedules are cancelled after source-state changes.
- [ ] Failed dedupe replacement preserves previous valid reminder.
- [ ] Successful notification replacement commits new state before stale OS cancellation.
- [ ] PIN setup/change/removal and rate-limited lockout tested.
- [ ] New/confirm PIN fields remain masked and clear after use.
- [ ] Temporary secure-storage provider failure fails closed when explicit enabled marker exists.
- [ ] Readable missing/corrupt verifier self-heals stale marker without permanent lock-screen trap.
- [ ] PIN removal failure does not falsely announce success.
- [ ] Inactivity lock tested.
- [ ] Biometric/Windows Hello success/cancel/unavailable/lockout uses PIN fallback and generic failure text.
- [ ] Sensitive-screen protection tested where supported and limitations documented.
- [ ] Local premium demo flag still labeled non-tamper-proof and not represented as commercial licensing.

## Temporary share artifacts

- [ ] CSV/PDF/backup/integrity-report share copies are generated only after explicit user action.
- [ ] Startup cleanup removes only matching Finora cache share copies older than 24 hours.
- [ ] Fresh share copies remain long enough for system share sheet use.
- [ ] Unrelated cache files and diagnostic logs are not deleted.
- [ ] Linked file entries are not followed to their target during cleanup.
- [ ] Copies saved/shared outside Finora cache are documented as destination-controlled.

## Accessibility and adaptive UI

- [ ] Light, dark, and system appearance.
- [ ] Large text / larger interface setting.
- [ ] Screen-reader semantics for changed flows.
- [ ] Lock screen has heading/status/PIN/biometric semantic descriptions.
- [ ] Settings secret fields identify masked purpose to accessibility APIs without exposing entered value.
- [ ] Recurring lifecycle controls have understandable labels/descriptions/state.
- [ ] Keyboard navigation/focus on desktop.
- [ ] Reduced motion.
- [ ] Sufficient contrast.
- [ ] Phone bottom-tab hierarchy and tablet/desktop flyout hierarchy.
- [ ] Resize between navigation modes preserves usable primary section.
- [ ] Empty/loading/error/permission-denied states remain actionable.

## Notifications

- [ ] Permission not requested before explicit user action/need.
- [ ] Denied permission handled without blocking finance functionality.
- [ ] Backup reminder can be disabled and stale schedule cancelled.
- [ ] Budget warning deduplicates and stale/inactive threshold schedules cancelled.
- [ ] Recurring reminder deduplicates.
- [ ] Paused/completed/archived recurring rules have stale reminders cancelled.
- [ ] Failed replacement scheduling does not cancel prior enabled reminder.
- [ ] DB/OS cancellation drift is retried during reconciliation.
- [ ] Expired enabled rows become disabled.
- [ ] App restart does not create duplicate scheduled records.
- [ ] OS-specific scheduling limitations documented/tested.

## Platform packaging

Use `docs/releases/STORE_READINESS.md` for full platform matrices.

### Android

- [ ] Signed AAB generated externally from repository secrets.
- [ ] Adaptive/monochrome icon and splash validated.
- [ ] Notification/biometric/file/share/capture behavior verified.
- [ ] `backup_rules.xml` and `data_extraction_rules.xml` packaged and honored on representative API levels.
- [ ] Device/cloud transfer test confirms Finora private finance store is not copied through ordinary Android backup mechanisms.
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

- [ ] Version/build number matches source/artifacts.
- [ ] Product name is Finora.
- [ ] Attribution is “Made by the Sanskar” in appropriate product surfaces, not over user content.
- [ ] Business/security email is `sanskarin@outlook.in`.
- [ ] Support email is `supportramsandesh@gmail.com`.
- [ ] Repository/profile links are correct.
- [ ] Store screenshots use synthetic data only.
- [ ] Store copy does not promise returns, financial advice, cloud sync, automatic exchange rates, bug-free operation, or tamper-proof local premium licensing.
- [ ] Store privacy/data-safety declarations match actual app behavior, Android backup exclusions, and permissions.

## Release decision

- [ ] All applicable gates above have evidence.
- [ ] Known limitations recorded in `PROJECT_STATUS.md` and release notes.
- [ ] No unresolved issue can cause silent financial corruption, mixed-currency misreporting, unsafe restore, privacy leakage, app-lock bypass, notification-state loss, or incorrect migration.
- [ ] Release tag/artifacts created only after candidate passes required gates.
