# Commit guard: literal, case-insensitive terms, one per line (# comments allowed).
# Set PUBLIC_REPO_DENYLIST or PUBLIC_REPO_DENYLIST_FILE, or configure locally:
# Unix: git config publication.denylistFile '~/.config/public-repo/denylist.txt'
# Windows: git config publication.denylistFile '%USERPROFILE%/.config/public-repo/denylist.txt'
# Never commit that file. Scan the complete index, not unstaged working files.
$ErrorActionPreference = 'Stop'

function Invoke-GitText([string[]] $GitArgs) {
    $info = [System.Diagnostics.ProcessStartInfo]::new('git')
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    foreach ($arg in $GitArgs) { $info.ArgumentList.Add($arg) }
    $process = [System.Diagnostics.Process]::Start($info)
    $stderr = $process.StandardError.ReadToEndAsync()
    $text = $process.StandardOutput.ReadToEnd()
    $process.WaitForExit()
    $null = $stderr.GetAwaiter().GetResult()
    $exitCode = $process.ExitCode
    $process.Dispose()
    if ($exitCode -ne 0) { throw 'Cannot read the Git index.' }
    return $text
}

try {
    if ($args.Count) { throw 'This commit guard accepts no arguments.' }
    $source = $env:PUBLIC_REPO_DENYLIST
    $file = $env:PUBLIC_REPO_DENYLIST_FILE
    if (-not $file) {
        $file = & git config --path --get publication.denylistFile
        if ($LASTEXITCODE -notin 0, 1) { throw 'Cannot read publication.denylistFile.' }
    }
    if ($file) {
        if ($file -match '^~([\\/]|$)') { $file = $HOME + $file.Substring(1) }
        $file = [System.Environment]::ExpandEnvironmentVariables($file)
        if ($file -match '%[^%]+%') { throw 'A denylist path environment variable is unset.' }
        $source += "`n" + [System.IO.File]::ReadAllText($file)
    }
    $terms = @($source -split '\r?\n' | ForEach-Object { $_.Trim().TrimStart([char]0xFEFF) } |
        Where-Object { $_ -and -not $_.StartsWith('#') })
    if (-not $terms.Count) {
        throw 'Set PUBLIC_REPO_DENYLIST, PUBLIC_REPO_DENYLIST_FILE, or git config publication.denylistFile to a non-empty private denylist.'
    }
    function Test-Denied([string] $Text) {
        $textWithoutNuls = $Text.Replace("`0", '')
        foreach ($term in $terms) {
            if ($textWithoutNuls.Contains($term, [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
        }
        return $false
    }
    $hits = 0
    foreach ($entry in (Invoke-GitText @('ls-files', '--stage', '--full-name', '-z')) -split "`0") {
        if (-not $entry) { continue }
        if ($entry -notmatch '^(\d+) ([0-9a-f]+) (\d)\t([\s\S]+)$' -or $Matches[3] -ne '0') {
            throw 'Resolve index conflicts before committing.'
        }
        $mode, $oid, $path = $Matches[1], $Matches[2], $Matches[4]
        if ((Test-Denied $path) -or ($mode -ne '160000' -and (Test-Denied (Invoke-GitText @('cat-file', 'blob', $oid))))) {
            $hits++
            # Report paths only, never repeat the private term or matching content.
            if ($hits -le 20) { [Console]::Error.WriteLine("Publication scan: denied content or path in $($path | ConvertTo-Json -Compress)") }
        }
    }
    if ($hits) { throw "Publication scan blocked: $hits indexed file(s) matched the private denylist." }
} catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
