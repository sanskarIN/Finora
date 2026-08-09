# Finora Architecture Overview

## Design goals

Finora is a local-first personal-finance application. The current release must remain useful without login or internet access, preserve financial correctness, avoid floating-point money arithmetic, keep private finance data on the device, and expose explicit user-controlled import/export/backup paths.

## Solution dependency direction

```text
Finora.App
  ├─> Finora.Application
  ├─> Finora.Infrastructure
  ├─> Finora.Domain
  └─> Finora.Shared

Finora.Infrastructure
  ├─> Finora.Application
  ├─> Finora.Domain
  └─> Finora.Shared

Finora.Application
  ├─> Finora.Domain
  └─> Finora.Shared

Finora.Domain
  └─> Finora.Shared (only where shared primitives/constants are appropriate)
```

The UI may compose application/infrastructure services through dependency injection, but domain rules do not depend on MAUI/platform APIs.

## Projects

### `Finora.Shared`

Contains cross-cutting constants/primitives that do not depend on finance persistence or MAUI. Product identity, repository/support links, backup format magic, and declared database schema version live here.

### `Finora.Domain`

Contains finance entities/value objects/enums/domain rules:

- accounts;
- transactions/transfers/splits;
- categories/tags;
- budgets/periods;
- savings goals/contributions;
- recurrence rules/occurrences;
- attachments metadata;
- settings/audit/backup metadata;
- schema-v2 transaction revisions, reconciliations, and notification schedule records;
- money-safe conversion/value behavior.

Money is stored/calculated as integer minor units at persistence/domain boundaries. User-entered major-unit values are parsed with `decimal`, never binary floating point.

### `Finora.Application`

Defines DTOs and service contracts used by presentation and infrastructure. It does not own SQLite, MAUI controls, platform notifications, biometrics, secure storage, or file pickers.

Contract areas include:

- finance store;
- transaction maintenance/revisions/duplicates;
- account management/reconciliation;
- category/tag management;
- recurring occurrence workflow;
- mapped CSV import;
- reports;
- attachment lifecycle;
- encrypted backup/restore;
- export;
- settings/app lock;
- local notifications;
- biometrics/capture protection;
- privacy logging;
- local data integrity diagnostics.

### `Finora.Infrastructure`

Owns platform-neutral persistence/integration implementation that can be exercised outside MAUI when possible:

- EF Core SQLite context/mapping;
- database initialization and versioned migration runner;
- finance store and transaction/account/category/reconciliation services;
- recurrence workflow;
- CSV import/export/PDF generation;
- AES-GCM encrypted backup/restore;
- app-private attachment storage/checksums;
- notification schedule persistence;
- privacy-aware diagnostics;
- data-integrity checker.

Infrastructure services use async I/O/database APIs and cancellation tokens where public contracts support them.

### `Finora.App`

Owns .NET MAUI presentation and native platform integration:

- Shell navigation and pages/ViewModels;
- design resources/theme/accessibility preferences;
- system file pickers/share sheets;
- secure storage/preferences adapters;
- PIN lifecycle UI;
- Android/iOS/Mac Catalyst/Windows notification gateways;
- biometric/Windows Hello adapters;
- sensitive-screen protection adapters;
- lifecycle auto-lock;
- packaged privacy/terms/notices resources.

## Persistence architecture

SQLite is the current system of record. The database file is stored in MAUI app-private data storage. EF Core relational mapping supplies foreign keys/indexes and async access. Finora enables:

- SQLite foreign keys;
- WAL journaling;
- busy timeout;
- transactional multi-record writes;
- schema-version setting;
- versioned migration path.

Schema v2 is declared in `Finora.Shared.AppConstants`. `DatabaseMigrationRunner` upgrades supported prior schemas transactionally. The version marker advances only after a migration step succeeds.

See `DATABASE_SCHEMA.md`.

## Financial correctness

### Amount representation

Persisted monetary values use signed 64-bit integer minor units plus currency code. `decimal` is used only at text/major-unit conversion boundaries. `float`/`double` must not be introduced for monetary arithmetic.

### Transfers

A same-currency transfer is represented by two linked transaction rows sharing one `TransferGroupId`:

