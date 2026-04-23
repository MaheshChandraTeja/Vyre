# Vyre Module 1 - Copy/Paste File Set

```text
Vyre-Module1/
  .gitignore
  .vscode/launch.json
  .vscode/tasks.json
  CMakeLists.txt
  CMakePresets.json
  Directory.Build.props
  Directory.Packages.props
  Vyre.sln
  docs/architecture.md
  global.json
  scripts/bootstrap.ps1
  scripts/bootstrap.sh
  scripts/ci-local.ps1
  scripts/ci-local.sh
  src/dotnet/Vyre.App.Core/Models/EngineBridgeStatus.cs
  src/dotnet/Vyre.App.Core/Services/BootstrapStatusFormatter.cs
  src/dotnet/Vyre.App.Core/Vyre.App.Core.csproj
  src/dotnet/Vyre.App/App.xaml
  src/dotnet/Vyre.App/App.xaml.cs
  src/dotnet/Vyre.App/AppShell.xaml
  src/dotnet/Vyre.App/AppShell.xaml.cs
  src/dotnet/Vyre.App/MauiProgram.cs
  src/dotnet/Vyre.App/Models/NativeBuildInfo.cs
  src/dotnet/Vyre.App/Pages/HomePage.xaml
  src/dotnet/Vyre.App/Pages/HomePage.xaml.cs
  src/dotnet/Vyre.App/Platforms/Android/AndroidManifest.xml
  src/dotnet/Vyre.App/Platforms/Android/MainActivity.cs
  src/dotnet/Vyre.App/Platforms/Android/MainApplication.cs
  src/dotnet/Vyre.App/Platforms/iOS/AppDelegate.cs
  src/dotnet/Vyre.App/Platforms/iOS/Info.plist
  src/dotnet/Vyre.App/Platforms/iOS/Program.cs
  src/dotnet/Vyre.App/Resources/AppIcon/appicon.svg
  src/dotnet/Vyre.App/Resources/AppIcon/appiconfg.svg
  src/dotnet/Vyre.App/Resources/Images/diagnostics.svg
  src/dotnet/Vyre.App/Resources/Splash/splash.svg
  src/dotnet/Vyre.App/Resources/Styles/Colors.xaml
  src/dotnet/Vyre.App/Resources/Styles/Styles.xaml
  src/dotnet/Vyre.App/Services/Engine/IVyreEngineService.cs
  src/dotnet/Vyre.App/Services/Engine/NativeMethods.cs
  src/dotnet/Vyre.App/Services/Engine/VyreEngineService.cs
  src/dotnet/Vyre.App/ViewModels/BaseViewModel.cs
  src/dotnet/Vyre.App/ViewModels/HomePageViewModel.cs
  src/dotnet/Vyre.App/Vyre.App.csproj
  src/native/tests/CMakeLists.txt
  src/native/tests/engine_smoke_tests.cpp
  src/native/vyre-core/CMakeLists.txt
  src/native/vyre-core/include/vyre/core/engine.hpp
  src/native/vyre-core/include/vyre/core/version.hpp
  src/native/vyre-core/src/engine.cpp
  src/native/vyre-interop/CMakeLists.txt
  src/native/vyre-interop/include/vyre/interop/vyre_api.h
  src/native/vyre-interop/src/vyre_api.cpp
```

## .gitignore
```
# Build output
/build/
/bin/
/obj/
/artifacts/

# VS Code user state
.vscode/*.log
.vscode/*.code-workspace

# CMake
CMakeUserPresets.json
cmake-build-*/

# OS files
.DS_Store
Thumbs.db

# Native/managed outputs
src/dotnet/Vyre.App/bin/
src/dotnet/Vyre.App/obj/
src/dotnet/Vyre.App.Core/bin/
src/dotnet/Vyre.App.Core/obj/
```

## .vscode/launch.json
```
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Debug Vyre (.NET MAUI)",
      "type": "dotnet",
      "request": "launch",
      "projectPath": "${workspaceFolder}/src/dotnet/Vyre.App/Vyre.App.csproj",
      "preLaunchTask": "maui:build:debug"
    },
    {
      "name": "Debug Vyre Native Smoke Tests (Windows)",
      "type": "cppvsdbg",
      "request": "launch",
      "program": "${workspaceFolder}/build/windows-debug/artifacts/bin/vyre-native-smoke-tests.exe",
      "cwd": "${workspaceFolder}/build/windows-debug/artifacts/bin",
      "preLaunchTask": "native:build:host",
      "stopAtEntry": false
    },
    {
      "name": "Debug Vyre Native Smoke Tests (macOS/Linux)",
      "type": "cppdbg",
      "request": "launch",
      "program": "${workspaceFolder}/build/macos-debug/artifacts/bin/vyre-native-smoke-tests",
      "cwd": "${workspaceFolder}/build/macos-debug/artifacts/bin",
      "preLaunchTask": "native:build:host",
      "MIMode": "lldb",
      "stopAtEntry": false,
      "linux": {
        "program": "${workspaceFolder}/build/linux-debug/artifacts/bin/vyre-native-smoke-tests",
        "cwd": "${workspaceFolder}/build/linux-debug/artifacts/bin",
        "MIMode": "gdb"
      }
    }
  ]
}
```

