#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
tmpdir="$(mktemp -d)"
trap 'rm -rf "$tmpdir"' EXIT

cd "$repo_root"

echo "== rconsharp: tests =="
DOTNET_ROLL_FORWARD=Major dotnet test RconSharp/RconSharp.Tests/RconSharp.Tests.csproj --configuration Release --verbosity minimal

echo "== rconsharp: vulnerability audit =="
DOTNET_ROLL_FORWARD=Major dotnet list RconSharp/RconSharp.sln package --vulnerable --include-transitive

echo "== rconsharp: package =="
DOTNET_ROLL_FORWARD=Major dotnet pack RconSharp/RconSharp/RconSharp.csproj --configuration Release --no-restore --output "$tmpdir"
test -n "$(find "$tmpdir" -maxdepth 1 -name 'RconSharp.*.nupkg' -print -quit)"

echo "== rconsharp: devstudio shape =="
if command -v devstudio >/dev/null 2>&1; then
  devstudio validate --repo "$repo_root"
else
  echo "devstudio not available; skipping DevStudio validation"
fi

echo "rconsharp validation complete."
