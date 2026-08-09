#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

python3 build/scripts/verify_structure.py
dotnet --info

for project in \
  tests/Finora.UnitTests/Finora.UnitTests.csproj \
  tests/Finora.IntegrationTests/Finora.IntegrationTests.csproj \
  tests/Finora.UiTests/Finora.UiTests.csproj
do
  dotnet restore "$project"
  dotnet test "$project" -c Release --no-restore
done

if [[ "${FINORA_SKIP_MAUI:-0}" == "1" ]]; then
  echo "FINORA_SKIP_MAUI=1: skipped native MAUI builds after core verification."
  exit 0
fi

case "$(uname -s)" in
  Darwin)
    dotnet workload restore src/Finora.App/Finora.App.csproj
    dotnet restore src/Finora.App/Finora.App.csproj
    dotnet build src/Finora.App/Finora.App.csproj -c Release -f net10.0-ios --no-restore
    dotnet build src/Finora.App/Finora.App.csproj -c Release -f net10.0-maccatalyst --no-restore
    ;;
  Linux)
    echo "Core verification passed. Native MAUI builds are intentionally delegated to supported CI runners from Linux."
    ;;
  *)
    echo "Core verification passed. Use build/scripts/verify.ps1 on Windows for Windows/Android native builds."
    ;;
esac
