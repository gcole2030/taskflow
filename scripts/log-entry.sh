#!/usr/bin/env bash
# Appends a timestamped AI-DEVELOPMENT.md stub so no session goes unlogged.
set -euo pipefail
cat >> docs/AI-DEVELOPMENT.md << ENTRY

## $(date '+%Y-%m-%d %H:%M') — ${1:-session}
- **Agent & entry point:** Claude Code, /implement-slice
- **Spec reference:** <!-- ACs -->
- **What the agent did:** <!-- branch, red run, migration, green run -->
- **Human corrections:** <!-- REQUIRED: what you rejected/redirected, or 'none — reviewed X, Y, Z' -->
- **Review:** <!-- AI reviewer findings, fix-or-rebut -->
- **Elapsed:** <!-- start noted at $(date '+%H:%M') -->
ENTRY
echo "→ AI-DEVELOPMENT.md stub appended (started $(date '+%H:%M'))"
