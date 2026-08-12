# Finora Support

## Before asking for help

1. Read `docs/setup/TROUBLESHOOTING.md`.
2. Check `PROJECT_STATUS.md` for current source/platform validation status.
3. Check `docs/NEXT_STEPS.md` for the prioritized release-validation and development roadmap.
4. For build/test issues, run the dependency-free preflight first:

```bash
python build/scripts/verify_structure.py
```

If the issue is a suspected security vulnerability, private-data exposure, app-lock bypass, unsafe backup/restore behavior, arbitrary file access, or cryptographic weakness, **do not open a public issue**. Follow `SECURITY.md` instead.

## Support contacts

- User support: `supportramsandesh@gmail.com`
- Business/project/security contact: `sanskarin@outlook.in`
- Repository: https://github.com/sanskarIN/Finora
- Creator/open-source profile: https://www.github.com/sanskarIN
- Optional project support: https://buymeacoffee.com/sanskarIN

Buy Me a Coffee is an optional external contribution link. A contribution does not unlock Finora features, create premium entitlement, guarantee or accelerate support, or change security/reporting priority. Product support and security reporting remain available through the contacts and repository guidance above without a contribution.

## Protect your privacy when requesting support

Never send:

- a real Finora SQLite database;
- a real encrypted backup together with its password;
- real bank/account statements;
- real transaction CSV/PDF exports;
- real receipt images/documents;
- PINs or biometric information;
- backup passwords;
- signing keys, certificates, keystores, provisioning secrets, API keys, or authentication tokens.

Use synthetic/example accounts and transactions to reproduce a problem. Replace names, merchants/payees, notes, locations, amounts, and receipt contents with invented values.

## Useful safe information to include

For ordinary product/build problems, include:

- Finora version/build, for example `0.2.0 (2)`;
- platform and OS version;
- whether the app is a debug, release, or packaged/store-style build;
- steps to reproduce using synthetic data;
- expected versus actual behavior;
- exact compiler/build error text when it contains no private data/secrets;
- the sanitized Finora diagnostic log if relevant;
- the sanitized developer data-integrity report if relevant.

The sanitized diagnostic/integrity exports are designed not to contain account names, merchant/payee names, transaction notes, transaction amounts, receipt names/contents, PINs, or backup passwords. Review any file yourself before sharing it.

## Backup/restore support

If a backup does not preview/restore:

- keep the original file unchanged;
- verify you are using the intended password locally;
- do not publish/upload the backup for debugging;
- note the Finora version/schema that created it if known;
- report only the generic validation/error behavior using synthetic backups when possible.

Finora cannot recover a forgotten backup password and support should never ask for it.

## Database/integrity problems

The hidden developer options include a local privacy-safe integrity check. It can detect database integrity/foreign-key issues, transfer-pair inconsistencies, split mismatches, category cycles, recurrence reference problems, and missing/changed receipt files.

If the integrity checker reports a problem:

- preserve any known-good external backup;
- do not manually edit `schema.version`;
- do not repeatedly overwrite your only backup;
- export only the sanitized integrity report if needed;
- use `SECURITY.md` if the problem suggests data exposure or a security vulnerability.

## Feature requests

Use the GitHub feature-request template. Explain the user outcome and affected area without including private finance data. Current product boundaries remain local-first: cloud synchronization, remote accounts, collaboration, and server-backed commercial entitlement are later-version concerns unless explicitly approved and designed.

The prioritized current roadmap is maintained in `docs/NEXT_STEPS.md`. Feature requests can still propose other work, but release-blocking financial correctness, migration, backup/recovery, privacy, native validation, and accessibility issues should be prioritized over cosmetic expansion.

## Response expectations

This open-source repository does not guarantee a response time or commercial service level. Support guidance is provided on a best-effort basis. Buy Me a Coffee contributions do not create a service-level agreement or guaranteed response time. Always keep independent backups of important local financial records before uninstalling, resetting app data, testing migrations, or using development builds.
