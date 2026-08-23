# Issue tracker: GitHub

Issues and specs for this repo live in the GitHub Issues for
`kobbyist/CS2-ProgressionControls`. Use the `gh` CLI for all operations.

## Conventions

- **Create an issue**: `gh issue create --title "..." --body "..."`
- **Read an issue**: `gh issue view <number> --comments`, also fetching labels.
- **List issues**: `gh issue list --state open --json number,title,body,labels,comments --jq '[.[] | {number, title, body, labels: [.labels[].name], comments: [.comments[].body]}]'` with appropriate `--label` and `--state` filters.
- **Comment on an issue**: `gh issue comment <number> --body "..."`
- **Apply or remove labels**: `gh issue edit <number> --add-label "..."` or `--remove-label "..."`
- **Close an issue**: `gh issue close <number> --comment "..."`

The `gh` CLI infers the repository from `git remote -v` when run inside this clone.

## Pull requests as a triage surface

**PRs as a request surface: no.**

Set this to `yes` if the repository later treats external pull requests as feature requests.

When enabled, pull requests use the same labels and states as issues:

- **Read a pull request**: `gh pr view <number> --comments` and `gh pr diff <number>`.
- **List external pull requests**: `gh pr list --state open --json number,title,body,labels,author,authorAssociation,comments`, keeping only `CONTRIBUTOR`, `FIRST_TIME_CONTRIBUTOR`, or `NONE`.
- **Comment, label, or close**: use `gh pr comment`, `gh pr edit`, or `gh pr close`.

GitHub shares one number sequence across issues and pull requests. For a bare reference such as `#42`, try `gh pr view 42` and then `gh issue view 42`.

## When a skill says "publish to the issue tracker"

Create a GitHub issue.

## When a skill says "fetch the relevant ticket"

Run `gh issue view <number> --comments`.

## Wayfinding operations

The map is one issue with child issues as tickets.

- **Map**: an issue labelled `wayfinder:map`, containing the Notes, Decisions-so-far, and Fog sections.
- **Child ticket**: an issue linked to the map as a GitHub sub-issue. If sub-issues are unavailable, add it to the map's task list and place `Part of #<map>` at the top of the child body. Apply a `wayfinder:<type>` label using `research`, `prototype`, `grilling`, or `task`.
- **Blocking**: use GitHub's native issue dependencies. If unavailable, place `Blocked by: #<n>, #<n>` at the top of the child body.
- **Frontier query**: find the first open child in map order that has no open blocker and no assignee.
- **Claim**: run `gh issue edit <n> --add-assignee @me`.
- **Resolve**: comment with the answer, close the child issue, and add a context pointer to the map's Decisions-so-far section.
