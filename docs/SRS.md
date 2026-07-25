# Progression Controls — Software Requirements Specification

**Status:** MVP draft
**Version:** 0.3
**Date:** 2026-07-23
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
- scale all vanilla XP from 0% to 100%;
- use population as the dominant or sole progression source; and
- see how the active rules affect the city.

The MVP changes only how XP is earned. It does not change the 20 vanilla
milestones, XP thresholds, rewards, development points, loan limits, or unlocks.

[City Watchdog](https://github.com/River-Mochi/CS2-CityWatchdog) is a design
reference for simple settings, compact live status, localization, and safe
game-native behavior. Progression Controls is an original implementation and
does not depend on City Watchdog.

## 2. MVP Behavior

### 2.1 Population XP

- The mod awards XP when the city exceeds its historical maximum population.
- The award equals the new-record population delta multiplied by the configured
  XP-per-resident rate.
- Population decline never removes XP or milestones.
- After a decline, population XP resumes only after the previous record is
  exceeded.
- Fractional XP is accumulated deterministically rather than discarded.

The default rate is derived from the game’s runtime Megalopolis XP requirement
so a population-only city reaches Megalopolis at approximately 200,000
population.

The player can edit either:

- XP per new resident; or
- the projected population-only Megalopolis target.

These are linked values. Changing one recalculates the other.

### 2.2 Vanilla XP

The MVP provides one global Vanilla XP multiplier from 0% to 100%.

- `0%` suppresses vanilla XP and enables population-only progression.
- `100%` preserves normal vanilla XP.
- The default is `25%`.

Post-MVP versions may expose a separate multiplier for every verified vanilla XP
reason.

### 2.3 Enablement and changes

- Custom progression is enabled by default after installation.
- It can be enabled or disabled in the mod Options.
- Disabling it stops population XP and restores 100% vanilla XP for future
  events.
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
| Preset | Applies and explains Vanilla, Population Heavy, or Population Only immediately |
| XP per new resident | Advanced numeric text input that sets the population XP rate |
| Megalopolis population target | Advanced linked numeric text alternative to XP per resident |
| Vanilla XP multiplier | Advanced integer slider that scales vanilla XP from 0% to 100% |
| Population update responsiveness | Advanced dropdown controlling 16 to 16,384 observations per in-game day; defaults to 4,096 |
| Show status widget | Shows or hides the in-game widget |
| Reset widget position | Returns the widget to a visible default location |
| Restore defaults | Restores the Population Heavy defaults |

Built-in presets are immutable:

| Preset | Population XP | Vanilla XP |
| --- | --- | ---: |
| Vanilla | Off | 100% |
| Population Heavy | Default 200,000 target | 25% |
| Population Only | Default 200,000 target | 0% |

Editing a preset value changes the displayed selection to **Custom**.

## 4. In-Game Widget

The MVP includes a compact read-only widget. Configuration remains in Options.

The widget shows:

- whether custom progression is active;
- current population;
- historical maximum population;
- population remaining before XP earning resumes after a decline; and
- the most recent population XP award.

The widget is visible by default, draggable, and remembers one global position.
It can be hidden without disabling progression. It must recover to the visible
viewport after resolution or UI-scale changes.

## 5. Save and Removal Safety

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
- schema version.

Missing or invalid tracking state uses the greater of current population and
the verified base-game maximum-population record as a fresh baseline and never
grants retroactive XP. The serialized simulation frame distinguishes multiple
save checkpoints that share one city session identifier.

The MVP does not detect or arbitrate conflicts with other XP or milestone mods.
Compatibility is the user’s responsibility, and support testing may require
disabling competing progression mods.

## 6. Functional Requirements

| ID | Requirement |
| --- | --- |
| FR-01 | Custom progression shall be globally enableable and enabled by default. |
| FR-02 | Population XP shall be awarded only for new historical population highs. |
| FR-03 | Population decline shall not remove XP or revoke milestones. |
| FR-04 | The population rate and projected Megalopolis target shall be linked editable values. |
| FR-05 | The Vanilla XP multiplier shall support every integer percentage from 0% to 100%. |
| FR-06 | The initial configuration shall use Population Heavy with 25% Vanilla XP. |
| FR-07 | Preset and setting changes shall affect future XP only. |
| FR-08 | Existing cities shall use the greater of current population and the reliable base-game maximum-population record as the initial baseline. |
| FR-09 | The three built-in presets shall be available as defined above. |
| FR-10 | Invalid or non-finite settings shall be rejected or replaced with safe defaults. |
| FR-11 | Vanilla milestone thresholds, rewards, tiers, and unlocks shall remain unchanged. |
| FR-12 | The compact widget shall provide the status fields defined above. |
| FR-13 | The city save shall not require Progression Controls to load. |
| FR-14 | All player-facing text shall use localization keys. |
| FR-15 | Advanced controls shall offer validated population observation cadences from 16 to 16,384 per in-game day, defaulting to 4,096. |

## 7. Quality Requirements

- **Performance:** Population evaluation runs outside UI rendering at the
  selected validated cadence. The maximum cadence matches the vanilla city
  population aggregation interval of 16 simulation frames; it never polls
  faster than new aggregate data can become available. Widget bindings update
  only when values change or at a bounded presentation cadence.
- **Determinism:** Identical observations and settings produce identical XP,
  including fractional accumulation.
- **Testability:** Population rules, target/rate conversion, validation, and
  preset application live in a pure C# domain layer without CS2, Unity, ECS,
  Harmony, UI, localization, or filesystem dependencies.
- **Failure safety:** Missing or invalid game data skips the affected award,
  emits a bounded diagnostic, and never guesses an XP value.
- **Logging:** Log lifecycle, settings migration, rejected configuration,
  adapter failures, and XP application without per-frame noise.
- **Localization:** Version 1 ships in English, with all text structured for
  later translation.
- **Cleanup:** Systems, settings, bindings, UI hooks, event subscriptions,
  localization sources, and patches are unregistered or disposed on unload.

## 8. Architecture and Verified Game Boundaries

The implementation consists of:

1. **Domain core** — population delta, fractional XP, validation, presets, and
   target/rate conversion.
2. **Game adapters** — current population, maximum population, milestone XP
   requirements, vanilla XP events, and XP submission.
3. **Progression coordinator** — evaluates population and applies configured
   scaling on a controlled cadence.
4. **Settings/state adapter** — global Options settings and minimal external
   state when necessary.
5. **UI bridge** — normalized read-only state for the React/TypeScript widget.

Local 1.6.0f1 assembly verification confirms:

- `Game.Modding.IMod.OnLoad(Game.UpdateSystem)` and `OnDispose()`;
- `Game.PSI.Telemetry.GetCurrentSession()` and serialized
  `Game.Assets.SaveInfo.sessionGuid`;
- serialized `Game.Simulation.SimulationSystem.frameIndex`;
- `Game.Simulation.XPGain` with amount and reason;
- `Game.Simulation.XPMessage`;
- `Game.Prefabs.XPParameterData` with population and happiness rates;
- `Game.Prefabs.MilestoneData.m_XpRequried`;
- `Game.City.XP` with current XP, maximum population, and maximum income; and
- vanilla XP reasons for population, happiness, income, service buildings and
  upgrades, roads, rail networks, electricity networks, waterways, pipes, and
  power lines.

The [local assembly verification report](./local-assembly-verification.md)
contains the exact signatures and assembly hashes.

Runtime implementation must determine whether Vanilla XP can be scaled through
a registered game system or requires a narrowly isolated Harmony patch.

## 9. Post-MVP Roadmap

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

## 10. Out of Scope for MVP

- Changing milestone thresholds, rewards, tiers, or unlock contents
- XP or milestone regression
- Per-city progression settings
- Profile import, export, or sharing
- User-authored formulas or scripting
- Automatic conflict detection
- Automatic balancing based on player behavior

## 11. Acceptance

The MVP is ready when:

- population XP, decline/recovery, fractional XP, and every boundary are covered
  by unit tests;
- presets, validation, and settings migration are tested;
- new-city, existing-city, disable/re-enable, save/reload, and missing-mod
  scenarios pass;
- vanilla, unlimited-money, and unlock-all modes are exercised;
- milestone rewards and unlocks remain game-native;
- small- and large-city performance is measured;
- the widget is verified at supported resolutions and UI scales;
- local builds install and unload cleanly; and
- the Paradox Mods package contains the required metadata, license, localization,
  and assets.
