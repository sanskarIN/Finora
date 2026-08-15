# Finora CI Evidence

Last verified source-build evidence: **2026-08-15**

This document records concrete GitHub Actions evidence for the current Finora 0.2.0 source line. It separates compiler/test evidence from device, packaging, signing, accessibility, recovery-failure-injection, and store evidence.

## Current verified source candidate

Candidate commit:

`f80b29d44a225a6d745529519e6c59cadbc152a8`

Commit message:

`test(recovery): reject linked rollback copy`

Finora CI run:

`31875164890`

CodeQL run:

`31875164864`

This candidate contains the previously verified cross-platform/XAML stabilization work plus the migration-safety, hostile-backup, data-integrity, receipt-checksum, privacy-logger synchronization, and restore-recovery regression work completed in the next release-hardening pass.

## Structural and automated test evidence

Finora CI run `31875164890` completed the following successfully:

- Structural preflight — job `94989697902`;
- Core tests — job `94989708606`.

Exact test results retained from the run logs/artifact:

| Test project | Passed | Failed |
| --- | ---: | ---: |
| Finora.UnitTests | 97 | 0 |
| Finora.IntegrationTests | 141 | 0 |
| Finora.UiTests | 35 | 0 |
| **Total** | **273** | **0** |

The core job restored each test project and executed all three suites in Release configuration with the repository warnings-as-errors policy active.

Core test artifact:

- `core-test-results` — artifact `9244540298`;
- SHA-256 digest: `73f1b762caf816bb1a8e4570cd9fa0fa28eb7957e83f94708fc6788384c989f5`.

## Native Release source-build evidence

The same exact candidate commit passed all four independent native source-build jobs:

| Target | Job | Result | Evidence boundary |
| --- | --- | --- | --- |
| Windows | `94989803961` | Passed | Unpackaged `net10.0-windows10.0.19041.0` Release source build; `WindowsPackageType=None` |
| Android | `94989803975` | Passed | `net10.0-android` Release source build |
| iOS | `94989804013` | Passed | `net10.0-ios` Release source build on GitHub macOS runner |
| Mac Catalyst | `94989803934` | Passed | `net10.0-maccatalyst` Release source build on GitHub macOS runner |

Retained native diagnostic artifacts for this exact candidate:

- Windows — `native-build-windows`, artifact `9244582889`, SHA-256 `7c356d9c0b63abd8b086a234eb93ba062283294bf9b9817c52eee7e693542377`;
- Android — `native-build-android`, artifact `9244614914`, SHA-256 `2814e38791792f2e2063148e46e3e465e505f29a043da9c634d33c9d905d764d`;
- iOS — `native-build-ios`, artifact `9244703368`, SHA-256 `a77ddfba4f335ccacf87dd5c56c31eaa14d4a5a7ff8859851cbcf526a5b52fe9`;
- Mac Catalyst — `native-build-maccatalyst`, artifact `9244580907`, SHA-256 `b726a7504f0382b79cdeb4c6561e4b6ab81d3118249c62c9b1b680a4d8f2c3a6`.

These jobs are intentionally independent. Failure of one target does not cancel another target before its diagnostic can be collected.

## CodeQL evidence

CodeQL run `31875164864` completed successfully for candidate `f80b29d44a225a6d745529519e6c59cadbc152a8`.

The CodeQL job restored the app, completed the Android analysis build, and completed the analysis step successfully.

## Migration-safety evidence represented by this candidate

The candidate adds production and integration evidence for the current schema-1 to schema-2 path:

- migration validates the target schema before advancing `schema.version`;
- fresh database initialization and reopen are covered;
- invalid/current/future schema-version guards are covered;
- representative schema-1 attachment data is preserved through the schema-2 transform;
- duplicate migration execution is idempotent;
- malformed target schema causes transactional rollback without pretending schema 2 is active;
- synthetic legacy foreign-key corruption is rejected;
- the migration runner performs SQLite foreign-key/integrity validation before the version marker is committed.

The current automated fixture does not replace an actual installed-version upgrade on every target platform. Release QA still needs a representative previously released profile upgraded by the candidate binary and followed by the normal integrity/backup checks.

## Encrypted-backup and recovery evidence represented by this candidate

The candidate adds or strengthens regression evidence for:

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
- existing pending-marker rollback and committed-restore finalization behavior.

The source still deliberately keeps real process-kill, low-disk, locked-file, native filesystem, and device restart/relaunch recovery injection as external/manual release gates.

## Data-integrity evidence represented by this candidate

The integration suite now directly exercises additional corruption classes including:

- split-total drift;
- transaction/account currency mismatch;
- missing receipt files;
- receipt-size metadata drift;
- changed receipt bytes/SHA-256 drift;
- missing/invalid receipt checksum metadata;
- category parent cycles;
- SQLite foreign-key violations.

This supplements the existing transfer, budget, savings, recurrence, reconciliation, attachment-path, and privacy-safe integrity coverage.

## Privacy-logger regression correction

During the migration test expansion, CI exposed a race in a privacy-log rotation assertion. The test now synchronizes through the logger/export gate before asserting rotated/current file state, so the test verifies completed writes instead of racing the asynchronous append.

No production privacy policy was weakened to make the test pass.

## Earlier strict source candidate retained for history

Earlier strict XAML candidate:

`f7dbfbb8691edc79cee559101f284ccd90a44cf7`

Finora CI run:

`31872362394`

CodeQL run:

`31872362398`

That candidate established the first retained four-target source-build proof under fatal `XC0022`, `XC0023`, and `XC0025` diagnostics, with 241/241 automated tests passing. The newer `f80b29d…` evidence supersedes its current test-count/source-candidate role but does not erase the historical stabilization evidence.

## GitHub Actions runtime maintenance

CI-only commit:

`6ba519bf69174c68b67f8595872546a259c783dc`

updated the primary workflow to Node-24-compatible current action majors used by the repository:

- `actions/checkout@v7`;
- `actions/setup-python@v7`;
- `actions/setup-dotnet@v6`;
- `actions/upload-artifact@v7`.

The current verified `f80b29d…` run executed through those updated action majors.

## What this evidence does prove

For exact source candidate `f80b29d44a225a6d745529519e6c59cadbc152a8`, it proves:

- repository structural preflight passes;
- all 273 current automated tests pass;
- warnings-as-errors source compilation passes all four MAUI targets;
- the strict compiled-binding warning classes remain cleared on those builds;
- CodeQL analysis completes successfully;
- the added migration, hostile-backup, receipt-integrity, corruption-detection, logger synchronization, and restore-link regression cases compile and pass in the integration suite.

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