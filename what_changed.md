# What Changed — Finora

Last continuation: **2026-08-24**  
Repository: https://github.com/sanskarIN/Finora  
Current branch: **feat/cross-platform-support-2026-08-20**  
Current source line: **Finora 0.2.0 (build 2)**  
Prepared next source line: **Finora 0.3.0 (build 3)**  
Current database schema: **2**

This is the active continuation ledger for work performed after the 2026-08-20 cross-platform pass.

Historical ledgers are preserved without deleting prior work:

- sections **1–163**: `docs/history/what_changed_through_2026-08-18.md`;
- sections **164–178**: `docs/history/what_changed_through_2026-08-20.md`.

The 0.3.0 value above is a prepared next-version target, not a claim that 0.3.0 has already shipped. The checked-in application version remains 0.2.0 (build 2) until the exact release candidate is ready for the coordinated version cut described in `docs/releases/0.3.0_PLAN.md`.

---

## 179. 2026-08-24 continuation — cross-platform CI blockers corrected

The continuation resumed PR #29, `feat: add Linux and Web universal platform foundations`, rather than starting a parallel implementation branch. The PR remained mergeable and already contained the additive Avalonia desktop/browser foundation documented in the archived 2026-08-20 ledger.

Hosted validation for the earlier PR head exposed three concrete warnings-as-errors failures:

1. `DesktopUniversalRuntime` declared the newly created `FinanceStore` through `IFinanceStore`, triggering analyzer `CA1859` on all three universal desktop build runners;
2. `TransactionsChartOnboardingContractTests.ReadResx` returned `IReadOnlyDictionary<string, string>` even though the implementation always returns a concrete `Dictionary<string, string>`, triggering `CA1859`;
3. `SupportVisibilityContractTests.ReadResx` had the same concrete-return analyzer failure.

The failures were corrected at source/test level without suppressing analyzers or weakening `TreatWarningsAsErrors`:

- `23a3562224bdad0b67f0ab515df420922366dd79` — `fix(universal): satisfy desktop analyzer contract`;
- `2b78f4e8756bc826492485d91e40e5de6b6a27c1` — `fix(tests): use concrete resource dictionary type`;
- `942f1b778ec9d493a4b077391315e15227437508` — `fix(tests): satisfy support contract analyzer`.

The earlier failing candidate had already demonstrated that the browser WebAssembly build succeeded and that Unit tests passed **115/115** and Integration tests passed **185/185**. Those historical passes are not reused as proof for the newer branch head; fresh exact-head validation is required after these commits.

---

## 180. Browser repository hygiene cleanup

A changed-file audit found two accidental temporary marker files still tracked under the WebAssembly/PWA `wwwroot` tree:

- `.binary-assets-readme.tmp`, whose own text explicitly stated that the temporary marker should be removed;
- `.binary-assets-readme.tmp2`, containing only a one-character sentinel.

They were removed in separate commits so generated/temporary scaffolding cannot become part of the intended browser source surface:

- `4045ed81906d9b48b25b2dfda8baa0b881e5fc64` — `chore(web): remove temporary binary asset marker`;
- `238ec25fb18f90a7a2e0cf0ed95eba3571d6a802` — `chore(web): remove stray binary asset sentinel`.

The actual committed browser icons, manifest, HTML, JavaScript, CSS, runtime configuration, and project source remain intact.

---

## 181. Dependabot maintenance defect corrected

Open Dependabot PR #30 reported that repository label `dependencies` did not exist, so the configured label assignment could not be applied. The problem was in `.github/dependabot.yml`, not in the package update itself.

The continuation removed unavailable custom-label assignments from both the NuGet and GitHub Actions update blocks while preserving schedules, pull-request limits, timezone, and commit-message prefixes:

- `8728269362a1c0bab5d7be528f7f7bedee9ca20a` — `fix(dependabot): remove unavailable custom labels`.

This prevents future Dependabot pull requests from repeatedly reporting a configuration warning merely because an optional repository label is absent.

---

## 182. Microsoft.Maui.Controls 10.0.100 folded into the unified candidate

