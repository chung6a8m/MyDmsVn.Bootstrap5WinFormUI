# Supported Build Environment

## Developer environment

The supported development/build baseline for the v1 release line is:

- Windows 10 or Windows 11.
- Visual Studio 2022 version 17.8 or newer with the **.NET desktop development** workload.
- .NET Framework 4.8 Developer/Targeting Pack.
- .NET 8 SDK. `global.json` establishes `8.0.100` as the minimum feature baseline and permits roll-forward to the latest installed .NET 8 feature band.
- PowerShell 7 (`pwsh`) is recommended for repository scripts and is the shell used by CI.

Visual Studio 2022 17.8 is the minimum because it is the first VS 2022 release with .NET 8/C# 12 support. Newer Visual Studio versions are acceptable when they provide the same required workloads/targeting packs and honor the repository SDK policy.

## CI environment

CI runs on GitHub Actions `windows-latest`, installs .NET SDK `8.0.x`, then executes the repository scripts rather than IDE-specific build commands.

During Phase 16 RED verification on 2026-08-28, GitHub Actions resolved:

- Windows Server 2025 runner image (`windows-2025-vs2026`).
- .NET SDK `8.0.424`.

Those hosted-runner details are evidence for the current CI execution, not a requirement that contributors install Visual Studio 2026. The supported developer baseline remains Visual Studio 2022 17.8+ with the .NET 8 SDK and .NET Framework 4.8 targeting pack.

## Canonical commands

```powershell
pwsh ./build.ps1 -Configuration Release
pwsh ./test.ps1 -Configuration Release -SkipBuild
pwsh ./release.ps1 -Configuration Release -Version 1.0.0-rc.1 -SkipBuild -SkipTests
```

`build.ps1` builds the core library, tests, and demo for both `net48` and `net8.0-windows`. `test.ps1` runs the automated suite for both targets. `release.ps1` produces and validates the NuGet package and symbols package.
