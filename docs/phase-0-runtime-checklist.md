# Phase 0 Runtime Checklist

Use a disposable test city. The explicit injection test permanently adds normal
base-game XP to the city.

## Preparation

1. Build and install `Kobbyist.ProgressionControls.Spike`.
2. Start Cities: Skylines II 1.6.0f1 with the spike as the only progression mod.
3. Open Options > Progression Controls — XP Spike.
4. Keep queue interception off until the test city is loaded.
5. Record starting XP, milestone, population, maximum population, money,
   development points, loan limit, and relevant unlocks.

## Cases

| ID | Setup and action | Expected result |
| --- | --- | --- |
| P0-01 | Enable interception at 100%, enable batch logging, and cause several vanilla XP events. | Logged input and output totals match; city XP matches vanilla behavior; no duplicate XP. |
| P0-02 | Set 0% and cause discrete vanilla XP events. | The intercepted input is non-zero, output is zero, and city XP does not rise from those events. |
| P0-03 | Set 25% and cause discrete vanilla XP events. | Output uses deterministic integer scaling and city XP rises by the logged output only. |
| P0-04 | Disable interception and cause the same event type. | Future XP returns to vanilla behavior without recalculating existing XP. |
| P0-05 | Set an injection amount that crosses the next milestone, then confirm Inject population XP. | Native XP increases once; milestone rewards, development points, loan limit, and unlocks are granted by the game. |
| P0-06 | Grow, decline, and recover population under 100% and 0%. | Record current population and `m_MaximumPopulation` behavior for each transition. |
| P0-07 | Save and reload after each mode. | No queued XP repeats, disappears, or applies twice. |
| P0-08 | Disable/remove the spike and reload. | The city loads and future XP uses vanilla behavior. |

## Evidence to retain

- `Player.log` excerpts from load through each case
- before/after screenshots of XP and milestone rewards
- exact game version from the runtime log
- setting values for each case
- pass/fail and observations appended to this document

Do not mark ADR 0001 accepted until all eight cases are complete.
