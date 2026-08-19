# What Changed — Finora

Last continuation: **2026-08-19**  
Repository: https://github.com/sanskarIN/Finora  
Current branch: **main**  
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

## Historical ledger integrity

Sections **1–163** remain available in full, unchanged form at `docs/history/what_changed_through_2026-08-18.md`. This split was performed only because the cumulative file was too large for a safe single contents-API append while GitHub-hosted runners were unavailable. No historical section was discarded.
