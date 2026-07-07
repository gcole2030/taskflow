#!/usr/bin/env bash
# Step 0, automated: verify toolchain, docker daemon, auth; pre-pull images.
set -uo pipefail
fail=0
need(){ command -v "$1" >/dev/null 2>&1 && echo "✔  $1 $($1 $2 2>/dev/null | head -1)" || { echo "❌ $1 missing"; fail=1; }; }
need dotnet --version
need docker --version
need node --version
need git --version
need gh --version
need claude --version
docker info >/dev/null 2>&1 && echo "✔  docker daemon reachable" || { echo "❌ docker daemon not running"; fail=1; }
gh auth status >/dev/null 2>&1 && echo "✔  gh authenticated" || { echo "❌ gh not authenticated (gh auth login)"; fail=1; }
[ $fail -ne 0 ] && { echo "── preflight FAILED — fix the above first ──"; exit 1; }
echo "── pulling base images (idempotent) ──"
for img in postgres:16-alpine mcr.microsoft.com/dotnet/sdk:10.0 mcr.microsoft.com/dotnet/aspnet:10.0 node:22-alpine; do
  docker pull -q "$img" && echo "✔  $img"
done
echo "✅ PREFLIGHT PASSED"
