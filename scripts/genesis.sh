#!/usr/bin/env bash
# Step 1, automated: harness-first first commit + repo creation.
# Refuses to run if application code already exists (protects the history story).
set -euo pipefail
[ -d src ] || [ -d web/src ] && { echo "❌ application code already present — genesis must be the FIRST commit"; exit 1; } || true
[ -d .git ] && { echo "❌ .git already exists — genesis runs once, on a fresh copy"; exit 1; }
git init -b main
git add -A
git commit -m "chore: AI-native development harness (spec, skill, commands, prompts, hooks, CI)"
read -rp "GitHub repo name [taskman]: " name; name=${name:-taskman}
gh repo create "$name" --private --source=. --push
echo "✅ Harness-first commit pushed."
echo "→ Manual (2 min, by design): on free-plan private repos branch protection isn't enforced — the gates below are the"
echo "  enforcement (make gate + CI + review discipline: never merge red), then: make next"
