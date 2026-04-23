#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

require_cmd() {
  local name="$1"
  local hint="$2"
  if ! command -v "$name" >/dev/null 2>&1; then
    echo "Missing required command '$name'. $hint" >&2
    exit 1
  fi
}

require_cmd git "Install Git."
require_cmd cmake "Install CMake 3.26 or newer."
require_cmd dotnet "Install .NET SDK 10.x."
require_cmd bash "Install bash."

printf '==> Restoring .NET solution\n'
dotnet restore "$repo_root/Vyre.sln"

printf '==> Restoring MAUI workloads\n'
dotnet workload restore "$repo_root/src/dotnet/Vyre.App/Vyre.App.csproj"

if [[ -z "${ANDROID_SDK_ROOT:-}" ]]; then
  printf 'WARNING: ANDROID_SDK_ROOT is not set. Android MAUI builds will fail until the SDK is configured.\n' >&2
fi

if [[ -z "${ANDROID_NDK_HOME:-}" ]]; then
  printf 'WARNING: ANDROID_NDK_HOME is not set. Native Android builds will fail until the NDK is configured.\n' >&2
fi

if [[ "$(uname -s)" == "Darwin" ]]; then
  printf '==> Configuring native macOS host build\n'
  cmake --preset macos-debug
else
  printf '==> Configuring native Linux host build\n'
  cmake --preset linux-debug
fi

printf 'Bootstrap finished successfully. Civilization survives another checkout.\n'
