# Cross-Platform File Reference

This companion inventory extends [`REPOSITORY_FILE_REFERENCE.md`](REPOSITORY_FILE_REFERENCE.md) for the universal cross-platform surface. It is intentionally narrow: every directory prefix identifies one concrete project, and root-level/tooling additions are listed exactly.

| File or area | Purpose |
|---|---|
| `Finora.CrossPlatform.slnx` | .NET solution definition that groups the existing MAUI/core projects with the universal desktop and WebAssembly hosts. |
| `scripts/check_cross_platform.py` | Dependency-free cross-platform source-contract checker for package pins, target frameworks, native desktop host coverage, WebAssembly/PWA wiring, browser persistence safety boundary, CI matrix, and platform documentation. |
| `src/Finora.Universal/` | Platform-neutral Avalonia presentation shell, runtime capability contract, shared desktop/single-view application lifetime wiring, and universal views/view-models. |
| `src/Finora.Universal.Desktop/` | Native Avalonia desktop host for Linux, Windows, and macOS; initializes Finora's existing local SQLite/EF Core finance store in an OS-local application-data directory. |
| `src/Finora.Universal.Browser/` | Avalonia WebAssembly/PWA host for modern browsers and ChromeOS-capable browser delivery. Native SQLite persistence is intentionally excluded until the browser-specific encrypted persistence adapter meets parity and privacy requirements. |

## Boundary rule

The existing `.NET MAUI` application remains the release path for Android, iOS/iPadOS, Mac Catalyst, and Windows while the universal hosts expand reach. Shared finance rules remain in `Finora.Domain`/`Finora.Application`; native or browser hosts must not duplicate or weaken those rules.

## Automated contract

Run:

```bash
python scripts/check_cross_platform.py
```

The check is deliberately dependency-free and executes before expensive universal builds in the cross-platform GitHub Actions workflow. It proves repository wiring, not native/runtime release readiness.

## Browser honesty rule

A successful WebAssembly build is not equivalent to full finance feature parity. The browser host must continue to report persistent finance storage as unavailable until a browser-local adapter has passed migration, backup/restore, integrity, privacy, and offline-recovery validation. This prevents a UI-only Web build from being documented as a completed secure finance runtime.
