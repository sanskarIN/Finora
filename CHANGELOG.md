# Changelog

All notable Finora source changes are documented here. Finora currently remains on a development/unreleased line; platform store release is gated by the build/device/store checks in `docs/releases/STORE_READINESS.md`.

## [Unreleased]

### Added

#### Foundation and architecture

- Multi-project .NET MAUI solution separating App, Application, Domain, Infrastructure, and Shared concerns.
- Nullable reference types, warnings-as-errors, deterministic builds, latest-recommended analysis, central package management, `.editorconfig`, `.gitattributes`, and hardened `.gitignore`.
- Apache-2.0 repository policy/documentation, contributor/security/privacy/support/legal files, architecture records, threat model, database documentation, release/test/store-readiness guides.
- Product branding sources for primary/adaptive/monochrome icons and light/dark splash artwork.
- English-first localization resources plus initial Hindi common-string resource structure.

#### Database and money correctness

- SQLite/EF Core local persistence with WAL, foreign keys, busy timeout, indices, and transactional multi-record operations.
- Signed 64-bit integer minor-unit monetary model with `decimal` major-unit parsing/conversion.
- Currency-specific minor-unit precision for known 0-/2-/3-decimal currencies.
- Persistence-boundary validation for account/transaction invariants.
- Database schema version 2.
- Transactional v1 → v2 migration adding transaction revision history, account reconciliation history, notification scheduling state, and attachment original filenames.
- Privacy-safe local data-integrity checker covering SQLite/FK state, transaction/account/currency values, transfer pairing, split totals, category cycles, budgets/periods, goals/contributions, recurrence rule/occurrence state, reconciliation, and receipt path/size/SHA-256 state.

#### Accounts and transfers

- Cash, bank, credit-card, digital-wallet, savings, investment-placeholder, and custom account types.
- Account icon/color/currency/opening-balance/state management.
- Credit-card limit and billing-day 1–31 metadata.
- Account detail/history, archive/restore, and default-account preferences.
- Atomic same-currency paired transfers using shared `TransferGroupId` and reciprocal counterparty accounts.
- Account reconciliation preview/history with explicit adjustment handling.
- Reconciled opening-balance protection.
- Account currency-change protection once transaction/recurrence relationships exist.
- Active recurring dependency protection during account archival.

#### Transactions

- Expense, income, transfer, refund, and adjustment transaction types.
- Quick add with decimal-safe calculator, account/category, date/time, merchant/payee, note, payment method, and manually entered location.
- Advanced search/filtering by account, category, type, date, and text.
- Split transaction editor and tag support.
- Split sign/total/category validation.
- Critical transaction revision history.
- Soft delete/restore with linked transfer handling.
- Bulk categorization and duplicate review.
- Selected/all transaction CSV/PDF export workflow.

#### Categories and tags

- Default/user categories and subcategories.
- Parent-cycle prevention.
- Reorder/archive/restore.
- Safe reassignment and merge workflows.
- Subcategory-budget-safe category reassignment.
- Tag create/update/archive/restore.
- Explicit-currency tag reporting so unlike currencies are not combined.

#### Receipts and attachments

- App-private receipt/document storage.
- Sanitized/generated internal filenames and path confinement.
- Platform-correct path comparison semantics for Windows versus Unix-style targets.
- Allowed receipt/document content types and per-file size limit.
- Asynchronous storage, byte count, SHA-256 metadata, open/delete/storage-usage/orphan-cleanup workflows.
- Receipt bytes included in encrypted backups/restores.

#### Budgets

- Overall, category, and subcategory budgets.
- Weekly, monthly, and custom periods.
- Central `BudgetPeriodPolicy` shared across store/report calculations.
- Monday–Sunday weekly windows and calendar-month windows.
- Custom budgets require explicit non-overlapping periods and are inactive outside those windows.
- Rollover option applied only when enabled; effective plan must remain positive under checked arithmetic.
- Warning threshold with overflow-safe percentage calculation.
- Planned/actual/variance calculations with recursive descendant and split-aware category spending.
- Transactional explicit-period replacement reliability path and rollback regression coverage.
- Local reminder coordination for budget warnings when enabled/authorized.

