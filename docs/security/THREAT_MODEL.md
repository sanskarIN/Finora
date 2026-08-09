# Finora Threat Model

## Scope

This threat model covers the current local-first release. Finora does not require an account or cloud service and does not automatically upload financial data or backups. Future cloud/account/remote-entitlement features require a new threat-model section before implementation.

## Assets

High-value assets include finance records, account/budget/goal/recurrence/reconciliation data, receipt files, SQLite/WAL state, encrypted backups, transient backup passwords, app-lock verifier material, platform secure-storage values, restore-recovery metadata, sanitized diagnostics, and release-signing credentials held outside the repository/application.

## Trust boundaries

1. **Finora process ↔ app-private storage** — SQLite, receipts, and restore-recovery artifacts are trusted only after schema/path/integrity validation.
2. **Finora ↔ OS secure storage** — small verifier/security values only; never the finance database or receipt tree.
3. **Finora ↔ system picker/share sheet** — finance data leaves app-private storage only after explicit user action; destination security is controlled by the OS/user-selected app.
4. **Finora ↔ notification subsystem** — reminder content may appear outside app lock and must remain generic.
5. **Finora ↔ biometric/Windows Hello APIs** — platform result is an optional unlock factor with PIN fallback, not a replacement for Finora's locked state.
6. **Finora ↔ GitHub/NuGet/build infrastructure** — dependency/action/build inputs require review, CI, static analysis, and release validation.
7. **Release engineer ↔ signing infrastructure** — signing secrets stay outside source control.

## Lost/casually accessed device

Controls include optional 4–12 digit PIN, PBKDF2-SHA256 verifier with random salt, OS secure storage, bounded escalating lockout, inactivity auto-lock, biometric/Hello with PIN fallback, privacy mode, and supported-platform screen-capture protection.

### Fail-closed verifier state

A persisted non-secret “PIN enabled” marker prevents a missing/corrupt secure-storage verifier from becoming a fail-open condition. If the marker indicates app lock is enabled but verifier material is unavailable/malformed, PIN verification fails and failure/lockout policy is applied rather than unlocking.

Residual risk: a device/OS compromise can bypass app-level controls. Fail-closed secure-storage loss can also require recovery/reinstallation rather than weakening the lock.

## Screenshot/screen-recording leakage

Android uses secure-window behavior; supported Windows paths use display-affinity behavior. Unsupported platforms report the limitation. Privacy mode can hide amounts. No app control prevents an external camera or OS-compromise capture.

## Database corruption/partial writes

Controls:

- SQLite relational persistence, foreign keys, WAL, busy timeout;
- transactions for multi-record finance workflows;
- persistence-boundary Account/FinanceTransaction validation;
- linked transfer invariants;
- unique recurrence occurrence index;
- transactional migrations;
- privacy-safe integrity checker;
- automated unit/integration/migration/recovery tests.

Integrity diagnostics detect SQLite/foreign-key issues, unsafe transaction amounts/signs/currencies, transfer/split/category/recurrence inconsistencies, and receipt path/size/hash problems without logging finance contents.

## Cross-resource restore interruption

Threat: the SQLite transaction and receipt-directory swap cannot share one filesystem/database transaction. A process kill between them could expose a new database with old receipts or an old database with new receipts.

Controls:

- production restore runs through `CrashSafeBackupService`;
- a random restore ID is written as transient `internal.restore.commit` metadata before replacement;
- app-private recovery journal records safe operation/directory state only;
- current receipt tree is copied to a private rollback directory before destructive replacement;
- validated encrypted restore executes underneath;
- the committed restore transaction removes the pending marker as part of replacing non-schema app settings;
- startup recovery runs before normal navigation;
- matching pending marker means DB restore did not commit and receipts roll back;
- absent matching marker means DB replacement committed and new receipts are finalized;
- stale staging/rollback directories are cleaned only after journal resolution;
- if safe recovery cannot be completed, normal app initialization is blocked instead of exposing mismatched state.

Recovery metadata must never contain backup passwords, derived keys, account names, merchant/payee text, amounts, notes, locations, or receipt filenames/contents.

Residual risk: severe filesystem/hardware loss can destroy both live and rollback copies. Users still need separately saved encrypted backups.

## Transfers

Threat: one transfer half changes/deletes independently or no longer balances.

