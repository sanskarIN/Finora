# Contributing to Finora

[![Support Finora on Buy Me a Coffee](src/Finora.App/Resources/Images/bmc_support.svg)](https://buymeacoffee.com/sanskarIN)

> **☕ Optional project support:** [Buy Me a Coffee — sanskarIN](https://buymeacoffee.com/sanskarIN). Contributions never affect code-review priority, feature access, product support, or security-report handling.

Thanks for contributing to Finora. This repository contains a local-first personal-finance application, so correctness, privacy, migration safety, and accessibility are part of the definition of done—not optional cleanup.

## Product boundaries

The current release is intentionally local-first:

- no required Finora login/account;
- no required internet access for core finance functionality;
- no analytics/advertising SDK by default;
- no automatic cloud backup upload;
- no server-backed entitlement validation;
- no background location collection.

Do not silently add a network service, analytics SDK, account requirement, cloud synchronization, or remote licensing dependency in an unrelated pull request. Such changes require an explicit architecture/privacy/security decision first.

## Development setup

Read `docs/setup/BUILD.md` and run:

```bash
python build/scripts/verify_structure.py
dotnet workload restore
dotnet restore Finora.sln
dotnet format Finora.sln --verify-no-changes --no-restore
dotnet build Finora.sln -c Release --no-restore
dotnet test Finora.sln -c Release --no-build
```

Use platform-appropriate MAUI tooling for native builds. Apple archive/device validation requires a compatible Mac/Xcode host.

## Branches and commits

- Use focused branches/commits.
- Prefer a commit that represents one coherent change.
- Keep commit messages descriptive, for example `feat(backup): validate attachment hashes before restore`.
- Do not mix broad formatting rewrites with a finance/security behavior change.
- Do not rewrite released migration history.

## Money correctness

Mandatory rules:

- persisted/calculated money uses signed integer minor units;
- major-unit text parsing uses `decimal`;
- do not use `float`/`double` for monetary arithmetic;
- use checked arithmetic where overflow could silently corrupt money;
- preserve currency codes and account/transaction currency invariants;
- same-currency transfers remain equal/opposite linked pairs.

Any change to amount semantics requires focused tests.

## Database changes

A schema change must:

1. increment `AppConstants.DatabaseSchemaVersion`;
2. add the next explicit migration step in `DatabaseMigrationRunner`;
3. preserve all earlier released migration steps;
4. update `docs/architecture/DATABASE_SCHEMA.md`;
5. update encrypted-backup compatibility if the entity graph changes;
6. add migration tests from the previous released schema;
7. run data-integrity checks after migration during QA;
8. never advance `schema.version` before the migration transaction succeeds.

Do not use `EnsureDeleted`, destructive database recreation, or manual schema-version manipulation as a production migration strategy.

## Transfers and recurrence

Transfer edits/deletes/restores must preserve both halves of the linked pair.

Recurring processing must remain idempotent. The unique `(RecurrenceRuleId, DueOn)` occurrence is the restart-safe guard. Do not reintroduce a design where every app start blindly creates another finance transaction.

## Backup and restore

Backup/restore changes are security-sensitive.

- Use established .NET/platform cryptographic APIs; never invent an encryption scheme.
- Preserve authenticated encryption and strict validation.
- Never persist/log the backup password or derived key.
- Validate schema, file length, attachment paths, byte size, and checksums.
- Failed restore must not leave a partially replaced finance dataset.
- Add tests for wrong password, tampering, truncation, unsupported schema, and rollback behavior.

## Receipts and files

- Keep user receipt/document bytes in app-private storage.
- Never trust a stored/imported relative path without canonical confinement checking.
- Enforce reasonable file size/content-type limits.
- Keep file operations asynchronous.
- Keep checksums/byte counts synchronized with metadata.
- Do not commit real receipts to tests/docs.

## Privacy and diagnostics

Never commit or log real finance data.

Forbidden in ordinary diagnostics/reports:

- account names;
- merchant/payee names;
- notes;
- transaction amounts;
- manually entered locations;
- receipt filenames/contents;
- PINs;
- backup passwords;
- encryption/signing secrets.

Use event/type tokens and synthetic data. The local integrity report is deliberately count/code based.

## Notifications

Notifications may appear outside the app lock. Keep title/body generic and privacy-safe. Permission must be user-controlled and reminders must be deduplicated/restart-safe.

## App lock and biometrics

Biometric/Windows Hello changes must retain PIN fallback. Cancellation/unavailable/error states must never result in an unlock. Secure storage is for small verifier/security values only.

## Accessibility and UI

Changed UI should support, as applicable:

- semantic labels/screen readers;
- scalable text;
- keyboard/focus on desktop;
- light/dark/system themes;
- reduced motion;
- sufficient contrast;
- meaningful loading/empty/error/permission-denied states;
- text/tabular equivalents for charts.

Use synthetic values in screenshots/recordings attached to pull requests.

## Dependencies

Before adding a package, review:

- whether framework/platform APIs already solve the problem;
- compatibility with all relevant target frameworks;
- maintenance status;
- license;
- security history/current alerts;
- package/transitive size and release impact.

`Directory.Packages.props` is the central package-version source. Dependabot and dependency-review help identify updates but do not replace human compatibility review.

## Tests expected by change type

Add tests with the change rather than deferring them:

- pure domain/parser/math → unit tests;
- SQLite/repository/import/backup/migration/integrity → integration tests;
- navigation/state contracts → UI-contract tests;
- native notifications/biometrics/capture/file-picker/signing → platform build/device evidence.

See `docs/TEST_PLAN.md`.

## Pull requests

Use `.github/pull_request_template.md` and explain:

- what changed;
- why;
- privacy/data impact;
- schema/backup compatibility;
- validation performed;
- affected platforms.

Do not mark platform validation complete unless it was actually run.

## Security vulnerabilities

Do not open a public issue. Follow `SECURITY.md` and use synthetic reproduction data only.

## Documentation

User-visible/architecture/release-impacting changes should update the relevant files in the same pull request, including as applicable:

- `README.md`;
- `CHANGELOG.md`;
- `PROJECT_STATUS.md`;
- `DECISIONS.md`;
- database/threat/privacy docs;
- test/release checklists;
- `what_changed.md` for major implementation sessions.

## Code of conduct and license

Participation is governed by `CODE_OF_CONDUCT.md`. Contributions are submitted under the repository's Apache-2.0 license unless explicitly stated otherwise by an applicable file/license notice.
