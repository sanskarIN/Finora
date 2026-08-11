# Finance Data Reset and Synthetic Sample Data

This document describes the two destructive local data operations currently implemented in Finora: complete finance-data deletion and developer synthetic sample reset.

## Why there are two operations

They serve different goals:

- **Delete all finance data** removes supported local finance records while preserving intentional non-finance application settings/metadata needed to keep Finora operable.
- **Reset sample data** is a developer/testing operation that destroys current finance records and immediately seeds a deterministic synthetic dataset.

Neither operation is a backup.

## Complete finance-data deletion

The user-facing Settings flow uses a dedicated `IFinanceDataResetService` rather than inline page-level table deletion.

This service exists so the destructive graph is centralized, transactional, and testable.

## Confirmation

The Settings UI requires an explicit typed destructive confirmation before finance deletion. The action is not intended to be triggered by a single accidental tap.

Do not weaken this confirmation when refactoring Settings.

## Finance data removed

The schema-2 reset implementation deletes supported finance graph records, including current tables such as:

- transaction revisions;
- account reconciliations;
- notification schedules;
- transaction-tag links;
- transaction splits;
- attachments metadata;
- recurrence occurrences;
- goal contributions;
- budget periods;
- transactions;
- recurrence rules;
- budgets;
- savings goals;
- tags;
- categories, including user-created categories;
- accounts;
- related finance/audit/backup metadata according to the service's current deletion order/contract.

The exact deletion order is designed around relationships/foreign keys and should remain centralized in the service.

## What is intentionally preserved

Finance reset is not a factory reset of every app preference/security value.

The current operation intentionally distinguishes finance records from application-operability data such as:

- schema version metadata;
- ordinary UI/application preferences;
- PIN/secure-storage verifier state unless separately removed through security settings;
- current installation/package identity.

Documentation/UI must not imply that finance deletion removes a separately saved external export/backup or every OS-level secure-storage preference.

## Attachments

After the database deletion transaction commits, the reset workflow invokes attachment orphan cleanup so receipt/document files no longer referenced by finance records can be removed.

Filesystem cleanup is a separate resource operation; failures should not rewrite the already committed database deletion into a false success state without accurate user/support handling.

## Transaction boundary

Database finance records are deleted inside a transaction. This avoids intentionally leaving a half-deleted relational graph if a database operation fails before commit.

## External copies are unaffected

Deleting Finora local finance data cannot delete:

- CSV/PDF files previously saved elsewhere;
- encrypted backups previously saved elsewhere;
- receipt copies shared to other apps;
- screenshots/photos;
- destination copies in cloud drives/file systems chosen by the user.

Those destinations control their own retention.

## Developer synthetic sample reset

The sample reset is hidden behind developer controls and separate typed confirmation.

Its intended purpose is deterministic development/testing without requiring real finance data.

## Sample reset sequence

Conceptually:

1. require developer destructive confirmation;
2. clear current finance graph through the intended reset behavior;
3. ensure required default/system categories are present;
4. create deterministic synthetic accounts;
5. create synthetic transactions;
6. create a valid same-currency linked transfer;
7. create a synthetic budget;
8. create a synthetic savings goal;
9. create a synthetic recurring rule;
10. leave the database in a reproducible development state.

## Sample data is not merge data

The reset operation does not merge sample rows into real user finance history. It is destructive by design.

Do not expose this action as a normal onboarding sample toggle after real finance data exists without preserving the current safeguards.

Onboarding's initial sample-data opt-in is a separate first-run experience and avoids duplicating sample/opening data when revisited after accounts already exist.

## Privacy

Synthetic sample data must remain fabricated and deterministic. Do not seed:

- real names from user data;
- real account numbers;
- real merchant histories;
- real locations;
- real receipt images;
- actual production credentials.

## Automated coverage

Current integration coverage verifies complete finance reset removes supported data while preserving schema-operability state and verifies deterministic sample reset produces the intended local synthetic shape.

## Recommended reset testing

Before release, use an isolated synthetic profile to test:

- cancel confirmation leaves all data intact;
- mistyped confirmation leaves all data intact;
- valid finance deletion removes all supported finance tables;
- user-created categories are removed;
- receipts become orphan-cleaned;
- schema metadata remains valid;
- preferences/security state behaves as documented;
- restart after reset opens a usable empty finance profile;
- sample reset creates deterministic expected rows;
- sample reset creates a balanced linked transfer;
- integrity checker reports healthy sample data;
- no real finance/export files are used during the test.

## Factory-reset distinction

If a future product requirement needs a true factory reset that also clears preferences, secure-storage app lock, cache, diagnostics, onboarding state, and every local app-owned artifact, that must be implemented/documented as a separate operation. Do not silently expand the meaning of the current finance-data reset because that would change destructive behavior and security recovery expectations.