#!/usr/bin/env bash
# Pack dotnet-test-axi from this clone and install or update it as a dotnet global tool.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
out="$root/nupkg"

rm -rf "$out"
dotnet pack "$root/src/DotnetTestAxi" -c Release -o "$out"

# `update` installs the tool when it is missing and upgrades it otherwise.
dotnet tool update --global dotnet-test-axi --add-source "$out"

dotnet-test-axi --version || echo "dotnet-test-axi is not on PATH yet: add ~/.dotnet/tools to PATH." >&2
