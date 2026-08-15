# Local CS2 Assembly Verification

> Generated from installed assembly metadata without loading or executing game code.
> This local evidence takes precedence over wiki and public-mod examples for exact signatures.

This versioned report records evidence for a specific game build, not a
workstation configuration. Installation paths are redacted. Regenerate and
commit the report whenever the supported game build changes.

- Generated at: `2026-07-23T14:21:32.9100802+00:00`
- Managed directory: `(redacted; pass -IncludeManagedPath to include it)`
- Reported CS2 game version: `1.6.0f1`
- Cities2.exe product version: `2022.3.71f1 (c9bf13b0b844)`

## Assemblies

| File | Assembly version | File version | SHA-256 |
| --- | --- | --- | --- |
| Game.dll | `0.0.0.0` | `0.0.0.0` | `721e7e17bf74299aa2b988c1bd07e90874bb8bc72d263229500c4bf639e7e4ee` |
| Unity.Entities.dll | `0.0.0.0` | `0.0.0.0` | `2cbf58ed7edcb6f697e8f8134443789a17c0cec918364fdec01d184b76c2136b` |
| Unity.Collections.dll | `0.0.0.0` | `0.0.0.0` | `146bfbd089ff31652efc34a586d1d5f405e22655f7da7c1b3c168093fffd2f04` |
| UnityEngine.CoreModule.dll | `0.0.0.0` | `0.0.0.0` | `ecc287942d2dd74b3d04dc181471b5beff11e9ac450a31e1d7a131f0b66a9bfd` |
| Colossal.Core.dll | `0.0.0.0` | `0.0.0.0` | `c92d6f214c2edb66419b75bb663b06078f93066c819bfd02005886581338e2f2` |
| Colossal.Logging.dll | `0.0.0.0` | `0.0.0.0` | `b076d59d6427cd90acf8b1ab731ee35a7612667e3a822d3211bcfa5c15021743` |
| Colossal.Localization.dll | `0.0.0.0` | `0.0.0.0` | `54979aa458c25e5da40e40bcc25f9f4195207bb8e019660240892979c0557846` |
| Colossal.PSI.Common.dll | `0.0.0.0` | `0.0.0.0` | `12463209f920d430ba8ab3d8921a1cf3605925d5bda3d2b6255642b0b055b061` |
| Colossal.UI.Binding.dll | `0.0.0.0` | `0.0.0.0` | `9d27b3c0ae8fa0c1cefc50fe1926503c52f1b9d4d9dfe6e24f7fb4aee2446d2b` |

## API boundaries

### `Game.Modding.IMod`

- Status: found
- Assembly: `Game`
- Declared methods:
  - `System.Void OnDispose()`
  - `System.Void OnLoad(Game.UpdateSystem updateSystem)`

### `Game.UpdateSystem`

- Status: found
- Assembly: `Game`
- Base type: `Game.GameSystemBase`
- Declared methods:
  - `System.Void GetInterval(Unity.Entities.ComponentSystemBase system, Game.SystemUpdatePhase phase, System.Int32& interval, System.Int32& offset)`
  - `System.Void OnBeginFrame(UnityEngine.Rendering.ScriptableRenderContext renderContext, UnityEngine.Camera[] cameras)`
  - `System.Void OnCreate()`
  - `System.Void OnDestroy()`
  - `System.Void OnUpdate()`
  - `System.Void RegisterGPUSystem(Game.IGPUSystem system)`
  - `System.Void RegisterGPUSystem<SystemType>()`
  - `System.Void Update(Game.SystemUpdatePhase phase)`
  - `System.Void Update(Game.SystemUpdatePhase phase, System.UInt32 updateIndex, System.Int32 iterationIndex)`
  - `System.Void UpdateAfter<SystemType, OtherType>(Game.SystemUpdatePhase phase)`
  - `System.Void UpdateAfter<SystemType>(Game.SystemUpdatePhase phase)`
  - `System.Void UpdateAt<SystemType>(Game.SystemUpdatePhase phase)`
  - `System.Void UpdateBefore<SystemType, OtherType>(Game.SystemUpdatePhase phase)`
  - `System.Void UpdateBefore<SystemType>(Game.SystemUpdatePhase phase)`
- Declared properties:
  - `Game.SystemUpdatePhase currentPhase`

### `Unity.Entities.SystemBase`

- Status: found
- Assembly: `Unity.Entities`
- Base type: `Unity.Entities.ComponentSystemBase`

### `Game.GameSystemBase`

- Status: found
- Assembly: `Game`
- Base type: `Colossal.Entities.COSystemBase`

### `Game.UI.UISystemBase`

- Status: found
- Assembly: `Game`
- Base type: `Game.GameSystemBase`

### `Colossal.Localization.LocalizationManager`

- Status: found
- Assembly: `Colossal.Localization`
- Production lifecycle methods verified:
  - `System.Void AddSource(System.String localeId, Colossal.IDictionarySource source)`
  - `System.Void RemoveSource(System.String localeId, Colossal.IDictionarySource source)`

