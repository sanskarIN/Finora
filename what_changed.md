# What Changed — Finora

Last continuation: **2026-08-20**  
Repository: https://github.com/sanskarIN/Finora  
Current branch: **feat/cross-platform-support-2026-08-20**  
Current source line: **Finora 0.2.0 (build 2)**  
Current database schema: **2**

This is the current continuation ledger. The complete prior 163-section ledger is preserved **byte-for-byte** at:

`docs/history/what_changed_through_2026-08-18.md`

No prior project history was deleted, summarized away, or rewritten during the final closure. The archive uses the exact Git blob that previously backed `what_changed.md`.

---

## 164. 2026-08-19 localization and accessibility completion

The final continuation completed the repository-level localization/accessibility work around the current Finora 0.2.0 source line without changing the local-first product boundary.

Completed repository surfaces include:

- localization resource parity validation and automated tests;
- localization workflow enforcement;
- native accessibility QA documentation;
- source-contract coverage for accessibility-sensitive UI behavior;
- localized onboarding project-support wording with no user-facing hard-coded entitlement text;
- a structural support invariant that keeps Buy Me a Coffee optional and external while preserving localized UI copy.

The optional support URL remains:

`https://buymeacoffee.com/sanskarIN`

It does not unlock app functionality, create premium entitlement, alter finance behavior, change security-report handling, or grant support priority.

---

## 165. Deterministic QA and artifact tooling completed

The repository now contains deterministic, local/synthetic QA tools and focused workflows for release-relevant data paths.

Completed tooling includes:

- deterministic synthetic finance CSV generation;
- sample-data generator tests and CI validation;
- CSV diagnostics and diagnostics tests/workflow;
- export-artifact verification and tests/workflow;
- encrypted-backup artifact verification and tests/workflow;
- Android native UI hierarchy smoke tooling;
- Windows native UI PowerShell smoke tooling;
- native UI harness parser/source validation workflow;
- one-command repository QA orchestration through `scripts/run_repo_qa.py`;
- repository QA unit coverage and documentation;
- structural release-readiness guard, tests, workflow, and documentation;
- `scripts/README.md` as the QA/developer-tool entry point.

These tools use deterministic synthetic data and do not require production finance data.

---

## 166. Final release-readiness guard defects found and fixed

The final audit deliberately exercised the new structural release-readiness guard and found two real path-handling defects.

### Dotfile normalization defect

The original normalization used `lstrip("./")`, which could transform `.env` into `env` and therefore bypass the intended forbidden-dotfile check.

The final implementation now removes only explicit leading `./` path segments while preserving dotfile names.

Regression coverage proves:

- `.env` remains `.env`;
- `./.env` normalizes to `.env`;
- `./.github/workflows/ci.yml` normalizes without damaging the `.github` directory name.

### Nested generated-output defect

The original forbidden-output check only recognized `bin/`, `obj/`, and `artifacts/` when they appeared at the start of a path.

The final implementation evaluates path segments, so nested outputs such as:

- `src/App/bin/Release/app.dll`;
- `obj/project.assets.json`;
- `tools/check/artifacts/result.json`

are rejected when tracked.

Regression coverage also proves ordinary source paths are not incorrectly blocked.

---

## 167. GitHub Actions runtime and queue hardening completed

The final workflow audit found that several newer Python QA workflows still referenced older Node-20-era GitHub Action majors even though the main Finora CI had already moved to current Node-24-compatible majors.

The following workflows were standardized to current repository action policy:

- backup artifact validation;
- CSV diagnostics validation;
- export artifact validation;
- localization validation;
- native UI harness validation;
- sample-data validation;
- repository release readiness.

Current enforced action-family policy is:

- `actions/checkout@v7`;
- `actions/setup-python@v7`;
- `actions/setup-dotnet@v6`;
- `actions/upload-artifact@v7`.

The release-readiness guard now detects older majors for those action families and has direct regression tests for both accepted and rejected versions.

The same seven lightweight QA workflows now define per-workflow concurrency groups with `cancel-in-progress: true`, preventing obsolete commits from indefinitely accumulating redundant validation work.

