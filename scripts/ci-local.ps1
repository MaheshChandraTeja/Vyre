Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot | Resolve-Path
Push-Location $repoRoot

try {
    & pwsh -NoProfile -ExecutionPolicy Bypass -File "$repoRoot/scripts/bootstrap.ps1"
    & cmake --build --preset windows-debug
    & ctest --preset windows-debug
    & dotnet build "$repoRoot/Vyre.sln" -c Debug
}
finally {
    Pop-Location
}
