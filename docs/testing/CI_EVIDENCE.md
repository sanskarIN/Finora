# Finora CI Evidence

Last verified source-build evidence: **2026-08-16**

This document records concrete GitHub Actions evidence for the current Finora 0.2.0 source line. It separates compiler/test evidence from device, packaging, signing, accessibility, recovery-failure-injection, and store evidence.

## Current verified source candidate

Candidate commit:

`8260ac02e4f683fa9749f9371185c25d5e3043f6`

Commit message:

`docs(testing): document precision and local-calendar regressions`

Finora CI run:

`31934249592`

CodeQL run:

`31934249613`

This candidate contains all previously verified cross-platform/XAML stabilization, migration-safety, hostile-backup, data-integrity, receipt-checksum, privacy-logger synchronization, reset-safety, and restore-recovery work, plus the currency-precision and local-calendar correctness pass completed on 2026-08-16.

Documentation-only commits after this exact candidate may advance `main`; the compiler/test/native-build evidence in this file remains anchored to the exact source candidate above unless a newer exact candidate is explicitly recorded.

## Structural and automated test evidence

Finora CI run `31934249592` completed the following successfully:

- Structural preflight — job `95133649345`;
- Core tests — job `95133666510`.

Exact test results retained from the run logs/artifact:

| Test project | Passed | Failed |
| --- | ---: | ---: |
| Finora.UnitTests | 102 | 0 |
| Finora.IntegrationTests | 173 | 0 |
| Finora.UiTests | 35 | 0 |
| **Total** | **310** | **0** |

The core job restored each test project and executed all three suites in Release configuration with the repository warnings-as-errors policy active.

Core test artifact:

- `core-test-results` — artifact `9260190133`;
- SHA-256 digest: `c80fe9a24b40f033524121a75fdfc1f3a5eca173c607bf4a973b8c6c7cc42999`.

## Native Release source-build evidence

The same exact candidate commit passed all four independent native source-build jobs:

| Target | Job | Result | Evidence boundary |
| --- | --- | --- | --- |
| Windows | `95133762880` | Passed | Unpackaged `net10.0-windows10.0.19041.0` Release source build; `WindowsPackageType=None` |
| Android | `95133762915` | Passed | `net10.0-android` Release source build |
| iOS | `95133762871` | Passed | `net10.0-ios` Release source build on GitHub macOS runner |
| Mac Catalyst | `95133762913` | Passed | `net10.0-maccatalyst` Release source build on GitHub macOS runner |

Retained native diagnostic artifacts for this exact candidate:

- Windows — `native-build-windows`, artifact `9260232838`, SHA-256 `cc753d899eac9c1ae46abfe59e15725d80ed54c2f36291650c53f335224f26b5`;
- Android — `native-build-android`, artifact `9260279323`, SHA-256 `b6f42dce4695d85614e866faa32d0a741e9232f5eb7a87c88f29b86e998f6250`;
- iOS — `native-build-ios`, artifact `9260383176`, SHA-256 `098d737945ec4d1024be5425020d83809b22e3689deaa13c90d0026e724eb50d`;
- Mac Catalyst — `native-build-maccatalyst`, artifact `9260224740`, SHA-256 `b691cab1e5a94ac6492b3d31bbbc9d25d38cf5e5ab5c3b56db99f95c5f92b8a3`.

These jobs are intentionally independent. Failure of one target does not cancel another target before its diagnostic can be collected.

## CodeQL evidence

CodeQL run `31934249613`, job `95133633181`, completed successfully for candidate `8260ac02e4f683fa9749f9371185c25d5e3043f6`.

The CodeQL job initialized analysis, installed the MAUI workload, restored the app, completed the Android analysis build, and completed the CodeQL analysis step successfully.

## Currency precision and exact round-trip evidence represented by this candidate

The current candidate adds explicit automated coverage for the supported precision classes represented by:

- JPY — 0 decimal places;
- INR — 2 decimal places;
- KWD — 3 decimal places;
- CLF — 4 decimal places.

Regression coverage now proves exact signed minor-unit behavior through multiple paths rather than only testing isolated conversion helpers:

- major-unit conversion and rounding;
- CSV major-unit import;
- CSV export values;
- exported CSV preview;
- exported `AmountMinor` re-import into a second database without value drift;
- account balances;
- overall budget planned/actual calculations;
- savings goal starting/contribution/current values;
- recurring rule amount, occurrence amount, and generated paid transaction;
- reconciliation preview, adjustment transaction, and final balance;
- income/expense reports;
- encrypted backup creation, authenticated preview, complete finance reset, restore, and post-restore integrity checking.

This evidence does not replace native UI editing/display validation for each precision class on every platform, but it closes the automated persistence/service/round-trip gap for representative 0-, 2-, 3-, and 4-decimal currencies.

