# Development

## Repository layout

| Location                                                  | Responsibility                                                                                                              |
| --------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `src/ProgressionControls.Core/`                           | Game-independent configuration, population tracking, and XP calculations.                                                   |
| `src/ProgressionControls.Core/ManualMilestones/`          | Held-XP banking, pending claims, and milestone catalog rules.                                                               |
| `src/ProgressionControls.Core/Persistence/`               | Checkpoint snapshots, filesystem storage, and retention.                                                                    |
| `src/ProgressionControls/`                                | Mod startup, settings, localization, and the simulation adapter. Both `ProgressionControlSystem` partials stay together.    |
| `src/ProgressionControls/Bindings/`                       | C# UI binding registration, publication state, and JSON serialization.                                                      |
| `src/ProgressionControls/UI/src/`                         | UI registration, compact panel components, and the mod glyph.                                                               |
| `src/ProgressionControls/UI/src/manual-milestone-claims/` | Manual-claims rendering and styles. `bindings.ts` owns binding names, transport types, state parsing, and request wrappers. |
| `src/ProgressionControls/UI/types/`                       | Authored declarations for assets and the verified CS2 API subset.                                                           |
| `src/ProgressionControls/Properties/`                     | Publishing metadata, profiles, thumbnail, and screenshots.                                                                  |
| `tests/ProgressionControls.Core.Tests/`                   | Core tests, with matching `ManualMilestones/` and `Persistence/` groups. Persistence tests also exercise temporary files.   |
| `scripts/`                                                | Repository verification and offline packaging commands.                                                                     |
| `docs/`                                                   | Development, design, packaging, dependency, and assembly-verification guidance.                                             |
| `artifacts/`                                              | Ignored validation output and staging bundles.                                                                              |

The game project references Core. Keep game and Unity dependencies in the game
project, and keep progression calculations independent of filesystem operations.
Core includes persistence so checkpoints can be tested without a game installation.
Its folders describe responsibilities within the existing assembly.

C# namespaces and assembly names remain stable across these folders. In particular,
moving source files must not change the published settings identity. Manual-mode
and recovery decisions still live with the simulation partial; extracting their
policy is a separate follow-up after the current correctness checks are settled.

The UI package stays inside the game project because its build target produces
and copies that package's bundle. Keep UI-specific tooling beside `package.json`
and repository-wide configuration at the root. Build output, dependencies, and
`.agents/memory.md` remain ignored.

## Prerequisites

- Cities: Skylines II Modding Toolchain
- Unity Editor version required by the installed CS2 toolchain
- .NET SDK 8.0.422 or a later servicing patch in the 8.0.4xx band, selected by `global.json`
- Node 24.x; CI selects the latest patch in that LTS line from `.node-version`
- PowerShell 7.2+ for the shared verification script
- A compatible runtime for the Colossal mod post-processor

The installed post-processor targets .NET 6. On a workstation with only .NET 8,
allow that process to roll forward to the installed major runtime.

## Install and verify

From the repository root:

```powershell
npm.cmd ci --prefix src/ProgressionControls/UI
pwsh -NoProfile -File ./scripts/verify.ps1
```

Verification checks web/config/document formatting, typed ESLint, TypeScript,
the production UI bundle, C# whitespace, Core tests, and Git whitespace. It
stops at the first failed command. C# compiler and adopted analyzer warnings
fail the build. UI lint and bundle warnings also fail verification.

The default command needs no game installation. On a workstation with the CS2
toolchain, include the production C# compile check:

```powershell
pwsh -NoProfile -File ./scripts/verify.ps1 -IncludeGame
```

This uses the existing compile-only gate and does not post-process or deploy.
The GitHub Actions `Verify` workflow runs the default command on Windows for
pull requests and pushes to `main`. The game compile check remains a local
requirement before delivering game-facing changes.

## Formatting and lint policy

EditorConfig owns indentation and LF line endings; Git attributes use the same
line-ending policy. Prettier formats authored JS/TS/TSX, JSON, SCSS, YAML, and
Markdown. Markdown prose keeps its existing wrapping. Generated output,
dependencies, lockfiles, and local continuity are excluded. Authored game
declarations remain checked.

From `src/ProgressionControls/UI`:

```powershell
npm.cmd run format
npm.cmd run format:check
npm.cmd run lint
npm.cmd run lint:fix
npm.cmd run typecheck
npm.cmd run check
```

The format commands also cover root JSON/Markdown, documentation, workspace
settings, and CI YAML. C# formatting uses the SDK formatter. To apply it from
the repository root without loading game build targets:

```powershell
$csharpFiles = @(git -c core.quotepath=false ls-files --cached --others --exclude-standard -- '*.cs')
$csharpFiles = @($csharpFiles | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf })
dotnet format whitespace . --folder --include $csharpFiles
```

The shared verification script uses the same file scope with
`--verify-no-changes`. Formatting and lint fixes are explicit commands;
verification never rewrites source files.

The .NET SDK's `8.0-recommended` analyzer baseline supplements the existing
Unity analyzers. CA1822 and CA1859 remain editor suggestions because static
methods and concrete collection return types are API-design choices. CA1716
is disabled only for the conventional `Mod` entry point. CA1861 is disabled
only in tests to keep input arrays local to each test. Other adopted warnings
are errors. Nullable reference checking remains enabled in tests; migration
of the production projects is a separate task.

ESLint uses the recommended JavaScript and type-checked TypeScript presets,
plus the Hooks ordering and dependency rules. Webpack/config JavaScript has
its own Node scope. Formatting rules do not overlap Prettier, and unused lint
suppression directives fail the check. The broader React Compiler rules are
not part of this baseline. New exceptions need a specific reason and the
narrowest useful scope.

TypeScript also checks indexed accesses, exact optional properties, return
paths, switch fallthrough, and declaration files. The local CS2 declarations
describe only the API subset this mod uses; verify new signatures against the
installed game before expanding them.

Opening the repository root in VS Code selects the local Prettier installation
and the UI ESLint working directory. Workspace settings disable overlapping
Biome/Oxc actions and select C# and PowerShell formatters by language. The
`Verify` task runs the portable checks; `Verify including game compile` adds
the local toolchain check. CLI checks remain authoritative in other editors.

Most tool versions are exact npm development dependencies. `typescript-eslint`
uses a caret range so compatible 8.x updates are allowed; the lockfile records
the resolved version and `npm ci` keeps installations reproducible. The UI
`.npmrc` rejects unsupported Node versions. Check peer compatibility when
updating the SDK, analysis level, compiler, or linter. Runtime React stays
externalized to the game.

TypeScript 6 uses bundler module resolution. The `mod.json` path alias is
relative to the UI configuration directory, and no deprecated `baseUrl` or
Node10 resolution setting is needed.

The [dependency review](dependency-review.md) records the September 2026
updates, compatibility holds, and upcoming .NET servicing work.

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
npm.cmd run typecheck
npm.cmd run build
```

The production build uses Sass's modern API and fails on warnings.

## Paradox Mods packaging

Publishing metadata, official publish profiles, offline staging, and the
approval boundary for publisher commands are documented in
[paradox-packaging.md](./paradox-packaging.md). The staging script validates
publisher inputs without authenticating or changing Paradox Mods state.
