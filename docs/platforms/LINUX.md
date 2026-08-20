# Linux

Finora's Linux path uses `src/Finora.Universal.Desktop/`, an Avalonia desktop host targeting `net10.0` and sharing the existing Finora domain/application/infrastructure projects.

## Current source behavior

At startup the host:

1. resolves the operating system's local application-data directory;
2. creates a Finora-specific subdirectory when needed;
3. opens `finora.db3` through EF Core SQLite;
4. runs the same `DatabaseInitializer` used by the existing application infrastructure;
5. initializes the native finance-store/storage boundary without loading finance rows into the universal landing surface;
6. exposes only platform/storage capability status while app-lock, privacy-mode, and full feature parity are still incomplete.

The host does not require a Finora account or cloud service. The current landing surface deliberately does not query account lists, balances, transaction rows, or other finance-derived metadata merely to demonstrate that the native storage foundation works.

## Build

```bash
dotnet restore src/Finora.Universal.Desktop/Finora.Universal.Desktop.csproj
dotnet build src/Finora.Universal.Desktop/Finora.Universal.Desktop.csproj -c Release --no-restore
```

Run during development:

```bash
dotnet run --project src/Finora.Universal.Desktop/Finora.Universal.Desktop.csproj
```

## Linux display-backend boundary

The current Finora desktop host uses Avalonia's normal `UsePlatformDetect()` path and does not opt into a separate native-Wayland backend. The stable Linux baseline for this source line is therefore the standard Avalonia X11 path; on Wayland desktops, XWayland behavior depends on the user's environment and still requires runtime validation.

Native Wayland must not be described as a validated Finora release target merely because the desktop project compiles. If a later Finora release deliberately enables Avalonia's opt-in native-Wayland path, that candidate must receive its own input, IME, scaling, windowing, clipboard, file-dialog, accessibility, packaging, and desktop-environment validation before the support matrix is expanded.

## Distribution targets

The source can be published for the desired Linux runtime identifier after native validation, for example x64 or Arm64. Packaging formats such as AppImage, Flatpak, Snap, `.deb`, or `.rpm` are distribution decisions and are not represented as signed/release-tested artifacts merely because the application builds.

## Remaining release-parity work

Linux currently has the native runtime/storage foundation, not a claim that every MAUI screen and OS integration has already been ported. Before a Linux release is called feature-complete, validate or implement:

- full account/transaction/budget/savings/recurring/report UI parity;
- encrypted backup/restore picker/save/share UX;
- receipt/document attachment picker/open behavior;
- desktop secure-storage strategy for app-lock secrets;
- app-lock and privacy-mode enforcement before any finance-derived content is exposed;
- notification integration or an explicitly documented no-notification mode;
- sensitive-screen/privacy limitations under common desktop environments;
- keyboard navigation, screen readers, scaling, high contrast and reduced motion;
- X11 behavior and, where applicable, XWayland behavior; native Wayland remains a separate opt-in validation decision;
- packaging, upgrades, uninstall/data-retention behavior and signing where applicable.

See [`CROSS_PLATFORM.md`](CROSS_PLATFORM.md) for the repository-wide support matrix.
