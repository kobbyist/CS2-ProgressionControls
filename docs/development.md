# Development

## Prerequisites

- Cities: Skylines II Modding Toolchain
- Unity 2022.3.62f2, matching the current local toolchain
- .NET SDK 8 for compilation
- .NET 6 runtime for the Colossal mod post-processor

The matching Unity editor already supplies a usable .NET 6 runtime at:

`C:\Program Files\Unity 2022.3.62f2\Editor\Data\NetCoreRuntime`

## Full local build and deployment

From the repository root:

```powershell
$env:DOTNET_ROOT = 'C:\Program Files\Unity 2022.3.62f2\Editor\Data\NetCoreRuntime'
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
dotnet build .\src\ProgressionControls\ProgressionControls.csproj --configuration Release
```

The official CS2 targets post-process the production assembly and deploy it to:

`%CSII_LOCALMODSPATH%\Kobbyist.ProgressionControls`

The retired Phase 0 spike is preserved through its ADR and runtime checklist,
not as a second deployable project. The production mod is the repository's only
build target that references the CS2 toolchain.

## Compile-only verification

Use this when validating production C# signatures without post-processing or
deployment:

```powershell
dotnet build .\src\ProgressionControls\ProgressionControls.csproj `
  --configuration Release `
  --no-restore `
  -p:ModPublisherCommand=Update
```

The compile-only property uses the installed toolchain's existing `NeedBuild`
gate; it does not alter the project file.

## Core tests

```powershell
dotnet test .\tests\ProgressionControls.Core.Tests\ProgressionControls.Core.Tests.csproj `
  --configuration Release
```

## Paradox Mods packaging

Publishing metadata, official publish profiles, offline staging, and the
approval boundary for publisher commands are documented in
[paradox-packaging.md](./paradox-packaging.md). The staging script validates
publisher inputs without authenticating or changing Paradox Mods state.
