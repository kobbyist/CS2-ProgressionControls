# Paradox Mods Packaging

The installed CS2 1.6.0f1 ModPublisher exposes only `Publish`, `NewVersion`, and
`Update`. Every command authenticates and changes Paradox Mods state; there is
no official offline/package-only command.

## Offline staging bundle

With CS2 closed, build the production project first:

```powershell
$env:DOTNET_ROOT = 'C:\Program Files\Unity 2022.3.62f2\Editor\Data\NetCoreRuntime'
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
dotnet build .\src\ProgressionControls\ProgressionControls.csproj `
  --configuration Release
```

Then create the audit bundle:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\scripts\build-paradox-staging.ps1
```

The script never invokes ModPublisher. It validates:

- required Private 0.1.0 listing metadata and the `Code Mod` tag;
- the 950x500 thumbnail;
- required managed and platform binaries;
- MIT license inclusion;
- absence of spike, widget, UI-module, and `mod.json` files; and
- SHA-256 hashes for every staged input.

It writes an ignored staging directory and ZIP below `artifacts/paradox/`.
This ZIP is an auditable copy of the publisher inputs, not a substitute format
accepted by Paradox Mods.

## Commands that change Paradox Mods

The official profiles are:

- `PublishNewMod` - creates the first listing; `ModId` is initially empty.
- `PublishNewVersion` - uploads a new binary version after `ModId` is assigned.
- `UpdatePublishedConfiguration` - changes listing metadata without a new
  binary version.

Do not invoke these profiles as part of routine builds. Publishing requires
explicit approval because it authenticates with the configured Paradox account
and changes external state.

The first uploaded listing must remain **Private** until it has been installed
from Paradox Mods and passed the same startup, settings, save, and removal smoke
checks as the local package.
