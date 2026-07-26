# Progression Controls — Implementation Plan

**Source:** [SRS.md](./SRS.md)
**Current verified game build:** 1.6.0f1
**Release target:** Latest public CS2 build at release time

## Guiding Order

Build the highest-risk game integration first, then the testable rules, then
settings and packaging. Do not build the complete settings surface until XP
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

The runtime spike has satisfied this gate on 1.6.0f1. Queue interception scaled
vanilla XP at 0%, 25%, and 100%; explicit queue submission triggered native
milestone rewards; and save/reload behavior is understood. Removing the spike
also left the save loadable and restored future vanilla XP. ADR 0001 is
accepted.

## Phase 1 — Project Scaffold

### Structure

- `src/ProgressionControls.Core` — pure progression rules
- `src/ProgressionControls` — CS2 entrypoint, systems, settings, adapters
- `tests/ProgressionControls.Core.Tests` — domain unit tests
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

Verified on 1.6.0f1: the private production package loaded once, exposed its
localized enabled-by-default setting, and executed `OnDispose` without related
errors. Phase 1 is complete.

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

- Population Balanced
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

Verified with 32 passing tests: baseline establishment, record-only awards,
decline and recovery, fractional restoration, prospective configuration
changes, presets, linked target/rate conversion, zero and maximum rates,
invalid inputs, and large XP totals. Phase 2 is complete.

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
- Advanced population update responsiveness; 16 to 16,384 observations per
  in-game day, default 4,096
- Restore defaults

The progression-rule controls are implemented with immutable preset selection,
linked numeric text inputs for XP per resident and projected population target,
and an integer 0% to 100% Vanilla XP slider. Presets immediately update their
source mix, while separate right-panel paragraphs explain each behavior and
point players to Show Advanced for customization. Individual rule controls and
their Apply custom rules action are hidden until the player enables advanced
options. Manual edits remain staged so partial text input cannot change the
running city. Applying validates all three rule values together, normalizes the
preset to Custom, restores the active safe configuration when invalid, and
establishes a prospective population baseline.

### State

Use the base-game maximum-population record as the safe first-run high-water
baseline. Persist only the stable city session ID, serialized simulation frame,
maximum observed population, population XP fraction, vanilla-scaling fraction,
outside the city save. The session ID plus frame identifies the exact save
checkpoint, so loading an older save cannot consume newer external progression
state.

After one-time city initialization, disabled mode returns before population
cadence evaluation or XP queue access and does not capture external
checkpoints. Re-enabling clears fractional carry
and establishes a no-award baseline from the greatest of current population,
the base-game population high-water mark, and the stored mod high-water mark.

### Exit gate

- New and existing cities establish the correct baseline.
- Disabling restores future vanilla XP behavior.
- Re-enabling grants no retroactive XP.
- Rate changes affect future XP only.
- Save/reload produces no duplicate award.
- Removing the mod leaves the save loadable.

All Phase 3 game-integration exit checks pass on 1.6.0f1, including cadence,
checkpoint restoration, dormant disable and re-enable, new-city behavior,
atomic custom rules, and clean removal. Evidence is recorded in
[phase-3-runtime-checklist.md](./phase-3-runtime-checklist.md).

## Phase 4 — Verification and Release

### In-game matrix

- New city
- Existing early-, mid-, and late-game cities
- Population growth, decline, recovery, and new record
- Disabled, Population Balanced, Population Heavy, Population Only, and Custom
- Multiplier boundaries: 0%, 25%, 50%, and 100%
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
| Competing progression mods alter the same pipeline | Document as unsupported; do not arbitrate in MVP |

## Immediate Next Task

Run the Phase 4 verification and release matrix, beginning with the remaining
preset and multiplier combinations.
