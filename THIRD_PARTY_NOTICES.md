# Third-Party Notices

Finora uses Microsoft .NET, .NET MAUI, Entity Framework Core/SQLite, Microsoft.NET.Test.Sdk, and xUnit. Their copyrights and licenses remain with their respective owners.

Because the exact dependency graph is produced by NuGet restore, release engineering **must** inspect the restored direct and transitive package license metadata before publishing binaries. This repository does not fabricate or freeze license text for a package version that has not been restored and verified in the active release toolchain.

Primary upstream sources:

- [.NET and .NET MAUI](https://github.com/dotnet)
- [Entity Framework Core](https://github.com/dotnet/efcore)
- [SQLite](https://www.sqlite.org/)
- [xUnit.net](https://github.com/xunit/xunit)

The package versions requested by the source tree are defined in `Directory.Packages.props`.
