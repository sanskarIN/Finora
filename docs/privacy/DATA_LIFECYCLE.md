# Finora Data Lifecycle

This document describes how current-release data enters, moves through, leaves, and is deleted from Finora. The current product is local-first and does not require a Finora cloud account.

## 1. Creation

Data can enter Finora through:

- onboarding preferences and optional opening balance;
- user-created accounts/categories/tags/budgets/goals/recurring rules;
- transaction quick-add/detail/edit workflows;
- linked transfers;
- reconciliation workflow;
- receipt/document selection;
- CSV import selected through system UI;
- encrypted backup restore selected through system UI;
- local platform permission/settings choices.

Location data is not collected automatically. The transaction location field exists only when the user manually enters text.

## 2. Primary local storage

### SQLite

The local SQLite database stores structured finance data including accounts, transactions, categories/tags, budgets, goals, recurrence state, transaction revisions, reconciliation history, reminder scheduling metadata, settings, audit metadata, and backup metadata.

Money is stored as signed integer minor units plus currency code. Major-unit input is converted with `decimal`; known currency precision is not universally assumed to be two decimals.

Added/modified schema-v2 rows also pass Domain validation at the EF `SaveChanges` boundary. This covers account/transaction/split/category/tag/budget/goal/recurrence plus attachment, revision, reconciliation, notification, setting, audit, and backup metadata shape before SQLite persistence.

### App-private receipt storage

Receipt/document bytes are stored as files under Finora's app-private `attachments` data area rather than arbitrary external paths. SQLite stores metadata such as relative path, original filename, content type, byte size, and SHA-256 checksum.

Path resolution is confined to that attachment root using platform-correct case sensitivity. Existing path components are additionally rejected when they are symbolic links/reparse points. Cleanup, backup validation, restore staging, restore recovery, and rollback copying reuse the same no-link physical-path policy.

### OS secure storage

Small app-lock verifier/security values can be stored using platform secure storage. Large financial datasets, SQLite database, receipts, and backups are not placed in secure storage.

PIN verifier material consists of a random salt plus PBKDF2-SHA256-derived verifier, not plaintext PIN. Direct PIN values must contain 4–12 ASCII digits before hashing. Byte-array verifier/salt/derived buffers are cleared after use where managed APIs permit it.

A local PIN-enabled preference supports fail-closed behavior if the secure-storage provider is temporarily unavailable. If secure storage is readable and verifier material is actually missing/malformed, stale lock markers are removed so the app cannot remain permanently trapped behind a verifier that no longer exists.

### Cache storage

Temporary transaction CSV/PDF exports, encrypted backup share copies, and integrity-report share copies can be written to Finora's cache directory before the user explicitly shares/saves them through system UI. Cache files are not system-of-record data and may be removed by the operating system.

On serialized startup, Finora performs best-effort cleanup of only known Finora share-copy patterns older than 24 hours. Fresh share copies remain to avoid racing a system share sheet. Diagnostic logs and unrelated cache files are excluded from this cleanup.

## 3. Secret entry lifecycle

Backup passwords and new/confirmed PINs use masked `Entry` controls instead of ordinary text prompts.

- backup password is used only for the requested create/preview/restore operation and is not persisted by Finora;
- backup password field is cleared after operation success/failure/cancel return paths handled by the Settings flow;
- new/confirm PIN fields are cleared after setup/change/removal attempts;
- lock-screen PIN entry is masked and cleared after verification attempts;
- biometric failure returns generic PIN-fallback guidance rather than raw provider text.

Managed `string` values cannot be deterministically zeroed by application code; clearing UI references reduces retention but is not equivalent to guaranteed immediate memory erasure.

## 4. Updates

Ordinary writes use asynchronous SQLite/file operations. Multi-record workflows use relational/database transactions where atomicity matters.

Examples:

- transfers update/create both linked rows together;
- critical transaction edits create local revision history;
- reconciliation can create an explicit adjustment and history entry;
- recurrence due processing persists unique occurrence state;
- recurring-rule pause/resume/archive changes persisted lifecycle state;
- custom-budget explicit-period replacement is one logical database operation;
- mapped CSV import commits validated rows transactionally;
- database migrations update supported schemas transactionally;
- notification replacement schedules the new OS reminder before committing replacement state and only then attempts stale OS cancellation.

Account currency is not allowed to change after transaction/recurrence dependencies exist. Active recurrence must be paused/completed/archived before its account can be archived.

## 5. Currency-scoped aggregation

Finora does not silently convert currencies or invent exchange rates.

- Dashboard aggregate totals use configured reporting currency.
- Other-currency transaction/goal/recurrence rows retain own currency.
- Category/merchant/monthly/tag report aggregates are currency-scoped.
- Same-currency transfer remains the only current transfer model.
- CSV major-unit conversion respects currency-specific minor-unit precision.

