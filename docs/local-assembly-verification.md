# Local CS2 Assembly Verification

> Generated from installed assembly metadata without loading or executing game code.
> This local evidence takes precedence over wiki and public-mod examples for exact signatures.

This versioned report records evidence for a specific game build, not a
workstation configuration. Installation paths are redacted. Regenerate and
commit the report whenever the supported game build changes.

- Metadata generated at: `2026-07-23T14:21:32.9100802+00:00`
- Behavioral notes updated through: `2026-08-30`
- Managed directory: `(redacted; pass -IncludeManagedPath to include it)`
- Reported CS2 game version: `1.6.0f1`
- Cities2.exe product version: `2022.3.71f1 (c9bf13b0b844)`

## Assemblies

| File                       | Assembly version | File version | SHA-256                                                            |
| -------------------------- | ---------------- | ------------ | ------------------------------------------------------------------ |
| Game.dll                   | `0.0.0.0`        | `0.0.0.0`    | `721e7e17bf74299aa2b988c1bd07e90874bb8bc72d263229500c4bf639e7e4ee` |
| Unity.Entities.dll         | `0.0.0.0`        | `0.0.0.0`    | `2cbf58ed7edcb6f697e8f8134443789a17c0cec918364fdec01d184b76c2136b` |
| Unity.Collections.dll      | `0.0.0.0`        | `0.0.0.0`    | `146bfbd089ff31652efc34a586d1d5f405e22655f7da7c1b3c168093fffd2f04` |
| UnityEngine.CoreModule.dll | `0.0.0.0`        | `0.0.0.0`    | `ecc287942d2dd74b3d04dc181471b5beff11e9ac450a31e1d7a131f0b66a9bfd` |
| Colossal.Core.dll          | `0.0.0.0`        | `0.0.0.0`    | `c92d6f214c2edb66419b75bb663b06078f93066c819bfd02005886581338e2f2` |
| Colossal.Logging.dll       | `0.0.0.0`        | `0.0.0.0`    | `b076d59d6427cd90acf8b1ab731ee35a7612667e3a822d3211bcfa5c15021743` |
| Colossal.Localization.dll  | `0.0.0.0`        | `0.0.0.0`    | `54979aa458c25e5da40e40bcc25f9f4195207bb8e019660240892979c0557846` |
| Colossal.PSI.Common.dll    | `0.0.0.0`        | `0.0.0.0`    | `12463209f920d430ba8ab3d8921a1cf3605925d5bda3d2b6255642b0b055b061` |
| Colossal.UI.Binding.dll    | `0.0.0.0`        | `0.0.0.0`    | `9d27b3c0ae8fa0c1cefc50fe1926503c52f1b9d4d9dfe6e24f7fb4aee2446d2b` |

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
  `(saveName, previewUri, start, success)` for saves. Local call-site inspection
  finds its invocations only in the save state machine.
- `GameManager` converts `saveName` to an asset data path and uses that path for
  both `SaveGameData` and `SaveGameMetadata` assets.
- Local 1.6.0f1 IL for `GameManager.<Save>d__87.MoveNext` invokes the start
  callback with `start=true` before `WaitForGPUFrame` and before
  `SaveSimulationData`. A mod callback can therefore flush a provisional
  checkpoint before the game begins serializing the city.
- After the start callback, the same state machine writes the current session
  GUID and `DateTime.Now` to `SaveInfo.sessionGuid` and
  `SaveInfo.lastModified` before serialization. `SaveGameMetadata.target`
  publicly exposes that `SaveInfo`.
- Because the in-memory modification time is assigned before the package
  operation, it is not completion proof. The sidecar marks a pending record as
  completed and flushes that marker only after the success callback. Promotion
  then moves the marked record to its committed path. A marked record remains
  recoverable after restart if promotion fails; an unmarked record is never
  inferred to be successful from save metadata timestamps.
- The same state machine invokes the completion callback only after the package
  operation finishes and supplies the final success result.
- `Colossal.IO.AssetDatabase.AssetDatabase.AllAssets()` is public and returns
  `IEnumerable<IAssetData>`.
