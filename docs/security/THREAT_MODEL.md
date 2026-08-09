# Threat Model

Assets: finance records, local DB, attachments, backup files, PIN verifier, and encryption material.

Controls:
- Device loss: optional PIN + OS secure storage.
- Backup theft/tamper: AES-GCM and password-derived key.
- Log leakage: redaction and no private transaction payload logging.
- Corruption: SQLite transactions, WAL, foreign keys, transactional restore.
- Recurrence duplicates: unique occurrence index + idempotent processing.
- Supply chain: minimal dependency surface.

Residual risks: rooted/compromised devices can bypass app controls. Local premium state is not secure licensing. Biometrics, screenshot blocking, and platform notifications require platform release verification.
