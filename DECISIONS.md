# Decisions

1. Money persists as signed 64-bit integer minor units; no floating-point money storage.
2. SQLite + EF Core provides relational integrity, transactions, async access, and testability.
3. Current release is local-first: no account service, analytics, telemetry endpoint, or cloud sync.
4. Transfers are paired rows linked by one `TransferGroupId`.
5. Backups use AES-GCM with PBKDF2-SHA256-derived keys; no custom cryptography.
6. Third-party chart/notification/biometric packages are not guessed without toolchain compatibility verification.
7. Basic PDF export is dependency-free behind `IExportService`.
