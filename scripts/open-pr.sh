#!/usr/bin/env bash
set -euo pipefail
branch=$(git rev-parse --abbrev-ref HEAD)
[ "$branch" = "main" ] && { echo "on main — nothing to PR"; exit 1; }
git push -u origin "$branch"
gh pr create --fill --template PULL_REQUEST_TEMPLATE.md 2>/dev/null || gh pr create --fill
echo "(claude-review workflow triggers automatically on PR open)"
gh pr view --web
