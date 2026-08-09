# Troubleshooting

- MAUI workload missing: `dotnet workload restore`.
- SQLite lock: close other debug instances; Finora enables WAL and a busy timeout.
- Backup restore rejected: check the password and schema compatibility.
- Apple builds on Windows: use a Mac/Xcode host.
- Windows packaging identity: replace development identity during release packaging.
