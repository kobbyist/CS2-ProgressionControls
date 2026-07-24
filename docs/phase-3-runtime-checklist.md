# Phase 3 Runtime Checklist

**Verified game build:** 1.6.0f1
**Test city:** `d23a583d9f664625b35450dcec120896`

## Cadence and checkpoint results

| Check | Result | Evidence |
| --- | --- | --- |
| Existing-city restore | Pass | External state restored at frame `15441441`; the city loaded paused at population 530 and 1,335/2,300 XP without a load-time award. |
| Default cadence | Pass | The default setting displayed 4,096 observations/day and runtime logging resolved it to a 64-frame interval. |
| Live cadence change | Pass | Changing to Immediate while paused resolved to 16,384 observations/day and a 16-frame interval without resetting progression state. |
| Pending record population | Pass | The live cadence change detected 11 residents above the restored maximum and queued 37 population XP, moving displayed XP from 1,335 to 1,372 exactly once. |
| Responsive population award | Pass | Population 530 to 531 queued 4 population XP. The panel moved from 1,372 to 1,389; the remaining 13 XP came from scaled vanilla events. |
| Fraction carry | Pass | After the prior 37- and 4-XP awards, the next resident queued 3 XP, consistent with the retained fractional population XP. |
| Setting persistence | Pass | Immediate (16,384/day) survived a full process restart. |
| Checkpoint reload | Pass | State restored at frame `15442692`. The previous awards did not replay; population 532 queued only the expected 3 XP for the genuinely new resident, moving XP from 1,389 to 1,392. |
| Lifecycle and errors | Pass | The mod disposed normally, reloaded once, and produced no related exception or error in the inspected runtime logs. |

## Static verification

- Core tests: 46 passed.
- Production compile-only build: 0 warnings and 0 errors.
- `TimeSystem.kTicksPerDay` is locally verified as 262,144.
- The default 4,096/day cadence divides the day into 64-frame intervals.
- The Immediate 16,384/day cadence uses a 16-frame interval, matching the
  locally verified population aggregate update interval.

## Remaining Phase 3 exit checks

- Verify first-run baseline behavior in a new city.
- Repeat enable, disable, and re-enable behavior with the production package.
- Verify an in-game rate change affects future population only.
- Remove the production package and confirm the save remains loadable.
