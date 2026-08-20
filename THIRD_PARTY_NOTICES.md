# Third-Party Notices

Finora uses Microsoft .NET, .NET MAUI, Entity Framework Core/SQLite, Avalonia UI, Microsoft.NET.Test.Sdk, and xUnit. Their copyrights and licenses remain with their respective owners.

Avalonia UI's open-source framework is licensed under the MIT License. Finora uses the open-source Avalonia framework packages for the universal desktop and WebAssembly presentation hosts; this notice does not imply inclusion or licensing of separate commercial Avalonia products or services.

Because the exact dependency graph is produced by NuGet restore, release engineering **must** inspect the restored direct and transitive package license metadata before publishing binaries. This repository does not fabricate or freeze license text for a package version that has not been restored and verified in the active release toolchain.

Primary upstream sources:

- [.NET and .NET MAUI](https://github.com/dotnet)
- [Entity Framework Core](https://github.com/dotnet/efcore)
- [SQLite](https://www.sqlite.org/)
- [Avalonia UI](https://github.com/AvaloniaUI/Avalonia)
- [xUnit.net](https://github.com/xunit/xunit)

The package versions requested by the source tree are defined in `Directory.Packages.props`.
