# ADR-0002: Vertical slice architecture with minimal APIs
Date: 2026-07-06 · Status: Accepted

## Context
AI agents implement features from Given/When/Then acceptance criteria. Layered
(n-tier) architecture scatters one feature across many folders, which enlarges
agent context windows and invites cross-feature regressions.

## Decision
One folder per feature (endpoint + repository + validator), shared code only in
Common/. Each slice maps to a spec AC group, one branch, one PR. Conventions are
encoded in .claude/skills/vertical-slice so agents follow them without re-prompting.

## Consequences
+ Agent context per task is one small folder + the skill + the spec section.
+ PRs review as complete features; the AC→test→code trace is linear.
− Some SQL duplication between slices; accepted deliberately at this scale.