- source amount is negative;
- destination amount is positive;
- magnitudes are equal;
- currencies match;
- each half identifies the counterparty account;
- edit/delete/restore operations preserve the pair.

Cross-currency movement requires a future explicit exchange workflow; it must not be approximated as a same-currency transfer.

### Recurrence idempotency

A persisted occurrence is unique for `(RecurrenceRuleId, DueOn)`. Due processing creates/maintains the occurrence; an actual finance transaction is created only through paid/partial-paid workflow. Re-running due processing must not duplicate an occurrence or financial transaction.

### Transaction revisions

Critical pre-change transaction state is persisted separately for user-visible edit history/auditability. User-facing revision summaries must not expose raw private snapshot JSON through diagnostics/logs.

## Attachments

Receipt/document bytes live under app-private attachment storage. Database rows store metadata/checksum, not arbitrary absolute paths.

Controls include:

- generated internal filename;
- canonical safe-path confinement;
- content-type/size checks;
- asynchronous copy;
- byte count;
- SHA-256 checksum;
- orphan cleanup;
- encrypted backup inclusion;
- restore staging/checksum verification;
- integrity-check verification.

## Backup/restore architecture

Backup serialization captures the supported local finance graph plus receipt bytes. The serialized payload is encrypted using a password-derived key and authenticated encryption.

Current format controls:

- Finora format magic;
- random salt;
- PBKDF2-SHA256 key derivation;
- AES-GCM random nonce/tag;
- authenticated associated data;
- size/schema validation;
- attachment path/size/hash validation;
- preview before replacement;
- staged attachment directory;
- database transaction;
- rollback path for attachment-directory replacement.

Backups are never uploaded automatically. The user chooses where to save/share them through system UI.

## Import/export

CSV import is intentionally a two-step user-reviewed process: mapping/preview, then validated transactional commit. Size/row limits protect the app from unbounded input. Money conversion is decimal-safe.

CSV/PDF export is created locally and exposed through explicit system share/save UI. Exporting private finance data transfers responsibility to the user-selected destination/app.

## Privacy/logging

The current release has no account requirement, cloud sync, analytics SDK, ad SDK, or automatic backup upload.

Privacy-sensitive values must not be written to diagnostics. Local notification text is deliberately generic because it may appear on the lock screen outside Finora's app lock.

The data-integrity report contains health codes/counts only; it excludes account names, merchants/payees, notes, transaction amounts, attachment names/contents, and credentials.

## Security adapters

PIN verifier material is stored through OS secure storage; large datasets are not. Biometrics/Windows Hello are optional platform factors with PIN fallback. Sensitive-screen protection is enabled only through supported platform APIs and limitations are reported rather than hidden.

## UI architecture

Pages bind to ViewModels for core state/workflows while small platform/shell interactions may live in page code-behind. Long-running database/disk/crypto/import/export work is asynchronous. Shell provides primary navigation; additional workflow routes are registered for details/tools/legal/import/reconciliation/category management.

UI requirements include:

- loading/error/empty/permission states;
- light/dark/system theme;
- reduced motion;
- larger interface preference;
- screen-reader/text equivalents for financial charts;
- keyboard/resizable-window validation on desktop;
- privacy mode that hides displayed amounts.

## Notifications

Notification schedules are persisted/deduplicated independently of platform scheduling APIs. The platform gateway requests permission and maps schedules to Android, Apple, and Windows local notification mechanisms. Finance data remains in the local database; notification payloads stay generic.

## Testing layers

1. Dependency-free structural preflight.
2. Unit tests for pure/domain logic.
3. SQLite integration/migration/backup/import/integrity tests.
4. UI-contract tests for route/state expectations.
5. Native platform builds.
6. Emulator/simulator/physical-device smoke/accessibility/security tests.

Source or contract tests do not substitute for native platform validation.

## Current non-goals / later-version boundaries

Not part of the current local-first architecture:

- cloud synchronization;
- Finora remote account/login;
- collaboration;
- mobile-number authentication;
- server-backed entitlement/licensing;
- remote analytics/telemetry by default.

Introducing any of these requires new architecture/security/privacy decisions rather than silently adding a network dependency to existing services.
