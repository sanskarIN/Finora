$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Push-Location $repoRoot
try {
    if (Get-Command python -ErrorAction SilentlyContinue) {
        python build/scripts/verify_structure.py
    }
    elseif (Get-Command python3 -ErrorAction SilentlyContinue) {
        python3 build/scripts/verify_structure.py
    }
    else {
        throw 'Python 3 is required for the dependency-free structural preflight.'
    }

    dotnet --info
    dotnet workload restore
    dotnet restore Finora.sln
    dotnet format Finora.sln --verify-no-changes --no-restore
    dotnet build Finora.sln -c Release --no-restore
    dotnet test Finora.sln -c Release --no-build --logger "trx;LogFileName=finora-tests.trx"
}
finally {
    Pop-Location
}
