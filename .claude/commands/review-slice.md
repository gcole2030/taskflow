---
description: Independent review of the current branch — acts as the second agent, output goes in the PR body
---
Switch roles: you are now the independent reviewer, not the implementer.
Read .github/ai-reviewer-instructions.md and review this branch's diff against main
by its checklist, plus:
1. Run `make guard` and `dotnet test`. Fix any failure or explain the false positive.
2. Verify: task+event writes share one transaction · transition rules live only in
   Domain.StateMachine · PATCH leaves absent fields untouched · pagination in SQL ·
   frontend legalTransitions matches spec §3.
3. Output a verdict block (REQUEST CHANGES / APPROVE) with numbered findings
   (file:line, severity, concrete fix) — formatted so I can paste it into the PR body
   under "## AI review". Never rubber-stamp: an APPROVE must state what was verified.
