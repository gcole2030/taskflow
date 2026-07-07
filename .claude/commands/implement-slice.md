---
description: Implement one vertical slice from the spec, test-first
argument-hint: <feature name or AC numbers, e.g. "transitions AC4-AC7">
---
Implement the slice: $ARGUMENTS

Follow the vertical-slice skill exactly. Steps:
1. Quote the relevant acceptance criteria from docs/spec-taskmanager.md verbatim.
2. Create branch `slice/$ARGUMENTS` (kebab-case).
3. Write the failing integration tests first; run `dotnet test` and show the red output.
4. Add a migration only if schema is needed.
5. Implement until green. Show the green `dotnet test` summary.
6. Run `dotnet format`. Commit with conventional commits (test commit separate from feat).
7. Append the AI-DEVELOPMENT.md entry (timestamped) describing this session.
Do not touch code outside this slice. Do not merge — stop after committing.