Release-readiness triggers were also expanded so changes to `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.gitignore`, `CHANGELOG.md`, and `PROJECT_STATUS.md` directly re-run structural readiness.

---

## 168. Reproducibility, dependency security, funding, and governance closure

The final closure series added or enforced:

- `global.json` selecting the supported .NET 10 SDK family with controlled feature-band roll-forward;
- explicit NuGet vulnerability auditing for direct and transitive dependencies through `Directory.Build.props`;
- `.github/FUNDING.yml` using Finora's canonical optional Buy Me a Coffee URL;
- strengthened required-file/workflow coverage in `scripts/check_release_readiness.py`;
- required protection for `docs/FINAL_REPOSITORY_CLOSURE.md`;
- required CodeQL, Dependency Review, performance, release-readiness, localization, sample-data, artifact-verification, native-UI, repository-QA, contributor, security, and governance surfaces;
- final closure documentation indexed from `docs/README.md`;
- explicit separation of repository engineering completion from external release-owner evidence.

No signing key, provisioning profile, local database, `.env` file, generated `bin`/`obj`/`artifacts` output, or backup-like `.finora` artifact is intended to be tracked.

---

## 169. Final dependency refresh merged

The four stale Dependabot update proposals were recreated together on the final repository-closure base, kept as separate commits, and merged through PR #22.

Final central versions changed in this continuation:

- `Microsoft.Maui.Controls`: `10.0.20` → `10.0.90`;
- `Microsoft.NET.Test.Sdk`: `18.8.1` → `18.9.0`;
- `SQLitePCLRaw.bundle_e_sqlite3`: `2.1.12` → `3.0.5`;
- `xunit.runner.visualstudio`: `3.1.5` → `4.0.0`.

The original Dependabot PRs #12, #13, #14, and #15 were then closed as superseded so they no longer represent a false maintenance backlog.

Earlier combined dependency candidates #18, #20, and #21 were also closed as superseded as the closure base evolved. They were intentionally not merged from stale bases.

---

## 170. Final PR and debt/backlog audit

Final implementation/repository-closure PR:

- PR #19 — `chore(release): close final repository engineering gaps`;
- merged with the 30 granular closure commits preserved through rebase merge.

Final dependency PR:

- PR #22 — `chore(deps): apply final validated dependency refresh`;
- merged with four dependency commits preserved through rebase merge.

Pre-ledger merged implementation/dependency head:

`994cbaf5f58b0dd561e719d3e0c23601941c1b0e`

Repository searches performed during the final audit found no open non-PR issue backlog and no source matches for the debt markers checked in this continuation:

- `TODO`;
- `FIXME`;
- `NotImplementedException`;
- `HACK`;
- `XXX`;
- `TEMP`.

This is evidence of the performed audit, not a mathematical claim that undiscovered defects are impossible.

---

## 171. Validation evidence boundary for the final continuation

The final continuation did not falsify runtime evidence merely to label the project complete.

The repository retains the earlier exact verified 319/319 automated-test + four-platform Release source-build + CodeQL + Dependency Review evidence anchored to source candidate `8a8e7e51a2bacecdc58405d3d5301e79f3d78c8b` and its recorded GitHub Actions runs/artifacts.

During the 2026-08-19 closure work:

- the first combined dependency candidate exposed a stale structural preflight invariant after support wording had moved into localized resources;
- that structural invariant was corrected without restoring user-facing hard-coded English;
- the strengthened release-readiness workflow then exposed the `.env` normalization and nested generated-directory guard defects described above;
- those defects were fixed and direct regression tests were added;
- later hosted workflow runs for the repeatedly advancing closure head were observed queued/pending rather than failed because many superseded commits had already created runner work;
- the new workflow concurrency policy prevents that queue pattern from recurring for future lightweight QA changes;
- the final four dependency versions were the same versions whose individual Dependabot branch commits had previously completed Finora CI, CodeQL, and Dependency Review successfully.

No queued check is represented here as a successful runtime check. Exact runtime/source evidence remains tied to the commit/run where it actually executed successfully.

