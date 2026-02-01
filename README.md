# Vyre

Milestone 0: Repo bootstrap + CI + “Hello, cross-platform misery”.

## C++ core (Windows)
Use **Visual Studio 2022 Developer PowerShell** (x64), then:

```powershell
cmake --preset windows-debug
cmake --build --preset windows-debug --config Debug
ctest --preset windows-debug --output-on-failure
