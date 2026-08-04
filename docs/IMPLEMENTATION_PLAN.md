# Progression Controls — MVP Delivery Plan

- **Source:** [SRS.md](./SRS.md)
- **Current verified game build:** 1.6.0f1
- **Release target:** Latest public CS2 build at release time
- **Status:** Local 0.1.0 release candidate; Paradox Mods upload not yet performed

## Delivered MVP

The local release candidate implements:

- population XP for new all-time population records;
- deterministic fractional XP accumulation;
- Population Balanced, Population Heavy, Population Only, and Custom rules;
- shared-queue XP scaling from 0% to 100%;
- linked XP-per-resident and projected Megalopolis-target inputs;
- configurable population observation and XP-notification cadences;
- prospective settings changes with atomic custom-rule validation;
- save-checkpoint-aware external state without required city-save components;
- dormant disable behavior and safe re-enable baselining;
- English localization and game-native Options UI; and
- exact publisher-content validation and offline audit staging.

The MVP has no Harmony patch, custom widget or overlay, required save
component, settings migration layer, or dependency on another mod.

## Verified architecture

### XP queue integration

One game system runs immediately before `Game.Simulation.XPSystem`. It obtains
the shared queue through `XPSystem.GetQueue`, completes registered writer
dependencies, preserves FIFO order and gain metadata, and scales positive gains
already present in the queue. Earned population XP is appended afterward, so it
is not scaled again. The base game remains responsible for milestone progress,
messages, rewards, development points, loan limits, and unlocks.

`XPGain` has no producer identity. XP queued by another mod may therefore be
scaled or left unchanged depending on update order. Other XP and milestone mods
remain unsupported combinations.

### Population and state

The city population component is read at a validated fixed cadence. Population
XP is awarded only above the greatest known historical record, never removed on
decline, and released through bounded notification batches.

External state is limited to the city session identifier, save simulation
frame, maximum observed population, fractional XP remainders, and pending
population XP. It is keyed to the exact save checkpoint and stored outside the
city save. Missing or invalid state establishes a no-award baseline.

### Discovery provenance

Initial integration discovery used these pinned public references as
orientation only:

- the `Game.Simulation.XPSystem` decompiled reference at roadmod commit
  [`5b49a4fc0c572f2b5133df83083ebb4afe2f76a6`](https://github.com/bworthy89/roadmod/blob/5b49a4fc0c572f2b5133df83083ebb4afe2f76a6/New%20folder/Game.Simulation/XPSystem.cs);
  and
- the generated API catalog at Cities-Skylines-2-Modding-Guide commit
  [`04c14691f4b766cc2cee0595be0b3c56542738be`](https://github.com/ps1ke/Cities-Skylines-2-Modding-Guide/blob/04c14691f4b766cc2cee0595be0b3c56542738be/Game/Simulation/XPSystem.md).

No public source code was reused. Exact signatures and assembly hashes are
governed by the versioned
[local verification report](./local-assembly-verification.md).

## Verification completed

- 79 domain tests pass in Release configuration.
- The isolated production build completes with zero warnings and errors.
- Queue scaling is runtime-tested at 0%, 25%, 50%, and 100%.
- Population growth, decline, recovery, fractional carry, batching, and
  checkpoint restoration are runtime-tested.
- New, early-, and mid-game cities establish safe baselines.
- Disable, re-enable, save, reload, clean removal, and vanilla continuation are
  runtime-tested.
- Unlimited Money is supported; Unlock All loads safely but bypasses visible
  milestone progression.
- City Watchdog co-loads without conflict and is not a dependency.
- Large-city applicability is covered by the fixed-work runtime path and large
  population/XP domain tests.
- Offline staging validates the exact nine-file publisher payload and creates a
  12-file audit bundle with metadata, thumbnail, and checksums.

## Remaining release work

1. Re-run local assembly verification and the isolated Release build against
   the latest public CS2 build available on release day.
2. Run one focused runtime smoke covering main-menu startup, invalid invariant
   numeric input, preset road and population XP, disable/re-enable, save/reload,
   and clean logs.
3. With explicit approval, create the first **Private** Paradox Mods listing.
4. Install the store-hosted package and repeat startup, settings, save, and
   removal smoke checks before changing its visibility.

Publishing is intentionally separate from routine builds because the official
publisher authenticates and changes external state.

## Post-MVP candidates

The first expansion may add individual controls for verified vanilla XP reasons.
Later candidates include employment, service coverage, traffic and transit,
pollution, education, healthcare, and housing outcomes. Changes to milestone
thresholds or rewards remain outside the current design.