---

## 172. Final repository closure boundary

As of the final 2026-08-19 repository pass, there is no known current-scope repository implementation, developer-tooling, automated-test, governance, or documentation backlog intentionally left unfinished.

What remains before public store distribution is external release evidence rather than hidden source work, including as applicable:

- production Android signing/AAB installation and Play Console review;
- Windows production packaging/signing/store review;
- Apple certificates/profiles, signed archives, notarization/distribution and App Store review;
- physical-device/emulator/simulator behavior for biometrics/Windows Hello, notifications, file pickers/sharing, screenshot/privacy behavior, locale/permission flows, interrupted restore, process-kill/low-disk conditions, and installed upgrades;
- native TalkBack/VoiceOver/Narrator, keyboard/focus, contrast, touch-target, text-scaling, high-contrast and related accessibility evidence;
- release signing-key custody and store-account operations;
- store-policy review of optional external contribution-link placement;
- explicitly requested heavy 10k/50k/100k complete benchmark evidence beyond the already recorded bounded 10k smoke.

Those actions must be recorded against the exact release candidate when performed and must never be marked complete merely because repository source exists.

Future repository work should begin only for a reproducible defect, dependency/security advisory, supported platform/toolchain change, release-evidence defect, documentation correction, or deliberately approved new-version feature.

For the current Finora 0.2.0 scope, the repository-engineering phase is closed.

---

## 173. Security-regression continuation after closure audit

A new continuation review was performed from main commit `b94fb403a2805b0fb581c649f25e931641d622df` against the Finora master build requirements. The review did not duplicate features already present in production source. Instead, it identified security-sensitive implementation surfaces that existed but were not all represented in the UI/source-contract suite.

Working branch:

`continue-hardening-2026-08-19`

Granular commits added in this continuation before CI validation:

- `fdb4ab559a0378c6b8ae1a74abfacac6075adbc4` — stage app/security lifecycle production files as read-only UI-contract inputs;
- `a33cf9fbb0a3885a35552e50721472037d54240f` — add PIN/app-lock lifecycle source contracts;
- `53f7e1003775dd0402624404bff5f1efd01b96bf` — add sensitive-screen/capture-protection source contracts;
- `e58317b1d6f6defa0ce77a0f2400c7491ce7eafe` — stage `LockPage.xaml` as a source-contract input;
- `06e2fe9c739f2815273750e204a15483de34a420` — add biometric/Windows Hello + PIN-fallback source contracts;
- `a17319cdfe1ac96bfc28c41a95a8acc8f435d641` — add local-premium/demo entitlement-boundary source contracts;
- `413c4f56c817737610773f0ee1f836f299b3970d` — add `docs/testing/SECURITY_ACCEPTANCE.md` with automated/native evidence boundaries.

The new automated source contracts protect these existing production requirements from silent regression:

- PBKDF2-SHA256 local PIN derivation and fixed-time verifier comparison;
- secure-storage-backed PIN verifier material and managed cryptographic buffer clearing;
- fail-closed behavior for an enabled lock when secure storage cannot be read;
- startup lock routing and inactivity-based re-lock wiring;
- permanent masked numeric PIN fallback on the lock screen;
- biometric preference/native-availability gating and failed-authentication handling;
- Android biometric prompt with explicit `Use PIN` fallback;
- Apple LocalAuthentication integration;
- Windows Hello integration;
- Android `FLAG_SECURE` and Windows display-affinity capture protection;
- explicit non-support behavior instead of claiming universal screenshot blocking;
- reapplication of capture protection at startup/activation;
- hidden local premium demo flag and its explicit non-commercial/tamperable entitlement wording;
- separation of optional Buy Me a Coffee support from feature entitlement.

`docs/testing/SECURITY_ACCEPTANCE.md` additionally records native validation that source-contract tests cannot prove, including physical/native biometric prompts, secure-storage lifecycle behavior, screen-capture behavior, suspend/resume timing, and lock-screen accessibility.

No source-contract test in this continuation is represented as physical-device evidence. GitHub Actions results for the exact continuation head will be recorded in a later ledger section after execution.

