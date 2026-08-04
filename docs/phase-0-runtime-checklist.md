# Phase 0 Runtime Checklist

**Historical evidence:** The disposable spike project was retired after this
matrix passed and ADR 0001 was accepted. These steps and results document the
verified discovery process; they are not current development commands.

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
| P0-04: disable interception | Pass | Interception was disabled at 90 XP. Subsequent road construction used vanilla processing and raised city XP to 125 without recalculating prior XP. |
| P0-05: native milestone processing | Pass | One explicit 2,539-XP injection was submitted once through `XPSystem`. The game advanced the milestone and granted 3 development points and a 150,000 loan-limit increase. |
| P0-06: maximum-population semantics | Pass | Under 0%, XP stayed at 3,581 while population grew, declined, recovered below its record, and reached a new record. `m_MaximumPopulation` never decreased and eventually advanced from 381 to 503 at the new record. |
| P0-07: save/reload | Pass with production-state requirement | A 0% save reloaded at population 503, maximum 503, and XP 3,581 with no delayed or duplicate batch. A 25% save reloaded with native city XP preserved at 3,592, but the spike's in-memory `25/100` fractional remainder reset. |
| P0-08: remove and reload | Pass | With all spike binaries moved outside the CS2 data tree, the options entry disappeared, the city loaded at 1,093/2,300 displayed XP, and one road restored vanilla progression to 1,143/2,300. No missing-mod or deserialization errors were logged. |
| Fractional accumulation regression | Pass | The exact earlier 19-event sequence totals 53 raw XP and now produces 13 XP with `25/100` retained; six focused automated tests pass. |
| Settings persistence | Pass | Interception enabled, 25%, and batch logging survived a full process restart. Startup logged the restored values before simulation began. |

The first P0-03 attempt exposed per-event integer truncation: 53 raw XP produced
only 2 XP. The scaler now carries hundredths between events, so rounding applies
to the cumulative stream rather than independently discarding each fraction.

The reload boundary was then tested with a known `25/100` remainder. After
reload, a 3-XP input produced zero and left `75/100`; had the previous remainder
been persisted, it would have produced one XP. The production mod must therefore
persist its fractional population and vanilla-scaling state outside the city
save. Native integer XP itself remained correct and no queued batch replayed.

The explicit injection slider was difficult to control precisely at its large
spike-only range. The production settings do not expose arbitrary XP injection,
so this diagnostic control will be removed rather than promoted to production
UI.

The first removal attempt left the backup under the CS2 data tree, where the
loader still discovered it. A valid removal test must move every loadable binary
outside that entire tree, not merely outside its `Mods` directory.

All eight Phase 0 cases are complete.

## Evidence to retain

- `Player.log` excerpts from load through each case
- before/after screenshots of XP and milestone rewards
- exact game version from the runtime log
- setting values for each case
- pass/fail and observations appended to this document

Do not mark ADR 0001 accepted until all eight cases are complete.
