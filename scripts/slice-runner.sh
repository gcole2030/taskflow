#!/usr/bin/env bash
# The slice loop, orchestrated. Human judgment stays in the loop at three points:
# the agent session itself, the gate review, and the merge.
# Usage: scripts/slice-runner.sh <prompt-file>   (or via `make next`)
set -euo pipefail
pf="$1"
[ -f "$pf" ] || { echo "❌ prompt file not found: $pf"; exit 1; }
slice=$(basename "$pf" .md | sed 's/^[0-9]*-//')
if [ "$slice" = "packaging" ]; then branch="chore/packaging"; else branch="slice/$slice"; fi

echo "══ SLICE: $slice ══"
git checkout main && git pull --ff-only
git checkout -b "$branch"
bash scripts/log-entry.sh "$branch"

echo "── launching Claude Code with the versioned prompt (prompts/$(basename "$pf")) ──"
echo "── YOUR JOB IN THERE: demand the red run, watch the catch-list, correct on the record ──"
claude "$(cat "$pf")"    # interactive session, seeded with the prompt; exits when you exit

echo "── agent session ended; running the gate ──"
if make gate; then
  echo "── gate green ──"
else
  echo "❌ gate failed. Re-enter the session to fix (claude -c), then re-run: make gate && make pr"
  exit 1
fi

read -rp "Reviewed the diff and the log entry? Open the PR now? [y/N] " yn
[ "${yn:-n}" = "y" ] && make pr || echo "→ when ready: make pr"
echo "── after CI + AI review: feed comments back with 'claude -c', then merge in GitHub ──"
echo "── then: make next ──"
