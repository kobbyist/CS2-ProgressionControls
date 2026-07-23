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

## Results to date (1.6.0f1)

| Check | Result | Evidence |
| --- | --- | --- |
| P0-01: 100% pass-through | Pass | Logged batches `7 -> 7`, `8 -> 8`, and `4 -> 4`; city XP matched the outputs. |
| P0-02: 0% suppression | Pass | Starting at 70 XP, logged inputs `11`, `12`, `13`, and `6` all produced zero output; city XP remained 70. |
| P0-03: 25% scaling | Pass after correction | A hospital and roads produced 158 raw XP. The corrected scaler awarded 39 XP with `50/100` XP retained, moving city XP from 51 to 90. |
| Fractional accumulation regression | Pass | The exact earlier 19-event sequence totals 53 raw XP and now produces 13 XP with `25/100` retained; six focused automated tests pass. |
| Settings persistence | Pass | Interception enabled, 25%, and batch logging survived a full process restart. Startup logged the restored values before simulation began. |

The first P0-03 attempt exposed per-event integer truncation: 53 raw XP produced
only 2 XP. The scaler now carries hundredths between events, so rounding applies
to the cumulative stream rather than independently discarding each fraction.

P0-04 through P0-08 remain open. The settings restart check does not satisfy
P0-07, which requires city save/reload behavior under each progression mode.

## Evidence to retain

- `Player.log` excerpts from load through each case
- before/after screenshots of XP and milestone rewards
- exact game version from the runtime log
- setting values for each case
- pass/fail and observations appended to this document

Do not mark ADR 0001 accepted until all eight cases are complete.
