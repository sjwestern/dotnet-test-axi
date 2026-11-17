# Run with: pwsh -NoProfile -File scripts/test-publication.ps1
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$script = Join-Path $PSScriptRoot 'check-publication.ps1'
$hook = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot '../.husky/pre-commit'))
$temp = Join-Path ([System.IO.Path]::GetTempPath()) "publication-guard-$([guid]::NewGuid())"
$repo = Join-Path $temp 'repo'
$keys = @('PUBLIC_REPO_DENYLIST', 'PUBLIC_REPO_DENYLIST_FILE', 'GIT_DIR', 'GIT_WORK_TREE', 'GIT_INDEX_FILE', 'PUBLICATION_TEST_ROOT')
$saved = @{}
foreach ($key in $keys) {
    $saved[$key] = [Environment]::GetEnvironmentVariable($key)
    Remove-Item "Env:$key" -ErrorAction SilentlyContinue
}
$null = New-Item -ItemType Directory -Path $repo -Force
Push-Location $repo
function Invoke-TestGit {
    & git @args | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Fixture git failed: $args" }
}
function Set-Staged([string] $Path, [string] $Text) {
    [System.IO.File]::WriteAllText((Join-Path $repo $Path), $Text)
    Invoke-TestGit add -- $Path
}
$checks = 0
function Assert-Check([int] $Expected) {
    $output = & pwsh -NoLogo -NoProfile -File $script 2>&1
    if ($LASTEXITCODE -ne $Expected) { throw "Expected $Expected, got ${LASTEXITCODE}: $output" }
    $script:checks++
}
try {
    $env:PUBLIC_REPO_DENYLIST = 'reserved-sample'
    Invoke-TestGit init -q
    Invoke-TestGit config user.name Test
    Invoke-TestGit config user.email test@example.invalid
    Invoke-TestGit config core.hooksPath .hooks
    Invoke-TestGit config publication.denylistFile ''
    Set-Staged example.txt 'generic example'
    Assert-Check 0
    $env:PUBLIC_REPO_DENYLIST = ''
    Assert-Check 1
    $env:PUBLIC_REPO_DENYLIST = 'reserved-sample'
    Set-Staged example.txt 'RESERVED-SAMPLE'
    [System.IO.File]::WriteAllText((Join-Path $repo 'example.txt'), 'unstaged clean text')
    Assert-Check 1
    Set-Staged example.txt 'generic example'
    [System.IO.File]::WriteAllText((Join-Path $repo 'example.txt'), 'reserved-sample')
    Assert-Check 0
    [System.IO.File]::WriteAllBytes((Join-Path $repo 'example.txt'), [System.Text.Encoding]::Unicode.GetBytes('reserved-sample'))
    Invoke-TestGit add -- example.txt
    Assert-Check 1
    Set-Staged example.txt 'generic example'
    Set-Staged 'RESERVED-SAMPLE file.txt' 'generic example'
    Assert-Check 1
    Invoke-TestGit rm --cached -- 'RESERVED-SAMPLE file.txt'
    $file = Join-Path $temp 'denylist.txt'
    [System.IO.File]::WriteAllText($file, "# comment`r`nreserved-sample`r`n")
    $env:PUBLIC_REPO_DENYLIST = ''
    $env:PUBLIC_REPO_DENYLIST_FILE = $file
    Assert-Check 0
    $env:PUBLICATION_TEST_ROOT = $temp
    $env:PUBLIC_REPO_DENYLIST_FILE = '%PUBLICATION_TEST_ROOT%/denylist.txt'
    Assert-Check 0
    # HOME is resolved by the child process; use a home-relative path to our temp file.
    $relative = [System.IO.Path]::GetRelativePath($HOME, $file).Replace('\', '/')
    $env:PUBLIC_REPO_DENYLIST_FILE = "~/$relative"
    Assert-Check 0
    $env:PUBLIC_REPO_DENYLIST_FILE = $file
    Set-Staged example.txt 'reserved-sample'
    Assert-Check 1
    $env:PUBLIC_REPO_DENYLIST_FILE = Join-Path $temp 'missing'
    Assert-Check 1
    $env:PUBLIC_REPO_DENYLIST_FILE = ''
    Invoke-TestGit config publication.denylistFile $file
    Assert-Check 1
    Set-Staged example.txt 'generic example'
    Assert-Check 0
    $null = New-Item -ItemType Directory -Path scripts, .hooks
    Copy-Item $script scripts/check-publication.ps1
    [System.IO.File]::WriteAllText((Join-Path $repo '.hooks/pre-commit'), "#!/bin/sh`n$hook")
    if (-not $IsWindows) { & chmod +x .hooks/pre-commit }
    Set-Staged example.txt 'reserved-sample'
    $null = & git commit -qm 'chore: sample' 2>&1
    if ($LASTEXITCODE -eq 0) { throw 'Commit hook failed to block denied content.' }
    $checks++
    Set-Staged example.txt 'generic example'
    Invoke-TestGit commit -qm 'chore: sample'
    $checks++
    Write-Host "Publication guard: $checks checks passed."
} finally {
    Pop-Location
    foreach ($key in $keys) {
        if ($null -eq $saved[$key]) { Remove-Item "Env:$key" -ErrorAction SilentlyContinue }
        else { [Environment]::SetEnvironmentVariable($key, $saved[$key]) }
    }
    Remove-Item -Recurse -Force $temp
}
