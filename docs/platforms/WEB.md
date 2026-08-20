# Web / WebAssembly / PWA

Finora includes `src/Finora.Universal.Browser/`, an Avalonia WebAssembly host targeting `net10.0-browser`. It shares the platform-neutral universal presentation project while keeping browser storage behind an explicit runtime boundary.

## Build

Install/restore the .NET WebAssembly workload and build the host:

```bash
dotnet workload install wasm-tools
dotnet restore src/Finora.Universal.Browser/Finora.Universal.Browser.csproj
dotnet build src/Finora.Universal.Browser/Finora.Universal.Browser.csproj -c Release --no-restore
```

The browser project includes:

- the WebAssembly entry point;
- a minimal privacy-safe startup shell;
- an installable web-app manifest;
- the shared Avalonia `App`/single-view UI;
- a browser runtime capability implementation.

## Publish a static WebAssembly candidate

Create the optimized publish output with:

```bash
dotnet publish src/Finora.Universal.Browser/Finora.Universal.Browser.csproj -c Release
```

For the current `net10.0-browser` target, the static site is emitted below the project's Release publish output, with the deployable browser files under `wwwroot`. Serve that directory through an HTTP(S) static-file server for validation; do not open `index.html` directly from a `file://` URL and treat that as browser-runtime evidence.

A publish artifact is only a candidate. Before hosting it publicly, verify at minimum:

- application startup through the generated WebAssembly runtime;
- direct and refreshed navigation to the deployed base path;
- manifest/icon resolution and install behavior where the browser exposes PWA installation;
- keyboard and screen-reader behavior;
- text scaling, zoom, high-contrast/forced-color behavior where available;
- browser-console output for accidental private-data disclosure;
- no finance records or secrets are persisted by the current disabled-persistence host;
- cache/CDN headers do not create a false claim of durable offline finance storage.

## Persistence status

**Finance persistence is intentionally disabled in the browser host in this phase.**

The existing native infrastructure uses SQLite, local files, and native filesystem semantics. A browser sandbox has materially different durability, quota, attachment, backup, and eviction behavior. Finora therefore does not claim that a compiled WebAssembly UI is equivalent to the existing native finance application.

The browser runtime returns a capability state explaining that a dedicated encrypted IndexedDB/OPFS-style adapter must pass finance, migration, backup/restore, integrity, privacy, and offline validation before persistent finance workflows are enabled.

## PWA / ChromeOS

`manifest.webmanifest` enables an installable-app presentation where the browser/platform supports installation. ChromeOS users also have the Android delivery path through Finora's existing Android target. Browser/PWA installation does not change the persistence boundary described above.

The current manifest is installation metadata; it is not a statement that finance persistence, background synchronization, or a service-worker-backed offline finance runtime is complete.

## Security requirements before finance data is enabled

A browser storage implementation must, at minimum:

- preserve signed 64-bit minor-unit semantics without floating-point conversion;
- preserve currency precision and same-currency transfer invariants;
- support atomic/transactional behavior appropriate to the browser store;
- survive schema upgrades and interrupted migrations;
- make storage quota/eviction risks visible rather than implying native-file durability;
- keep backups encrypted and explicitly user-initiated;
- validate restored graphs before committing them;
- avoid storing secrets or private finance payloads in logs, URLs, query strings, caches, service-worker logs, or analytics;
- support complete local reset;
- pass browser-specific integration and recovery tests.

See [`CROSS_PLATFORM.md`](CROSS_PLATFORM.md) for the complete matrix and release-readiness rules.
