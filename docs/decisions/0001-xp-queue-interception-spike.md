# ADR 0001: Test Native XP Queue Interception

**Status:** Proposed — runtime verification required
**Date:** 2026-07-23
**Game build:** 1.6.0f1

## Context

Progression Controls must scale all future vanilla milestone XP while submitting
custom population XP through the base game's progression pipeline. Directly
editing `Game.City.XP.m_XP` can advance the numeric value, but bypassing the
native XP consumer risks missing messages or milestone side effects.

Public decompiled references identify `Game.Simulation.XPSystem` as the queue
consumer. Its update drains `NativeQueue<XPGain>`, adds each non-zero amount to
the city XP component, and emits an `XPMessage`.

Local 1.6.0f1 metadata confirms the supported public boundary:

- `NativeQueue<XPGain> GetQueue(out JobHandle deps)`
- `void AddQueueWriter(JobHandle handle)`
- `void TransferMessages(IXPMessageHandler handler)`

Local metadata also confirms the private queue, message queue, and writer handle
fields described by the public reference.

## Proposed decision

Test a Harmony-free interceptor registered exactly once immediately before
`XPSystem` in `SystemUpdatePhase.ModificationEnd`.

On each armed update it will:

1. obtain the native XP queue and its writer dependencies;
2. complete the writer dependencies;
3. drain queued vanilla gains in FIFO order;
4. scale and re-enqueue those gains in the same order;
5. enqueue any explicit custom population XP after vanilla scaling; and
6. allow the native `XPSystem` to consume the resulting queue.

The two-type `UpdateBefore<Interceptor, XPSystem>` call is the interceptor's only
registration. Combining it with a separate registration can update a custom
system twice.

The spike is passive by default. Queue interception defaults off and its
multiplier defaults to 100%. Custom XP is submitted only after a confirmed
Options button action.

## Why this candidate

- It uses public game methods instead of private-field reflection.
- It preserves the vanilla XP consumer and its message flow.
- It can scale all reasons without patching every producer.
- It keeps custom population XP separate from the vanilla multiplier.
- It can be removed without adding components to the city save.

## Runtime questions

- Does the anchored system run after every vanilla writer and immediately before
  `XPSystem` on 1.6.0f1?
- Does completing the returned dependencies and synchronously re-enqueuing avoid
  races and double counting?
- Do 0%, 25%, and 100% produce the expected future XP?
- Does injected population XP trigger normal messages and milestone rewards?
- Does `Game.City.XP.m_MaximumPopulation` remain reliable when population XP is
  suppressed?

If any answer is no, the next candidate is a narrowly isolated Harmony prefix on
the native queue-processing boundary. No Harmony dependency is added by this
spike.

## Evidence

- `Game.Simulation.XPSystem` decompiled reference at commit
  `5b49a4fc0c572f2b5133df83083ebb4afe2f76a6`:
  <https://github.com/bworthy89/roadmod/blob/5b49a4fc0c572f2b5133df83083ebb4afe2f76a6/New%20folder/Game.Simulation/XPSystem.cs>
- Generated API catalog at commit
  `04c14691f4b766cc2cee0595be0b3c56542738be`:
  <https://github.com/ps1ke/Cities-Skylines-2-Modding-Guide/blob/04c14691f4b766cc2cee0595be0b3c56542738be/Game/Simulation/XPSystem.md>
- Local hashes and signatures:
  [local-assembly-verification.md](../local-assembly-verification.md)

The public repositories are orientation evidence only. The spike implementation
is original and exact signatures are governed by the locally installed
assemblies.