## Local-calendar and timezone evidence represented by this candidate

The shared `LocalDateRange` test matrix now directly covers:

- UTC local-midnight boundaries;
- positive non-hour offset UTC+05:30;
- negative offset UTC-07:00;
- deterministic daylight-saving start with a 23-hour UTC span;
- deterministic daylight-saving end with a 25-hour UTC span;
- multi-day exclusive end boundaries;
- reversed-range rejection.

The production `FinanceStore` now accepts an optional local timezone, defaults it to `TimeZoneInfo.Local`, and uses shared `LocalDateRange` `[from,toExclusive)` conversion for budget-period and legacy Dashboard date windows instead of treating local calendar dates as UTC midnight.

Integration regression coverage proves the store behavior with:

- a one-day budget in UTC+05:30 where a UTC timestamp before UTC midnight still belongs to the selected local day;
- a Dashboard day in UTC+05:30;
- a Dashboard day in UTC-07:00;
- a Dashboard day spanning a deterministic DST-start boundary.

Actual device/host timezone behavior is still part of native release QA; deterministic automated zones do not replace platform/device testing.

## Strict analyzer failure caught during this continuation

An intermediate candidate triggered the repository's warnings-as-errors policy in Finora CI run `31934141986` because three new integration assertions used an xUnit pattern rejected by analyzer `xUnit2031`.

The production calendar fix was not weakened or bypassed. Two focused follow-up commits changed the test assertions to `Assert.Single(collection, predicate)`, after which the exact verified candidate passed the full 310-test core suite and all native source-build jobs.

This intermediate failure is retained as useful evidence that analyzer warnings remain release-blocking rather than being silently ignored.

## Migration-safety evidence retained by this candidate

The candidate retains production and integration evidence for the current schema-1 to schema-2 path:

- migration validates the target schema before advancing `schema.version`;
- fresh database initialization and reopen are covered;
- invalid/current/future schema-version guards are covered;
- representative schema-1 attachment data is preserved through the schema-2 transform;
- duplicate migration execution is idempotent;
- malformed target schema causes transactional rollback without pretending schema 2 is active;
- synthetic legacy foreign-key corruption is rejected;
- the migration runner performs SQLite foreign-key/integrity validation before the version marker is committed.

The current automated fixture does not replace an actual installed-version upgrade on every target platform. Release QA still needs a representative previously released profile upgraded by the candidate binary and followed by the normal integrity/backup checks.

## Encrypted-backup and recovery evidence retained and extended by this candidate

The candidate retains or strengthens regression evidence for:

- wrong-password rejection;
- ciphertext/authentication tampering rejection;
- truncated encrypted backup rejection;
- authenticated unsupported-schema rejection;
- authenticated semantic relationship corruption rejection;
- authenticated receipt path escape rejection;
- authenticated receipt size drift rejection;
- authenticated receipt SHA-256 drift rejection;
- required 32-byte receipt checksum metadata on backup creation/preview/restore;
- linked restore-journal refusal;
- linked rollback-copy refusal while preserving the live receipt tree and recovery state;
- existing pending-marker rollback and committed-restore finalization behavior;
- exact JPY/INR/KWD/CLF minor-unit preservation through encrypted backup/reset/restore.

The source still deliberately keeps real process-kill, low-disk, locked-file, native filesystem, and device restart/relaunch recovery injection as external/manual release gates.

## Data-integrity evidence retained by this candidate

The integration suite directly exercises corruption classes including:

- split-total drift;
- transaction/account currency mismatch;
- missing receipt files;
- receipt-size metadata drift;
- changed receipt bytes/SHA-256 drift;
- missing/invalid receipt checksum metadata;
- category parent cycles;
- SQLite foreign-key violations.

This supplements the existing transfer, budget, savings, recurrence, reconciliation, attachment-path, privacy-safe integrity coverage, and the new post-restore multi-precision integrity check.

## Privacy-logger regression correction retained by this candidate

During the earlier migration test expansion, CI exposed a race in a privacy-log rotation assertion. The test synchronizes through the logger/export gate before asserting rotated/current file state, so the test verifies completed writes instead of racing the asynchronous append.

No production privacy policy was weakened to make the test pass.

## Immediately preceding verified reset-safety candidate

Candidate:

`4053c5eae3d9644dd518e72b2dd8e69cc604c423`

Finora CI run:

`31880138196`

That candidate passed structural preflight, **281/281** automated tests (101 unit, 145 integration, 35 UI-contract), and all four MAUI Release source-build jobs. It added reset-safety coverage proving that complete finance-data deletion preserves unrelated app settings. The newer `8260ac02…` candidate supersedes it as the current source-build evidence while retaining that source behavior.

