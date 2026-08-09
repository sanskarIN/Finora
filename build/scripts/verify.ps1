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

    $testProjects = @(
        'tests/Finora.UnitTests/Finora.UnitTests.csproj',
        'tests/Finora.IntegrationTests/Finora.IntegrationTests.csproj',
        'tests/Finora.UiTests/Finora.UiTests.csproj'
    )

    foreach ($project in $testProjects) {
        dotnet restore $project
        dotnet test $project -c Release --no-restore
    }

    if ($env:FINORA_SKIP_MAUI -eq '1') {
        Write-Host 'FINORA_SKIP_MAUI=1: skipped native MAUI builds after core verification.'
        return
    }

    dotnet workload restore src/Finora.App/Finora.App.csproj
    dotnet restore src/Finora.App/Finora.App.csproj

    if ($IsWindows -or $env:OS -eq 'Windows_NT') {
        dotnet build src/Finora.App/Finora.App.csproj -c Release -f net10.0-windows10.0.19041.0 --no-restore
        dotnet build src/Finora.App/Finora.App.csproj -c Release -f net10.0-android --no-restore
    }
    elseif ($IsMacOS) {
        dotnet build src/Finora.App/Finora.App.csproj -c Release -f net10.0-ios --no-restore
        dotnet build src/Finora.App/Finora.App.csproj -c Release -f net10.0-maccatalyst --no-restore
    }
    else {
        Write-Host 'Core verification passed. Native MAUI build skipped on this host; CI builds Windows/Android and Apple targets on supported runners.'
    }
}
finally {
    Pop-Location
}
