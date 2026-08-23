# Domain docs

This is a single-context repository.

## Before exploring

Read these sources when they exist:

- `CONTEXT.md` at the repository root
- Relevant ADRs under `docs/adr/`

Proceed silently when either source is absent. Do not propose creating it merely because it is missing. The `domain-modeling` skill creates domain documentation when the project resolves terminology or architectural decisions.

## File structure

```text
/
|-- CONTEXT.md
|-- docs/
|   `-- adr/
`-- src/
```

## Use the glossary vocabulary

When an issue, proposal, hypothesis, or test names a domain concept, use the term defined in `CONTEXT.md`. Do not substitute another term that the glossary rejects.

If the required concept is missing, reconsider whether the project already uses a different term. If the gap is real, record it for the `domain-modeling` skill.

## Flag ADR conflicts

If proposed work conflicts with an existing ADR, name the conflict instead of silently overriding the decision.