## .vscode/tasks.json
```
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "bootstrap",
      "type": "shell",
      "command": "pwsh",
      "args": ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "${workspaceFolder}/scripts/bootstrap.ps1"],
      "windows": {
        "command": "pwsh"
      },
      "osx": {
        "command": "bash",
        "args": ["${workspaceFolder}/scripts/bootstrap.sh"]
      },
      "problemMatcher": []
    },
    {
      "label": "native:configure:host",
      "type": "shell",
      "command": "cmake",
      "args": ["--preset", "windows-debug"],
      "windows": {
        "args": ["--preset", "windows-debug"]
      },
      "osx": {
        "args": ["--preset", "macos-debug"]
      },
      "linux": {
        "args": ["--preset", "linux-debug"]
      },
      "problemMatcher": ["$gcc"]
    },
    {
      "label": "native:build:host",
      "type": "shell",
      "command": "cmake",
      "args": ["--build", "--preset", "windows-debug"],
      "windows": {
        "args": ["--build", "--preset", "windows-debug"]
      },
      "osx": {
        "args": ["--build", "--preset", "macos-debug"]
      },
      "linux": {
        "args": ["--build", "--preset", "linux-debug"]
      },
      "dependsOn": ["native:configure:host"],
      "problemMatcher": ["$msCompile", "$gcc"]
    },
    {
      "label": "native:test:host",
      "type": "shell",
      "command": "ctest",
      "args": ["--preset", "windows-debug"],
      "windows": {
        "args": ["--preset", "windows-debug"]
      },
      "osx": {
        "args": ["--preset", "macos-debug"]
      },
      "linux": {
        "args": ["--preset", "linux-debug"]
      },
      "dependsOn": ["native:build:host"],
      "problemMatcher": []
    },
    {
      "label": "native:build:android",
      "type": "shell",
      "command": "cmake",
      "args": ["--build", "--preset", "android-arm64-debug"],
      "dependsOn": [
        {
          "task": "native:configure:android",
          "dependsOrder": "sequence"
        }
      ],
      "problemMatcher": ["$gcc"]
    },
    {
      "label": "native:configure:android",
      "type": "shell",
      "command": "cmake",
      "args": ["--preset", "android-arm64-debug"],
      "problemMatcher": ["$gcc"]
    },
    {
      "label": "native:build:ios-sim",
      "type": "shell",
      "command": "cmake",
      "args": ["--build", "--preset", "ios-sim-debug"],
      "osx": {
        "command": "cmake",
        "args": ["--build", "--preset", "ios-sim-debug"]
      },
      "problemMatcher": ["$gcc"]
    },
    {
      "label": "dotnet:restore",
      "type": "shell",
      "command": "dotnet",
      "args": ["restore", "${workspaceFolder}/Vyre.sln"],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "dotnet:workload:restore",
      "type": "shell",
      "command": "dotnet",
      "args": ["workload", "restore", "${workspaceFolder}/src/dotnet/Vyre.App/Vyre.App.csproj"],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "maui:build:debug",
      "type": "shell",
      "command": "dotnet",
      "args": ["build", "${workspaceFolder}/Vyre.sln", "-c", "Debug"],
      "dependsOn": ["dotnet:restore", "dotnet:workload:restore"],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "maui:build:android",
      "type": "shell",
      "command": "dotnet",
      "args": ["build", "${workspaceFolder}/src/dotnet/Vyre.App/Vyre.App.csproj", "-f", "net10.0-android", "-c", "Debug"],
      "dependsOn": ["dotnet:restore", "dotnet:workload:restore"],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "maui:build:ios",
      "type": "shell",
      "command": "dotnet",
      "args": ["build", "${workspaceFolder}/src/dotnet/Vyre.App/Vyre.App.csproj", "-f", "net10.0-ios", "-c", "Debug"],
      "dependsOn": ["dotnet:restore", "dotnet:workload:restore"],
      "osx": {
        "command": "dotnet",
        "args": ["build", "${workspaceFolder}/src/dotnet/Vyre.App/Vyre.App.csproj", "-f", "net10.0-ios", "-c", "Debug"]
      },
      "problemMatcher": "$msCompile"
    },
    {
      "label": "validate:local",
      "dependsOrder": "sequence",
      "dependsOn": ["native:test:host", "maui:build:debug"],
      "problemMatcher": []
    }
  ]
}
```

## CMakeLists.txt
```
cmake_minimum_required(VERSION 3.26)

project(Vyre
    VERSION 0.1.0
    DESCRIPTION "Vyre native engine and interop"
    LANGUAGES CXX
)

option(VYRE_BUILD_TESTS "Build native test targets" ON)
option(VYRE_BUILD_INTEROP "Build the C ABI interop layer" ON)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/artifacts/lib")
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/artifacts/bin")
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/artifacts/bin")

if(MSVC)
    add_compile_options(/permissive- /W4 /EHsc /Zc:__cplusplus)
    add_compile_definitions(_CRT_SECURE_NO_WARNINGS)
else()
    add_compile_options(-Wall -Wextra -Wpedantic -Wconversion -Wshadow -Wnon-virtual-dtor)
endif()

add_subdirectory(src/native/vyre-core)

if(VYRE_BUILD_INTEROP)
    add_subdirectory(src/native/vyre-interop)
endif()

if(VYRE_BUILD_TESTS)
    include(CTest)
    enable_testing()
    add_subdirectory(src/native/tests)
endif()
```

