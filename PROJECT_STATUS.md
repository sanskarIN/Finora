# Finora Project Status

Last status refresh: 2026-08-09  
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
- Decimal-only quick calculator expression engine.
- Account/transaction currency validation.
- Same-currency transfer equal/opposite pairing.
- Domain validation for accounts/transactions/recurrence and related finance workflows.

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

### Accounts

- Cash, bank, credit-card, digital-wallet, savings, investment-placeholder, and custom account types.
- Name/icon/color/currency/opening balance/current balance.
- Active/hidden/archived states.
- Credit limit/billing-day metadata.
- Account detail/history.
- Edit/archive/restore workflow.
- Default account preference.
- Same-currency paired transfer workflow.
- Account reconciliation preview/history with optional explicit adjustment transaction.

### Transactions

- Expense, income, transfer, refund, and adjustment.
- Quick-add form.
- Amount/account/category/date/time/merchant-payee/note/payment method/manual location fields.
- Decimal-safe calculator keypad.
- Search and advanced account/category/type/date/text filters.
- Transaction detail/edit workflow.
- Split transactions.
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
- Parent/subcategory assignment.
- Parent-cycle prevention.
- Reorder.
- Archive/restore.
- Safe reassignment/merge.
- Tag create/edit/archive/restore.
- Tag-linked reporting data.

### Receipts/attachments

- Transaction attachment metadata.
- App-private receipt/document file storage.
- Generated internal filenames and sanitized original filenames.
- Canonical safe-path confinement.
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
- Explicit budget periods.
- Rollover option.
- Warning threshold.
- Planned/actual/variance reporting.
- Category descendant/split-aware calculations.
- Reminder coordination where notification permission/settings allow.

### Savings goals

- Name/icon/target/starting amount/target date/notes.
- Contributions and withdrawals.
- Optional linked account transaction.
- Forecast/progress/milestones/completion state.
- Reduced-motion-aware completion behavior.

### Recurring items

- Daily/weekly/monthly/yearly/custom intervals.
- Recurring expense/income/transfer/refund templates.
- Start/end date.
- Grace period and reminder lead time.
- Persisted unique `(RecurrenceRuleId, DueOn)` occurrence state.
- Idempotent due processing.
- Paid/partial-paid/skipped/postponed workflows.
- Generated transaction linkage.
- Recurring transfer pair creation.

### Dashboard/reports

- Configurable dashboard cards for balance, income/spending/net, budget, upcoming recurring items, top categories, goals, recent transactions, and cash flow.
- Financial-month-start handling.
- Privacy mode that hides displayed amounts.
- Category spending report data.
- Income-versus-expense data.
- Account balance trend.
- Budget performance.
- Merchant/payee data.
- Tag data.
- Monthly comparison data.
- MAUI-drawn visual report surface with textual/tabular equivalent representation.

### CSV import

- System file selection.
- Header discovery and explicit mapping.
- Mapping preview/validation.
- Required date/type/amount/account mapping.
- Optional currency/category/merchant/note/payment method/manual location/transfer group/counterparty/tags mapping.
- Major/minor-unit option.
- Decimal-safe major-unit conversion.
- UTF-8 validation.
- File-size and row-count limits.
- Quoted-field parsing.
- Account/category/tag resolution.
- Optional missing-category creation.
- Duplicate protection.
- Transfer-group validation.
- Transactional import commit.
- Explicit row/import errors.

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
- Backup metadata/audit state without backup password/key storage.
- Staged attachment restore.
- Transactional database replacement.
- Attachment-directory swap/rollback handling.
- Wrong/tampered/truncated/incompatible backup rejection.

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

### App lock/security

- 4–12 digit PIN validation.
- Random salt and PBKDF2-SHA256-based PIN verifier.
- Platform secure storage for small verifier/security values.
- Failed-attempt counter and escalating local lockout.
- Configurable inactivity auto-lock.
- Optional Android biometrics.
- Optional Apple LocalAuthentication.
- Optional Windows Hello.
- PIN fallback requirement for biometric/Hello unlock.
- Android sensitive-window protection source.
- Supported Windows display-affinity protection source.
- Explicit unsupported/limited platform behavior instead of false universal capture-blocking claims.