#### Savings goals

- Target/starting amount, target date, notes, icon, deposit, withdrawal, and linked transaction support.
- Running contribution progress protection from negative/overflowed state.
- Linked transaction currency validation.
- Forecasting/milestone/completion state and reduced-motion-aware completion messaging.

#### Recurring items

- Daily, weekly, monthly, yearly, and custom recurrence intervals.
- Recurring expense/income/transfer/refund templates.
- Start/end, grace period, and reminder lead-time settings.
- Persisted unique occurrence state and idempotent processing with bounded backlog generation.
- Paid, partial-paid, skipped, postponed, and skipped-occurrence reopen workflows.
- Generated transaction/link and recurring transfer-pair validation.
- Recurring-rule Pause, Resume, and Archive lifecycle.
- Resume dependency/end-date revalidation.
- Archived rule hides from active list while keeping occurrence history.
- Stale recurring reminder cleanup during synchronization.

#### Dashboard and reports

- Configurable dashboard cards for balance, income/spending/net, budgets, upcoming obligations, categories, goals, recent transactions, and cash flow.
- Privacy mode/hidden displayed amounts.
- Dashboard aggregate cards explicitly scoped to the configured reporting currency.
- Native currency retained for other-currency transaction/goal/recurrence rows.
- No implicit exchange-rate conversion.
- Split-aware category spending and recursive category-budget reporting.
- Income/expense, balance trend, budget performance, merchant/payee, tag, and monthly-comparison report datasets.
- Tag totals require explicit currency scope.
- Dependency-free MAUI chart rendering with textual/tabular equivalents.

#### CSV import

- System file selection, header detection, explicit column mapping, preview, and transactional commit.
- Required date/type/amount/account mapping with optional currency/category/merchant/note/payment method/location/transfer group/counterparty/tags.
- Currency-specific major/minor-unit handling with decimal-safe conversion.
- UTF-8 validation, file/row limits, quoted-field parsing, category/tag/account resolution, duplicate protection, and transfer-group/counterparty validation.
- Same-batch duplicate protection.
- `long.MinValue` rejection before sign normalization.
- Parse errors counted once.

#### Export

- Rich CSV transaction export fields including identifiers, transfer linkage, payment method, manual location, and tags.
- Multi-page PDF transaction export.
- User-controlled system share/save surfaces.

#### Encrypted backup and restore

- User-created local encrypted backup format.
- PBKDF2-SHA256 password-derived keys with random salt.
- AES-GCM authenticated encryption with random nonce/tag.
- Schema metadata and preview.
- Receipt path/size/SHA-256 validation before backup and restore.
- Complete finance-graph validation before encryption and after authenticated decryption.
- Validation covers IDs, account/currency links, transfers, splits, categories, transaction tags, budgets/periods, goals/contributions, recurrence, attachments, revisions, reconciliation, notification metadata, and snapshot settings boundaries.
- Internal restore markers/settings excluded from snapshot restore.
- Sensitive plaintext/receipt buffers cleared as early as practical after success/failure.
- Crash-safe restore operation gate, journal, pending DB marker, startup recovery, attachment rollback/finalization, and orphan staging cleanup.
- Wrong/tampered/truncated/incompatible/semantically-invalid backup rejection.

#### Notifications

- Persisted local reminder schedules with dedupe keys.
- Permission-gated Android, iOS/Mac Catalyst, and Windows scheduling source paths.
- Backup, budget, and recurring reminder coordination.
- Generic privacy-safe notification content.
- Stale backup/budget/recurrence schedule cancellation after state changes.

#### Privacy and app security

