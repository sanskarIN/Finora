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

Money is stored as signed integer minor units plus currency code.

### App-private receipt storage

Receipt/document bytes are stored as files under Finora's app-private data area rather than arbitrary external paths. SQLite stores metadata such as relative path, original filename, content type, byte size, and SHA-256 checksum.

### OS secure storage

Small app-lock verifier/security values can be stored using platform secure storage. Large financial datasets, the SQLite database, receipts, and backups are not placed in secure storage.

### Cache storage

Temporary export/diagnostic/integrity files can be written to Finora's cache directory before the user explicitly shares/saves them through system UI. Cache files are not the system of record and may be removed by the operating system.

## 3. Updates

Ordinary writes use asynchronous SQLite/file operations. Multi-record workflows use relational/database transactions where atomicity matters.

Examples:

- transfers update/create both linked rows together;
- critical transaction edits create local revision history;
- reconciliation can create an explicit adjustment and history entry;
- recurrence due processing persists unique occurrence state;
- mapped CSV import commits validated rows transactionally;
- database migrations update supported schemas transactionally.

## 4. Integrity metadata

Finora keeps local controls to detect inconsistency:

- SQLite foreign keys/indexes;
- transfer group/counterparty relationships;
- split totals;
- unique recurrence occurrence key;
- receipt byte size and SHA-256 checksum;
- schema-version setting.

The hidden developer data-integrity check can inspect SQLite integrity, foreign keys, transfer pairs, split totals, category cycles, recurrence references, and receipt path/size/checksum state. Its exported report contains only health codes/counts rather than private finance contents.

## 5. Notifications

Local reminder schedules are stored locally and mapped to platform notification APIs after permission is granted. Notification title/body is intentionally generic because it can be visible outside the Finora app lock.

No notification workflow uploads the user's finance database.

## 6. Import trust boundary

CSV import begins after explicit file selection. Finora reads the selected file, validates/normalizes mapping/rows locally, shows preview/validation information, and then writes accepted records to SQLite.

The selected source file remains controlled by its original storage/provider; Finora does not delete it.

## 7. Export trust boundary

CSV/PDF exports are generated locally. The user explicitly invokes system share/save UI.

Once another application or location receives an export, the destination's privacy/security/storage lifecycle applies. Finora cannot automatically revoke the exported copy.

## 8. Encrypted backup creation

When requested by the user:

1. Finora reads the supported local finance graph.
2. Receipt paths/files/size/checksums are validated.
3. The snapshot plus receipt bytes is serialized.
4. A key is derived from the user-entered backup password using PBKDF2-SHA256 with a random salt.
5. The payload is encrypted/authenticated with AES-GCM using a random nonce/tag.
6. Finora records privacy-safe local backup metadata/audit state.
7. The encrypted bytes are offered to system share/save UI.

Finora does not automatically upload the backup and does not persist the backup password/derived key.

## 9. Encrypted backup restore

When requested by the user:

1. System file picker supplies the selected backup stream.
2. Finora validates basic format/size and authenticates/decrypts it with the entered password.
3. Schema and attachment metadata/bytes are validated.
4. A preview is displayed before replacement.
5. Receipt files are staged in a temporary private directory.
6. Supported local database records are replaced inside a database transaction.
7. Attachment directories are swapped with rollback handling.
8. Invalid/tampered/incompatible backups fail instead of being silently accepted.

## 10. Diagnostics

The privacy logger stores sanitized event/type tokens only. Caller-supplied private properties, exception messages, and stack traces are intentionally not serialized by this logger.

Sanitized diagnostic and integrity exports are user-initiated. They must not contain account names, merchant/payee names, notes, amounts, manually entered locations, receipt names/contents, PINs, backup passwords, or signing/encryption secrets.

## 11. App lock/security values

PIN setup stores verifier material rather than plaintext PIN. Failed PIN attempts can trigger local lockout. Biometric/Windows Hello uses platform authentication and requires PIN fallback in the current design.

Removing local finance data does not necessarily remove security/preferences automatically; the UI explains which data categories are being deleted.

## 12. Full local finance-data deletion

The explicit Settings deletion flow removes supported local finance records and invokes receipt-file cleanup. The confirmation is intentionally destructive/explicit.

Deletion from Finora cannot delete copies the user previously exported/shared/saved elsewhere.

## 13. Uninstall/reset

The operating system can remove Finora app-private SQLite data, receipts, preferences, secure-storage values, and/or cache data during uninstall/reset depending on platform behavior.

Users who need to preserve finance records should save and verify an external encrypted backup before uninstall/reset. Finora has no automatic cloud recovery service in the current release.

## 14. Logs/cache retention

Finora's privacy diagnostic log is bounded/rotated. Cache exports are temporary and may be removed by the OS. Release/device QA should verify cache cleanup behavior but must not treat cache artifacts as durable records.

## 15. Future cloud/account features

Cloud synchronization, remote account authentication, collaboration, mobile-number authentication, and server-backed entitlement validation are outside the current lifecycle. Adding any of them requires a new server/network data-flow design, privacy update, threat-model update, retention/deletion policy, and user-consent treatment before release.
