<#
.SYNOPSIS
    Builds and runs every test project in the solution.

.DESCRIPTION
    `dotnet test` is not used here. On .NET SDK 10.0.401 it drives xunit.v3 4.0.0's
    Microsoft.Testing.Platform host over a protocol the two do not agree on: every
    assembly reports "Zero tests ran" (exit code 5) even though running the same
    assembly directly discovers and passes all of its tests.

    xunit.v3 builds each test project into a self-hosting executable, so invoking
    those directly is the supported path and is exactly what `dotnet test` would do
    if the handshake worked. Revisit once a later SDK or xunit release lands - the
    fix will be to delete this script, not to change the tests.

.PARAMETER Configuration
    Build configuration. Defaults to Debug.

.PARAMETER Filter
    Optional xunit filter, e.g. -Filter '*Cursor*' to run one class.

.EXAMPLE
    ./test.ps1
    ./test.ps1 -Configuration Release
    ./test.ps1 -Filter '*Paging*'
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Debug',
    [string]$Filter
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# Keeps the platform's telemetry notice out of every run's output.
$env:TESTINGPLATFORM_TELEMETRY_OPTOUT = '1'

Write-Host "Building test projects ($Configuration)..." -ForegroundColor Cyan

$projects = Get-ChildItem -Path (Join-Path $root 'tests') -Filter '*.csproj' -Recurse

foreach ($project in $projects) {
    dotnet build $project.FullName --configuration $Configuration --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed: $($project.BaseName)" -ForegroundColor Red
        exit 1
    }
}

$failed = @()
$totals = @{ Total = 0; Passed = 0; Failed = 0; Skipped = 0 }

foreach ($project in $projects) {
    $name = $project.BaseName
    $assembly = Join-Path $project.DirectoryName "bin/$Configuration/net10.0/$name.dll"

    if (-not (Test-Path $assembly)) {
        Write-Host "No assembly for $name at $assembly" -ForegroundColor Red
        $failed += $name
        continue
    }

    Write-Host ''
    Write-Host "=== $name ===" -ForegroundColor Cyan

    $arguments = @($assembly)
    if ($Filter) { $arguments += @('--filter-class', $Filter) }

    $output = & dotnet @arguments 2>&1
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }

    foreach ($line in $output) {
        if ($line -match '^\s*(total|succeeded|failed|skipped):\s*(\d+)\s*$') {
            switch ($Matches[1]) {
                'total'     { $totals.Total   += [int]$Matches[2] }
                'succeeded' { $totals.Passed  += [int]$Matches[2] }
                'failed'    { $totals.Failed  += [int]$Matches[2] }
                'skipped'   { $totals.Skipped += [int]$Matches[2] }
            }
        }
    }

    # 0 = all passed, 8 = the run was filtered down to nothing. Exit code 5 means the
    # assembly holds no tests at all, which is legitimate for a project whose suites
    # have not been written yet.
    if ($exitCode -ne 0 -and $exitCode -ne 5 -and $exitCode -ne 8) {
        $failed += $name
    }
}

Write-Host ''
Write-Host '================ SUMMARY ================' -ForegroundColor Cyan
Write-Host ("  total     {0}" -f $totals.Total)
Write-Host ("  succeeded {0}" -f $totals.Passed) -ForegroundColor Green
Write-Host ("  failed    {0}" -f $totals.Failed) -ForegroundColor $(if ($totals.Failed -gt 0) { 'Red' } else { 'Gray' })
Write-Host ("  skipped   {0}" -f $totals.Skipped)

if ($failed.Count -gt 0) {
    Write-Host ''
    Write-Host ("Failing projects: {0}" -f ($failed -join ', ')) -ForegroundColor Red
    exit 1
}

Write-Host ''
Write-Host 'All tests passed.' -ForegroundColor Green
exit 0
