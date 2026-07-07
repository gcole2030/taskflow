#!/usr/bin/env bash
# Finds the first prompt in prompts/ whose slice branch hasn't been merged to main,
# and runs it. State lives in git, not a state file.
set -euo pipefail
git fetch -q origin main 2>/dev/null || true
for pf in prompts/*.md; do
  slice=$(basename "$pf" .md | sed 's/^[0-9]*-//')
  [ "$slice" = "packaging" ] && branch="chore/packaging" || branch="slice/$slice"
  if git log origin/main --oneline 2>/dev/null | grep -qiE "\(#[0-9]+\)|$slice"; then
    # crude check: was a commit mentioning this slice merged?
    if git log origin/main --grep="$slice" --oneline | grep -q .; then continue; fi
  fi
  echo "→ next up: $slice"
  exec bash scripts/slice-runner.sh "$pf"
done
echo "✅ all prompts consumed — run 'make drill' for the final gate"
