#!/usr/bin/env pwsh
<#
Bumps the tool and skill version together and releases the changelog.
Usage: pwsh scripts/bump-version.ps1 <major|minor|patch|X.Y.Z>
Edits Directory.Build.props, skills/dotnet-test-axi/SKILL.md and CHANGELOG.md. Does not commit or tag.
#>
param([Parameter(Mandatory)][string]$Bump)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$props = Join-Path $root 'Directory.Build.props'
$skill = Join-Path $root 'skills/dotnet-test-axi/SKILL.md'
$log = Join-Path $root 'CHANGELOG.md'

$propsText = Get-Content $props -Raw
if ($propsText -notmatch '<Version>(\d+)\.(\d+)\.(\d+)</Version>') { throw 'No <Version> found in Directory.Build.props' }
$old = "$($Matches[1]).$($Matches[2]).$($Matches[3])"
[int]$maj, [int]$min, [int]$pat = $Matches[1], $Matches[2], $Matches[3]

$new = switch -Regex ($Bump) {
    '^major$' { "$($maj + 1).0.0" }
    '^minor$' { "$maj.$($min + 1).0" }
    '^patch$' { "$maj.$min.$($pat + 1)" }
    '^\d+\.\d+\.\d+$' { $Bump }
    default { throw "Usage: bump-version.ps1 <major|minor|patch|X.Y.Z> (got '$Bump')" }
}
if ([version]$new -le [version]$old) { throw "New version $new must be greater than $old" }

$logText = Get-Content $log -Raw
if ($logText -notmatch '(?s)## \[Unreleased\]\r?\n(.*?)\r?\n## \[') { throw 'CHANGELOG.md has no [Unreleased] section' }
if ($Matches[1] -notmatch '^\s*- ' -and $Matches[1] -notmatch '(?m)^- ') { throw 'The [Unreleased] section has no entries. Add them first.' }

$date = Get-Date -Format 'yyyy-MM-dd'
$logText = $logText -replace '## \[Unreleased\]', "## [Unreleased]`n`n## [$new] - $date"
$logText = $logText -replace '(?m)^\[Unreleased\]: (.*)/compare/v[\d.]+\.\.\.HEAD$',
    "[Unreleased]: `$1/compare/v$new...HEAD`n[$new]: `$1/compare/v$old...v$new"
Set-Content $log $logText -NoNewline

Set-Content $props ($propsText -replace '<Version>[^<]+</Version>', "<Version>$new</Version>") -NoNewline
$skillText = Get-Content $skill -Raw
if ($skillText -notmatch '(?m)^version:') { throw 'No version: line in SKILL.md frontmatter' }
Set-Content $skill ($skillText -replace '(?m)^version:.*$', "version: $new") -NoNewline

Write-Host "Bumped $old -> $new"
Write-Host "Next: dotnet test --project tests/DotnetTestAxi.Tests, commit 'chore: release $new', git tag v$new, then pack and 'dotnet tool update'."
