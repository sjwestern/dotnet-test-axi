#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Regenerates fixtures/*.log from the sample projects in tests/Samples.
.DESCRIPTION
  Runs `dotnet test` for each sample framework and scenario, then rewrites machine-specific paths and
  random ids so the fixtures are stable and generic. Review the git diff before committing: a changed
  fixture means the framework or Microsoft Testing Platform output changed.
.EXAMPLE
  pwsh scripts/capture-fixtures.ps1
  pwsh scripts/capture-fixtures.ps1 -Framework MSTest
#>
[CmdletBinding()]
param(
    [string[]] $Framework = @('Xunit3', 'MSTest', 'NUnit', 'TUnit')
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$fixtures = Join-Path $root 'fixtures'
$samples = Join-Path $root 'tests/Samples'
$esc = [char]27

function Convert-Output([string] $text) {
    $text = $text -replace "$esc\[[0-9;?]*[ -/]*[@-~]", ''
    $text = $text.Replace($samples.Replace('\', '/'), '/work/samples').Replace($samples, '/work/samples')
    $text = $text.Replace($root.Replace('\', '/'), '/work/repo').Replace($root, '/work/repo')
    $text = $text -replace '/tmp/[0-9a-f]{32}', '/tmp/pipe'
    $text = $text -replace '(?m)^\s*- /work/.*TestResults.*\r?\n', ''
    ($text -replace "`r`n", "`n").TrimEnd() + "`n"
}

function Save([string] $name, [string[]] $dotnetArgs, [hashtable] $environment = @{}) {
    $old = @{}
    foreach ($k in $environment.Keys) { $old[$k] = [Environment]::GetEnvironmentVariable($k); [Environment]::SetEnvironmentVariable($k, $environment[$k]) }
    try {
        $out = & dotnet @dotnetArgs 2>&1 | Out-String
    }
    finally {
        foreach ($k in $old.Keys) { [Environment]::SetEnvironmentVariable($k, $old[$k]) }
    }
    Set-Content -Path (Join-Path $fixtures "$name.log") -Value (Convert-Output $out) -NoNewline
    Write-Host "captured $name"
}

foreach ($f in $Framework) {
    $project = Join-Path $samples "$f.Sample"
    $key = $f.ToLowerInvariant()
    $base = @('test', '--project', $project, '--no-progress')
    Save "$key-pass" $base
    Save "$key-fail" ($base + '--no-build') @{ SAMPLE_SCENARIO = 'fail' }
    Save "$key-crash" ($base + '--no-build') @{ SAMPLE_SCENARIO = 'crash' }
    Save "$key-class-filter" ($base + '--no-build' + '--filter-class' + 'Samples.Missing')
}

# A native filter that selects one test, as translated by the framework adapters for --method <ns>.SampleTests.Fails.
$native = [ordered]@{
    Xunit3 = @('--filter-method', 'Samples.Xunit3.SampleTests.Fails')
    MSTest = @('--filter', 'FullyQualifiedName=Samples.MSTest.SampleTests.Fails')
    NUnit  = @('--filter', 'FullyQualifiedName=Samples.NUnitSample.SampleTests.Fails')
    TUnit  = @('--treenode-filter', '/*/Samples.TUnitSample/SampleTests/Fails')
}
foreach ($f in $Framework) {
    if (-not $native.Contains($f)) { continue }
    $project = Join-Path $samples "$f.Sample"
    Save "$($f.ToLowerInvariant())-method-filter" (@('test', '--project', $project, '--no-progress', '--no-build') + $native[$f]) @{ SAMPLE_SCENARIO = 'fail' }
}

# Framework independent cases, captured once from the xUnit v3 sample.
$x = Join-Path $samples 'Xunit3.Sample'
Save 'build-error' @('test', '--project', $x, '--no-progress', '-p:SampleBuildError=true')

# VSTest sample: dotnet test in VSTest mode rejects --project, and an MTP solution rejects VSTest projects.
$v = Join-Path $samples 'VSTest.Sample'
Save 'vstest-project-flag-rejected' @('test', '--project', $v, '--no-progress')
