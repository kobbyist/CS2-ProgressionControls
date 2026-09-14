# Manual milestone claims

## Player contract

Manual milestone claims are off by default and require custom progression.
While active, city XP stops one point below the next vanilla threshold. The mod
holds the remainder in its save-specific checkpoint. The panel lists every
milestone supported by city XP plus held XP, but only the next row can be
claimed.

A claim sends exactly enough held XP through the game's XP queue to reach the
next threshold. Vanilla still advances the milestone and grants its rewards and
unlocks.

## Persistence and recovery

Schema 5 stores held XP and a pending claim's milestone index, released XP, and
threshold. Schema 2 from public version 0.1.1 remains readable with those fields
empty.

A pending claim is transactional. Loading confirms it when the milestone has
advanced, waits when city XP reached the threshold, or returns the released XP
to the bank when the gain did not reach the city.

Turning the option off with external state requires a player choice:

- Release adds the complete bank to city XP, then clears it.
- Discard clears the bank permanently.
- Cancel restores both progression settings.

Recovery offers restore, discard, or decide later. Decide later preserves the
checkpoint and suppresses the prompt for that session. Player-requested and
save fail-safe releases are all-or-nothing. If city XP cannot hold the complete
amount, the external state remains unchanged.

## Safety boundaries

- The implementation uses no Harmony patches.
- `ProgressionControlSystem` runs before `XPSystem`.
- Simulation code reads `MilestoneData` and `MilestoneLevel`; UI requests are
  validated again by the simulation system.
- A usable milestone catalog has contiguous indexes starting at 1,
  non-negative and strictly increasing cumulative thresholds, and one final
  `m_IsVictory` marker on the highest definition.
- Missing or invalid catalog data cannot prove final completion. Held and new
  positive XP stay external until a valid catalog is available.
- Final surplus XP is released only after the achieved definition carries the
  validated final marker and vanilla's canonical locked-milestone query is
  empty.

Installed signatures and IL evidence are recorded in
[local-assembly-verification.md](../local-assembly-verification.md).
