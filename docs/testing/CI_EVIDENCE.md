# Finora CI Evidence

Last verified source-build evidence: **2026-08-15**

This document records concrete GitHub Actions evidence for the current Finora 0.2.0 source line. It separates compiler/test evidence from device, packaging, signing, accessibility, recovery-failure-injection, and store evidence.

## Fully verified strict source candidate

Candidate commit:

`f7dbfbb8691edc79cee559101f284ccd90a44cf7`

Commit message:

`build(xaml): enforce compiled binding diagnostics`

Finora CI run:

`31872362394`

CodeQL run:

`31872362398`

The candidate enables strict MAUI compiled-binding diagnostics so `XC0022`, `XC0023`, and `XC0025` are build errors rather than silently accepted warnings.

## Structural and automated test evidence

Finora CI run `31872362394` completed the following successfully:

- Structural preflight — job `94982883413`;
- Core tests — job `94982906864`.

Exact test results retained from the run artifacts/logs:

| Test project | Passed | Failed |
| --- | ---: | ---: |
| Finora.UnitTests | 97 | 0 |
| Finora.IntegrationTests | 109 | 0 |
| Finora.UiTests | 35 | 0 |
| **Total** | **241** | **0** |

The core job restored each test project and executed all three suites in Release configuration.

## Native Release source-build evidence

The same exact candidate commit passed all four independent native source-build jobs:

| Target | Job | Result | Evidence boundary |
| --- | --- | --- | --- |
| Windows | `94983017634` | Passed | Unpackaged `net10.0-windows10.0.19041.0` Release source build; `WindowsPackageType=None` |
| Android | `94983017627` | Passed | `net10.0-android` Release source build |
| iOS | `94983017606` | Passed | `net10.0-ios` Release source build on GitHub macOS runner |
| Mac Catalyst | `94983017649` | Passed | `net10.0-maccatalyst` Release source build on GitHub macOS runner |

These jobs are intentionally independent. Failure of one target no longer cancels another target before its diagnostic can be collected.

## CodeQL evidence

CodeQL run `31872362398` completed successfully for candidate `f7dbfbb8691edc79cee559101f284ccd90a44cf7`.

Its Android analysis build also completed successfully before CodeQL analysis closed green.

## Build-stabilization evidence represented by this candidate

The successful candidate includes the fixes that cleared the previously observed native failures:

- runtime-recognized Android API-level guards for biometric APIs and notification permission;
- narrow Apple `AppDelegate` analyzer handling rather than global analyzer weakening;
- Entity Framework Core / SQLite servicing update to `10.0.10`, which removed the linker metadata failure previously emitted from EF Core;
- Windows compile validation separated from MSIX packaging so source compilation is not blocked by the runner's manifest-packaging toolchain;
- explicit `x:DataType` contracts across the XAML pages/templates that previously produced compiled-binding warnings;
- strict `XC0022`, `XC0023`, and `XC0025` enforcement to prevent regression.

## GitHub Actions runtime maintenance

After the fully verified source candidate, CI-only commit:

`6ba519bf69174c68b67f8595872546a259c783dc`

updated the main Finora CI workflow to the current Node-24-compatible action majors used by the repository:

- `actions/checkout@v7`;
- `actions/setup-python@v7`;
- `actions/setup-dotnet@v6`;
- `actions/upload-artifact@v7`.

Its Finora CI run is `31873092936` and CodeQL run is `31873092949`. Before the subsequent documentation sequence superseded the run through CI concurrency, the updated workflow had successfully executed structural preflight, .NET setup, all three test restores, and the unit test step. The final repository head must receive its own CI run after the documentation/ledger sequence completes.

## What this evidence does prove

For the exact strict source candidate it proves:

- repository structural preflight passes;
- all 241 current automated tests pass;
- warnings-as-errors source compilation reaches and passes all four MAUI targets;
- the compiled-binding warning classes promoted to errors do not remain on those builds;
- CodeQL analysis completes successfully.

## What this evidence does not prove

It does **not** mark the following release gates complete:

- signed Android AAB production packaging;
- Windows MSIX generation, publisher identity, or signing;
- iOS provisioning, signing, archive, TestFlight, or App Store submission;
- Mac Catalyst signing, notarization, or distribution packaging;
- physical-device/emulator notification behavior;
- biometric/Windows Hello native behavior on enrolled/unavailable/lockout states;
- Android merged-manifest and real backup/device-transfer behavior;
- process-kill backup/restore recovery validation on target devices;
- native screen-reader, keyboard, large-text, high-contrast, or reduced-motion QA;
- real file picker/share/receipt flows on every target;
- current store policy, privacy declaration, external support-link, or submission approval;
- an assertion that no undiscovered defect exists.

Those remain evidence-based release tasks in `docs/NEXT_STEPS.md`, `docs/releases/RELEASE_CHECKLIST.md`, and `docs/releases/STORE_READINESS.md`.

## Evidence policy

Future release-candidate evidence should be appended with:

1. exact commit SHA;
2. workflow/run IDs;
3. job IDs or retained artifacts;
4. exact test counts;
5. target framework/configuration;
6. packaging/signing/device boundary;
7. unresolved failures or external gates.

A green source build must never be relabeled as signed package, device, accessibility, recovery, or store evidence without executing that separate gate.