## CMakePresets.json
```
{
  "version": 6,
  "cmakeMinimumRequired": {
    "major": 3,
    "minor": 26,
    "patch": 0
  },
  "configurePresets": [
    {
      "name": "base",
      "hidden": true,
      "cacheVariables": {
        "CMAKE_CXX_STANDARD": "20",
        "CMAKE_CXX_STANDARD_REQUIRED": "ON",
        "CMAKE_CXX_EXTENSIONS": "OFF",
        "VYRE_BUILD_TESTS": "ON",
        "VYRE_BUILD_INTEROP": "ON"
      }
    },
    {
      "name": "windows-debug",
      "inherits": ["base"],
      "generator": "Ninja",
      "binaryDir": "${sourceDir}/build/windows-debug",
      "cacheVariables": {
        "CMAKE_BUILD_TYPE": "Debug"
      },
      "condition": {
        "type": "equals",
        "lhs": "${hostSystemName}",
        "rhs": "Windows"
      }
    },
    {
      "name": "macos-debug",
      "inherits": ["base"],
      "generator": "Ninja",
      "binaryDir": "${sourceDir}/build/macos-debug",
      "cacheVariables": {
        "CMAKE_BUILD_TYPE": "Debug"
      },
      "condition": {
        "type": "equals",
        "lhs": "${hostSystemName}",
        "rhs": "Darwin"
      }
    },
    {
      "name": "linux-debug",
      "inherits": ["base"],
      "generator": "Ninja",
      "binaryDir": "${sourceDir}/build/linux-debug",
      "cacheVariables": {
        "CMAKE_BUILD_TYPE": "Debug"
      },
      "condition": {
        "type": "equals",
        "lhs": "${hostSystemName}",
        "rhs": "Linux"
      }
    },
    {
      "name": "android-arm64-debug",
      "inherits": ["base"],
      "generator": "Ninja",
      "binaryDir": "${sourceDir}/build/android-arm64-debug",
      "cacheVariables": {
        "CMAKE_BUILD_TYPE": "Debug",
        "CMAKE_SYSTEM_NAME": "Android",
        "CMAKE_TOOLCHAIN_FILE": "$env{ANDROID_NDK_HOME}/build/cmake/android.toolchain.cmake",
        "ANDROID_ABI": "arm64-v8a",
        "ANDROID_PLATFORM": "24"
      }
    },
    {
      "name": "ios-sim-debug",
      "inherits": ["base"],
      "generator": "Xcode",
      "binaryDir": "${sourceDir}/build/ios-sim-debug",
      "cacheVariables": {
        "CMAKE_SYSTEM_NAME": "iOS",
        "CMAKE_OSX_SYSROOT": "iphonesimulator",
        "CMAKE_OSX_ARCHITECTURES": "arm64"
      },
      "condition": {
        "type": "equals",
        "lhs": "${hostSystemName}",
        "rhs": "Darwin"
      }
    }
  ],
  "buildPresets": [
    {
      "name": "windows-debug",
      "configurePreset": "windows-debug"
    },
    {
      "name": "macos-debug",
      "configurePreset": "macos-debug"
    },
    {
      "name": "linux-debug",
      "configurePreset": "linux-debug"
    },
    {
      "name": "android-arm64-debug",
      "configurePreset": "android-arm64-debug"
    },
    {
      "name": "ios-sim-debug",
      "configurePreset": "ios-sim-debug"
    }
  ],
  "testPresets": [
    {
      "name": "windows-debug",
      "configurePreset": "windows-debug",
      "output": {
        "outputOnFailure": true
      }
    },
    {
      "name": "macos-debug",
      "configurePreset": "macos-debug",
      "output": {
        "outputOnFailure": true
      }
    },
    {
      "name": "linux-debug",
      "configurePreset": "linux-debug",
      "output": {
        "outputOnFailure": true
      }
    }
  ]
}
```

## Directory.Build.props
```
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Deterministic>true</Deterministic>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <DebugType>portable</DebugType>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
```

## Directory.Packages.props
```
<Project>
  <ItemGroup>
  </ItemGroup>
</Project>
```

## Vyre.sln
```
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Vyre.App.Core", "src\dotnet\Vyre.App.Core\Vyre.App.Core.csproj", "{A5E28B6E-C743-4AA6-A512-906EF5E85774}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Vyre.App", "src\dotnet\Vyre.App\Vyre.App.csproj", "{4A5AC998-8D3E-4AC0-A925-6E4BE9518E20}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{A5E28B6E-C743-4AA6-A512-906EF5E85774}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A5E28B6E-C743-4AA6-A512-906EF5E85774}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A5E28B6E-C743-4AA6-A512-906EF5E85774}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A5E28B6E-C743-4AA6-A512-906EF5E85774}.Release|Any CPU.Build.0 = Release|Any CPU
		{4A5AC998-8D3E-4AC0-A925-6E4BE9518E20}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{4A5AC998-8D3E-4AC0-A925-6E4BE9518E20}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{4A5AC998-8D3E-4AC0-A925-6E4BE9518E20}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{4A5AC998-8D3E-4AC0-A925-6E4BE9518E20}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
```