This prevents unrelated minor-unit values such as INR and USD from being added and presented as one valid total.

## 6. Integrity metadata and diagnostics

Finora keeps local controls to detect inconsistency:

- SQLite foreign keys/indexes;
- EF Added/Modified structural Domain validation;
- account/transaction currency relationships;
- transfer group/counterparty relationships;
- transaction deletion-state/timestamp agreement;
- split signs/totals/category links;
- category hierarchy;
- unique recurrence occurrence key;
- budget/custom-period relationships;
- savings contribution/link/completion state;
- reconciliation arithmetic/adjustment link;
- receipt path/content type/size/checksum metadata;
- physical no-link receipt paths;
- schema-version setting.

The hidden developer data-integrity check can inspect SQLite integrity, foreign keys, transaction values, transfer pairs, split totals, category cycles, budgets/periods, savings contributions, recurrence dependencies/payment state, reconciliation links, and receipt parent/path/size/checksum state.

Its exported report contains health codes/counts rather than private finance contents.

## 7. Notifications

Local reminder schedules are stored locally and mapped to platform notification APIs after permission is granted. Notification title/body is intentionally generic because it can be visible outside Finora app lock.

Reminder synchronization is stateful:

- disabling backup reminders cancels stale backup schedule;
- budget schedules are removed when no current threshold condition needs them;
- paused/completed/archived recurring rules have stale recurrence schedules cancelled;
- failed deduplicated replacement leaves previous enabled reminder untouched;
- successful replacement persists new/disabled-old database state before stale OS cancellation;
- expired enabled rows are disabled during reconciliation;
- disabled/expired OS IDs receive best-effort cancellation retries.

No notification workflow uploads the user's finance database.

## 8. Recurring lifecycle

A recurring rule may be Active, Paused, Completed, or Archived.

- Active rules can prepare due occurrences.
- Paused rules stop generation without deleting history.
- Resume revalidates end date/account/category/currency dependencies.
- Archived rules are removed from active rule lists but occurrence history remains.
- A due occurrence persists independently and can be Pending, Paid, PartiallyPaid, Skipped, or Postponed; skipped occurrences can be explicitly reopened.
- Paid/partial state must have valid generated transaction/payment data.
- A paid occurrence can retain a valid historical postponed date to preserve when payment actually became due.
- Pending/skipped state cannot silently carry generated payment/postponement data.

## 9. Budget period lifecycle

Budget periods use one shared interpretation policy:

- explicit periods cannot overlap;
- weekly generated windows are Monday–Sunday;
- monthly generated windows are calendar months;
- custom budgets require explicit periods and are inactive outside them;
- rollover applies only when enabled;
- checked effective planned amount must remain positive.

Replacing explicit periods is intended to be atomic so failed replacement does not leave budget without its prior valid period set.

## 10. Import trust boundary

CSV import begins after explicit file selection. Finora reads selected file, validates/normalizes mapping/rows locally, shows preview/validation information, and then writes accepted records to SQLite.

Import controls include UTF-8/file/row limits, currency-specific decimal conversion, account/category/tag resolution, duplicate protection, transfer pair/counterparty checks, and `long.MinValue` rejection before sign normalization.

Selected source file remains controlled by its original storage/provider; Finora does not delete it.

## 11. Export/share trust boundary

CSV/PDF exports are generated locally. The user explicitly invokes system share/save UI.

App-owned cache share copies are eligible for stale cleanup only after the 24-hour grace period and only when filenames match managed Finora patterns. A file symlink entry is deleted as the entry; cleanup does not intentionally traverse into its target.

Once another application/location receives an export, backup, or report, that destination's privacy/security/storage lifecycle applies. Finora cannot automatically revoke or delete destination copies.

## 12. Encrypted backup creation

When requested by the user:

1. Finora reads supported local finance graph.
2. Receipt paths/files/size/checksums and physical no-link confinement are validated.
3. Financial graph relationships/invariants are validated before encryption.
4. Snapshot plus receipt bytes is serialized.
5. A key is derived from user-entered backup password using PBKDF2-SHA256 with random salt.
6. Payload is encrypted/authenticated with AES-GCM using random nonce/tag.
7. Finora records privacy-safe local backup metadata/audit state.
8. Serialized plaintext and every accumulated receipt byte buffer are cleared as early as practical on success or any later-file/query/validation/encryption failure path.
9. Encrypted bytes are written to a Finora cache share copy and offered to system share/save UI.
10. UI-side encrypted byte array is cleared after write/share handling.

Finora does not automatically upload the backup and does not persist backup password/derived key.

## 13. Encrypted backup preview/restore

