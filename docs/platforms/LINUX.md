# Linux

Finora's Linux path uses `src/Finora.Universal.Desktop/`, an Avalonia desktop host targeting `net10.0` and sharing the existing Finora domain/application/infrastructure projects.

## Current source behavior

At startup the host:

1. resolves the operating system's local application-data directory;
2. creates a Finora-specific subdirectory when needed;
3. opens `finora.db3` through EF Core SQLite;
4. runs the same `DatabaseInitializer` used by the existing application infrastructure;
5. exposes the finance store through the universal runtime boundary;
6. loads a privacy-safe account count into the universal landing surface.

The host does not require a Finora account or cloud service.

## Build

```bash
dotnet restore src/Finora.Universal.Desktop/Finora.Universal.Desktop.csproj
dotnet build src/Finora.Universal.Desktop/Finora.Universal.Desktop.csproj -c Release --no-restore
```

Run during development:

```bash
dotnet run --project src/Finora.Universal.Desktop/Finora.Universal.Desktop.csproj
```

## Distribution targets

The source can be published for the desired Linux runtime identifier after native validation, for example x64 or Arm64. Packaging formats such as AppImage, Flatpak, Snap, `.deb`, or `.rpm` are distribution decisions and are not represented as signed/release-tested artifacts merely because the application builds.

## Remaining release-parity work

Linux currently has the native runtime/storage foundation, not a claim that every MAUI screen and OS integration has already been ported. Before a Linux release is called feature-complete, validate or implement:

- full account/transaction/budget/savings/recurring/report UI parity;
- encrypted backup/restore picker/save/share UX;
- receipt/document attachment picker/open behavior;
- desktop secure-storage strategy for app-lock secrets;
- notification integration or an explicitly documented no-notification mode;
- sensitive-screen/privacy limitations under common desktop environments;
- keyboard navigation, screen readers, scaling, high contrast and reduced motion;
- X11 and supported Wayland behavior;
- packaging, upgrades, uninstall/data-retention behavior and signing where applicable.

See [`CROSS_PLATFORM.md`](CROSS_PLATFORM.md) for the repository-wide support matrix.
