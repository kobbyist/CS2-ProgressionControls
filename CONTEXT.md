# Progression Controls

Progression Controls is the domain for pacing Cities: Skylines II milestone progression around population growth and optional player confirmation. It changes how milestone XP is earned or held while leaving the game's milestones and rewards intact.

## Progression

**Milestone XP**:
Experience counted toward the game's cumulative milestone thresholds.
_Avoid_: Progression points, experience points

**City XP**:
Milestone XP already recorded in the loaded city save.
_Avoid_: Vanilla XP, effective XP

**Vanilla XP**:
Positive milestone XP supplied through the game's normal progression flow before Progression Controls adds population XP. It can originate from the base game or another mod.
_Avoid_: City XP, population XP

**Vanilla progression**:
The game's normal milestone progression without Progression Controls applying its custom XP rules or manual claims.
_Avoid_: Vanilla XP

**Population XP**:
Milestone XP earned from population above the city's previous population record.
_Avoid_: Vanilla XP, population points

**Custom progression**:
The enabled Progression Controls rule set for future milestone XP. Manual milestone claims can operate only while custom progression is enabled.
_Avoid_: Custom preset, manual progression

**Progression preset**:
A named combination of the population XP rate and the share of vanilla XP retained.
_Avoid_: Profile, mode

**Custom preset**:
The progression preset selected automatically after the player edits an XP rule.
_Avoid_: Custom progression

**Vanilla XP multiplier**:
The percentage of positive vanilla XP retained under custom progression.
_Avoid_: Difficulty multiplier, city XP multiplier

## Population progression

**Population record**:
The highest population recognized for a city. Population must exceed this value before more population XP is earned.
_Avoid_: Current population, population target

**New record resident**:
A resident above the previous population record, whether or not the resident is new to the city.
_Avoid_: New citizen

**XP per new resident**:
The population XP rate applied to each new record resident. This is the player-facing name for the rule.
_Avoid_: XP per citizen, population multiplier

**Population baseline**:
A population record established without awarding population XP when the mod lacks safe prior history or begins a new rule period.
_Avoid_: Starting population, population minimum

**Pending population XP**:
Population XP already earned but waiting for its next population XP award.
_Avoid_: Held XP, unearned XP

**Population update responsiveness**:
The selected frequency for detecting new population records. It changes detection delay, not total population XP.
_Avoid_: Population XP notification frequency

**Population XP notification frequency**:
The selected maximum frequency for combining pending population XP into awards. It changes award and notification frequency, not total population XP.
_Avoid_: Population update responsiveness

## Manual milestone claims

**Manual milestone claims**:
An optional mode under custom progression that keeps earned XP below the next milestone until the player claims it.
_Avoid_: Manual progression, manual unlocks

**Milestone threshold**:
The cumulative city XP required for the game to reach a milestone.
_Avoid_: Milestone cost

**Held XP**:
Earned milestone XP kept by Progression Controls outside the city save while manual milestone claims are active.
_Avoid_: Pending population XP, city XP

**Effective XP**:
City XP plus held XP. It determines which unachieved milestones have been earned.
_Avoid_: Total XP, available XP

**Claimable milestone**:
An unachieved milestone whose threshold is no greater than effective XP.
_Avoid_: Unlocked milestone, available milestone

**Milestone queue**:
The ordered set of claimable milestones. Only its first milestone can be claimed.
_Avoid_: Unlock queue

**Milestone claim**:
The player's request to move enough held XP into city XP for the game to reach the first milestone in the milestone queue.
_Avoid_: Manual unlock

**Pending claim**:
A milestone claim whose XP has entered the city but whose milestone advancement has not yet been confirmed.
_Avoid_: Pending population XP, held XP

**Release to vanilla**:
The choice that moves all held XP into city XP so vanilla progression can continue.
_Avoid_: Claim all

**Discard permanently**:
The irreversible choice that removes all held XP instead of adding it to city XP.
_Avoid_: Reset, cancel

**Restore manual claims**:
The recovery choice that re-enables manual milestone claims when held XP is found for the loaded save.
_Avoid_: Release to vanilla

## Save continuity

**Save checkpoint**:
One saved version of a city. Different saves or overwrites of the same city are distinct checkpoints.
_Avoid_: Save name, city

**Progression checkpoint**:
The mod-owned companion record for one save checkpoint.
_Avoid_: City save, settings

**Matching checkpoint**:
The progression checkpoint belonging to the loaded save checkpoint.
_Avoid_: Latest checkpoint