- Optional 4–12 digit PIN lock with random-salt password-based verifier and OS secure storage for small verifier/security values.
- Persistent enabled marker and fail-closed missing/corrupt verifier behavior.
- Bounded escalating failed-attempt lockout and configurable inactivity lock.
- Optional biometric/Windows Hello unlock with PIN fallback.
- Platform sensitive-screen protection where supported.
- Apple Face ID purpose text in platform manifests.
- Privacy mode and hide-amount-on-launch preferences.
- Local premium/demo state explicitly documented as non-tamper-proof.

#### Adaptive UI, localization, and settings

- Mobile bottom-tab and tablet/desktop flyout navigation hierarchies.
- Runtime adaptive root switching with primary-section preservation.
- Onboarding/unlock adaptive routing.
- Runtime locale normalization/application and number/date format preview.
- Light/dark/system theme preferences.
- Reduced-motion and larger-interface preferences.
- Notification/backup reminder/security/default-account/default-transaction/receipt-quality/dashboard preferences.
- Developer options behind repeated version taps.
- Typed destructive confirmation for full finance-data reset and deterministic synthetic sample-data reset.
- Accessible recurring-rule lifecycle and occurrence-reopen controls.

#### Diagnostics and reliability

- Bounded/rotating privacy-safe local diagnostic logger.
- Diagnostic logger intentionally ignores caller-supplied private properties and logs exception type rather than message/stack.
- Centralized unhandled/unobserved exception coordination for privacy-safe startup/lifecycle diagnostics; unobserved task failures are marked observed.
- User-exportable sanitized diagnostic log.
- User-exportable expanded local data-integrity report from hidden developer options.
- Startup activation serialization so DB initialization/restore recovery completes before finance navigation.

#### Tests and repository automation

- Unit tests for money/domain/calculator/culture/PIN/ViewModel/budget-period behavior.
- SQLite integration tests for transfer relations, recurrence payment links/rule lifecycle, account/reconciliation lifecycle, transaction revisions, category mutation safety, currency-aware tag/import/reporting, custom-budget persistence/rollback, backup graph validation, encrypted attachment backup/restore, crash-safe restore recovery, v1 → v2 migration, data reset/sample data, and expanded data-integrity diagnostics.
- UI-contract/navigation/privacy/recovery/recurring-lifecycle/dashboard-currency test coverage.
- Dependency-free repository structural preflight with version/schema/policy/money/privacy checks.
- Cross-platform GitHub Actions structural/core-test/Windows/Android/iOS/Mac Catalyst workflow.
- CodeQL C# analysis workflow.
- Pull-request dependency-review workflow.
- Dependabot configuration for NuGet and GitHub Actions.
- CODEOWNERS and privacy-aware issue/pull-request templates.

### Changed

- Dashboard no longer calls the legacy mixed-currency aggregate API; it derives aggregate cards only from currency-scoped sources.
- Category/tag report contract now carries explicit currency.
- Recurring UI now manages rule lifecycle in addition to due-occurrence lifecycle.
- Reminder synchronization removes obsolete native schedules rather than only adding/updating active schedules.
- Custom budget interpretation moved from duplicated store/report logic to a shared domain policy.
- Backup verification moved from structural/cryptographic-only validation to cryptographic plus complete financial-graph validation.
- Data integrity diagnostics expanded from transaction/attachment-centric checks to the broader persisted finance graph.
- Repository architecture/security/test/release documentation updated to match these invariants.

### Security

- Added platform-correct path confinement, fail-closed PIN storage behavior, bounded lockout arithmetic, crash-safe restore recovery, complete authenticated-backup graph validation, sensitive-buffer clearing, stale-notification cleanup, currency-isolation checks, and expanded privacy-safe integrity diagnostics.

### Known validation boundary

- Native platform compiler/device/store validation is not represented as complete merely because source exists. Android/Windows/iOS/Mac Catalyst Release builds, signing, packaging, notification/biometric/capture behavior, accessibility, upgrade/migration, and store declarations must pass the documented release gates before a production store release.
- The current ChatGPT execution environment does not provide a local `dotnet` SDK, so no local `dotnet build` or `dotnet test` pass is claimed.
