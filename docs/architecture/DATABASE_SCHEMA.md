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

Accounts with transaction history are archived rather than silently hard-deleted by ordinary UI workflows. Account currency cannot be changed after financial or recurring records depend on the account. An account used by an active recurring rule cannot be archived until that rule is paused/completed/archived.

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

A transaction's currency must match its account currency. Expense amounts are negative; Income/Refund amounts are positive; zero and `long.MinValue` amounts are invalid. These invariants are checked at service/domain/EF boundaries and by the integrity checker.

### `TransactionSplit`

Child rows allocate a non-transfer transaction amount to one or more categories/notes. Each split has the same sign as the parent, cannot be zero/`long.MinValue`, and the checked sum of split minor units must equal the parent transaction amount.

Category and category-budget reporting uses split allocations whenever splits exist; it does not also count the whole parent transaction.

### `Category`

Supports parent/subcategory hierarchy, icons, ordering, system/user-created state, archive/restore. Parent deletion is restricted at the relational level; application workflows reassign/merge safely.

Category hierarchy must remain acyclic. Category reassignment/merge must preserve `Subcategory` budget semantics: a subcategory budget cannot be bulk-reassigned to a root category.

### `Tag` / `TransactionTag`

Many-to-many tag relationship. `TransactionTag` uses composite primary key `(TransactionId, TagId)`.

Tag financial summaries are explicitly scoped to a reporting currency; unlike currencies are never added together.

### `Budget`

Stores overall/category/subcategory budget configuration, cadence, limit minor units, currency, rollover flag, warning threshold, and archive state.

Budget invariants include:

- positive limit;
- valid 1–100 warning threshold;
- category/subcategory kinds require a category;
- overall kind cannot target a category;
- subcategory kind must target a child category;
- custom cadence requires at least one explicit period at persistence/backup/integrity boundaries.

### `BudgetPeriod`

Stores explicit planned/rollover amounts and period boundaries. Unique index: `(BudgetId, StartsOn, EndsOn)`.

`BudgetPeriodPolicy` is the shared resolver used by store/report paths:

- explicit periods take precedence;
- periods cannot overlap;
- weekly generated windows are Monday–Sunday;
- monthly windows are calendar months;
- custom budgets are inactive outside explicit windows;
- rollover is included only when `RolloverEnabled`;
- checked effective planned amount must remain positive.

Replacement of an existing budget's explicit period set is intended to be transactionally atomic so a failed replacement does not erase the prior valid periods.

### `SavingsGoal`

Stores target/starting minor units, currency, optional target date, notes/icon, and completion state.

### `GoalContribution`

Stores signed contribution/withdrawal minor units, timestamp, optional linked finance transaction, and note.

Contribution history must never drive running goal progress below zero. A linked transaction must exist, be non-deleted when linked through the normal workflow, and use the goal currency.

### `RecurrenceRule`

Stores recurrence frequency/interval/date boundaries, reminder/grace configuration, transaction template fields, account/category/destination account, and next/last occurrence state.

Rule lifecycle includes Active, Paused, Completed, and Archived. Active rule dependencies must point to available accounts/categories with matching currency. Paused/completed/archived historical rules may preserve links to later-archived accounts but cannot resume without current dependency validation.

### `RecurrenceOccurrence`

Persists each due occurrence and workflow state (pending/paid/partial/skipped/postponed), optional generated transaction, amount paid, and postponed date.

Unique index: `(RecurrenceRuleId, DueOn)`.

This is the core restart/idempotency guard. Reprocessing the same due date must not produce a second occurrence. Paid/partial-paid state must have a valid generated transaction linked to the same recurrence rule; unpaid/skipped/postponed state must not silently contain generated-payment data.

### `Attachment`

Stores receipt/document metadata only; bytes live under app-private attachment storage.

Fields include transaction link, relative app-private path, original filename, content type, byte size, SHA-256 checksum, and timestamps.

Never replace `RelativePath` with an arbitrary absolute path. Attachment services canonicalize/check paths against the private attachment root using platform-correct path comparison semantics.

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

The difference must equal checked `statement - book`. If an adjustment is marked created, the linked transaction must exist, be an Adjustment for the same account, and have the exact difference amount.

### `NotificationSchedule` — schema v2

Stores local reminder scheduling state independently from native OS scheduling:

- kind;
- generic privacy-safe title/body;
- trigger timestamp;
- optional dedupe key;
- enabled/delivered state.

Indexes: `TriggerAtUtc`, `DedupeKey`.

Do not store private merchant/amount/note content in a notification merely because this table is local; notifications may be shown on the device lock screen. Reminder synchronization cancels stale recurring/budget/backup schedules when the persisted product state no longer requires them.

### `AppSetting`

Stores non-secret app preferences and the schema-version marker. `Key` is unique.

Small security verifier material belongs in OS secure storage, not this table. Large datasets do not belong in secure storage. Internal restore-journal/commit-marker settings are not imported from an encrypted snapshot.

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

Migration tests retain a representative v1 database shape and verify the v2 additions/version marker.

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
- transaction amount/sign/currency/account state;
- transfer pairing/balance/link state;
- split signs/totals;
- category hierarchy cycles;
- budget configuration, custom periods, and category relationships;
- savings-goal and contribution/link/running-progress state;
- recurrence rule dependencies and occurrence payment/postponement/generated-transaction state;
- reconciliation arithmetic/adjustment links;
- receipt/attachment path, presence, byte size, and SHA-256 checksum.

The exported integrity report contains status codes/counts only, not account names, merchants/payees, notes, monetary amounts, or receipt names/contents.

## Backup relationship

Encrypted backup serialization includes the supported schema-v2 finance graph plus attachment bytes. Backup restore is not a SQLite file copy.

Before preview/restore, an authenticated snapshot must pass graph validation for identifiers, account/currency references, transfers, splits, category hierarchy, transaction-tag links, budgets/periods, goals/contributions, recurrence rules/occurrences, attachments, revisions, reconciliations, notification metadata, and settings boundaries.

Restore then stages attachment files, replaces supported relational data transactionally, and coordinates the attachment directory with a durable restore journal/commit marker. Startup recovery decides whether to restore the previous receipt tree or finalize the committed new tree after an interrupted restore.

A backup created by a newer schema is rejected by an older build.

## Schema change rules

Every future schema modification must:

1. increase `AppConstants.DatabaseSchemaVersion`;
2. add exactly the required next migration step(s);
3. preserve prior migration paths;
4. include integration tests from every released schema;
5. update this file and backup compatibility documentation;
6. run the data-integrity checker after migration in release QA;
7. never silently reinterpret stored money, currency, transfer, split, budget-period, recurrence, or reconciliation semantics.
