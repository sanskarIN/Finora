# Finora Security Policy

Finora stores personal financial data locally, so security reports require extra care.

## Report vulnerabilities privately

**Do not open a public GitHub issue** for a vulnerability, suspected private-data exposure, backup/app-lock weakness, cryptographic defect, path traversal, database corruption vector, or signing/release secret.

Report privately to:

**sanskarin@outlook.in**

Include only the minimum information needed:

- affected Finora version/build;
- platform and OS version;
- concise vulnerability category/impact;
- reproduction steps using **synthetic data**;
- whether the issue can cause data disclosure, incorrect financial state, app-lock bypass, unsafe restore, arbitrary file access, or code execution;
- a safe proof of concept if required.

## Never send these materials

Do not email or attach:

- real financial databases;
- real bank/account statements;
- real receipts or attachment contents;
- real transaction exports;
- user PINs;
- backup passwords;
- encryption/signing keys;
- certificates/private keys/keystores;
- authentication tokens or unrelated personal information.

Use a synthetic/test Finora profile to reproduce the issue. If a vulnerability can only be demonstrated with a particular structure, describe that structure and replace identifying/private values with synthetic equivalents.

## Public issues

Ordinary non-security defects can use the GitHub bug template. The public template intentionally requires synthetic/example data and redirects security problems here.

## Current security architecture

Current source includes:

- signed integer minor-unit money representation rather than binary floating-point money storage;
- SQLite relational storage with foreign keys, WAL, transactions, and busy timeout;
- versioned transactional database migration;
- linked transfer-pair invariants;
- recurrence idempotency through persisted unique occurrences;
- app-private receipt storage with path confinement, size metadata, and SHA-256 checksums;
- password-protected encrypted backups using PBKDF2-SHA256-derived keys and AES-GCM authenticated encryption;
- staged/validated attachment restore and transactional database replacement;
- optional app PIN with random salt, password-based verifier derivation, rate-limited lockout, and inactivity lock;
- platform secure storage for small security values;
- optional biometric/Windows Hello unlock with PIN fallback;
- platform sensitive-screen protection where supported;
- generic privacy-safe local notification content;
- privacy-safe diagnostic logging and data-integrity reports;
- repository CodeQL/dependency-review/Dependabot workflows.

This list describes implemented controls, not a guarantee that Finora is vulnerability-free.

## Backup security notes

Encrypted backups can still be attacked offline if a user chooses a weak/reused password. Finora cannot recover a forgotten backup password. Never ask users to send an encrypted backup plus password for support.

A backup that fails authentication/schema/path/attachment validation must be rejected; do not weaken validation as a workaround for a damaged backup.

## Device trust limitation

App-level controls cannot fully protect a database on a rooted/jailbroken/compromised device or against an attacker with sufficient OS/filesystem privileges. Screenshot/capture blocking is also platform-dependent and cannot stop an external camera.

## Local premium limitation

The local premium/demo state is **not** secure commercial entitlement enforcement. Tampering with a local flag is outside any paid-license guarantee because reliable commercial entitlement requires future store/server validation.

## Supported versions

Until stable public releases/tags are established, security fixes target the current `main` development line. When release branches/tags are introduced, this section must be updated with an explicit supported-version table.

## Release-security requirements

Before publishing binaries:

- run structural/build/test/CodeQL/dependency review gates;
- review exact restored dependency vulnerabilities/licenses;
- run migration and encrypted backup/restore tests;
- run the local data-integrity diagnostic on synthetic release-candidate data;
- test PIN/biometric/capture/notification behavior on applicable platforms;
- keep all signing credentials outside the repository;
- verify logs/notifications/reports contain no private finance content;
- verify store privacy declarations match actual permissions/behavior.

See `docs/security/THREAT_MODEL.md`, `docs/TEST_PLAN.md`, and `docs/releases/STORE_READINESS.md`.