- `Game.Assets.SaveGameMetadata` is public and inherits the public asset `name`
  and `path` properties; `isValidSaveGame` is also public. `name` is the logical
  save identity supplied by the save callback, while `path` is the physical asset
  source and is not a compatible checkpoint identity. The live metadata names can
  therefore identify checkpoints whose associated saves no longer exist.
- `Game.Serialization.LoadGameSystem.dataDescriptor` is public and returns the
  `AsyncReadDescriptor` passed to `GameManager.LoadSimulationData`.
  `SaveInfo.saveGameData` and `AssetData.GetAsyncReadDescriptor()` are public.
  After load, matching those descriptors identifies the exact logical
  `SaveGameMetadata.name` without private access or patching.
- `Game.GameSystemBase` exposes `OnGamePreload(Purpose, GameMode)`,
  `OnGameLoaded(Context)`, and `OnDestroy()`.

A city session identifier is therefore stable across ordinary loads but is not
enough to distinguish separate save checkpoints. Production external state is
keyed by `{sessionGuid}/{simulationFrame}.{sha256(saveName)}.json`. Schema 5
stores the exact logical save name inside the checkpoint as a collision check.
Separate saves retain separate snapshots even when divergent branches reach the
same simulation frame. A pending checkpoint is captured and flushed before
serialization, durably marked after success, and then promoted. Successful
saves retain the current checkpoint for an overwritten save name. When live
save enumeration is trusted and includes the current save, cleanup removes
committed and confirmed-pending checkpoints for deleted saves. Unconfirmed
pending records older than seven days are also removed.

Runtime evidence from the `29-August-16-37-42` save confirms the frame can
advance between those boundaries: its completed checkpoint records frame
8,086,231, while its serialized `SaveGameData` contains frame 8,087,469. The
loader therefore permits a bounded drift of at most 4,096 frames, but only for
the same city session and exact logical save name. Exact matches remain
preferred. Future checkpoints and checkpoints more than 4,096 frames older are
not eligible.

Only schema 2 and schema 5 checkpoints are loadable. Schema 2 was published in
version 0.1.1 and remains discoverable through its frame-only filename. Schema 0
from version 0.1.0 and unpublished schemas 3 and 4 are ignored and left
untouched. Cleanup processes only supported checkpoints and remains isolated
from the completed game-save result.

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
  reset the population record, fractional XP, or total population XP earned.

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
- Local 1.6.0f1 IL for `XPSystem.XPQueueProcessJob.Execute` applies an `XPGain`
  by adding its amount directly to `m_XP`. The emergency save fallback writes
  the same persisted field before `SaveSimulationData`.

### `Game.City.MilestoneReachedEvent`

- Status: found
- Assembly: `Game`
- Base type: `System.ValueType`
- Declared fields:
  - `System.Int32 m_Index`
  - `Unity.Entities.Entity m_Milestone`

### Manual milestone claims boundaries

Local metadata confirms the remaining game-facing types used by manual
milestone claims:

- `Game.Simulation.MilestoneSystem` is a `GameSystemBase`. It declares the
  milestone-data, milestone-level, and city-XP queries plus the next-milestone,
  next-threshold, reached-event, and unlock-event state used by vanilla
  progression.
- `Game.City.MilestoneLevel.m_AchievedMilestone` is the achieved milestone
  index read by the adapter.
- `Game.Prefabs.MilestoneData.m_IsVictory` marks the terminal milestone.
  `MilestoneUISystem.GetVictoryMilestone` scans the milestone query for that
  flag. Manual claims use it as positive evidence before releasing final
  surplus XP.
- `MilestoneUISystem` builds its display catalog from entities with
  `PrefabData` and `MilestoneData`, excluding `Deleted` and `Temp`. Its
  `IsMaxMilestoneReached` result also requires that the equivalent query with
  `Locked` is empty. Manual claims mirror both query shapes and require the
  catalog's final marker and vanilla's locked-query result before reporting
  terminal completion or releasing final surplus XP.
