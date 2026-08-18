# Accounts and Transactions

This document describes the current Finora account, transfer, transaction, category/tag, receipt, and reconciliation behavior implemented in the 0.2.0 source line.

## Accounts

Accounts are local domain records. Current account capabilities include create, edit, archive, restore, detail/history, current balance, opening balance, currency, type, and optional credit-card metadata.

Supported account types come from the domain model and include cash, bank, credit card, wallet, savings, investment placeholder, and custom categories.

### Account currency invariant

An account currency is not presentation-only metadata. Transactions and active recurrence must agree with it. Once finance/recurrence dependencies exist, account currency cannot be silently changed into a different currency.

### Opening and current balance

Opening balance is stored in integer minor units. Current balance is derived through checked arithmetic and transaction history. User-entered major-unit values are converted with `decimal` and currency-specific precision.

### Credit-card metadata

Credit-card accounts can carry a credit limit and billing day. Billing day supports the domain range 1–31.

### Account state and archival

Archival preserves historical data. An account referenced by an active recurring rule cannot be archived until the dependency is no longer active. Paused/completed/archived recurrence can remain historical without generating future finance activity.

## Same-currency transfers

The current transfer model intentionally supports same-currency transfers only.

Each transfer uses two linked transaction rows:

- source account row: negative amount;
- destination account row: positive amount;
- equal magnitude;
- same currency;
- shared `TransferGroupId`;
- reciprocal counterparty account IDs.

Generic single-transaction save/edit paths are not allowed to mutate a linked transfer half independently. Dedicated transfer workflows preserve both rows through create/edit/delete/restore behavior.

Cross-currency movement is a later explicit FX workflow and is not approximated by changing signs or labeling one side with another currency.

## Transaction types

Current transaction types include:

- Expense — negative amount;
- Income — positive amount;
- Refund — positive amount;
- Adjustment — signed according to the explicit adjustment direction;
- Transfer — paired workflow only.

Zero amounts and `long.MinValue` are invalid at financial persistence boundaries because zero is not a meaningful transaction and `long.MinValue` cannot be safely negated for magnitude operations.

## Quick add

Quick-add supports the main transaction fields used by the current UI:

- type;
- account;
- optional category;
- date and time;
- amount;
- merchant/payee;
- payment method;
- manually entered location;
- note.

The embedded calculator uses decimal arithmetic and a bounded expression rather than binary floating point.

## Date/time behavior

Transaction timestamps are persisted in UTC. User-selected dates and times originate in local time and are converted to UTC before persistence.

Date-range filters use the shared `LocalDateRange` policy with an inclusive local start and exclusive UTC end boundary. This avoids treating a user's local calendar date as UTC midnight and avoids `23:59:59` end-of-day gaps.

## Search, filter, sort, and paging

Transaction history supports:

- free-text search;
- account filter;
- category filter;
- type filter;
- local date range;
- explicit sort choice;
- 50-row database pages;
- Load more for additional results.

`ITransactionHistoryStore` applies search, account/category/type/date filters, sort order, offset, and page size in the SQLite/EF Core query before materializing rows. The result returns the requested page, the total matching count, and whether another page exists.

The first UI request loads at most 50 matching rows. **Load more transactions** asks the store for the next offset rather than retaining the complete result set in the ViewModel. The ViewModel also snapshots the last applied query, so editing filter controls without applying them cannot mix rows from different query states.

Supported history sort modes remain newest first, oldest first, amount high-to-low, amount low-to-high, and merchant A–Z. Stable secondary ordering is applied so page boundaries remain deterministic when primary sort values tie.

The legacy `IFinanceStore.SearchTransactionsAsync` API remains available for existing bounded workflows that intentionally need complete result sets; interactive transaction history uses the paged read service.

## Transaction editing and revisions

Normal transaction edits preserve a local revision record before replacing persisted values. Revision history supports auditability without requiring cloud logging.

Revision snapshot contents are finance-sensitive and must not be written into privacy diagnostics. User-facing revision summaries are deliberately safer than raw serialized snapshot JSON.