Dependabot PR #30 proposed a single central package change from `Microsoft.Maui.Controls` 10.0.90 to 10.0.100. Rather than validating the same dependency independently beside the large cross-platform candidate, the update was applied to PR #29 so the established MAUI application and the new universal work can be tested together on one exact branch head:

- `30e017ab7c56711a7a80aedd447552cc3056356b` — `chore(deps): update MAUI controls to 10.0.100`.

Current selected package baseline on the branch includes:

- Microsoft.Maui.Controls 10.0.100;
- Microsoft.EntityFrameworkCore / Sqlite 10.0.10;
- SQLitePCLRaw.bundle_e_sqlite3 3.0.5;
- Avalonia family 12.1.1;
- Microsoft.NET.Test.Sdk 18.9.0;
- xUnit 2.9.3;
- xunit.runner.visualstudio 4.0.0.

PR #30 should be closed as superseded only after the equivalent 10.0.100 update is safely present in the merged/validated mainline candidate. Until then, it remains historical maintenance context rather than being falsely declared completed.

---

## 183. Finora 0.3.0 next-version preparation opened

The next semantic-minor source line is prepared as **Finora 0.3.0 (build 3)** because the current branch adds a new platform family/foundation while preserving the current finance-data contract. Database schema remains **2** unless a later deliberately approved persisted-model change requires a real migration.

Preparation was added in two granular documentation commits:

- `267fec849678aebbc312df45e73758ad8bcd562f` — `docs(release): add Finora 0.3.0 preparation plan`;
- `471dc9e0263a2a292881da1cb07f37811a371ad6` — `docs(index): link Finora 0.3.0 release plan`.

`docs/releases/0.3.0_PLAN.md` now defines:

- the proposed 0.3.0 objectives and supported delivery paths;
- the dependency baseline including MAUI 10.0.100 and Avalonia 12.1.1;
- an atomic version-cut checklist for MAUI metadata, Windows package metadata, README/docs/status/changelog/ledger, and store metadata;
- the exact automated gates required before the source version is cut;
- MAUI native packaging/device requirements;
- Linux runtime/packaging/accessibility requirements;
- Web/PWA publish/installability/privacy/durability requirements;
- ChromeOS Android/Web validation boundaries;
- explicit exclusions so browser persistence, full Avalonia feature parity, native Wayland validation, cloud accounts, automatic FX, analytics, signing, or store approval cannot be inferred from a successful source build.

The repository intentionally does **not** bump `ApplicationDisplayVersion`, `ApplicationVersion`, or the Windows manifest in this preparation step. Those values remain 0.2.0 / build 2 / 0.2.0.0 until the exact release candidate is ready for the coordinated cut.

---

## 184. Ledger rollover and exact-history preservation

The previous active `what_changed.md` had accumulated sections 164–178 after the earlier sections 1–163 were already archived. To keep the active ledger practical without deleting project history, the complete pre-2026-08-24 active blob was copied unchanged to:

`docs/history/what_changed_through_2026-08-20.md`

Archive commit:

- `7c41c9e591c2bbf3518371f75782c36800a8ebce` — `docs(history): archive cross-platform ledger through 2026-08-20`.

This active file now starts at section 179 and links both historical ledger files explicitly.

---

## 185. Validation and merge boundary for this continuation

Every source/dependency/documentation change above advances the PR head and therefore invalidates earlier queued workflow evidence for the purpose of an exact-candidate release claim.

The final branch head after this ledger update must receive fresh GitHub-hosted validation. The required set includes, where triggered:

- Finora CI structural preflight;
- Unit, Integration, and UI-contract tests;
- bounded performance smoke;
- Android, Windows, iOS, and Mac Catalyst Release source builds;
- Finora Cross-Platform contract validation;
- universal desktop Release builds on Ubuntu, Windows, and macOS;
- universal WebAssembly Release build;
- Repository release readiness;
- localization, deterministic sample-data, CSV diagnostics, and export-artifact validation;
- CodeQL;
- Dependency Review.

No queued or in-progress workflow is represented as passed. PR #29 should be merged only after the exact final candidate is green or any new concrete failures are corrected. Host-level `main` protection remains separately tracked by issue #28 and is not represented as completed through repository source changes.