## docs/architecture.md
```
# Vyre architecture

## Goal

Vyre is a layered monorepo for a cross-platform Wi-Fi analysis product. Module 1 keeps the repo boring on purpose: predictable builds, clean ownership, and no logic smear between UI and native code.

## Repository layout

```text
repo-root/
├─ src/
│  ├─ native/
│  │  ├─ vyre-core/          # C++ domain and analysis engine
│  │  ├─ vyre-interop/       # Stable C ABI for managed/native boundary
│  │  └─ tests/              # Native smoke/unit tests
│  └─ dotnet/
│     ├─ Vyre.App.Core/      # Shared managed application logic
│     └─ Vyre.App/           # MAUI UI shell for Android/iOS
├─ scripts/                  # Bootstrap and validation scripts
├─ docs/                     # Architecture and repo conventions
├─ .vscode/                  # Deterministic local tasks/launchers
├─ CMakeLists.txt            # Native build root
├─ CMakePresets.json         # Standard build presets
└─ Vyre.sln                  # Managed solution entry point
```

## Layer rules

### Rule 1: native owns analysis
The C++ engine owns analysis logic, parsing, scoring, modelable data structures, and performance-sensitive code.

### Rule 2: interop is the only native boundary
Managed code never reaches directly into `vyre-core`. MAUI calls `vyre-interop`, and `vyre-interop` translates between the managed world and C++ domain code.

### Rule 3: UI is orchestration only
`Vyre.App` owns rendering, navigation, platform lifecycle, and user interaction. It does not own analysis rules or native domain policy.

### Rule 4: shared managed code stays UI-agnostic
`Vyre.App.Core` can contain formatting, application state shaping, service contracts, and orchestration helpers. It must not depend on MAUI visual types.

### Rule 5: builds remain host-explicit
Native host builds use CMake presets. MAUI builds use `dotnet` and target frameworks that light up only on valid hosts.

## Dependency rules

Allowed:

- `Vyre.App` -> `Vyre.App.Core`
- `Vyre.App` -> `vyre-interop` through P/Invoke only
- `vyre-interop` -> `vyre-core`

Forbidden:

- `Vyre.App` -> `vyre-core`
- `Vyre.App.Core` -> MAUI UI types
- `vyre-core` -> platform UI code
- random script-generated files dropped into unrelated layers

## Build conventions

- C++ standard is C++20.
- .NET SDK is locked through `global.json`.
- Warning escalation is enabled by default.
- Native output lands in `build/<preset>/artifacts`.
- Managed builds go through the solution or explicit project targets.

## Working agreement for future modules

1. Add new native features inside `vyre-core` first.
2. Expose only stable C ABI surface from `vyre-interop`.
3. Shape returned data into managed models inside `Vyre.App` or `Vyre.App.Core`.
4. Wire new UI through view models and service contracts.
5. Extend VS Code tasks only when a workflow becomes repeatable.

Because nothing says “engineering discipline” like preventing a future code generator from panic-dumping business logic into `MainPage.xaml.cs`.
```

## global.json
```
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestPatch"
  }
}
```

## scripts/bootstrap.ps1
```
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
```

## scripts/bootstrap.sh
```
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
```

## scripts/ci-local.ps1
```
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
```

## scripts/ci-local.sh
```
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
```

## src/dotnet/Vyre.App.Core/Models/EngineBridgeStatus.cs
```
namespace Vyre.App.Core.Models;

public sealed record EngineBridgeStatus(
    bool IsNativeAvailable,
    string Source,
    string LibraryName,
    string Message);
```

## src/dotnet/Vyre.App.Core/Services/BootstrapStatusFormatter.cs
```
using Vyre.App.Core.Models;

namespace Vyre.App.Core.Services;

public static class BootstrapStatusFormatter
{
    public static string FormatHeadline(EngineBridgeStatus status) =>
        status.IsNativeAvailable
            ? "Native bridge is live and reachable."
            : "Managed shell is healthy, native bridge is not loaded yet.";

    public static string FormatDetail(EngineBridgeStatus status) =>
        status.IsNativeAvailable
            ? $"Source: {status.Source}. Library: {status.LibraryName}. Payload: {status.Message}"
            : $"Source: {status.Source}. Expected library: {status.LibraryName}. Reason: {status.Message}";
}
```

## src/dotnet/Vyre.App.Core/Vyre.App.Core.csproj
```
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>Vyre.App.Core</RootNamespace>
  </PropertyGroup>
</Project>
```

## src/dotnet/Vyre.App/App.xaml
```
<?xml version="1.0" encoding="utf-8" ?>
<Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="Vyre.App.App">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
                <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

## src/dotnet/Vyre.App/App.xaml.cs
```
namespace Vyre.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }
}
```

## src/dotnet/Vyre.App/AppShell.xaml
```
<?xml version="1.0" encoding="utf-8" ?>
<Shell xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
       xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
       xmlns:pages="clr-namespace:Vyre.App.Pages"
       x:Class="Vyre.App.AppShell"
       Shell.NavBarIsVisible="True">
    <TabBar>
        <ShellContent Title="Status"
                      Route="home">
            <pages:HomePage />
        </ShellContent>
    </TabBar>
</Shell>
```

## src/dotnet/Vyre.App/AppShell.xaml.cs
```
namespace Vyre.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }
}
```

## src/dotnet/Vyre.App/MauiProgram.cs
```
using Microsoft.Extensions.Logging;
using Vyre.App.Services.Engine;
using Vyre.App.ViewModels;

namespace Vyre.App;

public static class MauiProgram
{
    public static IServiceProvider Services { get; private set; } = default!;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(_ => { });

        builder.Services.AddSingleton<IVyreEngineService, VyreEngineService>();
        builder.Services.AddSingleton<HomePageViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        Services = app.Services;
        return app;
    }
}
```

## src/dotnet/Vyre.App/Models/NativeBuildInfo.cs
```
namespace Vyre.App.Models;

public sealed record NativeBuildInfo(
    bool IsNativeAvailable,
    string LibraryName,
    string Message,
    string Source);