## Earlier migration/backup/integrity candidate retained for history

Earlier candidate:

`f80b29d44a225a6d745529519e6c59cadbc152a8`

Finora CI run:

`31875164890`

CodeQL run:

`31875164864`

That candidate passed 273/273 automated tests (97 unit, 141 integration, 35 UI-contract), all four MAUI Release source builds, and CodeQL. It established the retained migration-safety, hostile-backup, receipt-integrity, deliberate-corruption, privacy-logger synchronization, and linked restore-recovery evidence that the current candidate builds on.

Historical core artifact:

- `core-test-results` — artifact `9244540298`;
- SHA-256 digest: `73f1b762caf816bb1a8e4570cd9fa0fa28eb7957e83f94708fc6788384c989f5`.

Historical native artifacts:

- Windows — artifact `9244582889`, SHA-256 `7c356d9c0b63abd8b086a234eb93ba062283294bf9b9817c52eee7e693542377`;
- Android — artifact `9244614914`, SHA-256 `2814e38791792f2e2063148e46e3e465e505f29a043da9c634d33c9d905d764d`;
- iOS — artifact `9244703368`, SHA-256 `a77ddfba4f335ccacf87dd5c56c31eaa14d4a5a7ff8859851cbcf526a5b52fe9`;
- Mac Catalyst — artifact `9244580907`, SHA-256 `b726a7504f0382b79cdeb4c6561e4b6ab81d3118249c62c9b1b680a4d8f2c3a6`.

## Earlier strict source candidate retained for history

Earlier strict XAML candidate:

`f7dbfbb8691edc79cee559101f284ccd90a44cf7`

Finora CI run:

`31872362394`

CodeQL run:

`31872362398`

That candidate established the first retained four-target source-build proof under fatal `XC0022`, `XC0023`, and `XC0025` diagnostics, with 241/241 automated tests passing. The newer evidence supersedes its current test-count/source-candidate role but does not erase the historical stabilization evidence.

## GitHub Actions runtime maintenance

CI-only commit:

`6ba519bf69174c68b67f8595872546a259c783dc`

updated the primary workflow to Node-24-compatible current action majors used by the repository:

- `actions/checkout@v7`;
- `actions/setup-python@v7`;
- `actions/setup-dotnet@v6`;
- `actions/upload-artifact@v7`.

The current verified `8260ac02…` run executed through those updated action majors.

## What this evidence does prove

For exact source candidate `8260ac02e4f683fa9749f9371185c25d5e3043f6`, it proves:

- repository structural preflight passes;
- all **310** current automated tests pass;
- warnings-as-errors source compilation passes all four MAUI targets;
- the strict compiled-binding warning classes remain cleared on those builds;
- CodeQL analysis completes successfully;
- the added JPY/INR/KWD/CLF currency-precision, CSV round-trip, encrypted-backup round-trip, report, budget, savings, recurrence, reconciliation, UTC/+05:30/-07:00/DST, and FinanceStore local-calendar regressions compile and pass;
- the previously verified migration, hostile-backup, receipt-integrity, corruption-detection, logger synchronization, reset-safety, and restore-link regressions remain part of the same source line.

## What this evidence does not prove

It does **not** mark the following release gates complete:

- signed Android AAB production packaging;
- Windows MSIX generation, publisher identity, or signing;
- iOS provisioning, signing, archive, TestFlight, or App Store submission;
- Mac Catalyst signing, notarization, or distribution packaging;
- physical-device/emulator notification behavior;
- biometric/Windows Hello native behavior on enrolled/unavailable/lockout states;
- Android merged-manifest and real backup/device-transfer behavior;
- real process-kill/low-disk/locked-file restore recovery validation on target devices;
- native screen-reader, keyboard, large-text, high-contrast, or reduced-motion QA;
- real file picker/share/receipt flows on every target;
- a complete installed prior-version upgrade/migration run on every target;
- final dependency-license/vulnerability acceptance;
- current store policy, privacy declaration, external support-link, or submission approval;
- an assertion that no undiscovered defect exists.

Those remain evidence-based release tasks in `docs/NEXT_STEPS.md`, `docs/releases/RELEASE_CHECKLIST.md`, `docs/releases/STORE_READINESS.md`, and `docs/testing/NATIVE_VALIDATION_MATRIX.md`.

## Evidence policy

Future release-candidate evidence should be appended with:

1. exact commit SHA;
2. workflow/run IDs;
3. job IDs or retained artifacts;
4. exact test counts;
5. target framework/configuration;
6. packaging/signing/device boundary;
7. migration/backup/recovery profiles exercised;
8. unresolved failures or external gates.

A green source build must never be relabeled as signed package, device, accessibility, recovery-injection, or store evidence without executing that separate gate.
