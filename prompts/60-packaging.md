Final packaging on this branch (chore/packaging):
1. Rewrite README.md as the product README: 3-command quickstart, Mermaid architecture
   diagram, link to the spec, a "Test strategy" section (unit: pure domain · integration:
   one test per AC on real Postgres · frontend: transition map + error rendering), a
   "Deliberate scope cuts" section from spec §9 with one-line rationale each, and a
   "How this was built" section pointing at CLAUDE.md, the skill, the commands, the
   prompts/ directory, and docs/AI-DEVELOPMENT.md.
2. Generate CHANGELOG.md from the conventional commit history.
3. Add a summary to the top of AI-DEVELOPMENT.md: slices shipped, total elapsed, and
   each human correction in one line.
Do not modify application code.
