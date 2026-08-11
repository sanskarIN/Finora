# Encrypted Backup and Crash Recovery

This document describes the current Finora 0.2.0 backup format, validation, restore, attachment handling, and interrupted-restore recovery behavior.

## Product boundary

Backups are user-triggered and local. Finora does not automatically upload backups in the current release.

The user chooses the final destination through system share/save UI. Once a copy is saved/shared outside Finora, the destination controls its retention/security.

## Backup format identity

Current shared format magic: `FINORA01`.

Current database schema: 2.

Current restore path accepts the schema supported by the build and rejects unsupported/future schema rather than silently coercing data.

## Cryptography

Current encrypted backup implementation uses:

- PBKDF2-SHA256;
- random salt;
- 210,000 key-derivation iterations;
- AES-GCM authenticated encryption;
- random nonce;
- authentication tag;
- Finora format magic as authenticated format context.

The backup password is entered by the user for the operation and is not intentionally persisted by Finora.

Finora cannot recover a forgotten backup password.

## Registered backup service

Normal application dependency injection registers `CrashSafeBackupService` as `IBackupService`.

The crash-safe wrapper serializes backup/restore/recovery operations and coordinates the durable restore journal/marker around the underlying encrypted backup service.

Do not bypass this wrapper for normal Settings backup/restore flows.

## Backup creation

Conceptual creation sequence:

1. validate backup password requirements;
2. open current database through EF Core;
3. load supported finance graph;
4. load attachment metadata;
5. resolve each attachment path inside the app-private attachment root;
6. reject unsafe link/reparse traversal;
7. require attachment file to exist;
8. read attachment bytes;
9. validate stored size;
10. validate SHA-256 when metadata is present;
11. validate financial/domain graph and unique IDs;
12. serialize snapshot;
13. derive encryption key;
14. encrypt/authenticate payload with AES-GCM;
15. add privacy-safe backup metadata/audit record;
16. return encrypted bytes to the UI;
17. UI writes a managed cache share-copy and invokes system share/save UI;
18. clear plaintext/receipt/key-related managed byte buffers as early as practical.

If a later file/query/graph/crypto operation fails, previously accumulated receipt buffers are also cleared where managed APIs permit.

## Snapshot content

Current schema-2 backup covers supported local finance records including:

- accounts;
- transactions;
- transaction splits;
- categories;
- tags;
- transaction-tag links;
- budgets;
- budget periods;
- savings goals;
- goal contributions;
- recurrence rules;
- recurrence occurrences;
- attachments and attachment bytes;
- transaction revisions;
- account reconciliations;
- notification schedules;
- supported app settings.

Internal restore markers/settings are intentionally excluded from portable backup state.

Backup metadata/audit records describe the backup operation without embedding finance contents in diagnostics.

## Validation before encryption

Authenticated encryption protects ciphertext integrity, but it does not make a logically broken finance graph correct.

Before encryption, current validation checks supported entity/domain/relationship constraints such as:

- unique IDs;
- account/transaction currency agreement;
- transfer pairing;
- split totals/signs/categories;
- category/tag links;
- budget/category/period rules;
- savings goal/contribution relationships;
- recurrence account/category/currency/state;
- reconciliation links;
- attachment path/size/hash;
- notification/settings metadata shape.

A locally corrupt graph should fail backup creation rather than creating a portable broken snapshot.

## Preview

Restore begins with preview, not immediate replacement.

Preview:

- checks basic backup bounds/format;
- authenticates/decrypts with entered password;
- validates schema and graph;
- returns safe summary counts/date/schema;
- returns generic failure text for wrong password/tamper/malformed file instead of leaking cryptographic/filesystem exception details.

## Restore staging

After validated confirmation, receipt files are written to a private staging directory before becoming the live attachment tree.

Staging paths are resolved under app-private storage and reject symbolic-link/reparse traversal.

## Database replacement

Current restore service deletes/replaces supported finance tables inside an EF Core database transaction, then adds the validated snapshot graph.

The database transaction does not by itself make the attachment-directory swap atomic. That cross-resource gap is why the crash-safe wrapper/recovery journal exists.

