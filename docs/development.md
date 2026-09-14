# Development

## Prerequisites

- Cities: Skylines II Modding Toolchain
- Unity Editor version required by the installed CS2 toolchain
- .NET SDK 8 for compilation
- A compatible runtime for the Colossal mod post-processor

The installed post-processor targets .NET 6. On a workstation with only .NET 8,
allow that process to roll forward to the installed major runtime.

## Full local build and deployment

From the repository root:

```powershell
$env:DOTNET_ROLL_FORWARD = 'Major'
dotnet build .\src\ProgressionControls\ProgressionControls.csproj --configuration Release
```

The official CS2 targets post-process the production assembly and deploy it to:

`%CSII_LOCALMODSPATH%\Kobbyist.ProgressionControls`

Close Cities: Skylines II before running this build because it writes directly
to the local mod directory.

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

## UI verification

From `src\ProgressionControls\UI`:

```powershell
npm.cmd exec tsc -- --noEmit
npm.cmd run build
```

The production build currently reports only the Dart Sass legacy JavaScript API
deprecation.

## Paradox Mods packaging

Publishing metadata, official publish profiles, offline staging, and the
approval boundary for publisher commands are documented in
[paradox-packaging.md](./paradox-packaging.md). The staging script validates
publisher inputs without authenticating or changing Paradox Mods state.
