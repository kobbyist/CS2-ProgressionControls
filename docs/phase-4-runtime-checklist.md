# Phase 4 Runtime Checklist

Target runtime: Cities: Skylines II 1.6.0f1.

## Build evidence

- [x] C# UI binding signatures verified against the installed assemblies.
- [x] Required `.mjs` extension and module banner verified against the installed
  `UIModuleAsset` parser.
- [x] TypeScript strict check passes.
- [x] Production webpack bundle succeeds.
- [x] Full CS2 Release post-processing and deployment succeeds with 0 warnings
  and 0 errors.
- [x] Core rules suite passes: 56/56.

## In-game checks

- [ ] Widget appears at the safe top-right default after loading a city.
- [ ] Enabled mode shows current population, population record, growth status,
  and the latest population XP award.
- [ ] Disabled mode shows only `Vanilla progression active`.
- [ ] Population values update at the configured cadence without visible
  polling jitter.
- [ ] Dragging from the header does not activate game controls.
- [ ] Drag position survives a full game restart.
- [ ] Widget remains inside the viewport when dragged to every edge.
- [ ] Resolution or UI-scale changes clamp the widget on-screen.
- [ ] `Reset widget position` returns the widget to the safe default.
- [ ] Turning off `Show status widget` removes it.
- [ ] Relaunching restores widget visibility.

Record observed values and screenshots beneath the applicable check while
testing.
