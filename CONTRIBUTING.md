# Contributing / Workflow
Trunk-based. `main` is protected; every change lands via PR from a `slice/*` or
`chore/*` branch. Squash merge. Conventional commits. CI (build + AC tests against
real Postgres via Testcontainers + web lint/build + compose smoke) must be green.
Primary implementation agent: Claude Code, driven through the commands in
`.claude/commands`. Review: independent AI review via /review-slice (verdict pasted in the PR body) + human. Every PR documents
its AI process in the template's "AI process notes" section.
