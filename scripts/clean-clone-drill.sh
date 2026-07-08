#!/usr/bin/env bash
# The final submission gate: clone fresh, boot, verify, report elapsed.
set -euo pipefail
url=$(git remote get-url origin)
dir=$(mktemp -d)/fresh
echo "cloning $url → $dir"
git clone --quiet "$url" "$dir"; cd "$dir"
start=$(date +%s)
docker compose up -d --build
bash scripts/wait-healthy.sh
end=$(date +%s)
echo "⏱  clean clone → healthy in $((end-start))s (target <120s post-pull)"
dotnet test --nologo
docker compose down -v
echo "✅ CLEAN-CLONE DRILL PASSED"
