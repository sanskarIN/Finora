[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Executable,

    [string[]]$ExpectName = @(),

    [string[]]$ExpectAutomationId = @(),

    [ValidateRange(1, 60)]
    [int]$StartupSeconds = 8,

    [ValidateRange(10, 10000)]
    [int]$MaxElements = 2500,

    [switch]$KeepRunning
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-ContainsOrdinalIgnoreCase {
    param(
        [AllowEmptyString()]
        [string]$Value,
        [Parameter(Mandatory = $true)]
        [string]$Expected
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    return $Value.IndexOf($Expected, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
}

if (-not (Test-Path -LiteralPath $Executable -PathType Leaf)) {
    throw "Finora executable was not found: $Executable"
}

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$resolvedExecutable = (Resolve-Path -LiteralPath $Executable).Path
$process = Start-Process -FilePath $resolvedExecutable -PassThru

try {
    $deadline = [DateTime]::UtcNow.AddSeconds($StartupSeconds)
    $windowHandle = [IntPtr]::Zero

    do {
        if ($process.HasExited) {
            throw "Finora exited before a native window became available. Exit code: $($process.ExitCode)"
        }

        $process.Refresh()
        $windowHandle = $process.MainWindowHandle
        if ($windowHandle -ne [IntPtr]::Zero) {
            break
        }

        Start-Sleep -Milliseconds 200
    } while ([DateTime]::UtcNow -lt $deadline)

    if ($windowHandle -eq [IntPtr]::Zero) {
        throw "No Finora main window was detected within $StartupSeconds second(s)."
    }

    $root = [System.Windows.Automation.AutomationElement]::FromHandle($windowHandle)
    if ($null -eq $root) {
        throw "Windows UI Automation could not resolve the Finora main window."
    }

    $elements = New-Object System.Collections.Generic.List[object]

    function Add-ElementSummary {
        param([System.Windows.Automation.AutomationElement]$Element)

        if ($elements.Count -ge $MaxElements) {
            return
        }

        try {
            $current = $Element.Current
            $elements.Add([pscustomobject]@{
                Name = [string]$current.Name
                AutomationId = [string]$current.AutomationId
                ControlType = [string]$current.ControlType.ProgrammaticName
                IsEnabled = [bool]$current.IsEnabled
            }) | Out-Null
        }
        catch [System.Windows.Automation.ElementNotAvailableException] {
            # A transient element disappeared between enumeration and inspection.
        }
    }

    Add-ElementSummary -Element $root
    $descendants = $root.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.Condition]::TrueCondition
    )

    for ($index = 0; $index -lt $descendants.Count -and $elements.Count -lt $MaxElements; $index++) {
        Add-ElementSummary -Element $descendants.Item($index)
    }

    $errors = New-Object System.Collections.Generic.List[string]

    foreach ($expected in $ExpectName) {
        $matched = $false
        foreach ($element in $elements) {
            if (Test-ContainsOrdinalIgnoreCase -Value $element.Name -Expected $expected) {
                $matched = $true
                break
            }
        }
        if (-not $matched) {
            $errors.Add("Expected accessible name not found: '$expected'") | Out-Null
        }
    }

    foreach ($expected in $ExpectAutomationId) {
        $matched = $false
        foreach ($element in $elements) {
            if (Test-ContainsOrdinalIgnoreCase -Value $element.AutomationId -Expected $expected) {
                $matched = $true
                break
            }
        }
        if (-not $matched) {
            $errors.Add("Expected automation ID not found: '$expected'") | Out-Null
        }
    }

    $result = [pscustomobject]@{
        passed = ($errors.Count -eq 0)
        processId = $process.Id
        elementCount = $elements.Count
        truncated = ($descendants.Count + 1 -gt $MaxElements)
        errorCount = $errors.Count
        errors = @($errors)
    }

    $result | ConvertTo-Json -Depth 4

    if ($errors.Count -gt 0) {
        exit 1
    }
}
finally {
    if (-not $KeepRunning -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
}
