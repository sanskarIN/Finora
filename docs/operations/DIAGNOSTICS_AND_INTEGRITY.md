# Diagnostics and Data Integrity

This document describes the current Finora privacy-safe diagnostic and integrity tooling.

## Goals

Finora stores sensitive local finance data. Diagnostic tooling must help identify structural problems without becoming a second copy of the user's financial history.

Current diagnostics are designed around:

- bounded local logging;
- sanitized event tokens;
- exception type rather than raw message/stack;
- privacy-safe integrity issue codes/counts;
- explicit user/developer-triggered integrity checks;
- optional sanitized report export;
- no automatic telemetry upload.

## Privacy logger

`IPrivacyLogger` is implemented by `PrivacyLogger` and registered in `MauiProgram`.

The logger intentionally ignores arbitrary caller property dictionaries for persistent diagnostic output and does not serialize raw exception messages or stack traces.

Error records use safe information such as:

- bounded/sanitized event token;
- exception type.

## Forbidden diagnostic contents

Diagnostics must not intentionally contain:

- transaction amounts;
- account names;
- merchant/payee names;
- transaction notes;
- manually entered location;
- receipt names or contents;
- PINs;
- backup passwords;
- encryption keys;
- signing keys/certificates/private keys;
- raw filesystem/database/provider exception messages;
- raw finance revision snapshot JSON.

## Log storage and rotation

Privacy logs live in Finora-controlled cache storage. The implementation maintains bounded current/previous log behavior rather than unbounded growth.

Diagnostic log paths are checked for symbolic-link/reparse traversal so an attacker cannot intentionally redirect the log file to an arbitrary location through an existing link path.

## Unexpected command failures

`MauiProgram` sets `AsyncCommand.UnexpectedFailureHandler` to route unexpected command exceptions to `IPrivacyLogger`.

The command layer handles deliberate validation/cancellation separately and prevents ordinary unexpected non-fatal `async void` command failures from becoming uncontrolled UI exceptions.

## Application exception coordinator

`AppExceptionCoordinator` centralizes privacy-safe reporting for supported application-level exception events.

Unobserved task exceptions are marked observed after reporting through the sanitized path.

## UI error mapping

Expected short validation messages may remain specific, for example:

- choose an account;
- enter a valid amount;
- end date cannot be before start date.

Unexpected infrastructure failures should become generic user-safe messages instead of exposing:

- database exception details;
- file paths;
- cryptographic provider messages;
- biometric provider text;
- stack traces.

## Structural privacy preflight

`build/scripts/verify_structure.py` performs dependency-free checks related to diagnostic/privacy safety, including current guards for:

- masked Settings secret fields;
- no password/PIN `DisplayPromptAsync` regression;
- no raw exception-message alert pattern;
- no raw biometric provider error flow into public failure text;
- Android backup/data-transfer privacy configuration;
- no passive XAML display of raw `*Minor` values with `minor` labels.

Passing structural preflight is not proof that C# compiles or native behavior works.

## Data integrity service

`IDataIntegrityService` / `DataIntegrityService` performs local privacy-safe consistency checks.

Current coverage includes major areas such as:

- SQLite integrity result;
- foreign-key violations;
- transaction sign/value/currency state;
- account/transaction currency mismatch;
- linked transfer pair completeness/balance/counterparty state;
- split sign/total/category state;
- category hierarchy cycles;
- budget configuration/category relationships;
- custom/overlapping budget periods;
- savings contribution running history/completion/link state;
- recurrence account/category/currency/payment/generated-transaction state;
- reconciliation arithmetic/adjustment links;
- attachment metadata/path/parent/size/hash state.

## Integrity output

Integrity output is intended to contain:

- health status;
- issue code;
- affected-record count;
- safe structural description.

It must not include private finance values/names/notes/receipt filenames.

## Why integrity diagnostics are independent

Domain validation prevents new invalid writes through supported paths, but a long-lived local database can still become inconsistent through:

- old application bugs;
- interrupted older versions;
- manual file/database modification;
- filesystem corruption;
- unsupported external tooling;
- migration defects;
- cross-resource attachment loss.

The integrity checker therefore validates the stored graph independently instead of assuming that every row was written by current code.

## Persistence-boundary validation vs integrity checks

These are complementary:

### Persistence-boundary validation

`FinoraDbContext.SaveChanges` validates Added/Modified supported entities before commit. It prevents direct EF paths from bypassing core domain shape rules.

### Integrity diagnostics

The integrity service examines already-stored relationships/data and can detect corruption or historical drift that current write validation cannot prevent retroactively.

## Attachment integrity

Attachment diagnostics verify that database metadata corresponds to safe app-private files.

Checks can include:

- path remains inside attachment root;
- no unsafe symbolic-link/reparse traversal;
- expected file exists;
- byte size matches metadata;
- SHA-256 matches when required;
- transaction/attachment parent relationship is valid.

## Exported integrity report

When the UI offers a sanitized integrity report export, the file is user-triggered and can temporarily live in Finora cache before system share/save UI.

The report remains subject to the no-private-finance-content rule.

Once the report is shared/saved elsewhere, that destination controls retention.

## Temporary artifact cleanup

`ITemporaryArtifactCleaner` removes only recognized old Finora share-copy patterns after the grace period.

It must:

- preserve fresh managed files;
- preserve unrelated cache files;
- preserve privacy diagnostic logs;
- avoid recursively following symlink targets;
- fail best-effort without blocking startup.

## Developer integrity workflow

Recommended developer/support workflow with synthetic/copy data:

1. reproduce the issue without real finance data when possible;
2. run structural preflight;
3. run the in-app integrity check;
4. record only issue codes/counts;
5. avoid asking for raw DB/receipts unless a secure explicit support process exists;
6. reproduce with synthetic rows matching the structural issue;
7. add regression test before changing repair logic;
8. repair only derived state that can be safely recomputed from valid source data;
9. do not silently rewrite corrupt source history merely to make the integrity report green.

## Release validation

Before release:

- run structural preflight;
- run unit/integration/UI-contract suites;
- create a healthy synthetic release-candidate database and require a healthy integrity report;
- inject synthetic corruption for each supported issue class and verify detection;
- verify report/log redaction;
- verify log rotation/bounds;
- verify link/reparse path refusal where host supports it;
- verify temporary-artifact cleanup boundaries;
- verify no automated upload/telemetry was introduced.