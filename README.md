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
- Optionally holds earned XP below the next milestone until you claim it.
- Shows every currently claimable milestone in order, with only the next claim
  enabled.

Population decline never removes XP. If population falls and later recovers,
population XP resumes only after the previous record is exceeded.

## Presets

| Preset              | Population progression         | Vanilla XP retained |
| ------------------- | ------------------------------ | ------------------: |
| Population Balanced | 1.5 XP per new record resident |                 50% |
| Population Heavy    | 1.5 XP per new record resident |                 25% |
| Population Only     | 1.5 XP per new record resident |                  0% |

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
1.5 matches the game's nominal population XP rate. Changes to either advanced
XP rule apply immediately to future XP, switch the preset to Custom, and save
automatically through the options system.

Progression Controls settings are global and independent of city saves.
Loading another city or an autosave does not replace them.

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

**Manual milestone claims** is a separate option and is off by default. While
enabled, city XP stops one point below the next vanilla threshold and excess XP
is held in the matching Progression Controls checkpoint. Open the in-game
Progression Controls button to claim earned milestones one at a time. The game
still grants its own rewards and unlocks.

Turning manual claims off with held XP asks whether to release it to vanilla
progression, discard it permanently, or cancel. Turn manual claims off and make
that choice before disabling or removing the mod.

The [manual milestone claims design](docs/design/manual-milestone-claims.md)
records the transaction, recovery, and game-integration rules.

## Save safety

Progression Controls does not add required components to the city save. XP and
milestones already written to the city remain normal game state. A city remains
loadable after disabling or removing the mod, and future progression returns to
vanilla behavior.

Held XP from manual milestone claims is external. Before removing the mod, use
the manual-claims disable prompt to release or discard it. Reinstalling the mod
can offer recovery only while the matching checkpoint still exists.

Minimal population-record, fractional-XP, and pending population-XP batch state
is stored outside the city save and keyed to the exact save checkpoint.
New checkpoints also record the game's save name. After a successful game save,
the mod keeps the checkpoint for that save name and retires older checkpoints
for overwritten saves. When the game save list can be read safely, checkpoints
for deleted saves are retired as well.

Checkpoints written by public version 0.1.1 remain readable and are replaced by
the current format after the next successful save. Checkpoints from 0.1.0 and
unpublished development formats are ignored and left untouched. Loading without
a supported checkpoint establishes a safe population baseline and does not grant
retroactive XP.

## License

[MIT](./LICENSE), copyright 2026 kobbyist