---

## 174. Merged security continuation and biometric availability resilience

The security-regression continuation was merged through PR #24 with all eight branch commits preserved plus merge commit:

`99a6016abf2ed85e9f89363386f6e42e28d40d50`

During the post-merge source review, a concrete production robustness issue was identified in `LockViewModel`: biometric availability discovery was launched fire-and-forget from the constructor, so a transient exception from a native credential provider could fault that task unobserved.

A second focused branch and PR fixed that path with three granular commits:

- `0475f784a062d6098f615ca627964fbce3df5d85` — `fix(security): contain biometric availability faults`;
- `0e571c65fb5605651db1030e3164d4d2e3e5a59d` — `test(security): guard biometric availability fallback`;
- `0b920f960bb15e6785d719f4a33570e11c965104` — `docs(security): document biometric availability resilience`.

The production behavior now:

- skips the native biometric availability probe when biometric unlock is disabled;
- contains native availability exceptions;
- leaves `CanUseBiometrics` false on an availability failure;
- keeps the normal PIN path available;
- prevents the constructor-started availability task from becoming an unobserved fault solely because a native provider is temporarily unavailable.

PR #25 preserved those three commits and merged with merge commit:

`6ffe43563302476f5c8ebe6b025539ce04516d79`

### GitHub Actions evidence state at this checkpoint

PR #24 exact head `b80e65219b2e50d247cfa44298b1f8dfbe70399c` created these hosted runs:

- Finora CI — run `32239329398`;
- Repository release readiness — run `32239329379`;
- Dependency Review — run `32239329278`;
- CodeQL — run `32239329520`.

PR #25 exact head `0b920f960bb15e6785d719f4a33570e11c965104` created these hosted runs:

- Finora CI — run `32239596350`;
- Repository release readiness — run `32239596378`;
- Dependency Review — run `32239596321`;
- CodeQL — run `32239596331`.

At the last observation during this continuation, every run listed above was **queued** with no conclusion. None was observed failed, but none is represented as passed. The earlier verified 319/319 and four-platform source-build evidence remains tied only to its historically recorded candidate and is not reused as runtime proof for these new commits.

The new source assertions were manually cross-checked against the exact production source they protect before merge. That source review is useful regression review evidence, but it remains distinct from hosted build/test execution and native-device validation.

---

## 175. Final correctness, restore-safety, and governance hardening

A final post-closure source audit was opened as PR #26 on branch `final-hardening-2026-08-19`. It found and corrected additional reproducible correctness/safety issues rather than adding speculative features.

### Budget threshold correctness

- Added `PercentageMath.CeilingPercentOf` for overflow-safe ceiling percentage arithmetic.
- Budget warning reminders now wait until the configured fractional threshold is actually reached instead of flooring the result and potentially firing one minor unit early.
- Added normal, fractional, invalid, zero, and `long.MaxValue` boundary tests.

### Transaction factory validity

- `TransactionFactory.Create` now refuses `TransactionType.Transfer`, because a generic single-row constructor cannot create Finora's required reciprocal balanced transfer pair.
- Returned non-transfer transactions are validated through `DomainRules.ValidateTransaction` before leaving the factory.
- Tests cover valid sign behavior plus transfer, missing-account, and missing-timestamp rejection.

### Receipt metadata consistency

- Receipt MIME values are trimmed/canonicalized before persistence.
- The private stored extension now follows the allowed MIME type rather than a conflicting original filename extension.
- The original user-facing filename remains stored separately.
- Integration tests cover mismatched original filename/MIME inputs and MIME whitespace normalization.

### Encrypted restore receipt swap safety

- Added an explicit receipt-directory recovery primitive and integration coverage.
- If the live receipt directory was moved to rollback storage and staging/promotion later fails, the original tree is restored instead of deleting rollback evidence.
- If promoted receipts exist but there was no original receipt directory, database-commit failure removes that newly promoted tree.
- If an expected rollback directory is unexpectedly missing, recovery fails closed instead of deleting the current live tree and falsely claiming success.
- Cancellation and commit-failure paths use the same explicit recovery state.
- Existing `CrashSafeBackupService` journal/marker recovery remains the outer process-interruption layer; the inner `BackupService` is now safer independently as well.