When requested by the user:

1. System file picker supplies selected backup stream.
2. Finora validates basic format/size and authenticates/decrypts it with entered password.
3. Schema, unique identifiers, attachment metadata/bytes, and complete supported financial graph are validated.
4. Decrypted receipt buffers are cleared if graph/attachment validation rejects snapshot.
5. Invalid account/currency, transfer, split, category/tag, budget/period, goal/contribution, recurrence, reconciliation, notification, or settings relationships fail before destructive replacement.
6. Internal restore markers/settings are not accepted from snapshot.
7. Preview is displayed before replacement.
8. Receipt files are staged in a private no-link temporary directory.
9. Crash-safe wrapper snapshots prior receipt tree using no-link traversal and records recovery state.
10. Supported local database records are replaced inside a database transaction.
11. Local DB commit marker distinguishes pre-commit from post-commit recovery.
12. Attachment directories are swapped/finalized with rollback handling.
13. On next startup, recovery runs before finance navigation: pending marker restores previous tree; committed marker absence finalizes new tree.
14. Recovery journal, staged paths, rollback paths, and cleanup refuse symbolic-link/reparse traversal.
15. Stale staging/rollback directories are cleaned only after recovery decision.
16. Invalid/tampered/incompatible backups fail instead of being silently accepted.

## 14. Diagnostics

Privacy logger stores sanitized event/type tokens only. Caller-supplied properties, exception messages, stack traces, filesystem paths, and provider details are intentionally not serialized.

- log current file is bounded and rotates to at most one previous file;
- diagnostic root/current/previous paths reject symbolic-link/reparse traversal;
- unexpected `AsyncCommand` failures are contained and routed to privacy logger;
- bound ViewModel infrastructure errors use generic text while deliberate short validation messages can remain actionable;
- primary Reports/Settings infrastructure alerts use generic messages and log exception type/event token separately;
- unobserved task exceptions are captured in sanitized path and marked observed after handling.

Sanitized diagnostic/integrity exports are user-initiated. They must not contain account names, merchant/payee names, notes, amounts, manually entered locations, receipt names/contents, PINs, backup passwords, or signing/encryption secrets.

## 15. App lock/security values

PIN setup stores verifier material rather than plaintext PIN. Failed PIN attempts can trigger bounded escalating local lockout. Biometric/Windows Hello uses platform authentication and requires PIN fallback in current design.

PIN removal updates biometric preference only after verifier removal succeeds. Secure-storage removal failure is reported generically and does not falsely announce success.

Removing local finance data does not necessarily remove security/preferences automatically; UI explains which data categories are being deleted.

## 16. Android automatic backup/device transfer

Current Android package configuration keeps `android:allowBackup="false"` and additionally ships explicit rules:

- legacy full-backup exclusions for root/file/database/shared preferences/external domains;
- Android 12+ cloud-backup exclusions for same domains;
- Android 12+ device-transfer exclusions for same domains.

These controls are intended to keep Finora's private finance store outside ordinary OS backup/transfer flows. Explicit encrypted Finora backup remains the supported portable backup path. Privileged/root/device-management tooling remains outside application-level guarantees.

## 17. Full local finance-data deletion

Explicit Settings deletion flow removes supported local finance records, including user-created categories and schema-v2 finance tables, and invokes receipt-file cleanup. Schema metadata/preferences needed to keep application operable are not silently reinterpreted as finance records.

Confirmation is intentionally destructive/explicit and uses typed confirmation phrase.

Deletion from Finora cannot delete copies user previously exported/shared/saved elsewhere.

## 18. Deterministic sample-data reset

Hidden developer reset uses synthetic local data only and requires separate typed destructive confirmation. It is intended for development/testing, not preserving real user finance history.

## 19. Uninstall/reset

Operating system can remove Finora app-private SQLite data, receipts, preferences, secure-storage values, and/or cache data during uninstall/reset depending on platform behavior.

Users who need to preserve finance records should save and verify an external encrypted backup before uninstall/reset. Finora has no automatic cloud recovery service in current release.

## 20. Logs/cache retention

- privacy diagnostic current log is bounded and rotates to one previous log;
- managed cache share copies older than 24 hours are best-effort deleted on startup;
- fresh share copies remain temporarily;
- OS may remove cache sooner;
- destination copies saved/shared by user are outside Finora retention control;
- cache artifacts are never treated as durable finance records.

## 21. Future cloud/account/exchange features

Cloud synchronization, remote account authentication, collaboration, mobile-number authentication, server-backed entitlement validation, and automatic exchange-rate conversion are outside current lifecycle. Adding any requires new server/network data-flow design, privacy update, threat-model update, retention/deletion policy, user-consent treatment, and migration strategy before release.
