# Structural release-readiness checks

Finora includes `scripts/check_release_readiness.py` for repository-level checks that should be true before packaging a release candidate.

This is intentionally a **structural guard**. A passing result does not claim that Android/Windows/iOS/macOS builds, signing, native accessibility, biometrics, notifications, file pickers, or store submission have passed.

## Run locally

```bash
python scripts/check_release_readiness.py
```

Machine-readable output:

```bash
python scripts/check_release_readiness.py --json
```

## What it checks

### Required project/release files

The guard requires the core repository, legal, security, contributor, QA, roadmap, and change-ledger files used by the current Finora release process.

It also requires the validation workflows for:

- normal CI,
- localization,
- deterministic sample data,
- CSV diagnostics,
- export artifact checks,
- backup artifact checks,
- native UI harness syntax/parser checks,
- repository release readiness.

Missing or empty required files fail the guard.

### Tracked secret/signing/database artifacts

Tracked paths matching high-risk local artifacts fail the guard, including common:

- `.env` files,
- signing certificates/keys,
- Android keystores,
- provisioning profiles,
- SQLite/database files,
- `.finora` backup-like artifacts.

Generated `artifacts`, `bin`, and `obj` directories are also rejected when tracked, including nested project output directories.

This is defense in depth, not a complete secret scanner. Credentials can still appear in ordinary source/text files, and release owners must continue normal secret-review practices.

### Merge-conflict markers

Tracked text/source files are scanned for unresolved Git conflict markers such as:

```text
<<<<<<<
>>>>>>>
|||||||
```

A conflict marker fails the check before packaging.

### Project ledgers

`what_changed.md` and `docs/NEXT_STEPS.md` must exist and contain substantive content. The guard cannot determine whether every sentence is current; maintainers must still update both ledgers as project work changes.

## CI

`.github/workflows/release-readiness.yml` runs the guard and its unit tests for changes to source, tests, scripts, docs, GitHub configuration, legal/security files, contributor guidance, and the project change ledger.

## Recommended pre-release sequence

1. Update `what_changed.md` and `docs/NEXT_STEPS.md` from the current branch state.
2. Run:

   ```bash
   python scripts/run_repo_qa.py --include-dotnet
   ```

3. Run:

   ```bash
   python scripts/check_release_readiness.py
   ```

4. Build the intended native release targets.
5. Use deterministic synthetic data for import/export/report/backup/performance checks.
6. Run the native UI smoke harnesses on supported target environments.
7. Complete `docs/accessibility/NATIVE_ACCESSIBILITY_QA.md`.
8. Complete backup restore drills rather than relying only on artifact verification.
9. Validate notification, biometric/Windows Hello, file picker/share, screenshot protection, locale, privacy-mode, and packaging behavior on real supported platforms.
10. Review release signing/store metadata separately; do not commit signing material to the repository.

## What a pass does not prove

A passing structural guard does not prove:

- successful .NET/MAUI compilation,
- database migration correctness,
- correct financial calculations,
- Android/iOS/macOS/Windows package signing,
- store acceptance,
- device accessibility behavior,
- biometric/notification permissions,
- backup password/recovery success,
- export visual correctness,
- absence of every possible secret.

Those remain covered by automated application tests, platform builds, native/manual QA, and release-owner review.
