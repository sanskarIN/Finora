# Finora Project Status

Last status refresh: 2026-08-10  
Current source version: **0.2.0 (build 2)**  
Current database schema: **2**  
Repository: https://github.com/sanskarIN/Finora

This file distinguishes **implemented source** from **validation that must still be executed on the appropriate .NET/MAUI/platform/store environment**. Source presence is not treated as proof that a native platform release has passed.

## Implemented in source

### Repository/foundation

- Multi-project `Finora.sln` with App, Application, Domain, Infrastructure, Shared, UnitTests, IntegrationTests, and UiTests projects.
- Apache-2.0 license.
- `.editorconfig`, `.gitattributes`, hardened `.gitignore`, central package management, deterministic builds, nullable reference types, warnings-as-errors, and latest-recommended analysis.
- README, changelog, project status, decisions, privacy, security, support, terms, code of conduct, contribution guide, third-party notices, architecture/database/privacy/security/setup/test/release/store-readiness documentation.
- Privacy-aware issue templates and pull-request template.
- CODEOWNERS.
- Dependabot NuGet/GitHub Actions configuration.
- Structural preflight, CI, CodeQL, and dependency-review workflows.

### Local-first product model

- No Finora login/account requirement for core finance functionality.
- No automatic cloud synchronization or backup upload in the current release.
- No required analytics/advertising telemetry service in current source.
- No background location collection; transaction location is manually entered text only.
- User-controlled import/export/share/backup flows through system UI.
- Local premium/demo state explicitly labeled non-tamper-proof.

### Money/domain correctness

- Signed 64-bit integer minor-unit money persistence.
- Decimal-safe major/minor conversion.
- Currency-specific minor-unit precision, including 0-/2-/3-decimal handling for known currencies.
- Decimal-only quick calculator expression engine.
- Account/transaction currency validation.
- `long.MinValue`, zero, sign, split, transfer, budget, goal, recurrence, and credit-card domain invariants.
- EF persistence-boundary validation so direct tracked writes cannot bypass core account/transaction checks.
- Checked arithmetic through balances, reports, budgets, goals, recurrence, reconciliation, imports, and integrity diagnostics.

### Database/schema

- EF Core SQLite persistence.
- WAL, foreign keys, and busy timeout initialization.
- Relational indexes and uniqueness controls.
- Schema version 2.
- Transactional schema-v1 → schema-v2 migration.
- Schema-v2 transaction revision records.
- Schema-v2 account reconciliation records.
- Schema-v2 persisted notification scheduling records.
- Attachment original filename metadata added in v2.
- Newer-than-supported database schema rejection.
- Startup initialization serialized before finance navigation.

### Accounts

- Cash, bank, credit-card, digital-wallet, savings, investment-placeholder, and custom account types.
- Name/icon/color/currency/opening balance/current balance.
- Active/hidden/archived states.
- Credit limit and billing-day 1–31 metadata.
- Account detail/history.
- Edit/archive/restore workflow.
- Default account preference.
- Same-currency paired transfer workflow.
- Account reconciliation preview/history with optional explicit adjustment transaction.
- Reconciled opening balance protection.
- Account currency cannot change after transaction/recurrence dependencies exist.
- Active recurring dependencies block account archival; paused historical dependencies may remain archived until a resume attempt revalidates them.

### Transactions

- Expense, income, transfer, refund, and adjustment.
- Quick-add form.
- Amount/account/category/date/time/merchant-payee/note/payment method/manual location fields.
- Decimal-safe calculator keypad.
- Search and advanced account/category/type/date/text filters.
- Transaction detail/edit workflow.
- Split transactions with sign/total/category validation.
- Tags.
- Critical edit revision history.
- Soft delete/restore.
- Linked-transfer-safe edit/delete/restore handling.
- Bulk categorization.
- Duplicate review.
- Selected/all transaction CSV/PDF export UI paths.

### Categories/tags

- Default category set.
- User categories/subcategories.
- Parent/subcategory assignment and arbitrary-depth cycle prevention.
- Reorder.
- Archive/restore.
- Safe reassignment/merge.
- Reassignment protects subcategory-budget hierarchy semantics.
- Tag create/edit/archive/restore.
- Tag reporting now requires explicit currency scope so unlike currencies are never combined.

### Receipts/attachments

