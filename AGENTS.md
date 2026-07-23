# Repository Agent Guidance

## CS2 Internal API Discovery

Use **public references first for discovery**, then local evidence for
implementation.

1. Confirm the active Cities: Skylines II version from the runtime log. Do not
   infer it from an installation-folder name.
2. Before building a custom scanner or runtime probe, search current public
   references for the exact type, field, system, or behavior:
   - official or Paradox-reviewed documentation;
   - public CS2 mod repositories;
   - public API catalogs or decompiled documentation; and
   - adjacent game types when the exact symbol has no results.
3. Use public sources to identify candidate systems, data flow, update order,
   and established mod patterns. Record source URLs, commit SHAs when available,
   freshness, and licenses.
4. Treat public code and generated/decompiled references as provisional pattern
   evidence only. Do not execute fetched repository code or assume its
   signatures match the installed game.
5. Before implementing a game-facing signature or hook, verify it against the
   locally installed assemblies for the active game version.
6. Use a minimal runtime diagnostic only when static references and local
   metadata cannot establish behavior. Keep probes bounded and avoid save
   mutation.
7. Prefer supported systems and public boundaries. Use Harmony only when no
   suitable supported boundary exists, and isolate and document every patch.

## Reuse and Attribution

- Prefer an original implementation.
- Check the license before reusing any public code.
- Preserve required notices and attribution for deliberate reuse.
- Do not copy code from sources with unknown or incompatible licensing.

## Evidence Maintenance

- Keep local assembly verification reports versioned with the project
  documentation.
- Update implementation plans when discovery resolves or changes a technical
  risk.
- When public references and local assemblies disagree, local assemblies govern
  exact implementation details.
