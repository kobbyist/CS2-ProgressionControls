# Local CS2 Assembly Verification

> Generated from installed assembly metadata without loading or executing game code.
> This local evidence takes precedence over wiki and public-mod examples for exact signatures.

- Generated at: `2026-08-22T16:02:32.5473648+00:00`
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
| Colossal.Logging.dll | `0.0.0.0` | `0.0.0.0` | `b076d59d6427cd90acf8b1ab731ee35a7612667e3a822d3211bcfa5c15021743` |
| Colossal.Localization.dll | `0.0.0.0` | `0.0.0.0` | `54979aa458c25e5da40e40bcc25f9f4195207bb8e019660240892979c0557846` |
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

### `Game.Simulation.MilestoneSystem`

- Status: found
- Assembly: `Game`
- Base type: `Game.GameSystemBase`
- Declared fields:
  - `Game.Common.ModificationEndBarrier m_ModificationEndBarrier`
  - `Game.Simulation.CitySystem m_CitySystem`
  - `Game.Tutorials.TutorialSystem m_TutorialSystem`
  - `System.Int32 m_LastRequired`
  - `System.Int32 m_NextMilestone`
  - `System.Int32 m_NextRequired`
  - `System.Int32 m_Progress`
  - `Unity.Entities.EntityArchetype m_MilestoneReachedEventArchetype`
  - `Unity.Entities.EntityArchetype m_UnlockEventArchetype`
  - `Unity.Entities.EntityQuery m_MilestoneGroup`
  - `Unity.Entities.EntityQuery m_MilestoneLevelGroup`
  - `Unity.Entities.EntityQuery m_XPGroup`

### `Game.Simulation.IMilestoneSystem`

- Status: found
- Assembly: `Game`

### `Game.UI.InGame.MilestoneUISystem`

- Status: found
- Assembly: `Game`
- Base type: `Game.UI.UISystemBase`
- Declared fields:
  - `Colossal.UI.Binding.GetterValueBinding<System.Boolean> m_MaxMilestoneReachedBinding`
  - `Colossal.UI.Binding.GetterValueBinding<System.Boolean> m_UnlockAllBinding`
  - `Colossal.UI.Binding.GetterValueBinding<System.Boolean> m_reachedPopulationGoalBinding`
  - `Colossal.UI.Binding.GetterValueBinding<System.Boolean> m_victoryPopupShownBinding`
  - `Colossal.UI.Binding.GetterValueBinding<System.Int32> m_AchievedMilestoneBinding`
  - `Colossal.UI.Binding.GetterValueBinding<System.Int32> m_AchievedMilestoneXPBinding`
  - `Colossal.UI.Binding.GetterValueBinding<System.Int32> m_NextMilestoneXPBinding`
  - `Colossal.UI.Binding.GetterValueBinding<System.Int32> m_TotalXPBinding`
  - `Colossal.UI.Binding.RawEventBinding m_XpMessageAddedBinding`
  - `Colossal.UI.Binding.RawMapBinding<Unity.Entities.Entity> m_MilestoneDetailsBinding`
  - `Colossal.UI.Binding.RawMapBinding<Unity.Entities.Entity> m_MilestoneUnlocksBinding`
  - `Colossal.UI.Binding.RawMapBinding<Unity.Entities.Entity> m_UnlockDetailsBinding`
  - `Colossal.UI.Binding.RawValueBinding m_MilestonesBinding`
  - `Colossal.UI.Binding.ValueBinding<Unity.Entities.Entity> m_UnlockedMilestoneBinding`
  - `Game.City.CityConfigurationSystem m_CityConfigurationSystem`
  - `Game.Prefabs.PrefabSystem m_PrefabSystem`
  - `Game.Simulation.CitySystem m_CitySystem`
  - `Game.Simulation.IMilestoneSystem m_XpMilestoneSystem`
  - `Game.Simulation.IXPSystem m_XPSystem`
  - `Game.Tutorials.TutorialSystem m_TutorialSystem`
  - `Game.UI.ImageSystem m_ImageSystem`
  - `System.Boolean m_reachedPopulationGoal`
  - `System.Boolean m_setVictoryPopupShown`
  - `System.Boolean m_victoryPopupShown`
  - `System.String kGroup`
  - `Unity.Entities.EntityArchetype m_UnlockEventArchetype`
  - `Unity.Entities.EntityQuery m_DevTreeNodeQuery`
  - `Unity.Entities.EntityQuery m_LockedMilestoneQuery`
  - `Unity.Entities.EntityQuery m_MilestoneLevelQuery`
  - `Unity.Entities.EntityQuery m_MilestoneQuery`
  - `Unity.Entities.EntityQuery m_MilestoneReachedEventQuery`
  - `Unity.Entities.EntityQuery m_ModifiedMilestoneQuery`
  - `Unity.Entities.EntityQuery m_PopulationVictoryConfigQuery`
  - `Unity.Entities.EntityQuery m_UnlockableAssetQuery`
  - `Unity.Entities.EntityQuery m_UnlockableFeatureQuery`
  - `Unity.Entities.EntityQuery m_UnlockablePolicyQuery`
  - `Unity.Entities.EntityQuery m_UnlockableZoneQuery`

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

