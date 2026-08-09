# Finora Database Schema

Current declared schema: **2**.

The SQLite database is the local system of record for the current release. Financial amounts are persisted as signed integer minor units (`long`/SQLite INTEGER) with a currency code. Binary floating-point storage is not used for money.

## Schema version metadata

`AppSetting` contains the unique key `schema.version`. `DatabaseMigrationRunner` reads this value for an existing database and executes registered version steps transactionally. A version greater than the application-supported schema is rejected. The version marker advances only after the corresponding migration step succeeds.

Current migration chain:

```text
v1 -> v2
```

A release that introduces schema v3 must add and test an explicit `v2 -> v3` path; it must not replace or delete the prior migration.

## Main entities

### `Account`

Represents cash, bank, credit card, wallet, savings, investment-placeholder, or custom account state.

Key persisted fields include:

- `Id`;
- name/type/icon/color label;
- currency;
- opening balance in minor units;
- active/hidden/archived state;
- optional credit limit/billing day;
- last reconciled timestamp/balance;
- creation/update timestamps.

Index: `(State, Name)`.

Accounts with transaction history are archived rather than silently hard-deleted by ordinary UI workflows.

### `FinanceTransaction` (`Transactions`)

Represents Expense, Income, Transfer, Refund, or Adjustment.

Key fields include:

- signed amount in minor units;
- currency;
- account/category;
- occurrence timestamp;
- merchant/payee;
- note;
- payment method;
- manually entered location;
- recurrence link;
- transfer group/counterparty account;
- soft-delete state/timestamp.

Indexes include `(AccountId, OccurredAtUtc)`, `(IsDeleted, OccurredAtUtc)`, and `TransferGroupId`.

Transfer invariant: two rows share a `TransferGroupId`, use the same currency, have equal/opposite amounts, reference reciprocal counterparty accounts, and transition delete/restore together.

### `TransactionSplit`

Child rows allocate a transaction amount to one or more categories/notes. The sum of split minor units must equal the parent transaction amount. The integrity diagnostic checks this invariant.

### `Category`

Supports parent/subcategory hierarchy, icons, ordering, system/user-created state, archive/restore. Parent deletion is restricted at the relational level; application workflows reassign/merge safely.

Category hierarchy must remain acyclic.

### `Tag` / `TransactionTag`

Many-to-many tag relationship. `TransactionTag` uses composite primary key `(TransactionId, TagId)`.

### `Budget`

Stores overall/category/subcategory budget configuration, cadence, limit minor units, currency, rollover flag, warning threshold, and archive state.

### `BudgetPeriod`

Stores explicit planned/rollover amounts and period boundaries. Unique index: `(BudgetId, StartsOn, EndsOn)`.

### `SavingsGoal`

Stores target/starting minor units, currency, optional target date, notes/icon, and completion state.

### `GoalContribution`

Stores signed contribution/withdrawal minor units, timestamp, optional linked finance transaction, and note.

### `RecurrenceRule`

Stores recurrence frequency/interval/date boundaries, reminder/grace configuration, transaction template fields, account/category/destination account, and next/last occurrence state.

### `RecurrenceOccurrence`

Persists each due occurrence and workflow state (pending/paid/partial/skipped/postponed), optional generated transaction, amount paid, and postponed date.

Unique index: `(RecurrenceRuleId, DueOn)`.

This is the core restart/idempotency guard. Reprocessing the same due date must not produce a second occurrence.

### `Attachment`

Stores receipt/document metadata only; bytes live under app-private attachment storage.

Fields include transaction link, relative app-private path, original filename, content type, byte size, SHA-256 checksum, and timestamps.

Never replace `RelativePath` with an arbitrary absolute path. Attachment services canonicalize/check paths against the private attachment root.

### `TransactionRevision` — schema v2

Stores critical pre-change transaction history:

- transaction ID;
- change kind;
- serialized local snapshot;
- change timestamp.

Index: `(TransactionId, ChangedAtUtc)`.

Relationship uses cascade delete from the parent transaction. Diagnostics/logs must not export raw private snapshot contents.

### `AccountReconciliation` — schema v2

Stores reconciliation history:

- account;
- statement date;
- book balance minor units;
- statement balance minor units;
- difference;
- whether an explicit adjustment was created;
- optional adjustment transaction;
- note;
- completion timestamp.

Index: `(AccountId, StatementDateUtc)`.

### `NotificationSchedule` — schema v2

Stores local reminder scheduling state independently from native OS scheduling:

- kind;
- generic privacy-safe title/body;
- trigger timestamp;
- optional dedupe key;
- enabled/delivered state.

Indexes: `TriggerAtUtc`, `DedupeKey`.

Do not store private merchant/amount/note content in a notification merely because this table is local; notifications may be shown on the device lock screen.

### `AppSetting`

Stores non-secret app preferences and the schema-version marker. `Key` is unique.

Small security verifier material belongs in OS secure storage, not this table. Large datasets do not belong in secure storage.

### `AuditEntry`

Records privacy-safe action metadata for critical workflows. Audit details must be sanitized and must not become a duplicate private transaction log.

### `BackupMetadata`

Records local metadata about backup creation such as backup ID, schema version, creation time, optional filename, and SHA-256 of the encrypted backup. `BackupId` is unique.

Backup metadata does not contain the backup password or encryption key.

## Schema-v1 to schema-v2 migration

The v1 → v2 step:

1. adds `Attachment.OriginalFileName` and populates a neutral fallback for old rows;
2. creates `TransactionRevisions` and its transaction/timestamp index;
3. creates `AccountReconciliations` and its account/statement-date index;
4. creates `NotificationSchedules` and scheduling/dedupe indexes;
5. updates `schema.version` to 2 only after the migration SQL and EF save succeed inside the migration transaction.

Migration tests must retain a representative v1 database shape and verify the v2 additions/version marker.

## Database runtime controls

Initialization enables:

- `PRAGMA journal_mode=WAL`;
- `PRAGMA foreign_keys=ON`;
- `PRAGMA busy_timeout=5000`.

Multi-record financial operations use explicit database transactions where atomicity matters.

## Data-integrity diagnostic

`DataIntegrityService` performs a privacy-safe local check of:

- SQLite `integrity_check`;
- SQLite `foreign_key_check`;
- transaction/account currency/reference consistency;
- transfer pairing/balance;
- split totals;
- category hierarchy cycles;
- recurrence duplicate/generated-transaction references;
- receipt/attachment path, presence, byte size, and SHA-256 checksum.

The exported integrity report contains status codes/counts only, not account names, merchants/payees, notes, monetary amounts, or receipt names/contents.

## Backup relationship

Encrypted backup serialization includes the supported schema-v2 finance graph plus attachment bytes. Backup restore is not a SQLite file copy: it validates/decrypts the snapshot, stages attachment files, replaces supported relational data transactionally, and swaps the attachment directory with rollback handling.

A backup created by a newer schema is rejected by an older build.

## Schema change rules

Every future schema modification must:

1. increase `AppConstants.DatabaseSchemaVersion`;
2. add exactly the required next migration step(s);
3. preserve prior migration paths;
4. include integration tests from every released schema;
5. update this file and backup compatibility documentation;
6. run the integrity checker after migration in release QA;
7. never silently reinterpret stored money or transfer semantics.
