# Finora Data Lifecycle

Finora's current release is local-first and requires no account/login. This document describes how user finance data is created, stored, transformed, exported, backed up, restored and deleted.

## 1. Creation

Finance data is created only through local app actions such as onboarding/opening balance, accounts, transactions/transfers, categories/tags, budgets, savings goals, recurring items, reconciliation, CSV import and optional receipt attachment.

Transaction location is manually entered. Finora does not collect background location in the current release.

The hidden developer sample-reset action is separate from normal user data creation. It requires typed destructive confirmation and creates deterministic **synthetic** records only after clearing existing finance data.

## 2. Local persistence

Structured finance records are stored in the app-private SQLite database. Monetary values use integer minor units plus currency code. Major-unit parsing/formatting uses decimal arithmetic and currency-aware precision metadata.

Receipt/document bytes are stored under the app-private `attachments` tree. SQLite stores receipt metadata such as transaction link, safe relative path, original filename, content type, size and SHA-256 checksum.

Small PIN verifier material is stored with OS secure storage. Non-secret app preferences use platform preferences. The full database/receipt tree is not stored in secure storage.

## 3. Runtime processing

Dashboard/report aggregation requires one explicit reporting currency. Other currencies remain separate; Finora does not silently convert/add them or automatically fetch exchange rates.

Runtime locale affects date/number formatting, not stored integer financial values.

Recurring processing creates persisted unique occurrences first. A finance transaction is created only from paid/partial-paid workflow, preventing restart-driven duplication.

## 4. Notifications

Opt-in local notification schedule state is stored locally. Notification content is intentionally generic because it may appear outside Finora's app lock. Finance records remain the source of truth; notification delivery is not.

## 5. Receipt lifecycle

A selected receipt is copied into app-private storage after validation. Finora can open/delete the local attachment and clean orphan files. Integrity checks can validate path confinement, existence, byte size and checksum.

A receipt leaves app-private storage only through an explicit user export/share/backup path.

## 6. CSV import

The user explicitly selects a CSV and maps columns. Finora validates/normalizes rows locally and commits valid supported records transactionally. Import does not automatically upload the CSV.

Currency precision, overflow/extreme values, account/category/tag/transfer relationships and likely duplicates are checked before commit.

## 7. Export/share

CSV/PDF exports and sanitized diagnostics are created only after user action and then handed to the system share/save UI.

Once the user chooses an external destination/app, that destination's privacy/security/storage behavior applies. Finora does not automatically transmit the export elsewhere.

## 8. Encrypted backup creation

The user explicitly requests a backup and supplies a password. Finora serializes supported local finance data plus validated receipt bytes, derives an encryption key using PBKDF2-SHA256 with a random salt and protects the payload with AES-GCM authenticated encryption.

The derived key is not persisted. The backup file is handed to a system share/save surface only after explicit user action. Finora does not automatically upload backups.

## 9. Backup preview/restore

Preview decrypts/validates the selected backup locally using the supplied password and shows safe metadata such as schema/counts. Restore validates schema, attachment metadata/bytes and encrypted integrity before replacement.

Production restore spans SQLite plus the receipt file tree, so it uses a crash-recovery protocol:

1. recover any previous interrupted restore;
2. write a transient random restore-operation marker in local app settings;
3. write an app-private recovery journal containing operation/directory state only;
4. copy the current receipt tree to an app-private rollback directory;
5. execute the validated encrypted database/receipt restore;
6. infer DB commit from the transient marker's presence/absence;
7. roll receipts back when DB replacement did not commit or finalize new receipts when it did;
8. remove recovery artifacts after safe resolution.

Startup performs recovery before normal finance navigation. If safe automatic recovery cannot complete, Finora blocks normal initialization rather than silently exposing a database/receipt mismatch.

Recovery journal/marker metadata does **not** contain backup passwords, derived keys, account names, merchant/payee names, notes, amounts, manual locations or receipt contents.

## 10. Diagnostics/integrity reports

Diagnostic logs are bounded and intentionally omit private finance payloads. The local integrity report exposes issue codes/counts rather than names/amounts/notes/receipt filenames.

Unhandled/unobserved exception capture records event/type metadata only, not exception messages/stacks containing potential private context.

## 11. App lock

When a PIN is enabled, small verifier material is held in OS secure storage and a non-secret enabled marker is retained in preferences. If verifier material is missing/malformed while the marker remains enabled, verification fails closed rather than bypassing app lock.

Removing the PIN explicitly clears verifier material and lockout state.

## 12. Full finance-data deletion

The Settings destructive reset requires explicit confirmation. `FinanceDataResetService` removes finance-domain records, transaction revisions, reconciliation/reminder/audit/backup metadata, user category/tag data and receipt metadata transactionally/dependency-safely. Receipt files are cleaned only after DB reset commit.

It intentionally preserves:

- `schema.version`;
- non-finance app preferences;
- app-lock/PIN configuration.

Self-referencing categories are deleted leaves-first. A category cycle causes reset rollback instead of partial deletion.

## 13. Developer synthetic reset

The hidden developer reset requires typing `RESET SAMPLE`. It first performs complete finance reset, reseeds system categories and then creates deterministic synthetic accounts/transactions/transfer/budget/goal/recurrence records.

It must not be treated as a backup/restore mechanism and must not be run against wanted data without a separately saved encrypted backup.

## 14. Uninstall/device loss

Uninstalling the app or clearing app storage may remove SQLite data, receipts, preferences and secure-storage material according to platform behavior. Android app-data backup is explicitly disabled in the current manifest.

Finora cannot recover local records after loss/uninstall unless the user separately saved a usable encrypted backup.

## 15. No automatic cloud lifecycle in current release

Current source has no required Finora account service, automatic cloud sync, analytics/advertising telemetry pipeline or automatic backup upload. Adding any later requires explicit architecture/privacy/security changes and updated user/store disclosures.
