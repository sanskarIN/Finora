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

- [ ] `dotnet workload restore` succeeds.
- [ ] `dotnet restore Finora.sln` succeeds.
- [ ] `dotnet format Finora.sln --verify-no-changes --no-restore` passes.
- [ ] Release build passes with warnings-as-errors.
- [ ] Unit tests pass.
- [ ] Integration tests pass.
- [ ] UI-contract tests pass.
- [ ] Platform MAUI builds pass on appropriate hosts.

## Database and data integrity

- [ ] Fresh schema creation succeeds.
- [ ] Every previously released schema migrates through the real production chain.
- [ ] Current declared schema version matches database documentation.
- [ ] Migration failure/rollback is tested using synthetic copies.
- [ ] WAL/foreign-key/busy-timeout behavior is verified.
- [ ] Hidden developer data-integrity report is healthy on release-candidate sample data.
- [ ] Linked transfers remain balanced and paired after edits/deletes/restores/imports.
- [ ] Split totals match parent transactions.
- [ ] Recurrence processing remains idempotent after restart.

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
- [ ] Failure leaves prior data usable.
- [ ] Backup password/key is never logged or persisted by Finora.

## Core functional smoke test

- [ ] First-run onboarding with no account/login requirement.
- [ ] Optional sample data is opt-in only.
- [ ] Create/edit/archive/restore account.
- [ ] Credit-card metadata behavior.
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
- [ ] Recurring paid/partial/skipped/postponed/transfer behavior.
- [ ] Dashboard period/privacy/configurable cards.
- [ ] Accessible reports and textual equivalents.
- [ ] CSV mapping/preview/import.
- [ ] CSV and PDF selected/all export.
- [ ] Full local finance-data deletion.

## Privacy and security

- [ ] No login/internet is required for current release functionality.
- [ ] No analytics, telemetry, advertising identifiers, or automatic cloud upload was introduced.
- [ ] Manual location remains user-entered only; no background location collection.
- [ ] Diagnostic logs are sanitized.
- [ ] Integrity reports are sanitized.
- [ ] Notification text is privacy-safe.
- [ ] PIN setup/change/removal and rate-limited lockout are tested.
- [ ] Inactivity lock is tested.
- [ ] Biometric/Windows Hello success/cancel/unavailable/lockout uses PIN fallback.
- [ ] Sensitive-screen protection is tested where supported and limitations are documented elsewhere.
- [ ] Local premium demo flag is still labeled non-tamper-proof and is not represented as commercial licensing.

## Accessibility and adaptive UI

- [ ] Light, dark, and system appearance.
- [ ] Large text / larger interface setting.
- [ ] Screen-reader semantics for changed flows.
- [ ] Keyboard navigation/focus on desktop.
- [ ] Reduced motion.
- [ ] Sufficient contrast.
- [ ] Phone, tablet/foldable where available, and resizable desktop window layouts.
- [ ] Empty/loading/error/permission-denied states remain actionable.

## Notifications

- [ ] Permission not requested before explicit user action/need.
- [ ] Denied permission is handled without blocking finance functionality.
- [ ] Backup reminder can be disabled.
- [ ] Budget warning deduplicates.
- [ ] Recurring reminder deduplicates.
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
- [ ] Store copy does not promise returns, financial advice, cloud sync, bug-free operation, or tamper-proof local premium licensing.
- [ ] Store privacy/data-safety declarations match actual app behavior and permissions.

## Release decision

- [ ] All applicable gates above have evidence.
- [ ] Known limitations are recorded in `PROJECT_STATUS.md` and release notes.
- [ ] No unresolved issue can cause silent financial corruption, unsafe restore, privacy leakage, app-lock bypass, or incorrect migration.
- [ ] Release tag/artifacts are created only after the candidate passes the required gates.
