# Repository QA runner

Finora includes `scripts/run_repo_qa.py` as a small cross-platform entry point for repository checks that do not require a native MAUI target.

## Default run

From the repository root:

```bash
python scripts/run_repo_qa.py
```

The default run executes:

1. all Python developer-tool unit tests under `scripts/tests/`, and
2. the localization validator for resource parity, global key uniqueness, placeholder parity, and XAML/C# localization references.

The runner exits with a non-zero code when any step fails.

## Include the .NET test suite

When the required .NET SDK/workloads are installed:

```bash
python scripts/run_repo_qa.py --include-dotnet
```

The default .NET configuration is `Release`.

For Debug:

```bash
python scripts/run_repo_qa.py --include-dotnet --dotnet-configuration Debug
```

The runner intentionally does not install SDKs/workloads or mutate the development environment.

## Fail fast

```bash
python scripts/run_repo_qa.py --fail-fast
```

This is useful while iterating on a known failing check. Normal release validation should usually run the complete set so multiple independent failures are visible together.

## Preview planned checks

```bash
python scripts/run_repo_qa.py --list
```

This prints the commands without running them.

## Relationship to native validation

A successful repository QA run does **not** prove Android/Windows/iOS/macOS behavior. Continue with the applicable checks under:

- `docs/testing/NATIVE_UI_AUTOMATION.md`
- `docs/accessibility/NATIVE_ACCESSIBILITY_QA.md`
- `docs/testing/SAMPLE_DATA.md`
- `docs/import/CSV_DIAGNOSTICS.md`
- `docs/export/EXPORT_VERIFICATION.md`
- `docs/backup/BACKUP_VERIFICATION.md`

Native biometrics, notifications, file pickers/share sheets, packaging/signing, platform screenshot protection, and accessibility tooling still require the supported platform/device environment.

## Recommended contributor sequence

For a normal code change:

```bash
python scripts/run_repo_qa.py

dotnet test
```

or, when the .NET environment is complete:

```bash
python scripts/run_repo_qa.py --include-dotnet
```

Then run the feature-specific native/manual checks affected by the change.

## Privacy

The repository QA runner itself does not inspect a Finora database, backup, export, or user finance file. Feature-specific artifact tools should be run on synthetic/disposable data for CI and shareable diagnostics.
