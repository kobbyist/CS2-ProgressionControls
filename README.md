# Progression Controls

Progression Controls makes Cities: Skylines II milestone progression slower and
more population-driven without replacing the game's milestones or rewards.

**Current status:** MVP 0.1.0 release candidate, verified on CS2 1.6.0f1. Release builds
target the latest public game version available at build time.

## What it changes

- Awards milestone XP when the city reaches a new all-time population record.
- Scales positive XP already queued for the game's milestone system from 0% to
  100%.
- Preserves the game's milestone thresholds, unlocks, development points,
  rewards, and loan limits.
- Accumulates fractional population XP so rounding does not discard progress.
- Batches population XP into a configurable maximum number of awards and
  notifications per in-game day.
- Applies settings changes prospectively without recalculating existing XP.

Population decline never removes XP. If population falls and later recovers,
population XP resumes only after the previous record is exceeded.

## Presets

| Preset | Population progression | Vanilla XP retained |
| --- | --- | ---: |
| Population Balanced | New population records award XP | 50% |
| Population Heavy | New population records are the main source of XP | 25% |
| Population Only | Only new population records award XP | 0% |

Population Heavy is the default. Editing an advanced XP rule changes the preset
to Custom.

## Configuration

Open **Options > Progression Controls**.

The main view contains the enable toggle and preset selector. Turn on
**Show Advanced** to edit:

- XP per new resident;
- the projected population-only Megalopolis target;
- the vanilla XP multiplier;
- population update responsiveness; and
- population XP notification frequency.

XP per resident and the Megalopolis target are linked. Advanced XP rules are
staged until **Apply custom rules** validates and applies them together.

**Vanilla XP multiplier** is player-facing shorthand for the shared XP queue.
Progression Controls scales every positive gain already in that queue when it
runs. Its own population XP is appended afterward and is not scaled again.

Population records are detected independently from population XP awards.
Earned population XP accumulates and is submitted as one batch at the selected
notification frequency, from 1 to 256 awards per in-game day. The default is
Regular (16/day), and total population XP is unchanged.

Turning off **Enable custom progression** leaves future queued XP unscaled and
makes the mod dormant. Re-enabling establishes a safe population baseline and
does not grant retroactive XP.

## Save safety

Progression Controls does not add required components to the city save. XP and
milestones already earned become normal game state. A city remains loadable
after disabling or removing the mod, and future progression returns to vanilla
behavior.

Minimal population-record, fractional-XP, and pending population-XP batch state
is stored outside the city save and keyed to the exact save checkpoint.

## Compatibility

- **Unlimited Money:** Supported and runtime-tested.
- **Unlock All:** The mod loads safely, but Unlock All bypasses milestone
  progression, so milestone XP is unavailable and progression changes have no
  meaningful visible effect.
- **City Watchdog:** Co-load smoke-tested; it is not a dependency.
- **Other XP or milestone mods:** Unsupported. Their positive gains may be
  scaled when queued before Progression Controls, or remain unscaled when
  queued afterward. Disable mods that add or intercept XP, rescale milestone
  progress, or replace milestone behavior before requesting support.

## Development and verification

- [Development commands](./docs/development.md)
- [Paradox Mods packaging](./docs/paradox-packaging.md)
- [Software requirements](./docs/SRS.md)
- [Implementation plan](./docs/IMPLEMENTATION_PLAN.md)
- [Local assembly verification](./docs/local-assembly-verification.md)

## License

[MIT](./LICENSE), copyright 2026 kobbyist