## Attachment rollback/finalization

The underlying restore flow stages new attachment files and can move the existing live attachment directory into a rollback directory before moving staged files into the live attachment path.

If database commit fails during the in-process restore path, it attempts to restore the old attachment tree.

Process termination at arbitrary points requires the durable recovery layer described below.

## Durable recovery journal

The crash-safe wrapper persists an app-private restore recovery journal describing the cross-resource phase/state.

It also writes an internal pending database marker used to distinguish whether database replacement committed.

The journal/marker are not portable user backup content.

## Recovery decision

On startup, `IStorageRecoveryService` runs before normal finance navigation.

Conceptually:

### Pending marker still exists

The database replacement did not reach the committed state expected by the wrapper. Recovery restores the pre-restore attachment snapshot when safe/available and removes temporary restore artifacts after the decision.

### Pending marker absent after the committed phase

The new database state is treated as committed. Recovery finalizes the new attachment tree and removes old rollback/staging artifacts after the decision.

## Orphan restore directories

Directories named like `attachments.restore.*` or `attachments.rollback.*` are not blindly removed before recovery decides which state belongs with the database.

Cleanup occurs only after the durable journal/marker decision and uses safe no-link traversal rules.

## Attachment path safety

Receipt and restore paths must remain descendants of the app-private root/attachment root.

Current path protections include:

- canonical full-path confinement;
- platform-correct comparison semantics;
- existing symbolic-link/reparse-point rejection;
- no-link directory traversal for rollback copy/cleanup;
- staged relative-path resolution;
- size/hash verification.

## Password UI handling

Settings uses a masked backup-password `Entry`.

The password is used only for the explicit operation and the field is cleared after handled operation paths. Managed strings cannot be guaranteed to be physically zeroed immediately by the runtime.

## Encrypted-byte cache copy

The UI may write encrypted backup bytes to a temporary Finora cache share-copy before invoking system share/save UI.

After write/share handling, the in-memory encrypted byte array is cleared where practical.

Stale managed backup share-copy files are eligible for best-effort cache cleanup after the grace period. A user-saved destination copy is not deleted by this cleanup.

## Failure behavior

Backup/restore errors are intentionally generic at the normal UI boundary for cryptographic, filesystem, database, and provider failures.

Privacy diagnostics can record a safe event token and exception type without serializing raw exception message/path/stack.

A failed restore must not claim success and should leave prior usable state when the failure occurred before the committed replacement boundary.

## Tests in source

Current automated coverage includes scenarios for:

- create/preview/restore round trip;
- wrong/tampered backup rejection;
- attachment validation;
- path confinement;
- semantic graph validation;
- crash-safe wrapper round trip;
- pending-marker recovery restoring prior attachments;
- committed restore finalization;
- incomplete rollback-copy safety;
- orphan restore-directory cleanup;
- receipt-buffer clearing on failure paths;
- internal restore marker/settings exclusion.

Automated source/integration tests do not replace real process-kill/low-disk/native-filesystem validation.

## Manual release failure injection

Before release, use synthetic data and test process interruption at least around:

1. before journal write;
2. after journal write;
3. after prior-attachment snapshot;
4. after pending marker write;
5. during backup decrypt/validation;
6. during attachment staging;
7. after DB row replacement but before commit;
8. after attachment swap but before DB commit;
9. immediately after DB commit;
10. before wrapper clears/finalizes marker/journal;
11. during next-startup recovery.

Also test:

- low disk during attachment copy;
- locked/unavailable file;
- missing receipt;
- checksum mismatch;
- symbolic link/reparse traversal;
- wrong password;
- modified ciphertext/tag;
- truncated file;
- unsupported schema;
- semantically invalid authenticated snapshot.

Never use a real user finance profile for destructive failure injection.

## Recovery limitations

Finora's crash-safe restore protects the modeled database/attachment replacement phases; it cannot protect against all hardware/filesystem/OS failures, privileged device modification, or loss of the only external backup.

Users who depend on the data should keep independently saved encrypted backups and verify restore capability before uninstall/reset/device migration.