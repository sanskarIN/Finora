# Architecture

Dependency direction: `Finora.App -> Finora.Infrastructure/Finora.Application -> Finora.Domain -> Finora.Shared`.

Domain contains finance rules only. Application defines DTOs/contracts. Infrastructure owns SQLite, backup/export, and diagnostics. App owns MAUI presentation and platform lifecycle. Disk/database work is asynchronous. Sensitive data stays local unless the user explicitly exports/shares it.
