# Pack dotnet-test-axi from this clone and install or update it as a dotnet global tool.
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root 'nupkg'

if (Test-Path $out) { Remove-Item -Recurse -Force $out }
dotnet pack (Join-Path $root 'src/DotnetTestAxi') -c Release -o $out
if ($LASTEXITCODE -ne 0) { throw 'dotnet pack failed' }

# `update` installs the tool when it is missing and upgrades it otherwise.
dotnet tool update --global dotnet-test-axi --add-source $out
if ($LASTEXITCODE -ne 0) { throw 'dotnet tool update failed' }

if (Get-Command dotnet-test-axi -ErrorAction SilentlyContinue) {
    dotnet-test-axi --version
} else {
    Write-Warning 'dotnet-test-axi is not on PATH yet: add %USERPROFILE%\.dotnet\tools to PATH.'
}