- Transaction attachment metadata.
- App-private receipt/document file storage.
- Generated internal filenames and sanitized original filenames.
- Canonical safe-path confinement using platform-correct comparison semantics.
- Allowed image/PDF content-type validation.
- Per-file size limit.
- Async copy.
- Stored byte count and SHA-256 checksum.
- List/open/delete workflow.
- Local storage usage.
- Orphan file cleanup.
- Receipt bytes included and verified in encrypted backups/restores.

### Budgets

- Overall/category/subcategory budget configuration.
- Weekly/monthly/custom cadence.
- Central `BudgetPeriodPolicy` shared by store/report paths.
- Weekly Monday–Sunday generated windows.
- Monthly calendar windows.
- Explicit non-overlapping budget periods.
- Custom budgets require explicit periods and are inactive outside those windows.
- Rollover option applied only when enabled.
- Effective plan checked and required to remain positive.
- Warning threshold with overflow-safe percentage arithmetic.
- Planned/actual/variance reporting.
- Recursive category descendant and split-aware calculations.
- Transactional explicit-period replacement intended to preserve prior valid period state on failed update.
- Reminder coordination where notification permission/settings allow.

### Savings goals

- Name/icon/target/starting amount/target date/notes.
- Contributions and withdrawals.
- Optional linked account transaction.
- Running progress cannot fall below zero.
- Linked transaction must use the goal currency.
- Forecast/progress/milestones/completion state.
- Reduced-motion-aware completion behavior.

### Recurring items

- Daily/weekly/monthly/yearly/custom intervals.
- Recurring expense/income/transfer/refund templates.
- Start/end date.
- Grace period and reminder lead time.
- Persisted unique `(RecurrenceRuleId, DueOn)` occurrence state.
- Idempotent due processing with backlog guard.
- Paid/partial-paid/skipped/postponed/reopened workflows.
- Generated transaction linkage validation.
- Recurring transfer pair creation/validation.
- Rule lifecycle: pause, resume, archive.
- Paused rules do not generate occurrences.
- Resume revalidates end date/account/category/currency dependencies.
- Archived rules disappear from active-rule lists while preserving occurrence history.
- Stale recurring reminder schedules are cancelled during synchronization.

### Dashboard/reports

- Configurable dashboard cards for balance, income/spending/net, budget, upcoming recurring items, top categories, goals, recent transactions, and cash flow.
- Financial-month-start handling.
- Privacy mode that hides displayed amounts.
- Dashboard aggregate cards are explicitly scoped to the configured reporting currency.
- Other-currency transactions/goals/recurrence items retain their native currency labels.
- No implicit/fabricated exchange rates.
- Category spending report uses transaction splits when present.
- Category-budget reporting resolves descendants recursively.
- Income-versus-expense data.
- Account balance trend with correct end-boundary handling.
- Budget performance uses shared budget-period policy.
- Merchant/payee data.
- Tag data with explicit currency scope.
- Monthly comparison data.
- MAUI-drawn visual report surface with textual/tabular equivalent representation.

### CSV import

- System file selection.
- Header discovery and explicit mapping.
- Mapping preview/validation.
- Required date/type/amount/account mapping.
- Optional currency/category/merchant/note/payment method/manual location/transfer group/counterparty/tags mapping.
- Major/minor-unit option.
- Currency-specific decimal-safe major-unit conversion.
- UTF-8 validation.
- File-size and row-count limits.
- Quoted-field parsing.
- Account/category/tag resolution.
- Optional missing-category creation.
- Duplicate protection including duplicates within the same import batch.
- Transfer-group/counterparty validation.
- `long.MinValue` protection before sign normalization.
- Transactional import commit.
- Parse errors counted once with explicit row/import errors.

### CSV/PDF export

- Transaction CSV export with identifiers, dates/types/amounts/currency/accounts/categories/merchant-note/payment method/manual location/transfer linkage/tags.
- Multi-page PDF transaction export.
- Explicit user-controlled share/save workflow.

### Encrypted backup/restore