### Strict build-policy review

The new test code was reviewed for the repository's `Nullable=enable`, `TreatWarningsAsErrors=true`, latest-recommended analyzer policy. A nullable-flow dereference in new receipt metadata coverage was corrected before final documentation.

### Documentation/governance

- Added `docs/FINAL_HARDENING_2026-08-19.md` with the complete post-closure audit, fixes, evidence boundary, backlog state, and release boundary.
- Added `docs/development/BRANCH_PROTECTION.md` defining the intended `main` ruleset/status-check policy and administrative validation steps.
- Updated `docs/README.md` to index both documents.
- GitHub currently reports `main` as **not protected**. This repository interface does not expose a safe branch-protection mutation action, so the ledger does not falsely claim that host-level setting was enabled.
- Repository issue search returned no open non-PR issues; the only open PR at the audit checkpoint was PR #26 itself.

### Hosted evidence state

The older exact verified candidate `8a8e7e51a2bacecdc58405d3d5301e79f3d78c8b` remains the recorded **319/319 automated tests + four-platform Release source builds + CodeQL + Dependency Review** evidence baseline and is not reused as runtime proof for PR #26.

The pre-ledger PR #26 documentation head `4950134c1667b37b0d4ef170cbfe7712eae4ff3c` created these observed runs:

- Finora CI — run `32242452702` — queued;
- Repository release readiness — run `32242452546` — queued;
- Dependency Review — run `32242452637` — queued;
- CodeQL — run `32242452674` — pending.

None had a conclusion at the recorded observation. This ledger update itself advances the PR head and therefore creates a newer exact candidate; no older run is represented as validating that newer commit.

Remaining gates continue to be native/release-owner evidence: signed packages, installation and device behavior, process-kill/low-disk restore injection, accessibility validation, store-console policy checks, signing-key custody, and store approval.

---

## 176. Exhaustive repository documentation coverage closure

After PR #26 merged, a second final documentation/tooling branch was reviewed and merged through PR #27 to make repository-file ownership mechanically complete rather than relying only on human memory.

### Repository-file reference and QA gate

- Added `docs/development/REPOSITORY_FILE_REFERENCE.md` as the exhaustive tracked-file responsibility and change-impact map.
- Added `scripts/check_documentation_coverage.py`, backed by `git ls-files`, so ignored/untracked local files do not enter the public documentation contract.
- The checker rejects tracked files missing from the reference, stale entries that cover no tracked file, and overly broad one-component directory declarations such as `src/`, `docs/`, `tests/`, `.github/`, or `scripts/`.
- Integrated the coverage checker into `scripts/run_repo_qa.py` and the primary CI structural preflight.
- Added/updated contributor, build, developer, code-map, documentation-status, repository-QA, scripts, and pull-request guidance so new/moved/deleted files carry explicit documentation ownership.
- Updated the inventory to cover `docs/FINAL_HARDENING_2026-08-19.md` after synchronizing the documentation branch with the already-merged hardening work.

### Checker defects found during final review

Two real defects were identified before merge and fixed as separate granular commits:

1. a successful run using an absolute temporary/external `--reference` path could throw `ValueError` while formatting the success message because it unconditionally attempted `reference.relative_to(REPO_ROOT)`;
2. `--list-missing` could return exit code 0 when the only defect was a stale/unused reference entry, inconsistent with the strict normal-mode coverage contract.

The success-message path now safely displays repository-local or external references, and list mode now returns failure for missing, stale, or invalid coverage. Regression coverage protects both behaviors, including the existing temporary-reference main-path test and a dedicated stale-only list-mode test.

### Merge and evidence state

PR #27 was synchronized to the PR #26 hardening result and was **25 commits ahead / 0 behind** current `main` immediately before merge. It was rebase-merged successfully; the returned merged head was:

`f4aa57ef7815cc81a067ad1013f83081b3f18bbc`

