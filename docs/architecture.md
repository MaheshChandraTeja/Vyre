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
