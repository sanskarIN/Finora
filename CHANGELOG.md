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
- Database schema version 2.
- Transactional v1 → v2 migration adding transaction revision history, account reconciliation history, notification scheduling state, and attachment original filenames.
- Privacy-safe local data-integrity checker covering SQLite integrity, foreign keys, transfer pairing, split totals, category cycles, recurrence references, and receipt file path/size/SHA-256 state.

#### Accounts and transfers

- Cash, bank, credit-card, digital-wallet, savings, investment-placeholder, and custom account types.
- Account icon/color/currency/opening-balance/state management.
- Credit-card limit/billing metadata.
- Account detail/history, archive/restore, and default-account preferences.
- Atomic same-currency paired transfers using shared `TransferGroupId` and reciprocal counterparty accounts.
- Account reconciliation preview/history with explicit adjustment handling.

#### Transactions

- Expense, income, transfer, refund, and adjustment transaction types.
- Quick add with decimal-safe calculator, account/category, date/time, merchant/payee, note, payment method, and manually entered location.
- Advanced search/filtering by account, category, type, date, and text.
- Split transaction editor and tag support.
- Critical transaction revision history.
- Soft delete/restore with linked transfer handling.
- Bulk categorization and duplicate review.
- Selected/all transaction CSV/PDF export workflow.

#### Categories and tags

- Default/user categories and subcategories.
- Parent-cycle prevention.
- Reorder/archive/restore.
- Safe reassignment and merge workflows.
- Tag create/update/archive/restore and report linkage.

#### Receipts and attachments

- App-private receipt/document storage.
- Sanitized/generated internal filenames and path confinement.
- Allowed receipt/document content types and per-file size limit.
- Asynchronous storage, byte count, SHA-256 metadata, open/delete/storage-usage/orphan-cleanup workflows.
- Receipt bytes included in encrypted backups/restores.

#### Budgets

- Overall, category, and subcategory budgets.
- Weekly, monthly, and custom periods.
- Rollover option, warning threshold, planned/actual/variance calculations, and split-aware category spending.
- Local reminder coordination for budget warnings when enabled/authorized.

#### Savings goals

- Target/starting amount, target date, notes, icon, deposit, withdrawal, and linked transaction support.
- Forecasting/milestone/completion state and reduced-motion-aware completion messaging.

#### Recurring items

- Daily, weekly, monthly, yearly, and custom recurrence intervals.
- Recurring expense/income/transfer/refund templates.
- Start/end, grace period, and reminder lead-time settings.
- Persisted unique occurrence state and idempotent processing.
- Paid, partial-paid, skipped, and postponed workflows.
- Recurring linked transfer creation.

#### Dashboard and reports

- Configurable dashboard cards for balance, income/spending/net, budgets, upcoming obligations, categories, goals, recent transactions, and cash flow.
- Privacy mode/hidden displayed amounts.
- Category spending, income/expense, balance trend, budget performance, merchant/payee, tag, and monthly-comparison report datasets.
- Dependency-free MAUI chart rendering with textual/tabular equivalents.

#### CSV import

- System file selection, header detection, explicit column mapping, preview, and transactional commit.
- Required date/type/amount/account mapping with optional currency/category/merchant/note/payment method/location/transfer group/counterparty/tags.
- Major/minor-unit handling with decimal-safe conversion.
- UTF-8 validation, file/row limits, quoted-field parsing, category/tag/account resolution, duplicate protection, and transfer-group validation.

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
- Staged attachment restore, transactional database replacement, and attachment-directory rollback handling.
- Wrong/tampered/truncated/incompatible backup rejection.

#### Notifications

- Persisted local reminder schedules with dedupe keys.
- Permission-gated Android, iOS/Mac Catalyst, and Windows scheduling source paths.
- Backup, budget, and recurring reminder coordination.
- Generic privacy-safe notification content.

#### Privacy and app security

- Optional 4–12 digit PIN lock with random-salt password-based verifier and OS secure storage for small verifier/security values.
- Escalating failed-attempt lockout and configurable inactivity lock.
- Optional biometric/Windows Hello unlock with PIN fallback.
- Platform sensitive-screen protection where supported.
- Privacy mode and hide-amount-on-launch preferences.
- Local premium/demo state explicitly documented as non-tamper-proof.

#### Diagnostics and reliability

- Bounded/rotating privacy-safe local diagnostic logger.
- Diagnostic logger intentionally ignores caller-supplied private properties and logs exception type rather than message/stack.
- Centralized unhandled/unobserved exception coordination for privacy-safe startup/lifecycle diagnostics.
- User-exportable sanitized diagnostic log.
- User-exportable sanitized local data-integrity report from hidden developer options.

#### Accessibility and settings

- Light/dark/system theme preferences.
- Reduced-motion and larger-interface preferences.
- Localization-ready locale preference.
- Notification/backup reminder/security/default-account/default-transaction/receipt-quality/dashboard preferences.
- Developer options behind repeated version taps.
- Text equivalents for report graphics and documented native accessibility validation gates.

#### Tests and repository automation

- Unit tests for money/domain/calculator behavior.
- SQLite integration tests for transfer invariants, recurrence idempotency, transaction revisions, reconciliation, mapped CSV import, encrypted attachment backup/restore, v1 → v2 migration, and data-integrity diagnostics.
- UI-contract/navigation/privacy/recovery test coverage.
- Dependency-free repository structural preflight.
- Cross-platform GitHub Actions structural/core-test/Windows/Android/iOS/Mac Catalyst workflow.
- CodeQL C# analysis workflow.
- Pull-request dependency-review workflow.
- Dependabot configuration for NuGet and GitHub Actions.
- CODEOWNERS and privacy-aware issue/pull-request templates.

### Changed

- Expanded README, security policy, privacy policy, support guidance, terms, contribution guidelines, code of conduct, architecture/schema/threat-model documentation, test plan, build/troubleshooting documentation, and release/store-readiness checklists to match schema-v2 source.
- Packaged in-app privacy/terms summaries now match the current local-first data lifecycle and security model.
- Diagnostic logging changed from an unbounded event log to bounded/rotated sanitized event/type logging.
- Recurrence processing uses persisted occurrence-first state rather than creating duplicate financial transactions on repeated scheduler/startup passes.
- Project status now distinguishes implemented source from native compiler/device/store validation.

### Security

- Added receipt safe-path/checksum validation, authenticated backup validation/restore staging, PIN rate limiting, biometric PIN fallback, platform capture controls where available, privacy-safe reminder contents, data-integrity diagnostics, CodeQL, dependency review, expanded secret/private-artifact exclusions, and private vulnerability-report routing.

### Known validation boundary

- Native platform compiler/device/store validation is not represented as complete merely because source exists. Android/Windows/iOS/Mac Catalyst release builds, signing, packaging, notification/biometric/capture behavior, accessibility, upgrade/migration, and store declarations must pass the documented release gates before a production store release.