The last exact pre-merge PR #27 head observed was:

`59b6689bc0b416f4cde7638f9bd045ec74f192ff`

Its hosted workflows were observed with no failure conclusion but still queued:

- Finora CI — run `32243298450` — queued;
- Repository release readiness — run `32243298475` — queued;
- CodeQL — run `32243298484` — queued;
- Dependency Review — run `32243298500` — queued.

Queued checks are not represented as successful runtime evidence. The older exact verified candidate `8a8e7e51a2bacecdc58405d3d5301e79f3d78c8b` remains the recorded 319/319 automated-test + four-platform Release source-build + CodeQL + Dependency Review baseline.

The repository-engineering completion claim remains bounded: native signing/package installation, physical-device behavior, accessibility, interrupted-process/low-disk restore injection, store-console policy review, signing-key custody, and store approval are external release evidence, not hidden repository implementation work.

---

## 177. 2026-08-20 universal cross-platform continuation and validation hardening

The supported-platform/toolchain continuation reopened repository engineering through PR #29 on branch `feat/cross-platform-support-2026-08-20`. The branch extends Finora beyond its established MAUI application without replacing that application or weakening the local-first/privacy model.

### Cross-platform source foundation

The PR adds and documents:

- `Finora.Universal`, an Avalonia 12.1.1 shared presentation/runtime-capability layer;
- `Finora.Universal.Desktop`, a `net10.0` desktop host for Linux, Windows, and macOS;
- native desktop reuse of the existing EF Core/SQLite finance store and `DatabaseInitializer`;
- `Finora.Universal.Browser`, a `net10.0-browser` WebAssembly host;
- an installable PWA manifest and Finora web icon;
- a strict browser boundary that leaves finance persistence disabled until a browser-local encrypted persistence adapter passes migration, recovery, integrity, privacy, quota/eviction, backup/restore, attachment, and offline validation;
- `Finora.CrossPlatform.slnx`, now including Shared, Domain, Application, Infrastructure, MAUI App, all three universal projects, Unit/Integration/UI tests, and the performance harness;
- a dedicated cross-platform workflow with dependency-free preflight, desktop Release builds on Ubuntu/Windows/macOS, and a WebAssembly Release build;
- Linux, Web/PWA, ChromeOS, support-matrix, build, file-reference, and documentation-index material.

The established MAUI targets remain Android, iOS/iPadOS, Mac Catalyst, and Windows. The universal path adds Linux desktop and Web/PWA reach and an additional desktop host for Windows/macOS. ChromeOS is represented through Android and/or browser delivery paths, not a fabricated dedicated native ChromeOS project.

### Defects found by hosted validation and corrected

The first PR candidate exposed multiple concrete defects rather than a generic red status:

1. the cross-platform manifest gate searched for an exact textual token and rejected the valid manifest path `./finora-icon.svg`;
2. the repository placeholder scanner interpreted historical audit prose naming its own marker categories as unfinished work;
3. the release-readiness merge-conflict scanner embedded the literal marker text it searched for, causing the checker and its own regression test to report themselves;
4. strict .NET analyzers promoted `CA1859` in localization resource-manager storage and `CA1863` in repeated localized lockout formatting to build errors;
5. Avalonia 12 compiled bindings required the universal `MainView` to declare a concrete binding data type, which the initial view omitted;
6. the initial cross-platform solution omitted the existing performance harness.

Those defects were corrected without disabling warnings, weakening repository QA, restoring hard-coded English, or enabling unsafe browser persistence.

### Hardening commits added in this continuation

The continuation added these granular commits after the initial cross-platform foundation:

- `55925aee10d31eae3c46b5fe45887efa5bef186c` — `fix(cross-platform): validate PWA manifest semantically`;
- `d92c028e54bc89e38f31257cc7231e169bc27f46` — `test(cross-platform): cover valid relative PWA icon paths`;
- `f1a5d8e62d5060f82f4d5ed966c60e00d18b8211` — `fix(qa): prevent conflict scanner from matching itself`;
- `d05802f2eff63b0fc60dfea11e64c7a2338e9415` — `test(qa): construct conflict fixtures without self-triggering`;
- `006bb2c877b394ca5e0c019933425d449a704bfe` — `docs(qa): avoid audit prose triggering placeholder gate`;
- `611ccabf968de800d5bf2be9678d930140ad2a42` — `perf(localization): keep resource manager storage concrete`;
- `e46ba6d61c445bf48c1f0d65b00cb2ecd75cbe68` — `perf(localization): cache parsed localized format templates`;
- `42c708ff2796ed9cccce2a7841bb5f251f2b763c` — `perf(security): reuse localized composite lockout format`;
- `d1c16112e4dc4f89a7ab0002cbbf31e6fba09e3f` — `test(localization): guard cached localized formatting path`;
- `20da6edf75a1931d88597db6344bb1670f435081` — `fix(universal): declare compiled binding data type`;
- `72cd64bbaad5f651d172142c7e073b744f955ffc` — `build(universal): make compiled binding policy explicit`;
- `c746d7947a537b4331e29c87474a1317a16f8857` — `testability(cross-platform): enforce compiled binding contract`;
- `050dec3bdd1e2e9c3e7ddbb55fd614ad17f05e36` — `test(cross-platform): guard Avalonia compiled bindings`;
- `937eb45224b653dfd7275572ab4a0dff2643cc84` — `docs(linux): define X11 and Wayland support boundary`;
- `4b3049bb350e821dad2855943f158389aac4af59` — `build(cross-platform): include performance harness in solution`;
- `0240f69c8c7eb8284a7b31974d8e1a943f2556d7` — `qa(cross-platform): enforce complete solution project set`;
- `68dd5b37767fb7c3fe0a7bbe8a38ac2ad793a661` — `test(cross-platform): guard complete solution inventory`;
- `5011e1f16f3f5d1b306f30ba6f66598a1e861dad` — `docs(web): add publish and runtime validation workflow`.

### Linux and browser honesty boundaries

Linux documentation now distinguishes the stable X11 baseline used by the normal Avalonia platform-detection path from a separate native-Wayland opt-in/validation decision. Finora does not claim native-Wayland release validation merely because the desktop project targets Linux.

Web documentation now separates build, optimized publish output, HTTP(S) runtime validation, PWA installation metadata, and actual finance-persistence readiness. The current manifest does not imply that service-worker-backed offline finance, background sync, or durable browser finance storage is complete.

### Exact validation evidence boundary

The pre-ledger branch head was:

`5011e1f16f3f5d1b306f30ba6f66598a1e861dad`

GitHub created these exact-head runs at the recorded checkpoint:

- Finora Cross-Platform — run `32352968779` — queued;
- Repository release readiness — run `32352968897` — queued;
- CodeQL — run `32352968766` — queued;
- Finora CI — run `32352968828` — queued;
- Dependency Review — run `32352968819` — queued.

No queued run is represented as passed. This ledger commit advances the branch again and therefore requires new exact-head evidence before merge/release claims are strengthened. The earlier verified candidate `8a8e7e51a2bacecdc58405d3d5301e79f3d78c8b` remains historical evidence for 319/319 automated tests, four MAUI Release source builds, CodeQL, and Dependency Review; it is not reused as proof for the universal-host branch.

GitHub still reports `main` as unprotected, and open issue #28 tracks that host-administration action. The repository connector used in this continuation does not expose a safe branch-protection/ruleset mutation, so that issue remains open rather than being falsely marked complete.

Remaining cross-platform release work is intentionally explicit: complete Linux feature/UI parity, native Linux packaging/runtime/accessibility validation, browser-local encrypted persistence design and recovery testing, real browser/PWA validation, ChromeOS-path validation, and the existing signing/device/store evidence for MAUI platforms.

---

## Historical ledger integrity

Sections **1–163** remain available in full, unchanged form at `docs/history/what_changed_through_2026-08-18.md`. This split was performed only because the cumulative file was too large for a safe single contents-API append while GitHub-hosted runners were unavailable. No historical section was discarded.