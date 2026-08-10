# Finora Database Schema

Current schema version: **2**

Finora stores financial state locally in SQLite through EF Core. Monetary fields use signed 64-bit integer minor units (`long`) plus currency code. User-facing major-unit conversion uses `decimal`; floating-point values are not persistence/calculation types for money.

## Core tables

| Table | Purpose / important invariants |
| --- | --- |
| `Accounts` | account name/type/currency/opening balance/state; credit-card metadata only for credit-card type |
| `Transactions` | expense/income/transfer/refund/adjustment; account/date/currency; soft-delete state; transfer/recurrence linkage |
| `TransactionSplits` | split amounts/categories; split total and sign must match parent transaction |
| `Categories` | hierarchical category tree; parent cycles forbidden |
| `Tags` | transaction labels |
| `TransactionTags` | many-to-many transaction/tag relation |
| `Budgets` | overall/category/subcategory budget definition, cadence, rollover and warning threshold |
| `BudgetPeriods` | explicit budget windows/planned/rollover values; `(BudgetId, StartsOn, EndsOn)` unique |
| `SavingsGoals` | target/starting amount/currency/date/completion state |
| `GoalContributions` | contribution/withdrawal history and optional linked transaction |
| `RecurrenceRules` | persisted recurring templates and lifecycle state |
| `RecurrenceOccurrences` | due-instance state; `(RecurrenceRuleId, DueOn)` unique |
| `Attachments` | app-private receipt metadata, relative path, size and SHA-256 |
| `AppSettings` | app/database settings; key unique |
| `AuditEntries` | privacy-safe local action metadata |
| `BackupMetadata` | local encrypted-backup metadata; backup ID unique |

## Schema-v2 tables

### `TransactionRevisions`

- points to a transaction;
- stores `ChangeKind`, privacy-safe/local snapshot JSON and `ChangedAtUtc`;
- indexed by transaction/time;
- cascades with transaction deletion at the relational layer.

### `AccountReconciliations`

- account/statement date;
- book balance, statement balance and explicit difference;
- optional adjustment-transaction link metadata;
- completion timestamp/note;
- indexed by account/statement date.

### `NotificationSchedules`

- privacy-safe kind/title/body;
- trigger time;
- optional dedupe key;
- enabled/delivered state;
- indexed by trigger and dedupe key.

## Important indexes

- account state/name;
- transaction account/date;
- transaction soft-delete/date;
- transfer group;
- budget period uniqueness;
- recurrence occurrence uniqueness;
- app setting key uniqueness;
- backup ID uniqueness;
- transaction revision history;
- reconciliation history;
- notification trigger/dedupe.

## Persistence-boundary validation

SQLite foreign keys and indexes are not the only validation layer. Before every EF `SaveChanges`/`SaveChangesAsync`, Added/Modified entities pass Domain structural validation.

The current boundary covers:

- account name/currency/credit-card metadata;
- transaction amount/sign/currency/account/date/transfer/deletion-state metadata;
- transaction split amount/reference shape;
- category/tag/transaction-tag metadata;
- budgets and explicit budget periods;
- savings goals and contributions;
- recurrence rules and occurrence structural state;
- attachment path/content-type/size/hash metadata;
- transaction revision metadata;
- reconciliation arithmetic/adjustment-state metadata;
- notification schedule lengths/timestamps;
- app setting keys/values;
- audit entry metadata;
- backup metadata.

This validation prevents direct EF write paths from bypassing basic schema-v2 invariants. It does **not** replace relationship/aggregate services, SQLite foreign keys, backup graph validation, or the explicit data-integrity checker.

## Financial graph invariants

### Account / transaction currency

Transaction currency must equal its account currency. Changing account currency is blocked after transaction or recurrence references exist. Current transfers are same-currency only.

### Transfers

A transfer is represented by exactly two transaction rows sharing a `TransferGroupId`:

- both rows are `Transfer`;
- accounts differ;
- counterparties are reciprocal;
- currencies match;
- amounts are equal and opposite;
- soft-delete state matches.

Generic single-transaction mutation paths must not silently alter a linked transfer half.

### Splits

For a split transaction:

- nonzero split amounts use same sign as parent;
- checked split sum equals parent amount;
- split categories must remain valid;
- reports/budgets use split allocations rather than double-counting parent amount.

### Budgets / periods

- explicit periods cannot overlap;
- custom cadence requires explicit periods and is inactive outside them;
- weekly/monthly generated windows use shared `BudgetPeriodPolicy`;
- rollover participates only when enabled;
- effective planned amount must remain positive;
- explicit-period replacement is transactional.

### Savings goals

- target is positive;
- starting amount is between zero and target;
- contribution/withdrawal history uses checked arithmetic and cannot drive running progress below zero;
- linked transaction must exist/not be deleted and use goal currency;
- `IsCompleted` is derived from validated starting + contribution progress.

Earlier source versions could persist a stale `IsCompleted` flag. Initialization now repairs only this safe derived flag when underlying goal/contribution history validates. Invalid/overflowing/negative history is not silently repaired and remains visible to the integrity checker.

### Recurrence

- active rule account/destination/category dependencies must be usable;
- rule currency must match involved accounts;
- pending occurrence is created before a transaction;
- paid/partially-paid occurrence has generated transaction/payment data;
- paid history may retain a valid postponed date;
- pending/skipped occurrence cannot silently contain generated payment/postponement data;
- generated transfer must remain balanced/reciprocal;
- `(RecurrenceRuleId, DueOn)` prevents duplicate due instances;
- paused/archived/completed rules stop generation.

### Reconciliation

`DifferenceMinor` must equal checked `StatementBalanceMinor - BookBalanceMinor`. If an adjustment is recorded, adjustment-state metadata and the linked adjustment transaction must agree.

### Attachments

Receipt metadata remains beneath the logical `attachments/` root. Runtime file operations additionally use canonical physical-path checks that reject symbolic-link/reparse traversal. Size is bounded to 20 MB per receipt and SHA-256 metadata, when present for legacy compatibility, must be exactly 32 bytes.

## Migration: v1 → v2

The migration runner transactionally adds:

- `Attachment.OriginalFileName` support;
- transaction revision table/index;
- reconciliation table/index;
- notification schedule table/indexes;
- schema version advance.

Schema version advances only inside successful migration transaction. Existing database initialization invokes the migration runner before normal use.

## Validation layers

Finora deliberately uses multiple layers:

1. **Domain/entity structural rules** — basic field/monetary/state shape.
2. **EF SaveChanges boundary** — enforces those rules for all Added/Modified tracked entities.
3. **SQLite relationships/indexes** — foreign keys and uniqueness.
4. **Application/infrastructure services** — account/currency/category/lifecycle/atomic workflow relations.
5. **Backup graph validator** — authenticated snapshot semantic validation before restore replacement.
6. **Data integrity service** — read-only detection of SQLite/relationship/aggregate/file drift.
7. **Startup derived-state repair** — only safe `SavingsGoal.IsCompleted` normalization when underlying history validates.
8. **Tests/release QA** — failure-path and platform behavior.

No single layer should be treated as sufficient by itself.
