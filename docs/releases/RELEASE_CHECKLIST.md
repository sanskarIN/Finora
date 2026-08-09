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
- [ ] App display/build version matches platform package metadata.
- [ ] Declared database schema matches `docs/architecture/DATABASE_SCHEMA.md`.

## Dependencies and licenses

- [ ] Restore exact dependency graph with the release SDK/workloads.
- [ ] Review direct and transitive dependency licenses.
- [ ] Review known vulnerabilities and incompatible/deprecated dependencies.
- [ ] Update `THIRD_PARTY_NOTICES.md` when the verified graph requires it.
- [ ] Do not add a dependency only to satisfy a cosmetic feature if a maintained platform/API implementation already exists.
- [ ] Review GitHub Action revisions used by release workflows.

## Build and analysis

- [ ] Structural preflight passes.
- [ ] Core test projects restore on .NET 10.
- [ ] Release build passes with warnings-as-errors/recommended analyzers.
- [ ] Unit tests pass.
- [ ] Integration tests pass.
- [ ] UI-contract tests pass.
- [ ] Windows + Android MAUI builds pass on the supported Windows host/CI runner.
- [ ] iOS + Mac Catalyst MAUI builds pass on the supported macOS/Xcode host/CI runner.
- [ ] No native platform gate is marked complete from a Linux/core-only run.

Formatting cleanup is encouraged, but formatting-only drift is not used as a substitute for compiler/analyzer/test/platform correctness gates.

## Database and data integrity

- [ ] Fresh schema creation succeeds.
- [ ] Every previously released schema migrates through the real production chain.
- [ ] Current declared schema version matches database documentation.
- [ ] Migration failure/rollback is tested using synthetic copies.
- [ ] WAL/foreign-key/busy-timeout behavior is verified.
- [ ] Persistence-boundary validation rejects invalid account/transaction currency/sign/value states even when code bypasses normal UI services.
- [ ] Hidden developer data-integrity report is healthy on release-candidate sample data.
- [ ] Integrity checker detects raw sign/extreme-amount/receipt-path corruption using synthetic failure injection.
- [ ] Linked transfers remain balanced and paired after edits/deletes/restores/imports.
- [ ] Split totals/signs match parent transactions.
- [ ] Recurrence processing remains idempotent after restart.
- [ ] Skipped recurrence can reopen; completed recurrence cannot create duplicate payment rows.

## Money and multi-currency correctness

- [ ] Integer minor-unit storage is preserved throughout the DB/import/export/report paths.
- [ ] Zero-/two-/three-decimal currency precision required by release markets has been verified against current authoritative currency metadata.
- [ ] JPY-style zero-decimal major-unit import/formatting is tested.
- [ ] KWD-style three-decimal major-unit import/formatting is tested.
- [ ] `long.MinValue`/overflow inputs are rejected without overflow during sign normalization.
- [ ] Dashboard aggregate totals use one explicit reporting currency.
- [ ] Reports aggregate only the selected reporting currency.
- [ ] Other account/budget/goal/recurrence currencies remain labeled/displayed separately.
- [ ] Finora never implies an exchange rate or silently adds unlike currencies.

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
- [ ] Backup/preview/restore calls are serialized and cannot race recovery metadata.
- [ ] Process termination is injected before/after recovery journal write, rollback receipt copy, inner DB restore, receipt swap, DB commit, and final cleanup.
- [ ] Matching pending marker rolls previous receipt tree back.
- [ ] Missing matching marker finalizes a DB-committed receipt tree.
- [ ] Incomplete rollback-copy state preserves an untouched live receipt tree.
- [ ] Startup recovery completes before normal finance navigation.
- [ ] Successful recovery removes transient marker/journal/staging/rollback artifacts.
- [ ] If recovery cannot safely resolve state, the app blocks normal initialization instead of exposing mismatched DB/receipt data.

## Core functional smoke test

- [ ] First-run onboarding with no account/login requirement.
- [ ] Optional onboarding sample data is opt-in only.
- [ ] Hidden developer “Reset to synthetic sample data” requires typed destructive confirmation and creates synthetic data only.
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
- [ ] Recurring paid/partial/skipped/reopened/postponed/transfer behavior.
- [ ] Dashboard period/privacy/configurable cards/reporting-currency notice.
- [ ] Accessible reports and textual equivalents.
- [ ] CSV mapping/preview/import including zero-/three-decimal currencies.
- [ ] CSV and PDF selected/all export.
- [ ] Full local finance-data deletion.
- [ ] Full finance reset removes user-created categories/tags/finance metadata/receipt records while keeping schema/app preferences/PIN configuration.

