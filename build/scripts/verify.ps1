$ErrorActionPreference = 'Stop'
dotnet restore Finora.sln
dotnet format Finora.sln --verify-no-changes --no-restore
dotnet build Finora.sln -c Release --no-restore
dotnet test Finora.sln -c Release --no-build
