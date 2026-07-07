# AGENTS.md
Open-standard agent instructions. Canonical, detailed instructions live in CLAUDE.md;
this file exists so any non-Claude agent used in this repo gets the same contract.

- Source of truth: docs/spec-taskmanager.md. Implement only what an AC covers.
- Test-first: failing Testcontainers AC test before implementation. Real Postgres only.
- Vertical slices per .claude/skills/vertical-slice/SKILL.md.
- Dapper + parameterized SQL; DbUp additive migrations; task+event writes share one tx.
- Conventional commits; short-lived branches; never push or merge without confirmation.
- After each slice: append docs/AI-DEVELOPMENT.md entry.
