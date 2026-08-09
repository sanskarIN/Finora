# Build and Run

Install a supported .NET 10 SDK and MAUI workload, plus Android SDK, Xcode for Apple targets, and Windows App SDK tooling for Windows.

```bash
dotnet workload restore
dotnet restore Finora.sln
dotnet format Finora.sln --verify-no-changes
dotnet build Finora.sln -c Debug
dotnet test Finora.sln -c Debug --no-build
```

Android example:

```bash
dotnet build src/Finora.App/Finora.App.csproj -f net10.0-android -c Debug
```

Keep signing secrets outside source control.