### `Game.Prefabs.MilestonePrefab`

- Status: found
- Assembly: `Game`
- Base type: `Game.Prefabs.PrefabBase`
- Declared fields:
  - `System.Boolean m_IsVictory`
  - `System.Boolean m_Major`
  - `System.Int32 m_DevTreePoints`
  - `System.Int32 m_Index`
  - `System.Int32 m_LoanLimit`
  - `System.Int32 m_MapTiles`
  - `System.Int32 m_Reward`
  - `System.Int32 m_XpRequried`
  - `System.String m_Image`
  - `UnityEngine.Color m_AccentColor`
  - `UnityEngine.Color m_BackgroundColor`
  - `UnityEngine.Color m_TextColor`

### `Game.City.MilestoneLevel`

- Status: found
- Assembly: `Game`
- Base type: `System.ValueType`
- Declared fields:
  - `System.Int32 m_AchievedMilestone`

### `Game.City.MilestoneReachedEvent`

- Status: found
- Assembly: `Game`
- Base type: `System.ValueType`
- Declared fields:
  - `System.Int32 m_Index`
  - `Unity.Entities.Entity m_Milestone`

### `Game.City.XP`

- Status: found
- Assembly: `Game`
- Base type: `System.ValueType`
- Declared fields:
  - `Game.City.XPRewardFlags m_XPRewardRecord`
  - `System.Int32 m_MaximumIncome`
  - `System.Int32 m_MaximumPopulation`
  - `System.Int32 m_XP`

### `Game.City.XPRewardFlags`

- Status: found
- Assembly: `Game`
- Base type: `System.Enum`
- Declared fields:
  - `Game.City.XPRewardFlags ElectricityGridBuilt`
  - `System.Byte value__`

## Manual milestone claims behavioral verification

Mono.Cecil inspection of the installed Game.dll established the behavior
needed by the adapter:

- MilestoneSystem.OnCreate queries MilestoneLevel, XP, and MilestoneData.
- MilestoneData.m_XpRequried is read as the cumulative threshold for the
  achieved and next milestone.
- MilestoneSystem.OnUpdate compares CitySystem.XP with the next threshold and
  increments MilestoneLevel.m_AchievedMilestone by one.
- The private NextMilestone(int) path creates the normal milestone-reached
  event and unlock work. The mod therefore changes XP only and leaves vanilla
  rewards and unlocks intact.
- MilestoneSystem.TryGetMilestone is private, so the implementation uses the
  same read-only MilestoneData query shape instead of reflection.
- PrefabSystem.GetPrefab<T>(Entity) is public. MilestonePrefab.m_Image plus its
  background and text colors allow the standalone panel to reuse the native
  milestone artwork and card palette. The XP range uses the native green fill
  on a dark track. Its solid readout sits on the card background because
  Gameface renders overlapping clipped labels poorly at this compact size.
- SystemUpdatePhase.UIUpdate and UpdateSystem.UpdateAt<T> are present for
  supported UISystemBase registration.
- ValueBinding<string> and TriggerBinding<T> constructors accept their
  writer/reader arguments as optional, matching the implemented binding
  surface.

No installed game code was executed during this verification.
