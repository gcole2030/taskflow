---
description: Prepare the current branch for PR
---
Prepare this branch for a pull request:
1. Run `make gate` (guardrails + full test pyramid + web lint/test/build). All green or stop and fix.
2. `git log main..HEAD --oneline` — verify conventional commits; squash-fix if messy.
3. Draft the PR body using .github/PULL_REQUEST_TEMPLATE.md: spec sections covered,
   AC test names and their status, migrations added, AI-DEVELOPMENT.md entry link,
   and anything a human reviewer should scrutinize.
4. Output the PR title + body as a fenced block for me to paste (or use `gh pr create`
   if I confirm). Do not push without my confirmation.