### `Game.Simulation.XPSystem`

- Status: found
- Assembly: `Game`
- Base type: `Game.GameSystemBase`
- Declared fields:
  - `Game.Simulation.CitySystem m_CitySystem`
  - `Game.Simulation.SimulationSystem m_SimulationSystem`
  - `TypeHandle __TypeHandle`
  - `Unity.Collections.NativeQueue<Game.Simulation.XPGain> m_XPQueue`
  - `Unity.Collections.NativeQueue<Game.Simulation.XPMessage> m_XPMessages`
  - `Unity.Jobs.JobHandle m_QueueWriters`
- Production queue methods verified:
  - `Unity.Collections.NativeQueue<Game.Simulation.XPGain> GetQueue(Unity.Jobs.JobHandle& dependencies)`
  - `System.Void AddQueueWriter(Unity.Jobs.JobHandle handle)`

### Save-checkpoint identity and lifecycle

Static metadata and IL inspection confirms:

- `Game.PSI.Telemetry.GetCurrentSession()` is public, static, parameterless,
  and returns `System.Guid`.
- `Game.Assets.SaveInfo.sessionGuid` is a serialized `System.Guid`; save code
  writes the current telemetry session and load code restores it.
- `Game.SceneFlow.GameManager.GetSessionGuid(Purpose, Guid)` creates a new
  identifier for `NewGame` and `NewMap`, while `LoadGame` preserves the saved
  identifier.
- `Game.Simulation.SimulationSystem.frameIndex` is a serialized `System.UInt32`.
- `Game.SceneFlow.GameManager.onGameSaveLoad` supplies
  `(saveName, previewUri, start, success)`, and `isGameLoading` is available to
  distinguish load callbacks.
- `GameManager` converts `saveName` to an asset data path and uses that path for
  both `SaveGameData` and `SaveGameMetadata` assets.
- The completion callback runs after the package save operation and reports its
  success result. This gives the mod a boundary for writing and pruning external
  state only after a completed save.
- `Colossal.IO.AssetDatabase.AssetDatabase.AllAssets()` is public and returns
  `IEnumerable<IAssetData>`.
- `Game.Assets.SaveGameMetadata` is public and inherits the public asset `name`
  and `path` properties; `isValidSaveGame` is also public. `name` is the logical
  save identity supplied by the save callback, while `path` is the physical asset
  source and is not a compatible checkpoint identity. The live metadata names can
  therefore identify checkpoints whose associated saves no longer exist.
- `Game.GameSystemBase` exposes `OnGamePreload(Purpose, GameMode)`,
  `OnGameLoaded(Context)`, and `OnDestroy()`.

A city session identifier is therefore stable across ordinary loads but is not
enough to distinguish separate save checkpoints. Production external state is
keyed by `{sessionGuid}/{simulationFrame}.json`, captured when saving starts,
and written only after the save succeeds. Schema version 2 records the exact
`saveName` in each new checkpoint. Successful saves then retain the current
checkpoint for an overwritten save name and, when live save enumeration is
trusted and includes the current save, remove indexed checkpoints for deleted
saves.

Legacy schema checkpoints remain loadable. Because they predate save-name
indexing, their cleanup uses a deterministic fallback: retain the newest 16 per
city session by last-write time, then simulation frame, then path. Cleanup is
best-effort and is isolated from the completed game-save result.

### Data path and evaluation cadence

- `Colossal.PSI.Environment.EnvPath.kUserDataPath` is a public static string.
  Production state is rooted below
  `ModsData/Kobbyist.ProgressionControls`.
- `Game.Simulation.TimeSystem.kTicksPerDay` is `262144`.
- `Game.Simulation.CountHouseholdDataSystem.GetUpdateInterval(...)` returns
  `16`; this is the locally verified city-population aggregation interval.
- The advanced cadence choices divide the vanilla day exactly: 16, 64, 256,
  1,024, 4,096, or 16,384 observations per day. The default 4,096/day runs
  every 64 frames. The 16,384/day maximum runs every 16 frames and therefore
  never samples faster than the vanilla population aggregate can update.
- Changing cadence affects only observation latency and batch size. It does not
  reset the population record, fractional XP, or total earned XP.

### Options UI slider metadata