```

## src/dotnet/Vyre.App/Pages/HomePage.xaml
```
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="Vyre.App.Pages.HomePage"
             Title="Vyre Status">
    <ScrollView>
        <VerticalStackLayout Padding="24" Spacing="18">
            <Label Text="{Binding Title}"
                   FontSize="32"
                   FontAttributes="Bold" />

            <Label Text="{Binding ModuleName}"
                   FontSize="18"
                   TextColor="{StaticResource SecondaryTextColor}" />

            <Border StrokeThickness="1"
                    Stroke="{StaticResource BorderColor}"
                    BackgroundColor="{StaticResource CardBackgroundColor}"
                    Padding="18">
                <VerticalStackLayout Spacing="12">
                    <Label Text="{Binding StatusPill}"
                           FontSize="12"
                           FontAttributes="Bold"
                           TextColor="{StaticResource AccentColor}" />
                    <Label Text="{Binding Headline}"
                           FontSize="24"
                           FontAttributes="Bold" />
                    <Label Text="{Binding Detail}"
                           FontSize="15"
                           LineBreakMode="WordWrap" />
                    <Label Text="{Binding LastUpdated}"
                           FontSize="12"
                           TextColor="{StaticResource SecondaryTextColor}" />
                    <Button Text="Refresh native status"
                            Command="{Binding RefreshCommand}" />
                </VerticalStackLayout>
            </Border>

            <Label Text="Repo guarantees"
                   FontSize="20"
                   FontAttributes="Bold" />

            <CollectionView ItemsSource="{Binding Checklist}">
                <CollectionView.ItemTemplate>
                    <DataTemplate>
                        <Border StrokeThickness="1"
                                Stroke="{StaticResource BorderColor}"
                                BackgroundColor="{StaticResource CardBackgroundColor}"
                                Margin="0,0,0,10"
                                Padding="14">
                            <HorizontalStackLayout Spacing="12">
                                <Label Text="•"
                                       FontSize="18"
                                       VerticalOptions="Start" />
                                <Label Text="{Binding .}"
                                       FontSize="15"
                                       LineBreakMode="WordWrap"
                                       HorizontalOptions="Fill" />
                            </HorizontalStackLayout>
                        </Border>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

## src/dotnet/Vyre.App/Pages/HomePage.xaml.cs
```
using Vyre.App.ViewModels;

namespace Vyre.App.Pages;

public partial class HomePage : ContentPage
{
    private bool _loaded;

    public HomePage()
    {
        InitializeComponent();
        BindingContext = MauiProgram.Services.GetRequiredService<HomePageViewModel>();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        if (BindingContext is HomePageViewModel viewModel)
        {
            await viewModel.RefreshAsync();
        }
    }
}
```

## src/dotnet/Vyre.App/Platforms/Android/AndroidManifest.xml
```
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <application android:allowBackup="true"
                 android:icon="@mipmap/appicon"
                 android:roundIcon="@mipmap/appicon_round"
                 android:supportsRtl="true" />
    <uses-sdk android:minSdkVersion="24" android:targetSdkVersion="35" />
</manifest>
```

## src/dotnet/Vyre.App/Platforms/Android/MainActivity.cs
```
using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Vyre.App;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize |
                           ConfigChanges.Orientation |
                           ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout |
                           ConfigChanges.SmallestScreenSize |
                           ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
```

## src/dotnet/Vyre.App/Platforms/Android/MainApplication.cs
```
using Android.App;
using Android.Runtime;

namespace Vyre.App;

[Application]
public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
```

## src/dotnet/Vyre.App/Platforms/iOS/AppDelegate.cs
```
using Foundation;

namespace Vyre.App;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
```

## src/dotnet/Vyre.App/Platforms/iOS/Info.plist
```
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDisplayName</key>
    <string>Vyre</string>
    <key>CFBundleIdentifier</key>
    <string>com.kairais.vyre</string>
    <key>CFBundleShortVersionString</key>
    <string>0.1.0</string>
    <key>CFBundleVersion</key>
    <string>1</string>
    <key>LSRequiresIPhoneOS</key>
    <true/>
    <key>UIDeviceFamily</key>
    <array>
        <integer>1</integer>
        <integer>2</integer>
    </array>
    <key>UISupportedInterfaceOrientations</key>
    <array>
        <string>UIInterfaceOrientationPortrait</string>
        <string>UIInterfaceOrientationLandscapeLeft</string>
        <string>UIInterfaceOrientationLandscapeRight</string>
    </array>
    <key>UISupportedInterfaceOrientations~ipad</key>
    <array>
        <string>UIInterfaceOrientationPortrait</string>
        <string>UIInterfaceOrientationPortraitUpsideDown</string>
        <string>UIInterfaceOrientationLandscapeLeft</string>
        <string>UIInterfaceOrientationLandscapeRight</string>
    </array>
</dict>
</plist>
```

## src/dotnet/Vyre.App/Platforms/iOS/Program.cs
```
namespace Vyre.App;

public static class Program
{
    static void Main(string[] args)
    {
        UIKit.UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
```

## src/dotnet/Vyre.App/Resources/AppIcon/appicon.svg
```
<svg width="512" height="512" viewBox="0 0 512 512" fill="none" xmlns="http://www.w3.org/2000/svg">
  <rect width="512" height="512" rx="112" fill="#111827"/>
  <path d="M128 124H192L256 252L320 124H384L282 388H230L128 124Z" fill="#22C55E"/>
</svg>
```

## src/dotnet/Vyre.App/Resources/AppIcon/appiconfg.svg
```
<svg width="512" height="512" viewBox="0 0 512 512" fill="none" xmlns="http://www.w3.org/2000/svg">
  <path d="M128 124H192L256 252L320 124H384L282 388H230L128 124Z" fill="#22C55E"/>
</svg>
```

## src/dotnet/Vyre.App/Resources/Images/diagnostics.svg
```
<svg width="256" height="256" viewBox="0 0 256 256" fill="none" xmlns="http://www.w3.org/2000/svg">
  <rect x="24" y="36" width="208" height="184" rx="20" fill="#121A2E" stroke="#23304B" stroke-width="8"/>
  <path d="M56 168H88L112 120L136 152L168 96L200 168" stroke="#22C55E" stroke-width="12" stroke-linecap="round" stroke-linejoin="round"/>
</svg>
```

