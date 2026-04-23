#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

bash "$repo_root/scripts/bootstrap.sh"

if [[ "$(uname -s)" == "Darwin" ]]; then
  cmake --build --preset macos-debug
  ctest --preset macos-debug
else
  cmake --build --preset linux-debug
  ctest --preset linux-debug
fi

dotnet build "$repo_root/Vyre.sln" -c Debug
