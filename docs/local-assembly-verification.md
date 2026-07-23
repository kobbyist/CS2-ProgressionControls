# Local CS2 Assembly Verification

> Generated from installed assembly metadata without loading or executing game code.
> This local evidence takes precedence over wiki and public-mod examples for exact signatures.

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
