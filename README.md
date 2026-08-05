# Progression Controls

Vanilla milestone progression can move so quickly that unlocks arrive before a
city has had time to grow into them. XP from roads, services, happiness, and
other sources stacks up fast, disconnecting progression from population growth.

Progression Controls slows that pace and makes new population records the
foundation of milestone XP without replacing the game's milestones or rewards.

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
| Population Balanced | 1.5 XP per new record resident | 50% |
| Population Heavy | 1.5 XP per new record resident | 25% |
| Population Only | 1.5 XP per new record resident | 0% |

Population Heavy is the default. Editing an advanced XP rule changes the preset
to Custom.

## Configuration

Open **Options > Progression Controls**.

The main view contains the enable toggle and preset selector. Turn on
**Show Advanced** to edit:

- XP per new resident;
- the vanilla XP multiplier;
- population update responsiveness; and
- population XP notification frequency.

XP per new resident is a slider from 0 to 10 in 0.25 increments. The default
1.5 matches the game's nominal population XP rate. The two advanced XP rules
are staged until **Apply custom rules** applies them together.

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

## License

[MIT](./LICENSE), copyright 2026 kobbyist