Controls: shared `TransferGroupId`, same currency, equal/opposite checked integer amounts, reciprocal counterparties, paired edit/delete/restore workflows, persistence/integrity checks, and integration tests. Cross-currency paired transfers are rejected until a deliberate exchange workflow exists.

## Multi-currency correctness

Threat: unrelated currencies are added together or one amount is displayed with another currency label.

Controls:

- each account/transaction/budget/goal/recurrence record retains currency metadata;
- money stays integer minor units with currency-aware display/conversion precision;
- aggregate reports require an explicit reporting currency;
- dashboard/report totals filter to that currency;
- other currencies remain separate with explicit explanatory UI;
- Finora does not invent exchange rates.

Release QA must verify the built-in minor-unit precision metadata required by supported markets. Currency precision metadata is not an exchange-rate source.

## Recurrence duplication/state corruption

Controls: persisted occurrence rows, unique `(RecurrenceRuleId, DueOn)`, idempotent due processing, transaction generation only from paid/partial-paid workflow, explicit skipped→reopen transition, repeated full-payment idempotency, and account/currency availability guards.

## Receipt/path traversal/tampering

Controls include generated private paths, canonical path confinement to the receipt root, file/type/size limits, SHA-256/byte-count metadata, backup/restore validation, and integrity checks. A privileged attacker who can rewrite both DB metadata and files remains outside app-level protection.

## Backup theft/tampering

Controls:

- PBKDF2-SHA256 password-derived key with random salt;
- AES-GCM authenticated encryption with random nonce/tag;
- authenticated Finora format magic;
- derived key zeroing;
- strict length/schema/path/size/hash validation;
- explicit user-created backup only;
- no automatic upload;
- serialized backup/preview/restore operations.

Weak/reused backup passwords remain susceptible to offline guessing. Finora cannot recover a forgotten backup password.

## CSV import abuse/corruption

Controls include UTF-8 validation, file/row limits, explicit mapping/preview, currency-aware decimal-to-minor conversion, overflow/`long.MinValue` rejection, account/category/type/date/currency validation, transfer-pair/counterparty checks, within-batch duplicate protection, tags, transactional commit, and explicit row errors.

Semantic correctness still requires user review.

## Logs/diagnostics leakage

Privacy logger emits event/type metadata only and keeps bounded files. Unhandled/unobserved failures are captured without exception messages/stacks/finance payloads. Integrity reports expose codes/counts only.

Forbidden diagnostic content includes monetary amounts, account names, merchant/payee names, notes, manually entered locations, receipt names/contents, PINs, backup passwords, encryption keys, signing material, or raw transaction revision snapshots.

## Notifications

Local reminders are permission-gated/deduplicated and use generic text. No merchant/amount/note/location belongs in lock-screen notifications.

## Local premium tampering

The local premium flag is development/demo state only. It is not commercial entitlement. Paid entitlement requires future store/server validation and a new threat assessment.

## Supply chain

Controls include central package versions, Dependabot, dependency review, CodeQL, warnings-as-errors/recommended analyzers, code ownership, structural preflight, small dependency surface, and release-time exact dependency/license review. Mutable action tags remain a residual risk; high-assurance releases may pin reviewed immutable action commits after compatibility validation.

## Secret/signing-material exposure

No production signing secrets belong in source, diagnostics, issue attachments, test data, or backups. `.gitignore`, review templates, security guidance, and release checklists reinforce this boundary.

## Privacy principles

- No required account/login in current release.
- No analytics/advertising telemetry by default.
- No background location collection; transaction location is manually entered only.
- No automatic backup upload.
- System pickers/share sheets only after explicit user action.
- Full finance reset is explicit/confirmed and preserves app-lock/preferences/schema metadata by design.
- Developer sample reset requires typed destructive confirmation and creates synthetic data only.
- Uninstalling without separately saved backup may remove local data.

## Security regression gates before release

Verify PIN/biometric/capture paths, fail-closed secure-storage loss, wrong/tampered backup rejection, interrupted restore at every journal/copy/DB/swap/finalization phase, migration + integrity checks, currency-isolated reports/import precision, generic notifications, sanitized logs/reports, no new account/network/telemetry requirement, exact dependency/license review, and external signing-secret handling.

## Out of scope for current release

Cloud sync, remote account authentication, collaboration service, server entitlement, remote key escrow/recovery, and automatic exchange-rate conversion. Adding any requires new architecture/privacy/security analysis before implementation.
