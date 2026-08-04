# Progression Controls — Software Requirements Specification

**Status:** MVP release candidate
**Version:** 1.0
**Date:** 2026-08-04
**Game:** Cities: Skylines II
**Verified build:** 1.6.0f1
**Support policy:** Latest public game version at build and release time
**Author/publisher:** kobbyist
**Namespace:** `Kobbyist.ProgressionControls`
**License:** MIT
**Distribution:** Paradox Mods and local development builds

## 1. Purpose

Vanilla milestone progression is too fast for players who want city growth to
matter more. Progression Controls slows and reshapes XP earning by letting the
player:

- award configurable XP for population growth;
- scale positive XP already present in the shared game queue from 0% to 100%;
- use population as the dominant or sole progression source.

The MVP changes only how XP is earned. It does not change the 20 vanilla
milestones, XP thresholds, rewards, development points, loan limits, or unlocks.

[City Watchdog](https://github.com/River-Mochi/CS2-CityWatchdog) is a design
reference for simple settings, clear explanations, localization, and safe
game-native behavior. Progression Controls is an original implementation.

## 2. MVP Behavior

### 2.1 Population XP

- The mod awards XP when the city exceeds its historical maximum population.
- The award equals the new-record population delta multiplied by the configured
  XP-per-resident rate.
- Population decline never removes XP or milestones.
- After a decline, population XP resumes only after the previous record is
  exceeded.
- Fractional XP is accumulated deterministically rather than discarded.
- Record detection and XP submission use separate cadences.
- Earned population XP is batched into at most 1, 4, 16, 64, or 256 awards and
  notifications per in-game day; the default is 16.
- Pending population XP is persisted at the exact save checkpoint, so batching
  changes notification timing without changing total earned XP.

The default rate is derived from the game’s runtime Megalopolis XP requirement
so a population-only city reaches Megalopolis at approximately 200,000
population.

The player can edit either:

- XP per new resident; or
- the projected population-only Megalopolis target.

These are linked values. Changing one recalculates the other.

### 2.2 Vanilla XP

The MVP provides one global Vanilla XP multiplier from 0% to 100%. The label is
player-facing shorthand for the verified shared XP queue boundary:

- every positive `XPGain` already queued when Progression Controls runs is
  scaled;
- the mod's population XP is appended after scaling and is not scaled again;
- `XPGain` exposes reason, amount, and entity but no producer identity; and
- another mod's XP may be scaled or unscaled depending on update order, so XP
  and milestone mods are unsupported combinations.

- `0%` suppresses positive gains already in the queue and enables
  population-only progression for supported configurations.
- `100%` preserves queued positive XP.
- The default is `25%`.

Post-MVP versions may expose a separate multiplier for every verified vanilla XP
reason.

### 2.3 Enablement and changes

- Custom progression is enabled by default after installation.
- It can be enabled or disabled in the mod Options.
- Disabling it stops population XP and leaves future shared-queue gains
  unscaled.
- Disabling flushes population XP already earned while enabled as one final
  batch before the integration becomes dormant.
- While disabled, the mod performs no recurring population observations, XP
  queue interception, or external progression checkpoint writes.
- Growth while disabled receives no population XP.
- Re-enabling uses the greatest of the stored population record, current
  population, or verified base-game maximum-population record as its baseline,
  with no retroactive award.
- Rate, target, multiplier, and preset changes affect future XP only.
- Existing XP, milestones, rewards, and unlocks are never recalculated.

### 2.4 Existing cities

The first time Progression Controls runs in an existing city, the greater of
current population and the reliable base-game maximum-population record becomes
the baseline. Installation or enablement does not grant retroactive XP.

## 3. Settings and Presets

All MVP configuration is global and stored through the mod’s Options settings.
There are no per-city settings, saved user profiles, imports, or exports.

| Setting | Behavior |
| --- | --- |
| Enable custom progression | Enables or disables all custom rules |
| Preset | Applies Population Balanced, Population Heavy, or Population Only immediately; the right-side help panel explains each preset and advanced customization |
| XP per new resident | Advanced numeric text input that stages the population XP rate |
| Megalopolis population target | Advanced linked numeric text alternative that stages the population target |
| Vanilla XP multiplier | Advanced integer slider that stages vanilla XP scaling from 0% to 100% |
| Apply custom rules | Validates and atomically applies the three staged XP rule values |
| Population update responsiveness | Advanced dropdown controlling 16 to 16,384 observations per in-game day; defaults to 4,096 |
| Population XP notification frequency | Advanced dropdown batching earned population XP into at most 1 to 256 awards per in-game day; defaults to 16 |
| Restore defaults | Restores the Population Heavy defaults |

Built-in presets are immutable:

| Preset | Population XP | Vanilla XP |
| --- | --- | ---: |
| Population Balanced | Default 200,000 target | 50% |
| Population Heavy | Default 200,000 target | 25% |
| Population Only | Default 200,000 target | 0% |

Applying an advanced-rule edit changes the displayed selection to **Custom**.
Partially typed or otherwise unsubmitted values never change the running
configuration.

## 4. Save and Removal Safety

- The mod must not serialize custom components or required mod data into the
  city save.
- XP and milestones earned while the mod is active become normal base-game
  state and remain after removal.
- A city must load without Progression Controls and continue with vanilla
  progression.
- Disabling or removing the mod requires no save conversion.

The installed game exposes `Game.City.XP.m_MaximumPopulation`. The implementation
should use this base-game high-water value if runtime testing confirms it remains
reliable while vanilla XP is scaled.

If external per-city state is still required, it must be limited to:

- stable city session identifier and serialized simulation frame;
- maximum observed population;
- fractional population XP and vanilla-scaling remainders; and
- population XP earned but not yet submitted as a notification batch.

Missing or invalid tracking state uses the greater of current population and
the verified base-game maximum-population record as a fresh baseline and never
grants retroactive XP. The serialized simulation frame distinguishes multiple
save checkpoints that share one city session identifier.

The MVP does not detect or arbitrate conflicts with other XP or milestone mods.
Compatibility is the user’s responsibility, and support testing may require
disabling competing progression mods.

## 5. Functional Requirements

| ID | Requirement |
| --- | --- |
| FR-01 | Custom progression shall be globally enableable and enabled by default. |
| FR-02 | Population XP shall be awarded only for new historical population highs. |
| FR-03 | Population decline shall not remove XP or revoke milestones. |
| FR-04 | The population rate and projected Megalopolis target shall be linked editable values. |
| FR-05 | The Vanilla XP multiplier shall support every integer percentage from 0% to 100% and scale every positive gain already present in the shared XP queue. |
| FR-06 | The initial configuration shall use Population Heavy with 25% Vanilla XP. |
| FR-07 | Presets shall apply immediately; advanced XP rule edits shall apply atomically on confirmation; both shall affect future XP only. |
| FR-08 | Existing cities shall use the greater of current population and the reliable base-game maximum-population record as the initial baseline. |
| FR-09 | The three built-in presets shall be available as defined above. |
| FR-10 | Invalid or non-finite settings shall be rejected or replaced with safe defaults. |
| FR-11 | Vanilla milestone thresholds, rewards, tiers, and unlocks shall remain unchanged. |
| FR-12 | The city save shall not require Progression Controls to load. |
| FR-13 | All player-facing text shall use localization keys. |
| FR-14 | Advanced controls shall offer validated population observation cadences from 16 to 16,384 per in-game day, defaulting to 4,096. |
| FR-15 | Advanced controls shall offer save-safe population XP batching from 1 to 256 awards per in-game day, defaulting to 16, without changing total earned XP. |

## 6. Quality Requirements

- **Performance:** Population evaluation runs outside UI rendering at the
  selected validated cadence. The maximum cadence matches the vanilla city
  population aggregation interval of 16 simulation frames; it never polls
  faster than new aggregate data can become available. Each observation reads
  one city population component and does not iterate citizens or buildings.
  XP interception makes one pass over pending XP events, so its variable work
  depends on event count rather than city population. Population XP batching
  reduces custom queue events and notification work at lower frequencies.
- **Determinism:** Identical observations and settings produce identical XP,
  including fractional accumulation.
- **Testability:** Population rules, target/rate conversion, validation, and
  preset application live in a pure C# domain layer without CS2, Unity, ECS,
  Harmony, UI, localization, or filesystem dependencies.
- **Failure safety:** Missing or invalid game data skips the affected award,
  emits a bounded diagnostic, and never guesses an XP value.
- **Logging:** Log lifecycle, rejected configuration, adapter failures, and XP
  application without per-frame noise.
- **Localization:** Version 1 ships in English, with all text structured for
  later translation.
- **Cleanup:** Systems, settings, event subscriptions, localization sources, and
  patches are unregistered or disposed on unload.

## 7. Architecture and Verified Game Boundaries

The implementation consists of:

1. **Domain core** — population delta, fractional XP, validation, presets, and
   target/rate conversion.
2. **Game adapters** — current population, maximum population, milestone XP
   requirements, queued XP events, and XP submission.
3. **Progression coordinator** — evaluates population and applies configured
   scaling on a controlled cadence.
4. **Settings/state adapter** — global Options settings and minimal external
   state when necessary.

Local 1.6.0f1 assembly verification confirms:

- `Game.Modding.IMod.OnLoad(Game.UpdateSystem)` and `OnDispose()`;
- `Game.PSI.Telemetry.GetCurrentSession()` and serialized
  `Game.Assets.SaveInfo.sessionGuid`;
- serialized `Game.Simulation.SimulationSystem.frameIndex`;
- `Game.Simulation.XPGain` with amount, reason, and entity;
- `Game.Simulation.XPMessage`;
- `Game.Prefabs.XPParameterData` with population and happiness rates;
- `Game.Prefabs.MilestoneData.m_XpRequried`;
- `Game.City.XP` with current XP, maximum population, and maximum income; and
- vanilla XP reasons for population, happiness, income, service buildings and
  upgrades, roads, rail networks, electricity networks, waterways, pipes, and
  power lines.

The [local assembly verification report](./local-assembly-verification.md)
contains the exact signatures and assembly hashes.

Runtime verification selected one Harmony-free system immediately before
`XPSystem`. It transforms positive gains already in the shared queue, preserves
their order and metadata, then appends custom population XP. Because the queue
does not identify producers, other XP and milestone mods remain unsupported.

## 8. Post-MVP Roadmap

The first expansion should add individual controls for the game’s existing
vanilla XP reasons.

Later candidates include new outcome-based factors such as:

- employment and unemployment;
- service coverage;
- traffic flow and transit ridership;
- pollution and environmental health;
- education and healthcare; and
- housing availability or affordability.

Total-population milestone target mode may also be added after the MVP
population-growth model is stable.

## 9. Out of Scope for MVP

- Changing milestone thresholds, rewards, tiers, or unlock contents
- XP or milestone regression
- Per-city progression settings
- Profile import, export, or sharing
- User-authored formulas or scripting
- Automatic conflict detection
- Automatic balancing based on player behavior
- A custom in-game widget or overlay

## 10. Acceptance

The MVP is ready when:

- population XP, decline/recovery, fractional XP, and every boundary are covered
  by unit tests;
- presets and validation are tested;
- new-city, existing-city, disable/re-enable, save/reload, and missing-mod
  scenarios pass;
- vanilla, unlimited-money, and unlock-all modes are exercised;
- milestone rewards and unlocks remain game-native;
- the city-size-independent update path and large population/XP values are
  verified;
- local builds install and unload cleanly; and
- the Paradox Mods package contains the required metadata, license, localization,
  and assets.
