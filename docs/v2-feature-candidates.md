# V2 feature candidates

This note records the current product direction for the next feature release.
These themes are selected for further design, but their detailed behavior and
scope are not yet an implementation commitment.

## Selected direction

### XP source mix

Replace the single vanilla-XP multiplier with controls for meaningful source
groups, such as population, city performance, and construction. Existing
settings should migrate without changing behavior by applying the old
multiplier to every new group.

Open design questions include the final grouping of game XP reasons, how
unknown reasons are handled, and whether fractional carry is tracked per group
or after aggregation.

### Manual progression

Let players control when newly earned milestones are accepted. XP continues to
accumulate in an external bank while milestones wait for a manual claim. The UI
shows every milestone supported by effective XP in an ordered claim queue.
Players claim one milestone at a time, and cannot claim them out of order. Each
claim releases only the banked XP needed to reach the next vanilla threshold,
so the game still grants its own milestone rewards and unlocks.

Manual milestone claims are a separate opt-in setting and default to off. Its
description must explain that unclaimed XP is stored outside the city save and
depends on the matching Progression Controls checkpoint.

Turning manual claims off while XP is banked must open a confirmation prompt.
The player chooses either to release the accumulated XP through the vanilla
queue or discard it permanently. Removing the mod cannot display this prompt,
so the option description must tell players to turn manual claims off before
removing the mod. Reinstalling the mod can restore held XP only while the
matching checkpoint remains available.

Checkpoint durability is a prerequisite for manual progression. A checkpoint
is prepared and flushed before the game serializes a save, then committed after
the save succeeds. Each logical save name has its own checkpoint, including
when two divergent branches reach the same city and simulation frame. Cleanup
cannot remove a checkpoint while its save remains present.

Population gates, minimum milestone age, and one-milestone-per-day pacing
remain possible extensions rather than part of the initial commitment.

### Visibility and feedback

Make the progression rules and their effects understandable in play. The
leading views are:

- an XP ledger showing population XP plus retained and suppressed XP by source;
- a milestone forecast in residents and, when supportable, estimated game time;
- record status showing current population, the all-time record, and the point
  at which population XP resumes;
- an ordered queue of earned milestones with only the next claim enabled; and
- milestone history and concise diagnostics for unrecognized XP sources.

The first version should favor clear totals and explanations over a large
analytics interface.

## Product shape

Together, these features let players control where progression comes from,
decide when to accept the next milestone, and see why progression changed.
They are a stronger basis for a feature release than stretching debatable
behavior quirks into bug-fix justification.

The restored-checkpoint/configuration interaction remains classified as a
behavior quirk: restored progress state is authoritative, while current global
settings govern subsequent observations. It is not currently a v2 defect or
migration target.