- Encrypted backup creation on explicit user action only.
- PBKDF2-SHA256 password-derived key.
- Random salt.
- AES-GCM authenticated encryption with random nonce/tag.
- Backup format magic/size validation.
- Backup schema metadata and preview.
- Schema-v2 finance graph serialization.
- Receipt byte inclusion.
- Receipt path/size/SHA-256 validation.
- Platform-correct path confinement.
- Full pre-encryption and post-decryption financial graph validation: IDs, account/currency references, transfers, splits, category hierarchy, tags, budgets/periods, goals/contributions, recurrence, attachments, revisions, reconciliation, notifications, settings boundaries.
- Internal restore markers/settings excluded from imported snapshot settings.
- Sensitive plaintext/receipt buffers cleared as early as practical after use.
- Crash-safe restore wrapper with serialized operation gate.
- Durable restore journal and DB commit marker.
- Startup recovery before finance UI.
- Pre-commit interruption restores previous attachment tree; post-commit interruption finalizes new tree.
- Stale staging/rollback cleanup after recovery decision.
- Wrong/tampered/truncated/incompatible/semantically-invalid backup rejection.

### Local notifications

- Persisted reminder schedules and dedupe keys.
- Permission state handling.
- Android local notification/alarm implementation source.
- iOS/Mac Catalyst UserNotifications implementation source.
- Windows scheduled-toast implementation source.
- Backup reminder coordination.
- Budget threshold reminder coordination.
- Recurring reminder coordination.
- Generic privacy-safe reminder text.
- Stale backup/budget/recurrence schedules cancelled when source state no longer requires them.

### App lock/security

- 4–12 digit PIN validation.
- Random salt and PBKDF2-SHA256-based PIN verifier.
- Platform secure storage for small verifier/security values.
- Persistent enabled marker so missing/corrupt secure-storage material fails closed.
- Failed-attempt counter and bounded escalating local lockout.
- Configurable inactivity auto-lock.
- Optional Android biometrics.
- Optional Apple LocalAuthentication.
- Optional Windows Hello.
- PIN fallback requirement for biometric/Hello unlock.
- Android sensitive-window protection source.
- Supported Windows display-affinity protection source.
- Apple Face ID purpose text included in platform manifests.
- Explicit unsupported/limited platform behavior instead of false universal capture-blocking claims.

### Adaptive UI/settings/developer options

- Mobile bottom-tab hierarchy.
- Tablet/desktop flyout/sidebar hierarchy.
- Runtime adaptive root switching while preserving primary section.
- Onboarding/unlock use adaptive destination.
- Theme/system-light-dark preference.
- Currency/locale/financial-month-start preferences.
- Locale applied at runtime with safe fallback.
- Number/date formatting preview.
- Privacy/hide-amount preferences.
- Reduced-motion and larger-interface preferences.
- Default account/transaction type.
- Notifications/backup reminders.
- Receipt quality/storage controls.
- Auto-lock/biometric/capture preferences.
- Dashboard card preferences.
- Local premium demo flag.
- Hidden developer options behind repeated version taps.
- Schema/feature-flag/reminder-sync tools.
- Local privacy-safe data-integrity checker.
- Typed destructive confirmation for full finance-data reset.
- Typed destructive confirmation for deterministic synthetic sample-data reset.

### Diagnostics/reliability

- Privacy logger that ignores caller-provided properties and stores sanitized event/type tokens only.
- Exception type only rather than exception message/stack in privacy log.
- Bounded/rotated local diagnostic file.
- Explicit sanitized diagnostic export.
- Centralized AppDomain/unobserved-task exception coordination; unobserved task failures are marked observed after privacy-safe capture.
- Startup/lifecycle exception reporting through privacy-safe coordinator.
- Local data-integrity checker for SQLite/FK, transaction/account/currency, transfers, splits, category cycles, budgets/periods, goals/contributions, recurrence, reconciliation, and receipt path/presence/size/SHA-256.
- Sanitized integrity-report export.

### Branding/localization/accessibility source

- Primary SVG icon.
- Adaptive Android foreground source.
- Monochrome icon source.
- Light/dark splash source.
- Branding/store guidance.
- English resource baseline.
- Initial Hindi common-string resource structure.
- Light/dark/system theme support.
- Reduced-motion/larger-interface preferences.
- Text equivalents for chart/report meaning.
- Accessible recurring lifecycle controls.

Full screen-by-screen Hindi translation and native accessibility verification are not represented as completed release validation.

### Tests/quality automation

