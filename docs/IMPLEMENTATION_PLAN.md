# Progression Controls — Implementation Plan

**Source:** [SRS.md](./SRS.md)
**Current verified game build:** 1.6.0f1
**Release target:** Latest public CS2 build at release time

## Guiding Order

Build the highest-risk game integration first, then the testable rules, then UI
and packaging. Do not build the complete widget or settings surface until XP
scaling and population XP can be proven in-game.

## Phase 0 — XP Feasibility Spike

### Goals

1. Confirm the runtime behavior and update ordering of the locally verified
   `Game.Simulation.XPSystem`, which owns the `NativeQueue<XPGain>` and
   `NativeQueue<XPMessage>`.
2. Determine whether vanilla XP can be scaled through `XPSystem.GetQueue`,
   registered writer dependencies, or another supported system boundary.
3. If not, identify the narrowest stable Harmony patch point.
4. Confirm how `Game.City.XP.m_MaximumPopulation` behaves when:
   - population rises;
   - population falls and later recovers;
   - vanilla XP is scaled to zero; and
   - a city is saved and reloaded.
5. Confirm that adding XP through the chosen boundary triggers vanilla
   milestones, rewards, development points, loan limits, and unlocks.

### Deliverables

- A minimal local-only spike mod.
- A short architecture decision record documenting the chosen XP hook.
- Runtime logs for each verification case.
- Updated local assembly report if the active game build changes.

### Exit gate

Proceed only when:

- vanilla XP can be scaled from 0% to 100%;
- population XP can be submitted without manually implementing milestone
  rewards;
- no XP is double-counted; and
- save/reload behavior is understood.

If the gate fails, revise the affected SRS requirement before building the full
mod.

## Phase 1 — Project Scaffold

### Structure

- `src/ProgressionControls.Core` — pure progression rules
- `src/ProgressionControls` — CS2 entrypoint, systems, settings, adapters
- `tests/ProgressionControls.Core.Tests` — domain unit tests
- `ui/` — React, TypeScript, and SCSS widget
- `docs/` — SRS, implementation plan, verification, and decisions

Use the current local CS2 toolchain template as the source of truth for target
framework, assembly references, packaging, and local deployment. Keep
machine-specific paths in ignored local configuration.

### Foundation work

- Add `Kobbyist.ProgressionControls` mod identity.
- Add MIT license and 2026 kobbyist copyright.
- Implement `IMod.OnLoad` and `OnDispose`.
- Add a unique logger.
- Add English localization keys.
- Establish local build, deploy, and test commands.

### Exit gate

- Empty mod loads and unloads without errors.
- Local package is discovered by CS2.
- Settings and localization register and unregister cleanly.

## Phase 2 — Domain Core

Implement without CS2, Unity, ECS, Harmony, UI, or filesystem dependencies.

### Rules

- Historical-maximum population evaluation
- No XP loss during population decline
- XP only for population above the previous record
- Fractional XP accumulation
- XP-per-resident calculation
- Megalopolis-target-to-rate conversion
- Vanilla XP percentage validation
- Prospective-only settings changes

### Presets

- Vanilla
- Population Heavy
- Population Only
- Custom state after manual edits

### Tests

- First observation establishes a baseline
- Growth, decline, recovery, and new record
- Very small and very large population deltas
- Fractional remainder over repeated awards
- Zero and boundary rates
- Target/rate round trips
- All preset values
- Invalid and non-finite values

### Exit gate

All domain tests pass without loading game assemblies.

## Phase 3 — Game Integration

### Systems and adapters

- Read current city population on a controlled cadence.
- Read runtime Megalopolis XP requirement.
- Read and validate the base-game maximum-population value.
- Scale vanilla XP using the Phase 0 decision.
- Submit custom population XP through the verified native boundary.
- Preserve milestone processing entirely in the base game.

### Settings

- Enable custom progression; default on
- Preset selector
- XP per new resident
- Linked Megalopolis population target
- Vanilla XP multiplier; default 25%
- Widget visibility
- Reset widget position
- Restore defaults

### State

Prefer the existing base-game maximum-population record. If additional state is
required, store only the stable city key, maximum population, fractional
remainder, and schema version outside the city save.

### Exit gate

- New and existing cities establish the correct baseline.
- Disabling restores future vanilla XP behavior.
- Re-enabling grants no retroactive XP.
- Rate changes affect future XP only.
- Save/reload produces no duplicate award.
- Removing the mod leaves the save loadable.

## Phase 4 — Compact Widget

### C# binding model

Expose only normalized values:

- enabled state;
- current population;
- historical maximum population;
- population required to resume earning;
- most recent population XP award; and
- widget visibility and position.

### React UI

- Compact read-only Gameface widget
- Visible by default
- Draggable with a global saved position
- Viewport clamping after resolution or UI-scale changes
- Styling through CSS modules
- English localization through CS2 localization APIs

Do not query ECS or calculate progression rules from React render code.

### Exit gate

- Widget updates without visible polling jitter.
- Dragging does not interfere with normal game controls.
- Hidden widget adds no ongoing UI work.
- Reset position recovers an off-screen widget.
- Supported resolutions and UI scales pass visual checks.

## Phase 5 — Verification and Release

### In-game matrix

- New city
- Existing early-, mid-, and late-game cities
- Population growth, decline, recovery, and new record
- Vanilla, Population Heavy, Population Only, and Custom
- Multiplier boundaries: 0%, 25%, and 100%
- Enable, disable, and re-enable
- Save, reload, remove mod, and load without mod
- Unlimited Money and Unlock All
- Small and large cities

### Release work

- Run unit tests and build verification.
- Review logs for repeated errors or per-frame noise.
- Re-run assembly verification against the latest public build.
- Produce local and Paradox Mods packages.
- Verify metadata, dependencies, MIT license, localization, and assets.
- Document competing XP/milestone mods as unsupported combinations.

### Release gate

Release only when the SRS acceptance criteria pass and the package installs,
runs, saves, reloads, and uninstalls cleanly on the latest public game build.

## Key Risks

| Risk | Response |
| --- | --- |
| No supported global XP scaling hook | Use one isolated, documented Harmony patch |
| Vanilla maximum-population semantics change under scaling | Use minimal external per-city state |
| Fractional XP duplicates after reload | Persist remainder atomically and test reload boundaries |
| Game update changes XP types or order | Re-run local verification and block release until retested |
| Widget causes render or binding overhead | Push normalized values only when changed |
| Competing progression mods alter the same pipeline | Document as unsupported; do not arbitrate in MVP |

## Immediate Next Task

Complete Phase 0 cases P0-04 through P0-08. Do not scaffold the full production
UI until disable/re-enable behavior, maximum-population behavior, native
milestone side effects, city save/reload, and mod removal are proven.
