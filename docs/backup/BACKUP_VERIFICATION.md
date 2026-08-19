# Backup verification and recovery drill

Finora includes `scripts/verify_backup_artifact.py` for checking basic backup-artifact integrity without decrypting the backup or logging its contents.

This helper supports the backup/recovery workflow; it does **not** replace an actual restore test.

## What the verifier checks

The script can confirm that a backup artifact:

- exists and is a regular file,
- meets a configurable minimum size,
- is not entirely zero bytes,
- does not begin with the plaintext SQLite database signature,
- can be read completely enough to calculate SHA-256,
- optionally matches a previously recorded SHA-256 digest.

It never asks for a backup password and never attempts to decrypt or parse financial records.

## Privacy behavior

Text and JSON reports contain only:

- pass/fail,
- artifact size,
- SHA-256 digest,
- generic diagnostic codes/messages.

They intentionally omit:

- the backup path,
- file name,
- database contents,
- account/transaction data,
- backup password,
- encryption keys.

A SHA-256 digest can still uniquely identify a particular artifact. Treat it as operational metadata and share it only where appropriate.

## Verify a new backup

After explicitly creating an encrypted Finora backup:

```bash
python scripts/verify_backup_artifact.py path/to/backup.finora
```

A successful artifact check prints the size and SHA-256 digest.

Record the digest in your private release/recovery notes when you want to verify that a later copy is byte-for-byte identical.

## Verify a copied backup

```bash
python scripts/verify_backup_artifact.py copied-backup.finora \
  --expected-sha256 <RECORDED_64_CHARACTER_SHA256>
```

A digest mismatch returns a non-zero exit code and `sha256_mismatch`.

The verifier does not “repair” a mismatched file. Keep the original backup until a complete restore drill has succeeded.

## JSON output

```bash
python scripts/verify_backup_artifact.py path/to/backup.finora --json
```

Example shape:

```json
{
  "passed": true,
  "sizeBytes": 2048,
  "sha256": "<64-character digest>",
  "diagnostics": []
}
```

No path or content is included.

## Minimum-size check

The default minimum is 128 bytes. Override it for a stricter release policy:

```bash
python scripts/verify_backup_artifact.py path/to/backup.finora --min-size 4096
```

The minimum is a corruption/sanity guard, not proof that every expected finance record is present.

## Plaintext SQLite protection

A Finora backup intended to be encrypted should not simply be a raw SQLite database. The verifier rejects artifacts beginning with:

```text
SQLite format 3\0
```

with diagnostic code:

```text
plaintext_sqlite_header
```

This is a narrow defense against accidentally treating an obvious raw SQLite database as an encrypted backup. It is not a general cryptographic audit.

## All-zero corruption

A file consisting only of zero bytes is rejected as:

```text
all_zero_content
```

This catches one simple corruption/failure mode while keeping the tool format-agnostic.

## Recommended backup drill

Use synthetic/disposable finance data for repeatable release validation.

1. Populate a test profile with representative accounts, transactions, budgets, goals, recurring items, receipts/attachments, categories/tags, and reconciliation history.
2. Record a small verification manifest outside the app: expected counts, selected synthetic balances, locale/theme/security settings, and a known synthetic attachment name.
3. Create an encrypted Finora backup through the app.
4. Run `verify_backup_artifact.py` and record its SHA-256.
5. Copy the backup to a second test location.
6. Verify the copy against the recorded SHA-256.
7. Restore into a clean disposable Finora environment using the correct backup password.
8. Verify the expected finance counts/balances/history/attachments from step 2.
9. Verify locale/theme/security preferences according to the documented restore semantics.
10. Run integrity checks, reports, privacy mode, transaction search, recurring workflows, and reconciliation after restore.
11. Re-launch Finora and verify restored data remains consistent.
12. Only after a successful restore drill consider the backup workflow release-ready.

## Negative drill

Also validate expected failure behavior:

- wrong backup password,
- truncated backup copy,
- all-zero artifact,
- plaintext SQLite file presented as a backup,
- SHA-256 mismatch after copy corruption,
- canceled restore,
- unavailable destination/storage,
- app restart after a failed restore.

A failed restore must not silently replace the active finance database with partial/corrupt data.

## What a passing artifact verification does not prove

It does not prove:

- that encryption is cryptographically strong,
- that the password is correct,
- that every database row is present,
- that attachments are complete,
- that application/database schema versions are compatible,
- that atomic restore replacement succeeds,
- that restored data passes domain invariants,
- that the backup can actually be opened on another device/platform.

Those require application-level backup/restore tests and native/manual recovery drills.

## CI

`.github/workflows/backup-artifact.yml` runs the verifier tests and checks two synthetic artifacts:

- a non-plaintext synthetic backup-like payload that should pass,
- a raw SQLite-signature payload that must fail.

No real backup or password is committed to the repository or CI logs.

## Diagnostic codes

Current codes:

```text
all_zero_content
missing_file
not_a_file
plaintext_sqlite_header
read_error
sha256_mismatch
too_small
```

Use the diagnostic code rather than English message text when automating checks.
