# Finora developer and QA tools

The `scripts/` directory contains dependency-light helpers for repeatable development and release validation. They are designed for synthetic/disposable data and do not replace Finora's application-level tests or native platform QA.

## Fast repository checks

### `run_repo_qa.py`

Runs the dependency-free Python tool tests, tracked-file documentation coverage, and localization validation from one command.

```bash
python scripts/run_repo_qa.py
```

Include the .NET suite when the SDK/workloads are available:

```bash
python scripts/run_repo_qa.py --include-dotnet
```

Guide: `docs/testing/REPOSITORY_QA.md`

### `check_documentation_coverage.py`

Compares `docs/development/REPOSITORY_FILE_REFERENCE.md` with the exact tracked-file set returned by `git ls-files`.

It fails when:

- a tracked file has no documented responsibility;
- an inventory entry no longer covers a tracked file; or
- the reference uses an overly broad one-component directory catch-all such as `src/`, `docs/`, or `tests/`.

```bash
python scripts/check_documentation_coverage.py
```

Print only uncovered paths:

```bash
python scripts/check_documentation_coverage.py --list-missing
```

Reference: `docs/development/REPOSITORY_FILE_REFERENCE.md`

### `check_release_readiness.py`

Checks required release/contributor files, tracked high-risk artifacts, generated output directories, merge-conflict markers, and the project ledgers.

```bash
python scripts/check_release_readiness.py
```

Guide: `docs/testing/RELEASE_READINESS.md`

## Localization

### `validate_localization.py`

Validates:

- neutral/Hindi bundle pairing,
- identical resource keys,
- placeholder parity,
- non-empty values,
- global key uniqueness,
- XAML `DynamicResource Text.*` references,
- C# `LocalizationResources.Get("...")` references.

```bash
python scripts/validate_localization.py
```

Guide: `docs/localization/LOCALIZATION_IMPLEMENTATION.md`

## Synthetic finance fixtures

### `generate_sample_finance_csv.py`

Creates deterministic synthetic CSV data for import, performance, reports, backup, export, accessibility, and native UI testing.

```bash
python scripts/generate_sample_finance_csv.py artifacts/sample.csv --rows 10000 --seed 20260819
```

It never reads user data or contacts a network service.

Guide: `docs/testing/SAMPLE_DATA.md`

## CSV import diagnostics

### `diagnose_finora_csv.py`

Performs privacy-safe structural diagnostics without writing to a Finora database or echoing transaction values.

```bash
python scripts/diagnose_finora_csv.py artifacts/sample.csv
```

For integer minor-unit fixtures:

```bash
python scripts/diagnose_finora_csv.py artifacts/sample.csv --minor-units
```

Guide: `docs/import/CSV_DIAGNOSTICS.md`

## Export verification

### `verify_export_artifact.py`

Checks CSV structure/row counts/configured columns or a PDF envelope without printing exported financial content.

```bash
python scripts/verify_export_artifact.py path/to/export.csv \
  --require-column Date \
  --require-column Amount
```

Guide: `docs/export/EXPORT_VERIFICATION.md`

## Backup artifact verification

### `verify_backup_artifact.py`

Checks basic encrypted-backup artifact properties without asking for a password or decrypting the file.

```bash
python scripts/verify_backup_artifact.py path/to/backup.finora
```

It can also compare a previously recorded SHA-256 digest.

Guide: `docs/backup/BACKUP_VERIFICATION.md`

## Native UI smoke harnesses

### `android_ui_smoke.py`

Uses ADB/UIAutomator hierarchy inspection to validate launch/accessibility expectations on an installed Android build. It does not persist screenshots or the full hierarchy.

```bash
python scripts/android_ui_smoke.py --package <FINORA_APPLICATION_ID> --expect-text Dashboard
```

### `windows_ui_smoke.ps1`

Uses built-in Windows UI Automation to validate accessible names/automation IDs for a built Finora executable.

```powershell
pwsh -File scripts/windows_ui_smoke.ps1 -Executable "<PATH_TO_FINORA_EXE>" -ExpectName "Dashboard"
```

Guide: `docs/testing/NATIVE_UI_AUTOMATION.md`

## Unit tests

All Python tool tests live under `scripts/tests/` and can be run together:

```bash
python -m unittest discover -s scripts/tests -p "test_*.py" -v
```

Each tool also has focused workflow coverage under `.github/workflows/` where appropriate. The primary Finora CI structural-preflight job also runs `scripts/run_repo_qa.py`, which includes the tracked-file documentation coverage check.

## Privacy rules for developer tooling

- Prefer deterministic synthetic data.
- Do not commit real Finora databases, backups, exports, PINs, backup passwords, signing material, or personal finance files.
- Do not put real account names, transaction descriptions, balances, or other private data in CI arguments/logs.
- Artifact validators intentionally report structural metadata rather than contents.
- Native smoke harnesses intentionally avoid screenshots/full hierarchy dumps by default.
- Documentation coverage reads tracked path names only; it does not open or publish user finance artifacts.

## Scope boundary

These tools do not prove native packaging/signing, biometric behavior, notification delivery, accessibility-tool behavior, store submission readiness, or financial correctness by themselves. Combine them with `dotnet test`, target-platform builds, native/manual QA, and the release checklists under `docs/`.
