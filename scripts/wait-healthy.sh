#!/usr/bin/env bash
set -euo pipefail
for i in $(seq 1 30); do
  if curl -fsS http://localhost:8080/readyz >/dev/null 2>&1; then
    echo "✅ api healthy"; 
    if curl -fsS http://localhost:3000 >/dev/null 2>&1; then echo "✅ web healthy"; fi
    exit 0
  fi
  sleep 4
done
echo "❌ stack did not become healthy"; docker compose logs --tail=50; exit 1