## src/dotnet/Vyre.App/Resources/Splash/splash.svg
```
<svg width="256" height="256" viewBox="0 0 256 256" fill="none" xmlns="http://www.w3.org/2000/svg">
  <rect width="256" height="256" rx="56" fill="#111827"/>
  <path d="M64 60H96L128 124L160 60H192L141 196H115L64 60Z" fill="#22C55E"/>
</svg>
```

## src/dotnet/Vyre.App/Resources/Styles/Colors.xaml
```
<?xml version="1.0" encoding="UTF-8" ?>
<ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
    <Color x:Key="AccentColor">#22C55E</Color>
    <Color x:Key="PageBackgroundColor">#0B1020</Color>
    <Color x:Key="CardBackgroundColor">#121A2E</Color>
    <Color x:Key="PrimaryTextColor">#F8FAFC</Color>
    <Color x:Key="SecondaryTextColor">#94A3B8</Color>
    <Color x:Key="BorderColor">#23304B</Color>
</ResourceDictionary>
```

## src/dotnet/Vyre.App/Resources/Styles/Styles.xaml
```
<?xml version="1.0" encoding="UTF-8" ?>
<ResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
    <Style TargetType="Page">
        <Setter Property="BackgroundColor" Value="{StaticResource PageBackgroundColor}" />
    </Style>

    <Style TargetType="Label">
        <Setter Property="TextColor" Value="{StaticResource PrimaryTextColor}" />
        <Setter Property="FontFamily" Value="OpenSansRegular" />
    </Style>

    <Style TargetType="Button">
        <Setter Property="BackgroundColor" Value="{StaticResource AccentColor}" />
        <Setter Property="TextColor" Value="Black" />
        <Setter Property="CornerRadius" Value="12" />
        <Setter Property="Padding" Value="16,12" />
    </Style>

    <Style TargetType="Border">
        <Setter Property="StrokeShape">
            <Setter.Value>
                <RoundRectangle CornerRadius="16" />
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
```

## src/dotnet/Vyre.App/Services/Engine/IVyreEngineService.cs
```
using Vyre.App.Models;

namespace Vyre.App.Services.Engine;

public interface IVyreEngineService
{
    Task<NativeBuildInfo> GetBuildInfoAsync(CancellationToken cancellationToken = default);
}
```

## src/dotnet/Vyre.App/Services/Engine/NativeMethods.cs
```
using System.Runtime.InteropServices;
using System.Text;

namespace Vyre.App.Services.Engine;

internal static class NativeMethods
{
#if IOS
    internal const string LibraryName = "__Internal";
#else
    internal const string LibraryName = "vyre-interop";
#endif

    [DllImport(LibraryName, EntryPoint = "vyre_get_build_info", CallingConvention = CallingConvention.Cdecl)]
    private static extern int VyreGetBuildInfo(byte[] buffer, int bufferLength);

    internal static bool TryGetBuildInfo(out string value, out string error)
    {
        try
        {
            var buffer = new byte[512];
            var required = VyreGetBuildInfo(buffer, buffer.Length);

            if (required > buffer.Length)
            {
                buffer = new byte[required];
                required = VyreGetBuildInfo(buffer, buffer.Length);
            }

            value = DecodeUtf8(buffer);
            error = string.Empty;
            return !string.IsNullOrWhiteSpace(value) && required > 0;
        }
        catch (DllNotFoundException ex)
        {
            value = string.Empty;
            error = ex.Message;
            return false;
        }
        catch (EntryPointNotFoundException ex)
        {
            value = string.Empty;
            error = ex.Message;
            return false;
        }
        catch (BadImageFormatException ex)
        {
            value = string.Empty;
            error = ex.Message;
            return false;
        }
    }

    private static string DecodeUtf8(byte[] buffer)
    {
        var terminator = Array.IndexOf(buffer, (byte)0);
        if (terminator < 0)
        {
            terminator = buffer.Length;
        }

        return Encoding.UTF8.GetString(buffer, 0, terminator).Trim();
    }
}
```

## src/dotnet/Vyre.App/Services/Engine/VyreEngineService.cs
```
using Microsoft.Extensions.Logging;
using Vyre.App.Models;

namespace Vyre.App.Services.Engine;

public sealed class VyreEngineService(ILogger<VyreEngineService> logger) : IVyreEngineService
{
    public Task<NativeBuildInfo> GetBuildInfoAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (NativeMethods.TryGetBuildInfo(out var buildInfo, out var error))
        {
            logger.LogInformation("Native build info retrieved successfully.");
            return Task.FromResult(new NativeBuildInfo(
                IsNativeAvailable: true,
                LibraryName: NativeMethods.LibraryName,
                Message: buildInfo,
                Source: "native"));
        }

        logger.LogWarning("Native build info unavailable: {Reason}", error);
        return Task.FromResult(new NativeBuildInfo(
            IsNativeAvailable: false,
            LibraryName: NativeMethods.LibraryName,
            Message: string.IsNullOrWhiteSpace(error)
                ? "Native library is not packaged yet for this runtime. Module 1 keeps the bridge shape ready so later modules can drop in platform binaries cleanly."
                : error,
            Source: "managed-fallback"));
    }
}
```

## src/dotnet/Vyre.App/ViewModels/BaseViewModel.cs
```
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Vyre.App.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

## src/dotnet/Vyre.App/ViewModels/HomePageViewModel.cs
```
using System.Collections.ObjectModel;
using System.Windows.Input;
using Vyre.App.Core.Models;
using Vyre.App.Core.Services;
using Vyre.App.Services.Engine;

namespace Vyre.App.ViewModels;

public sealed class HomePageViewModel : BaseViewModel
{
    private readonly IVyreEngineService _engineService;
    private bool _isBusy;
    private string _headline = "Checking native bridge...";
    private string _detail = "The app shell is alive. We are validating the managed/native seam.";
    private string _statusPill = "BOOTSTRAP";
    private string _lastUpdated = "Not loaded yet.";