Linked transfers are edited through their transfer workflow rather than the normal one-row edit path.

## Soft delete and restore

Normal transaction deletion is soft deletion. Deletion state and deletion timestamp are validated together at the persistence boundary. Restore returns a soft-deleted transaction to active history when valid.

Transfer delete/restore must preserve both linked halves.

## Categories

Categories support hierarchy and maintenance operations:

- create/update;
- parent/subcategory relationship;
- cycle prevention;
- reorder;
- archive/restore;
- merge/reassign.

Category mutations also protect budget relationships. A subcategory budget cannot be silently converted into an invalid root-level relationship through archive/merge/reassignment.

## Tags

Tags support create/update/archive/restore and transaction linkage. Tag reporting is currency-scoped; values from different currencies are not silently aggregated.

## Splits

A normal transaction may contain category splits. The following rules are enforced:

- each split amount is nonzero;
- unsupported extreme values are rejected;
- split sign matches the parent transaction;
- split total equals the parent amount;
- referenced categories must be available;
- category/budget reporting uses split allocations when splits exist.

The full parent amount is not additionally attributed to the parent category when splits are present.

## Receipt attachments

Receipt/document bytes live under Finora app-private attachment storage. SQLite stores metadata such as:

- attachment ID;
- transaction relationship;
- relative internal path;
- original filename;
- content type;
- size;
- SHA-256 checksum.

Current safety controls include generated internal names, content/size validation, canonical path confinement, symbolic-link/reparse-point rejection where applicable, integrity checks, orphan cleanup, backup inclusion, and restore verification.

Opening/sharing a receipt crosses into the OS/application trust boundary. Once another provider receives a copy, its storage/retention rules apply.

## Duplicate review

Duplicate detection surfaces likely candidates without deleting records automatically. Candidate matching considers account/type/amount/currency and temporal/text similarity. The user must review before destructive action.

## Bulk categorization

Bulk categorization applies a selected category (or uncategorization where supported) to selected normal transactions. Existing transfer halves are excluded from unsafe one-row mutation paths. Revision history is retained before changes.

## Transaction Tools

Transaction Tools provides bounded date-period review, selection, bulk categorization, selected export, and duplicate scanning.

Displayed passive amounts use currency-aware formatting and respect privacy/hide-on-launch behavior.

## Reconciliation

Reconciliation compares a statement ending balance with Finora's book balance for an account.

Workflow:

1. choose account;
2. enter statement ending balance;
3. choose local statement date;
4. optionally add note;
5. preview book/statement/difference;
6. optionally create an explicit adjustment transaction;
7. complete and persist reconciliation history.

The difference is explicit and checked. A reconciliation cannot silently hide a discrepancy.

Statement date uses the local-date policy through the end of the selected local day rather than a hard-coded UTC/`23:59:59` boundary.

Reconciliation metadata validates:

- account relationship;
- difference arithmetic;
- adjustment flag/transaction ID consistency;
- linked adjustment transaction account/type/amount;
- completed timestamp and history.

After reconciliation history exists, opening balance cannot be silently changed in a way that invalidates the historical book comparison.

## Privacy-mode display

Passive money display across account lists, transaction lists, Transaction Tools, account detail history, and reconciliation history uses currency-aware formatting and can be replaced with `••••` when privacy mode or hide-on-launch is active.

Editable values remain user-controlled input and are not equivalent to passive summary disclosure.

## Export

Selected/all transaction export supports CSV and dependency-free multipage PDF through explicit user actions. Generated files may temporarily live in app cache before system share/save UI is invoked. Destination copies are outside Finora's deletion/retention control.

## Integrity and diagnostics

The data-integrity service checks transaction/account currency agreement, sign rules, transfer pairs, split totals/categories, reconciliation relationships, and receipt path/size/hash state using sanitized issue codes/counts rather than finance contents.

For the broader data model and schema, see [Database Schema](../architecture/DATABASE_SCHEMA.md).