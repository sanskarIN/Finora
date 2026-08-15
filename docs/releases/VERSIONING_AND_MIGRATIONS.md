# Versioning and Migration Policy

This document defines how Finora source/app/database/backup versions are coordinated for the current local-first product.

## Current versions

- Product source line: **0.2.0**
- MAUI application display version: **0.2.0**
- Application build version: **2**
- Windows package version: **0.2.0.0**
- SQLite schema version: **2**
- Backup format magic: **FINORA01**

These values serve different purposes and must not be conflated.

## Application version

`ApplicationDisplayVersion` is the user-facing semantic-style version from `Finora.App.csproj`.

`ApplicationVersion` is the monotonically increasing platform build/version code used by MAUI/platform packaging.

The About UI reads packaged version/build metadata rather than maintaining a separate hard-coded copy.

## Windows package version

Windows package version is four-part. Structural preflight checks current manifest version against the MAUI display version convention used by this source line.

A release must verify the final generated package metadata, not only source manifest text.

## Database schema version

`AppConstants.DatabaseSchemaVersion` is the local relational schema marker.

Schema changes require an explicit migration path from every released schema that remains supported.

Current schema: 2.

Current required released migration coverage includes v1 → v2.

## Migration rules

A database migration must:

1. detect current schema safely;
2. perform one supported step at a time;
3. use a database transaction where the migration changes relational data/schema;
4. validate transformed data as appropriate;
5. advance the schema marker only after the step succeeds;
6. preserve user finance history/relationships;
7. fail without pretending the new schema is active if the step rolls back;
8. be covered with representative synthetic prior-schema data;
9. run integrity diagnostics after migration in release QA.

## Current automated migration evidence — 2026-08-15

Verified source candidate:

`f80b29d44a225a6d745529519e6c59cadbc152a8`

Finora CI run:

`31875164890`

The current production runner and integration suite now prove the following source-level cases for schema 1 → schema 2:

- safe schema-version guards for missing/invalid/current/future markers;
- fresh database initialization and reopen;
- transactional v1 → v2 migration;
- schema-2 target validation before advancing `schema.version`;
- required changed-table column validation;
- SQLite `foreign_key_check` and integrity validation before version-marker commit;
- preservation of representative legacy attachment data while adding the intended schema-2 filename field;
- idempotent repeated migration execution;
- rollback when a pre-existing malformed target table makes the schema invalid;
- rejection of synthetic legacy foreign-key corruption.

The exact current automated result is retained in `docs/testing/CI_EVIDENCE.md`.

This automated evidence does **not** replace installed prior-version upgrade testing on every target platform. Release QA still needs a representative previously released profile upgraded through the candidate binary, followed by integrity diagnostics, finance-data verification, receipt verification, and encrypted backup/restore.

## Adding schema v3 or later

For a future schema v3:

- add v2 → v3 production migration;
- retain v1 → v2 so v1 can migrate through the chain if still supported;
- update `DatabaseSchemaVersion` to 3 only when migration code/tests/docs are complete;
- update `DATABASE_SCHEMA.md`;
- update backup snapshot/restore validation for new entity/field;
- update persistence-boundary validation;
- update integrity service;
- update complete finance reset;
- update sample data if relevant;
- update migration/integration tests;
- update test plan/release checklist/store readiness;
- update data lifecycle/threat model if data meaning/privacy changes.

Do not skip backup/reset/integrity work for a new persisted field.

## Migration test data

Use synthetic prior-schema databases containing representative:

- accounts;
- transactions/transfers/splits;
- categories/tags;
- budgets/periods;
- goals/contributions;
- recurrence;
- attachments metadata;
- schema-specific metadata.

Never commit real user databases as migration fixtures.

The current compact migration fixtures intentionally target the production schema-transition invariants they exercise. A release upgrade profile should additionally model the complete released schema and representative finance graph rather than treating a partial fixture as full installed-version evidence.

## Backup format vs database schema

The encrypted backup carries schema/versioned graph information but is not simply a raw SQLite file copy.

Backup compatibility decisions are separate from database migration decisions.

Current restore path expects the schema supported by the build and rejects unsupported backups rather than guessing transformations.

If future releases support restoring older backup schemas directly, that requires an explicit validated backup-migration layer; do not assume database migration code can be applied blindly to serialized backup DTOs.

## Backup format magic

`FINORA01` identifies the current encrypted container family.

Change the backup magic only for a deliberate incompatible container-format change, not for every database schema/app patch.

A new backup container version requires:

- parser/version design;
- authenticated format metadata;
- backward-compatibility decision;
- wrong-version error behavior;
- cryptographic review;
- backup/restore tests;
- release/user migration guidance.

## Semantic version intent

The project follows semantic-versioning intent where practical during development:

- PATCH — backward-compatible bug/documentation/reliability fixes;
- MINOR — backward-compatible user/developer capability additions;
- MAJOR — intentionally incompatible public/product changes.

Because app-store build numbers and DB schema version are independent, a PATCH release can still require a schema migration if the data model changes; the migration must be explicit.

## Release tagging

A release tag should point to the exact commit for which evidence exists.

Before tag:

- version/build metadata aligned;
- changelog/status updated;
- schema docs aligned;
- structural preflight passed;
- tests/builds/device gates passed;
- signing/store metadata ready;
- no unresolved migration/backup/data-loss issue.

## Upgrade testing

For every platform release candidate:

1. install prior release with synthetic data;
2. exercise representative finance/attachments/settings;
3. install/upgrade candidate without clearing app data;
4. launch and allow migration/startup recovery;
5. verify schema marker;
6. run integrity check;
7. verify transactions/transfers/budgets/goals/recurrence/receipts;
8. create new encrypted backup;
9. restore that backup in a clean candidate profile;
10. verify no duplicate sample/onboarding data.

## Downgrade policy

Do not assume a newer schema database can be safely opened by an older app. Downgrade is not automatically supported.

Users should preserve a verified encrypted backup appropriate to the target version before destructive version experiments.

## Rollback of a bad release

If a release with a new schema must be rolled back, shipping the older binary may be unsafe if it cannot understand the new schema.

A rollback plan must consider:

- forward-fix build;
- compatible hotfix migration;
- store rollout controls;
- backup/restore compatibility;
- user communication.

Never tell users to manually edit/delete schema markers.

## Release evidence

Record for each release:

- commit/tag;
- app display/build version;
- schema version;
- backup container version/magic;
- migration paths tested;
- platforms built;
- upgrade profiles tested;
- backup round trip tested;
- known compatibility limitations.