### Settings/developer options

- Theme/system-light-dark preference.
- Currency/locale/financial-month-start preferences.
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

### Diagnostics/reliability

- Privacy logger that ignores caller-provided properties and stores sanitized event/type tokens only.
- Exception type only rather than exception message/stack in privacy log.
- Bounded/rotated local diagnostic file.
- Explicit sanitized diagnostic export.
- Centralized AppDomain/unobserved-task exception coordination.
- Startup/lifecycle exception reporting through privacy-safe coordinator.
- Local data-integrity checker for SQLite integrity, foreign keys, transaction/account reference/currency state, transfer pairs, split totals, category cycles, recurrence references, and receipt path/presence/size/SHA-256.
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

Full screen-by-screen Hindi translation and native accessibility verification are not represented as completed release validation.

### Tests/quality automation

- Unit test project.
- SQLite integration test project.
- UI-contract test project.
- Money/domain tests.
- Decimal calculator tests.
- Transfer conservation tests.
- Recurrence idempotency/no-transaction-until-paid tests.
- Transaction revision/bulk-categorization tests.
- Reconciliation tests.
- User-mapped CSV import tests.
- Encrypted receipt backup round-trip test.
- Schema-v1 → v2 migration test.
- Data-integrity healthy/broken-transfer tests.
- Dependency-free structural preflight script.
- PowerShell full verification wrapper.
- GitHub Actions structural/core-test/cross-platform MAUI build workflow.
- CodeQL workflow.
- Pull-request dependency-review workflow.
- Dependabot configuration.

## External validation gates not completed by source generation alone

These remain required before representing Finora 0.2.0 as a production store release:

### Compiler/package validation

- Restore the exact NuGet dependency graph on the supported .NET/MAUI release toolchain.
- Execute `dotnet format`, Release build, and all tests.
- Resolve any compiler/analyzer/test failures.
- Review exact direct/transitive package licenses and security advisories.
- Review GitHub CI/CodeQL/dependency-review results for the final release commit.

The active ChatGPT execution environment used for implementation did not provide a local `dotnet` SDK, so no local `dotnet build`/`dotnet test` success is claimed here.

### Android

- Compile/package signed Release AAB using external signing credentials.
- Validate adaptive/monochrome icon and splash on actual/emulated devices.
- Validate notifications across permission/reboot/doze/force-stop behavior.
- Validate biometric states and PIN fallback.
- Validate `FLAG_SECURE` behavior/limitations.
- Validate system picker/share, receipt, import/export, backup/restore.
- Validate accessibility, theme, layout, large text, reduced motion.
- Validate package upgrade and schema migration.
- Complete Play Console privacy/data-safety/store listing review.

### Windows

- Compile/package final signed Windows release with final identity/publisher.
- Validate Windows Hello and PIN fallback.
- Validate scheduled toasts.
- Validate display-affinity capture behavior/limitations.
- Validate file picker/share/export/backup/receipt flows under packaged identity.
- Validate keyboard/focus/resizing/high-DPI/accessibility.
- Validate package upgrade and schema migration.

### iOS

- Build/archive with a supported Mac/Xcode host.
- Configure release provisioning/signing outside Git.
- Validate LocalAuthentication and PIN fallback.
- Validate UserNotifications permission/scheduling.
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
- Wrong/tampered/truncated backup tests on release builds.
- Every released schema migration path on packaged builds.
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
- server-backed commercial entitlement/licensing.

Any future implementation requires new architecture, privacy, threat-model, retention/deletion, authentication, migration, and server-security work before release.

## Current release decision

**Source implementation is substantially expanded and internally documented, but Finora 0.2.0 must not be represented as a production store release until the external compiler/platform/device/store gates above have actual passing evidence.**

No claim is made that Finora is bug-free.