- `Game.Prefabs.MilestonePrefab` exposes the milestone index, cumulative XP
  threshold, image, background color, accent color, and text color.
- `Game.UI.InGame.MilestoneUISystem` is a `UISystemBase`. Its declared bindings
  include achieved milestone, next-milestone XP, total XP, milestone details,
  unlock details, and XP-message events.
- `SystemUpdatePhase.UIUpdate` and `UpdateSystem.UpdateAt<T>` support the
  standalone UI system registration used by the mod.
- `ValueBinding<string>` and `TriggerBinding<T>` accept optional writer and
  reader arguments, matching the implemented value and request bindings.

Mono.Cecil inspection of this `Game.dll` established the behavior required by
the adapter:

- `MilestoneSystem.OnCreate` queries `MilestoneLevel`, `XP`, and
  `MilestoneData`.
- `MilestoneData.m_XpRequried` is the cumulative threshold for the achieved and
  next milestone.
- `MilestoneSystem.OnUpdate` compares city XP with the next threshold and
  increments `MilestoneLevel.m_AchievedMilestone` by one.
- The private `NextMilestone(int)` path creates the normal milestone-reached
  event and unlock work. Progression Controls can therefore change XP while
  leaving vanilla rewards and unlocks intact.
- `MilestoneSystem.TryGetMilestone` is private. The adapter uses the same
  read-only `MilestoneData` query shape instead of reflection.
- `PrefabSystem.GetPrefab<T>(Entity)` is public. The panel can read the native
  milestone image, background color, and text color without patching the
  vanilla milestone screen.

No installed game code was executed during this inspection.

## UI declaration verification, 2026-09-14

The runtime log reports `1.6.0f1 (419.d6c6) [6216.19404]`. Static inspection of
the installed `Cities2_Data/Content/Game/UI/index.js` supports the authored
declaration subset used by the linting baseline. Bundle SHA-256:
`AE8A9054B526C1EC2F0A3C84CB86713BF5A6F0536301BCC386BFD25A16923A55`.

- `window["cs2/ui"]` exports Button, Icon, Panel, and Scrollable. It does not
  export `FOCUS_DISABLED`. That value is exported by `window["cs2/input"]`;
  the mod imports it there and externalizes that module in webpack.
- The bundled React implementation reports `18.3.1`; the renderer also
  identifies the `18.3.1` release line. The dependency review retains React 18
  and matching typings because webpack resolves React and ReactDOM to the
  game's supplied instances.
- The focus-key implementation constructs the disabled key as a FocusSymbol
  with a string `debugName`, numeric `r`, and `toString` method.
- The public Button wrapper chooses the `flat`, `primary`, `round`, `menu`,
  `default`, `icon`, `floating`, or `text` theme. It forwards props to the
  underlying native button, which reads `focusKey`, `tooltipLabel`, `onClick`,
  and `onSelect`, and forwards remaining HTML attributes. The local declaration
  covers the button form used here, not every native variant.
- Icon reads `src`, `tinted`, `className`, and `children`; a string tint becomes
  a background color. Panel forwards its props to the native panel, including
  `header`, `contentClassName`, and HTML attributes. Scrollable reads boolean
  `vertical`/`horizontal`, `className`, and `children`.
- `cs2/l10n.useLocalization` resolves to `useCachedLocalization` in
  `game-ui/common/localization/localization.tsx`. Its `translate` property is
  an arrow function closing over the localization context. Destructuring it
  does not lose a receiver; the declaration uses a function-valued property.

Public discovery found the FocusSymbol/disabled-key pattern in
[CS2MultiplayerMod declarations](https://github.com/Rollocraft/CS2MultiplayerMod/blob/0fa5dedf9af9bcae2d8565a9138bc7ea4c2355d1/CS2MultiplayerMod/UI/types/bindings.d.ts),
last changed by that commit on 2026-07-07. GitHub reports its license as
`NOASSERTION`; no source was copied or executed. Public/generated declarations
were discovery leads only; the installed bundle governs these signatures.

The declarations are original, limited contracts for the mod's current usage.
This check did not execute the game bundle or replace in-game UI acceptance.
