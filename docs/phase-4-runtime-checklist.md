# Phase 4 Runtime Checklist

**Verified game build:** 1.6.0f1

**Verification started:** 2026-07-26

This checklist records release-matrix coverage beyond the completed Phase 3
integration checks. Phase 3 evidence remains in
[phase-3-runtime-checklist.md](./phase-3-runtime-checklist.md).

## City and preset coverage

| Check | Result | Evidence |
| --- | --- | --- |
| Existing early-game city, Population Balanced | Pass | At population 45 and 392 XP, a small coal power plant plus roads moved XP to 544. A water tower then moved XP to 619, retaining 50% of its 150 vanilla XP award. No retroactive population XP was logged. |
| Existing mid-game city, Population Balanced | Pass | The city loaded at population 503 and 1,081 XP. An elementary school awarded 150 XP, moving the total to 1,231, which is exactly 50% of its 300 vanilla award. No load-time population XP was logged. |
| Existing late-game city | Not run | No late-game save is currently available. This remains release coverage rather than inferred evidence. |
| New city, Population Balanced | Pass | A disposable city established a zero-population, zero-XP baseline. With Unlimited Money enabled, a 96 m road produced 1 XP at the 50% multiplier. |
| New city, Population Only | Pass | After switching prospectively to Population Only, a second 96 m road left XP unchanged at 1. Population growth from 0 to 4 awarded 13 XP; growth from 4 to 6 awarded 7 XP. |
| Fractional population XP carry | Pass | `4 * 3.3585 = 13.434`, so 13 XP was awarded and 0.434 retained. The next two residents produced `0.434 + (2 * 3.3585) = 7.151`, so 7 XP was awarded and 0.151 retained. Runtime logging reported the matching 13- and 7-XP queue submissions. |
| Custom, 100% vanilla XP | Pass | Applying the advanced rules produced one prospective runtime change to Custom with rate 3.3585 and a 100% vanilla multiplier. At unchanged population 6, another 96 m road awarded the full 2 XP, moving the total from 21 to 23. |

## Built-in mode compatibility

| Check | Result | Evidence |
| --- | --- | --- |
| Unlimited Money | Pass | The mod established a normal baseline, scaled road XP at 50%, blocked road XP at 0%, and awarded population XP with fractional carry. The inspected mod log contained no related errors. |
| Unlock All | Smoke pass | A disposable city with Unlimited Money and Unlock All loaded at population 0. Milestone XP was unavailable because Unlock All bypasses progression, so XP scaling is not observable in this mode. No load failure was observed. |

## Static release verification

| Check | Result | Evidence |
| --- | --- | --- |
| Active game version | Pass | `SceneFlow.log` reports game version 1.6.0f1 and Unity 2022.3.71f1. The version was taken from the runtime log rather than the installation-folder name. |
| Core tests | Pass | Release-mode test run completed with 56 passed, 0 failed, and 0 skipped. |
| Production build and deployment | Pass | The official CS2 targets compiled, post-processed, built platform libraries, and deployed the local package with 0 warnings and 0 errors. |
| Local assembly refresh | Pass | The metadata-only verifier reproduced game version 1.6.0f1, Unity 2022.3.71f1, and the same hashes recorded in the versioned report for every overlapping assembly. |
| Deployed package | Pass | The local package contains the expected eight managed, debug, and platform files. The deployed production DLL is byte-identical to the successful build output. |
| Production boundaries | Pass | Deployed metadata contains no Harmony dependency, `Colossal.UI.Binding` reference, widget/overlay system type, spike type, or UI module asset. Localization and settings cleanup remain symmetric in `OnDispose`. |
| Runtime log review | Pass | The final tested process logged one production load and a clean `OnDispose`; no Progression Controls warning, error, or exception was found in the inspected logs. |
| Release metadata and license | Pass | Publishing metadata identifies version 0.1.0, game version 1.6.*, Private access, display text, and descriptions. The repository contains the MIT license with 2026 kobbyist copyright. |

## Rebuilt local package smoke test

| Check | Result | Evidence |
| --- | --- | --- |
| Fresh production load | Pass | The rebuilt local package loaded its core and production assemblies once, loaded the generated Windows Burst library, and initialized without a related warning, error, or exception. |
| Settings restoration | Pass | The Options entry restored Custom, XP per resident 3.3585, Megalopolis target 200,000, vanilla multiplier 100%, and the saved 4,096/day cadence. |
| Existing-city checkpoint | Pass | The mod restored city `d23a583d9f664625b35450dcec120896` at frame `15451872` and activated the restored configuration without a load-time award. |

## Remaining release coverage

- Existing late-game city, if a suitable save becomes available.
- Large-city performance and progression behavior.
- Paradox Mods package generation and publishing checks when development moves
  beyond local-only testing.
