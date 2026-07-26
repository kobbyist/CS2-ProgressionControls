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

## Dormant disable and re-enable results

| Check | Result | Evidence |
| --- | --- | --- |
| Dormant startup | Pass | The disabled setting survived restart. State restored at frame `15443091`, the city loaded at population 532 and 1,392 XP, and no cadence or population XP entry was logged. |
| Disabled population growth | Pass | Population grew from 532 to 535 while XP remained 1,392. The mod produced no population, cadence, queue, or checkpoint log activity. |
| Vanilla pass-through | Pass | Placing a school while disabled awarded the full 300 vanilla XP, moving displayed XP from 1,392 to 1,692. |
| Safe re-enable | Pass | Re-enabling while paused restored the 16-frame cadence and established a no-award baseline at population 535. Displayed XP remained 1,692. |
| Post-enable growth | Pass | Population 535 to 542 queued 23 population XP in bounded batches. Displayed XP rose by 35 total; the remaining 12 XP came from scaled vanilla events. |
| Checkpoint dormancy | Pass | No external checkpoint was written while disabled. Saving resumed after re-enable at frame `15446828`. |

## New-city results

| Check | Result | Evidence |
| --- | --- | --- |
| First-run baseline | Pass | A disposable new city loaded at population 0 and 0 XP. The mod established a zero baseline, configured the 16-frame cadence, and queued no load-time XP. |
| First population growth | Pass | Population 0 to 3 queued 10 population XP as batches of 3 and 7. Displayed XP reached 111; the remaining 101 XP came from scaled vanilla setup events. |

## Static verification

- Core tests: 56 passed.
- Production compile-only build: 0 warnings and 0 errors.
- `TimeSystem.kTicksPerDay` is locally verified as 262,144.
- The default 4,096/day cadence divides the day into 64-frame intervals.
- The Immediate 16,384/day cadence uses a 16-frame interval, matching the
  locally verified population aggregate update interval.

## Custom-rule runtime test

| Check | Result | Evidence |
| --- | --- | --- |
| First-launch rate initialization | Pass | After the development settings reset, XP per resident remained empty in the main menu because the runtime Megalopolis requirement was not yet available. Loading the city populated 3.3585 for the 200,000 target and persisted the requirement. |
| Immediate preset application | Pass | Population Only applied prospectively as one configuration change with rate 3.3585 and 0% vanilla XP. |
| Draft isolation | Pass | Replacing the 200,000 target with 100,000 while paused left XP at 117 and the displayed rate at 3.3585. No intermediate configuration change appeared in the runtime log. |
| Atomic application | Pass | Apply custom rules produced exactly one configuration change: Custom, target 100,000, rate 6.717, and 0% vanilla XP. Existing XP remained 117. |
| Future-only award | Pass | Population subsequently rose from 5 to 7. The mod queued 13 XP (`2 × 6.717`, floored with 0.434 carried), moving XP from 117 to 130 with no vanilla contribution. |

## Remaining Phase 3 exit checks

- Remove the production package and confirm the save remains loadable.