The official
[Options UI guide](https://cs2.paradoxwikis.com/Options_UI) was consulted on
2026-07-25 for the public settings pattern. Exact 1.6.0f1 behavior was then
verified from the installed `Game.dll` metadata:

- `SettingsUISliderAttribute` supports `min`, `max`, `step`, `unit`,
  `scalarMultiplier`, `scaleDragVolume`, and `updateOnDragEnd`; the installed
  game uses it with both `System.Int32` and `System.Single` properties.
- `SettingsUICustomFormatAttribute` exposes `fractionDigits`,
  `separateThousands`, `maxValueWithFraction`, and `signed`. Its constructor
  defaults `fractionDigits` to zero, so float sliders require an explicit
  precision attribute when their displayed values include fractional steps.

### Settings persistence identity

Local 1.6.0f1 IL confirms the complete options-save path:

- Automatic settings callbacks for supported Boolean, integer, floating-point,
  and enum controls call `Setting.ApplyAndSave()`.
- `ApplyAndSave()` calls the virtual `Apply()` callback and then awaits
  `AssetDatabase.SaveSpecificSetting(GetType().Name)`.
- The target resolver compares `source.GetType().Name` values only. Namespace,
  mod identity, and `FileLocation` do not disambiguate two settings classes
  with the same simple name.
- The settings type is therefore named
  `KobbyistProgressionControlsSettings` and must remain globally unique.
  The existing `Kobbyist_ProgressionControls` asset name, `FileLocation`,
  serialized property names, and localization identifiers remain unchanged for
  compatibility with the existing settings file.

### Vanilla population XP reference

Public discovery used the
[PrefabDumpMax repository](https://github.com/CitiesSkylinesModding/PrefabDumpMax)
as provisional prefab evidence. Its 1.3.6f1 game-parameter dump reports
`m_XPPerPopulation = 48`. No repository license was detected, so no source or
content was copied into this project.

Local 1.6.0f1 metadata and IL remain authoritative for the implementation:

- `XPAccumulationSystem` divides population XP across
  `kUpdatesPerDay = 32` update slots and floors each individual gain.
- The nominal population rate is therefore `48 / 32 = 1.5` XP per new record
  resident. Vanilla batching can award less for small population changes due
  to per-update flooring; Progression Controls retains fractional XP instead.

### `Game.Simulation.XPReason`

- Status: found
- Assembly: `Game`
- Base type: `System.Enum`
- Declared fields:
  - `Game.Simulation.XPReason Count`
  - `Game.Simulation.XPReason ElectricityNetwork`
  - `Game.Simulation.XPReason Happiness`
  - `Game.Simulation.XPReason Income`
  - `Game.Simulation.XPReason Pipe`
  - `Game.Simulation.XPReason Population`
  - `Game.Simulation.XPReason PowerLine`
  - `Game.Simulation.XPReason Road`
  - `Game.Simulation.XPReason ServiceBuilding`
  - `Game.Simulation.XPReason ServiceUpgrade`
  - `Game.Simulation.XPReason SubwayTrack`
  - `Game.Simulation.XPReason TrainTrack`
  - `Game.Simulation.XPReason TramTrack`
  - `Game.Simulation.XPReason Unknown`
  - `Game.Simulation.XPReason Waterway`
  - `System.Int32 value__`

### `Game.Simulation.XPGain`

- Status: found
- Assembly: `Game`
- Base type: `System.ValueType`
- Declared fields:
  - `Game.Simulation.XPReason reason`
  - `System.Int32 amount`
  - `Unity.Entities.Entity entity`

### `Game.Simulation.XPMessage`

- Status: found
- Assembly: `Game`
- Base type: `System.ValueType`
- Declared fields:
  - `Game.Simulation.XPReason <reason>k__BackingField`
  - `System.Int32 <amount>k__BackingField`
  - `System.UInt32 <createdSimFrame>k__BackingField`

### `Game.Simulation.IXPSystem`

- Status: found
- Assembly: `Game`

### `Game.Prefabs.XPParameterData`

- Status: found
- Assembly: `Game`
- Base type: `System.ValueType`
- Declared fields:
  - `System.Single m_XPPerHappiness`
  - `System.Single m_XPPerPopulation`

### `Game.Prefabs.MilestoneData`

- Status: found
- Assembly: `Game`
- Base type: `System.ValueType`
- Declared fields:
  - `System.Boolean m_IsVictory`
  - `System.Boolean m_Major`
  - `System.Int32 m_DevTreePoints`
  - `System.Int32 m_Index`
  - `System.Int32 m_LoanLimit`
  - `System.Int32 m_MapTiles`
  - `System.Int32 m_Reward`
  - `System.Int32 m_XpRequried`

### `Game.City.Population`

- Status: found
- Assembly: `Game`
- Base type: `System.ValueType`
- Declared fields:
  - `System.Int32 m_AverageHappiness`
  - `System.Int32 m_AverageHealth`
  - `System.Int32 m_Population`
  - `System.Int32 m_PopulationWithMoveIn`

### `Game.City.XP`

- Status: found
- Assembly: `Game`
- Base type: `System.ValueType`
- Declared fields:
  - `Game.City.XPRewardFlags m_XPRewardRecord`
  - `System.Int32 m_MaximumIncome`
  - `System.Int32 m_MaximumPopulation`
  - `System.Int32 m_XP`

### `Game.City.MilestoneReachedEvent`

- Status: found
- Assembly: `Game`
- Base type: `System.ValueType`
- Declared fields:
  - `System.Int32 m_Index`
  - `Unity.Entities.Entity m_Milestone`
