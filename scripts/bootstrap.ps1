[CmdletBinding()]
param(
    [switch]$SkipNativeConfigure,
    [switch]$SkipWorkloadRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-Command {
    param([Parameter(Mandatory)] [string]$Name)
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function Assert-Command {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [string]$Hint = "Install $Name and retry."
    )

    if (-not (Test-Command -Name $Name)) {
        throw "Missing required command '$Name'. $Hint"
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot | Resolve-Path
Push-Location $repoRoot

try {
    Write-Host "==> Validating command line prerequisites"
    Assert-Command -Name git -Hint 'Install Git for Windows.'
    Assert-Command -Name cmake -Hint 'Install CMake 3.26+.'
    Assert-Command -Name dotnet -Hint 'Install .NET SDK 10.x.'
    Assert-Command -Name pwsh -Hint 'Install PowerShell 7.'

    if (-not (Test-Path "$repoRoot/.vscode")) {
        New-Item -ItemType Directory -Path "$repoRoot/.vscode" | Out-Null
    }

    Write-Host "==> Restoring .NET solution"
    & dotnet restore "$repoRoot/Vyre.sln"

    if (-not $SkipWorkloadRestore) {
        Write-Host "==> Restoring MAUI workloads"
        & dotnet workload restore "$repoRoot/src/dotnet/Vyre.App/Vyre.App.csproj"
    }

    if ($env:ANDROID_SDK_ROOT) {
        Write-Host "Android SDK detected at $($env:ANDROID_SDK_ROOT)"
    } else {
        Write-Warning 'ANDROID_SDK_ROOT is not set. Android builds will fail until the Android SDK is installed and the environment variable is configured.'
    }

    if ($env:ANDROID_NDK_HOME) {
        Write-Host "Android NDK detected at $($env:ANDROID_NDK_HOME)"
    } else {
        Write-Warning 'ANDROID_NDK_HOME is not set. Native Android CMake builds will fail until the Android NDK is installed and the environment variable is configured.'
    }

    if (-not $SkipNativeConfigure) {
        Write-Host "==> Configuring native host build"
        & cmake --preset windows-debug
    }

    Write-Host "Bootstrap finished successfully. Humans remain, regrettably, optional."
}
finally {
    Pop-Location
}
