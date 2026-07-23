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
dotnet build .\src\ProgressionControls.Spike\ProgressionControls.Spike.csproj --configuration Release
```

The official CS2 targets post-process the assembly and deploy the passive spike
to:

`%CSII_LOCALMODSPATH%\Kobbyist.ProgressionControls.Spike`

## Compile-only verification

Use this when validating C# signatures without post-processing or deployment:

```powershell
dotnet build .\src\ProgressionControls.Spike\ProgressionControls.Spike.csproj `
  --configuration Release `
  --no-restore `
  -p:ModPublisherCommand=Update
```

The compile-only property uses the installed toolchain's existing `NeedBuild`
gate; it does not alter the project file.
