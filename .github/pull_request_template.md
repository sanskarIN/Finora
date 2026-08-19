## What changed

Describe the behavior, affected platform(s), and files/features changed.

## Why

Explain the user or engineering problem this change solves.

## Privacy and data impact

- Does this change read, write, export, back up, log, notify about, or display financial data?
- Does it add a permission, dependency, platform API, file format, migration, or external service?
- If yes, explain the privacy/security treatment. Do not paste real user data.

## Database / backup compatibility

- [ ] No schema or backup-format change.
- [ ] OR schema/backup change includes migration, backward/forward compatibility decision, tests, and documentation.

## Validation

- [ ] `python build/scripts/verify_structure.py`
- [ ] `python scripts/run_repo_qa.py`
- [ ] Tracked-file ownership remains covered by `docs/development/REPOSITORY_FILE_REFERENCE.md`
- [ ] `dotnet workload restore`
- [ ] `dotnet restore Finora.sln`
- [ ] `dotnet format Finora.sln --verify-no-changes --no-restore`
- [ ] Release build passes for applicable target(s)
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] UI-contract tests pass
- [ ] Applicable device/emulator/simulator smoke tests pass
- [ ] Accessibility behavior was checked for changed UI
- [ ] No secrets, credentials, signing material, real financial records, or private receipt contents were added
- [ ] Logs/notifications remain privacy-safe
- [ ] Documentation and changelog are updated when required

## Documentation ownership

For every new, moved, or deleted tracked file, confirm the repository file reference still describes the narrow area correctly. `scripts/check_documentation_coverage.py` is part of `scripts/run_repo_qa.py` and CI structural preflight; do not bypass the check with a broad catch-all directory entry.

## Screenshots / recordings

Use synthetic data only. Do not attach real financial records or private receipts.