- Unit test project.
- SQLite integration test project.
- UI-contract test project.
- Money/domain/calculator/culture/PIN/ViewModel tests.
- Budget period/rollover boundary tests.
- Transfer conservation/relation tests.
- Account/reconciliation dependency/overflow tests.
- Recurrence idempotency/payment-link/rule-lifecycle tests.
- Transaction revision/bulk-categorization tests.
- Category reassignment/tag-currency tests.
- Currency-aware CSV import tests.
- Encrypted receipt backup round-trip and graph-validation tests.
- Crash-safe restore recovery tests.
- Schema-v1 → v2 migration test.
- Expanded aggregate data-integrity regression tests.
- Finance/sample-data reset tests.
- Adaptive-navigation/UI source contract tests.
- Dependency-free structural preflight script.
- PowerShell/Bash verification wrappers.
- GitHub Actions structural/core-test/cross-platform MAUI build workflow.
- CodeQL workflow.
- Pull-request dependency-review workflow.
- Dependabot configuration.

## External validation gates not completed by source generation alone

These remain required before representing Finora 0.2.0 as a production store release:

### Compiler/package validation

- Restore the exact NuGet dependency graph on the supported .NET/MAUI release toolchain.
- Execute Release builds and all tests.
- Resolve any compiler/analyzer/test failures.
- Review exact direct/transitive package licenses and security advisories.
- Review GitHub CI/CodeQL/dependency-review results for the final release commit.

The active ChatGPT execution environment used for implementation does not provide a local `dotnet` SDK, so no local `dotnet build`/`dotnet test` success is claimed here.

### Android

- Compile/package signed Release AAB using external signing credentials.
- Validate adaptive/monochrome icon and splash on actual/emulated devices.
- Validate notifications across permission/reboot/doze/force-stop behavior and lifecycle cleanup.
- Validate biometric states and PIN fallback.
- Validate `FLAG_SECURE` behavior/limitations.
- Validate system picker/share, receipt, import/export, backup/restore and interrupted-restore recovery.
- Validate accessibility, theme, layout, large text, reduced motion.
- Validate package upgrade and schema migration.
- Complete Play Console privacy/data-safety/store listing review.

### Windows

- Compile/package final signed Windows release with final identity/publisher.
- Validate Windows Hello and PIN fallback.
- Validate scheduled toasts and stale-reminder cleanup.
- Validate display-affinity capture behavior/limitations.
- Validate file picker/share/export/backup/receipt flows under packaged identity.
- Validate keyboard/focus/resizing/high-DPI/accessibility.
- Validate package upgrade and schema migration.

### iOS

- Build/archive with a supported Mac/Xcode host.
- Configure release provisioning/signing outside Git.
- Validate LocalAuthentication and PIN fallback.
- Validate UserNotifications permission/scheduling/lifecycle cleanup.
- Validate file picker/share/import/export/backup/receipt flows on device/simulator.
- Validate VoiceOver/Dynamic Type/reduced motion/dark mode/layout.
- Validate migration/upgrade and App Store privacy declarations.

### Mac Catalyst

- Build/archive/sign/notarize with supported Apple tooling.
- Validate LocalAuthentication/UserNotifications.
- Validate keyboard/mouse/resizable-window/focus/high-DPI/accessibility.
- Validate file/share/backup/import/export/receipt flows.
- Validate migration/upgrade.

### Cross-platform release QA

- Force-close/reliability tests around critical writes.
- Low-disk/cancelled-picker/permission-revocation/failure-injection tests.
- Wrong/tampered/truncated/semantically-invalid backup tests on release builds.
- Interrupted restore recovery on packaged builds.
- Every released schema migration path on packaged builds.
- Mixed-currency reporting smoke tests.
- Custom-budget period replacement rollback tests.
- Recurring pause/resume/archive + notification cleanup tests.
- Native screen-reader/accessibility passes.
- Exact dependency/license notices.
- External signing-secret handling.
- Final store screenshot/listing review using synthetic data only.

Use `docs/TEST_PLAN.md`, `docs/releases/RELEASE_CHECKLIST.md`, and `docs/releases/STORE_READINESS.md` for the detailed gates.

## Intentionally later-version product work

The following are not unfinished requirements for the current local-first release; they are explicitly later-version product boundaries:

- cloud synchronization;
- remote Finora account/login system;
- collaboration/shared finance data;
- mobile-number authentication;
- server-backed commercial entitlement/licensing;
- automatic exchange-rate conversion.

Any future implementation requires new architecture, privacy, threat-model, retention/deletion, authentication, migration, and server-security work before release.

## Current release decision

**Source implementation is substantially expanded and hardened, but Finora 0.2.0 must not be represented as a production store release until the external compiler/platform/device/store gates above have actual passing evidence.**

No claim is made that Finora is bug-free.
