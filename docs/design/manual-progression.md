# Manual milestone claims

## Product behavior

Manual milestone claims are an opt-in mode under **Enable custom progression**
and default to off.

While the mode is active, positive XP is allowed to reach one point below the
next cumulative vanilla milestone threshold. Any remainder is held by
Progression Controls outside the city save. Effective XP is city XP plus held
XP.

The in-game panel shows every milestone supported by effective XP in ascending
order. Only the first row can be claimed. A claim releases exactly enough held
XP through the game's XP queue to reach that milestone. The vanilla milestone
system remains responsible for advancing the milestone, publishing its event,
granting money, and creating unlock events.

After the final milestone, any surplus held XP is released into the city XP
component because there is no longer a threshold to gate.

## Disable and recovery decisions

Turning manual claims off with held XP keeps gating active until the player
chooses:

- **Release to vanilla** updates the save-owned city XP, then clears the
  external bank.
- **Discard permanently** clears the external bank.
- **Cancel** restores both progression settings.

Loading a matching checkpoint with held XP while manual claims are off prompts
the player to restore manual claims, discard the bank, or decide on a later
load. Choosing **Decide later** preserves the checkpoint and suppresses the
prompt for the current session only.

A claim is persisted as a transaction containing milestone index, released XP,
and threshold. On load, the transaction is confirmed if the milestone already
advanced, left pending if city XP reached the threshold, or rolled back into
the bank if the queued gain did not reach the city.

## Storage

Schema 5 adds:

- held milestone XP;
- pending claim milestone index;
- pending claim XP; and
- pending claim threshold.

Schema 4 checkpoints remain readable with these values defaulted to zero.
Fail-safe checkpoint preparation adds held XP to the save-owned city XP.
Pending claim XP is added only when the city has not already reached its
recorded threshold. Both fail-safe release and player-requested release are
all-or-nothing: if vanilla city XP cannot hold the complete amount, external
state is retained unchanged.

## Integration boundaries

- No Harmony patches.
- The progression control system still runs immediately before the XP system.
- Milestone definitions come from a read-only milestone-data query.
- Achieved state comes from the milestone-level singleton.
- The frontend uses a supported UI system value binding and trigger bindings.
  UI callbacks request work; the simulation system validates and applies it.
- The vanilla milestone screen is not patched.

Exact installed signatures and hashes are recorded in
[manual-progression-assembly-verification.md](../manual-progression-assembly-verification.md).

## UI direction

The in-game screenshots supplied during the August 2026 UI review are the active
visual reference. The panel follows the compact CS2 utility-mod convention:

- a cyan rounded-square toolbar tile with the original white route-and-flag
  glyph;
- a 320 rem translucent panel with a 36 rem header;
- game typography variables instead of custom display type;
- a compact next-target banner using the milestone's native illustration,
  palette, a solid cumulative XP readout, and a slim green progress rail;
- 46 rem milestone rows with native milestone images;
- one cyan Claim action on the first claimable row.

The next-target banner stays visible when its milestone is already claimable,
so it can show a full range above the ordered claim queue. Reward capsules and
unlock lists remain in the vanilla progression screen rather than being
duplicated in this compact panel.

Large status eyebrows, metric cards, decorative gradients, oversized empty states,
and the earlier blue-gold concept artwork are intentionally excluded.

City Watchdog package 144908_22 was inspected locally at version 1.2.2 and
commit `c77cbd4ed8498c063a233a91486a72c9d5772f0f`. Its SourceLink record points
to `River-Mochi/CS2-CityWatchdog`, although that repository was unavailable
during this review. The installed module confirms that City Watchdog composes
native `Button` and `Panel` controls, uses `cs2/modding.getModule()` for the
native round-highlight button theme, and confines panel chrome to its own UI
primitives. Progression Controls now follows that architecture with an original
implementation; no City Watchdog code or assets were copied.

## Public discovery references

Public examples were used only to identify supported patterns; exact
signatures were verified locally before implementation.

- CitiesSkylinesModding,
  [StockModTemplatesDiffer](https://github.com/CitiesSkylinesModding/StockModTemplatesDiffer)
  (official-template tracking, Unlicense).
- Eriksas,
  [Cities-Skylines-2-Metro-Viewer](https://github.com/Eriksas/Cities-Skylines-2-Metro-Viewer)
  (supported UI binding and request-queue pattern; no code copied).
- BrokeAssSoftware,
  [Write Everywhere runtime UI guide](https://github.com/BrokeAssSoftware/write-everywhere/blob/13c70ebc1807df5f52ce4ad58de299089d213145/docs/WRITE_YOUR_OWN_UI.md)
  (runtime UI discovery at pinned commit; no code copied).

The shipped TypeScript declarations and webpack helper are taken from the
locally installed CS2 UI mod template for the active toolchain.