    public HomePageViewModel(IVyreEngineService engineService)
    {
        _engineService = engineService;
        RefreshCommand = new Command(async () => await RefreshAsync(), () => !IsBusy);

        Checklist = new ObservableCollection<string>
        {
            "Native engine is isolated under src/native and builds with CMake presets.",
            "Interop is the only supported boundary between MAUI and C++.",
            "Managed shared logic sits in Vyre.App.Core, not in page code-behind.",
            "VS Code tasks can bootstrap, build, test, and validate the repo in one place."
        };
    }

    public string Title => "Vyre";
    public string ModuleName => "Module 1 · Monorepo Foundation, Build System, and Dev Workflow";
    public ObservableCollection<string> Checklist { get; }
    public ICommand RefreshCommand { get; }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ((Command)RefreshCommand).ChangeCanExecute();
            }
        }
    }

    public string Headline
    {
        get => _headline;
        private set => SetProperty(ref _headline, value);
    }

    public string Detail
    {
        get => _detail;
        private set => SetProperty(ref _detail, value);
    }

    public string StatusPill
    {
        get => _statusPill;
        private set => SetProperty(ref _statusPill, value);
    }

    public string LastUpdated
    {
        get => _lastUpdated;
        private set => SetProperty(ref _lastUpdated, value);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var buildInfo = await _engineService.GetBuildInfoAsync(cancellationToken);
            var status = new EngineBridgeStatus(
                buildInfo.IsNativeAvailable,
                buildInfo.Source,
                buildInfo.LibraryName,
                buildInfo.Message);

            Headline = BootstrapStatusFormatter.FormatHeadline(status);
            Detail = BootstrapStatusFormatter.FormatDetail(status);
            StatusPill = buildInfo.IsNativeAvailable ? "NATIVE READY" : "BRIDGE STAGED";
            LastUpdated = $"Updated at {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

## src/dotnet/Vyre.App/Vyre.App.csproj
```
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0-android</TargetFrameworks>
    <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('OSX'))">$(TargetFrameworks);net10.0-ios</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <RootNamespace>Vyre.App</RootNamespace>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <ApplicationTitle>Vyre</ApplicationTitle>
    <ApplicationId>com.kairais.vyre</ApplicationId>
    <ApplicationDisplayVersion>0.1.0</ApplicationDisplayVersion>
    <ApplicationVersion>1</ApplicationVersion>
    <SupportedOSPlatformVersion Condition="'$(TargetFramework)' == 'net10.0-android'">24.0</SupportedOSPlatformVersion>
    <SupportedOSPlatformVersion Condition="'$(TargetFramework)' == 'net10.0-ios'">15.0</SupportedOSPlatformVersion>
    <NeutralLanguage>en</NeutralLanguage>
    <NoWarn>$(NoWarn);CA1416</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Vyre.App.Core\Vyre.App.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#111827" />
    <MauiSplashScreen Include="Resources\Splash\splash.svg" Color="#111827" BaseSize="128,128" />
    <MauiImage Include="Resources\Images\*" />
    <MauiXaml Update="**\*.xaml">
      <Generator>MSBuild:Compile</Generator>
    </MauiXaml>
  </ItemGroup>
</Project>
```

## src/native/tests/CMakeLists.txt
```
add_executable(vyre-native-smoke-tests
    engine_smoke_tests.cpp
)

target_link_libraries(vyre-native-smoke-tests
    PRIVATE
        vyre::core
        vyre::interop
)

if(UNIX)
    set_target_properties(vyre-native-smoke-tests PROPERTIES BUILD_RPATH "$ORIGIN")
endif()

add_test(NAME vyre-native-smoke-tests COMMAND vyre-native-smoke-tests)
```

## src/native/tests/engine_smoke_tests.cpp
```
#include "vyre/core/engine.hpp"
#include "vyre/interop/vyre_api.h"

#include <cassert>
#include <cstring>
#include <iostream>
#include <vector>

int main() {
    const auto info = vyre::core::Engine::GetBuildInfo();
    assert(info.product_name == "Vyre");
    assert(!info.version.empty());
    assert(!info.compiler.empty());
    assert(!info.platform.empty());

    std::vector<char> buffer(256, '\0');
    const int required = vyre_get_build_info(buffer.data(), static_cast<int>(buffer.size()));
    assert(required > 0);
    assert(std::strlen(buffer.data()) > 0);

    std::vector<char> version_buffer(32, '\0');
    const int version_required = vyre_get_version(version_buffer.data(), static_cast<int>(version_buffer.size()));
    assert(version_required > 0);
    assert(std::strlen(version_buffer.data()) > 0);

    std::cout << "Native smoke tests passed. Build info: " << buffer.data() << std::endl;
    return 0;
}
```

## src/native/vyre-core/CMakeLists.txt
```
add_library(vyre-core STATIC
    src/engine.cpp
)

add_library(vyre::core ALIAS vyre-core)

target_include_directories(vyre-core
    PUBLIC
        ${CMAKE_CURRENT_SOURCE_DIR}/include
)

target_compile_features(vyre-core PUBLIC cxx_std_20)

target_compile_definitions(vyre-core
    PUBLIC
        VYRE_CORE_VERSION_MAJOR=${PROJECT_VERSION_MAJOR}
        VYRE_CORE_VERSION_MINOR=${PROJECT_VERSION_MINOR}
        VYRE_CORE_VERSION_PATCH=${PROJECT_VERSION_PATCH}
)
```

## src/native/vyre-core/include/vyre/core/engine.hpp
```
#pragma once

#include <string>

namespace vyre::core {

struct BuildInfo final {
    std::string product_name;
    std::string version;
    std::string compiler;
    std::string platform;
    bool interop_enabled;
};

class Engine final {
public:
    static BuildInfo GetBuildInfo();
    static std::string GetBuildInfoString();

private:
    static std::string DetectCompiler();
    static std::string DetectPlatform();
};

} // namespace vyre::core
```

## src/native/vyre-core/include/vyre/core/version.hpp
```
#pragma once

#include <string_view>

namespace vyre::core {

struct Version final {
    static constexpr int Major = VYRE_CORE_VERSION_MAJOR;
    static constexpr int Minor = VYRE_CORE_VERSION_MINOR;
    static constexpr int Patch = VYRE_CORE_VERSION_PATCH;
    static constexpr std::string_view SemVer = "0.1.0";
};

} // namespace vyre::core
```

## src/native/vyre-core/src/engine.cpp
```
#include "vyre/core/engine.hpp"
#include "vyre/core/version.hpp"

#include <sstream>

#if defined(__APPLE__)
#include <TargetConditionals.h>
#endif

namespace vyre::core {

std::string Engine::DetectCompiler() {
#if defined(_MSC_VER)
    return "MSVC " + std::to_string(_MSC_VER);
#elif defined(__clang__)
    return "Clang " + std::to_string(__clang_major__) + "." + std::to_string(__clang_minor__);
#elif defined(__GNUC__)
    return "GCC " + std::to_string(__GNUC__) + "." + std::to_string(__GNUC_MINOR__);
#else
    return "UnknownCompiler";
#endif
}

std::string Engine::DetectPlatform() {
#if defined(__ANDROID__)
    return "Android";
#elif defined(__APPLE__) && defined(TARGET_OS_IPHONE) && TARGET_OS_IPHONE
    return "iOS";
#elif defined(__APPLE__)
    return "Apple";
#elif defined(_WIN32)
    return "Windows";
#elif defined(__linux__)
    return "Linux";
#else
    return "UnknownPlatform";
#endif
}

BuildInfo Engine::GetBuildInfo() {
    return BuildInfo{
        .product_name = "Vyre",
        .version = std::string(Version::SemVer),
        .compiler = DetectCompiler(),
        .platform = DetectPlatform(),
        .interop_enabled = true,
    };
}

std::string Engine::GetBuildInfoString() {
    const BuildInfo info = GetBuildInfo();

    std::ostringstream stream;
    stream << info.product_name
           << "/" << info.version
           << " | compiler=" << info.compiler
           << " | platform=" << info.platform
           << " | interop=" << (info.interop_enabled ? "enabled" : "disabled");

    return stream.str();
}

} // namespace vyre::core
```

## src/native/vyre-interop/CMakeLists.txt
```
set(VYRE_INTEROP_SOURCES
    src/vyre_api.cpp
)

if(APPLE AND CMAKE_SYSTEM_NAME STREQUAL "iOS")
    add_library(vyre-interop STATIC ${VYRE_INTEROP_SOURCES})
else()
    add_library(vyre-interop SHARED ${VYRE_INTEROP_SOURCES})
endif()

add_library(vyre::interop ALIAS vyre-interop)

target_include_directories(vyre-interop
    PUBLIC
        ${CMAKE_CURRENT_SOURCE_DIR}/include
)

target_link_libraries(vyre-interop
    PRIVATE
        vyre::core
)

target_compile_features(vyre-interop PRIVATE cxx_std_20)
set_target_properties(vyre-interop PROPERTIES OUTPUT_NAME "vyre-interop")
target_compile_definitions(vyre-interop PRIVATE VYRE_INTEROP_BUILDING)
```

## src/native/vyre-interop/include/vyre/interop/vyre_api.h
```
#pragma once

#if defined(_WIN32)
  #if defined(VYRE_INTEROP_BUILDING)
    #define VYRE_API __declspec(dllexport)
  #else
    #define VYRE_API __declspec(dllimport)
  #endif
#elif defined(__GNUC__) && __GNUC__ >= 4
  #define VYRE_API __attribute__((visibility("default")))
#else
  #define VYRE_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

// Returns the required UTF-8 byte count including the null terminator.
// If the provided buffer is too small, the output is truncated and still null-terminated.
VYRE_API int vyre_get_build_info(char* buffer, int buffer_length);
VYRE_API int vyre_get_version(char* buffer, int buffer_length);

#ifdef __cplusplus
}
#endif
```

## src/native/vyre-interop/src/vyre_api.cpp
```
#include "vyre/interop/vyre_api.h"

#include "vyre/core/engine.hpp"
#include "vyre/core/version.hpp"

#include <algorithm>
#include <cstring>
#include <string>

namespace {

int CopyToCallerBuffer(const std::string& value, char* buffer, const int buffer_length) {
    const int required_length = static_cast<int>(value.size()) + 1;

    if (buffer == nullptr || buffer_length <= 0) {
        return required_length;
    }

    const int copy_length = std::max(0, std::min(static_cast<int>(value.size()), buffer_length - 1));
    std::memcpy(buffer, value.data(), static_cast<std::size_t>(copy_length));
    buffer[copy_length] = '\0';
    return required_length;
}

} // namespace

extern "C" {

int vyre_get_build_info(char* buffer, const int buffer_length) {
    return CopyToCallerBuffer(vyre::core::Engine::GetBuildInfoString(), buffer, buffer_length);
}

int vyre_get_version(char* buffer, const int buffer_length) {
    return CopyToCallerBuffer(std::string(vyre::core::Version::SemVer), buffer, buffer_length);
}

} // extern "C"
```