## Privacy and security

- [ ] No login/internet is required for current release functionality.
- [ ] No analytics, telemetry, advertising identifiers, or automatic cloud upload was introduced.
- [ ] Manual location remains user-entered only; no background location collection.
- [ ] Diagnostic logs are bounded/sanitized.
- [ ] Integrity reports are sanitized.
- [ ] Restore-recovery marker/journal contain operation metadata only, no finance contents/password/key.
- [ ] Notification text is privacy-safe.
- [ ] PIN setup/change/removal and rate-limited lockout are tested.
- [ ] Missing/corrupt secure-storage PIN verifier fails closed when app-lock-enabled state remains.
- [ ] Inactivity lock is tested.
- [ ] Biometric/Windows Hello success/cancel/unavailable/lockout uses PIN fallback.
- [ ] iOS/Mac Catalyst biometric purpose text is present and accurate.
- [ ] Sensitive-screen protection is tested where supported and limitations are documented elsewhere.
- [ ] Local premium demo flag is still labeled non-tamper-proof and is not represented as commercial licensing.

## Accessibility and adaptive UI

- [ ] Light, dark, and system appearance.
- [ ] Large text / larger interface setting.
- [ ] Minimum usable touch/input target sizing.
- [ ] Screen-reader semantics for changed flows.
- [ ] Keyboard navigation/focus on desktop.
- [ ] Reduced motion.
- [ ] Sufficient contrast.
- [ ] Phone primary bottom tabs.
- [ ] Tablet/desktop flyout/sidebar hierarchy.
- [ ] Resizing between navigation modes preserves the equivalent primary section.
- [ ] Onboarding/unlock/startup choose the correct adaptive root.
- [ ] Empty/loading/error/permission-denied states remain actionable.

## Locale/formatting

- [ ] Saved locale is applied on startup before normal navigation.
- [ ] Changing locale in Settings updates number/date preview and future formatting.
- [ ] Invalid persisted locale safely falls back to a valid culture.
- [ ] Currency change refreshes format preview.
- [ ] English-first UI/localization readiness is represented accurately; untranslated literal strings are not claimed translated.

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
- [ ] `allowBackup=false` and `usesCleartextTraffic=false` remain effective.
- [ ] Phone/tablet adaptive navigation verified.
- [ ] Upgrade/migration/recovery tested.

### Windows

- [ ] Source/package version metadata agrees.
- [ ] Final package identity/publisher/signing configured securely (development publisher is not treated as production signing evidence).
- [ ] Windows Hello/toasts/file-share/capture behavior verified.
- [ ] Resizing/flyout/keyboard/high-DPI verified.
- [ ] Upgrade/migration/recovery tested.

### iOS / Mac Catalyst

- [ ] Supported Xcode archive/build completes.
- [ ] Provisioning/signing/notarization handled securely.
- [ ] Face ID/biometric purpose text is accepted by native packaging/review.
- [ ] LocalAuthentication/UserNotifications/file-share behavior verified.
- [ ] Phone/iPad/desktop adaptive navigation verified as applicable.
- [ ] VoiceOver/Dynamic Type or desktop accessibility verified.
- [ ] Upgrade/migration/recovery tested.

## Store metadata

- [ ] Version/build number matches source and artifacts.
- [ ] Product name is Finora.
- [ ] Attribution is “Made by the Sanskar” in appropriate product surfaces, not over user content.
- [ ] Business/security email is `sanskarin@outlook.in`.
- [ ] Support email is `supportramsandesh@gmail.com`.
- [ ] Repository/profile links are correct.
- [ ] Store screenshots use synthetic data only.
- [ ] Store copy does not promise returns, financial advice, cloud sync, bug-free operation, exchange-rate conversion, or tamper-proof local premium licensing.
- [ ] Store privacy/data-safety declarations match actual app behavior and permissions.

## Release decision

- [ ] All applicable gates above have evidence.
- [ ] Known limitations are recorded in `PROJECT_STATUS.md` and release notes.
- [ ] No unresolved issue can cause silent financial corruption, mixed-currency totals, unsafe restore, privacy leakage, app-lock bypass, or incorrect migration.
- [ ] Release tag/artifacts are created only after the candidate passes the required gates.
