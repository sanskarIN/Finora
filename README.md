# Finora

Finora is an open-source, local-first personal finance application built with .NET MAUI, C#, XAML, SQLite/EF Core, and MVVM-style presentation logic. The current release requires no login and keeps finance data on the device.

> Made by the Sanskar

## Scope

Accounts, transactions and transfers, budgets, savings goals, recurring items, dashboard/report summaries, CSV/PDF export, encrypted backup/restore, privacy-aware diagnostics, onboarding, settings, and tests.

## Build

```bash
dotnet workload restore
dotnet restore Finora.sln
dotnet build Finora.sln -c Debug
dotnet test Finora.sln -c Debug --no-build
```

See `docs/setup/BUILD.md`.

## Links

- Repository: https://github.com/sanskarIN/Finora
- Creator: https://www.github.com/sanskarIN
- Business: sanskarin@outlook.in
- Support: supportramsandesh@gmail.com

Apache-2.0 licensed.
