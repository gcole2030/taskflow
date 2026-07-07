#!/usr/bin/env bash
# Mechanized catch-lists: the mistakes agents actually make, checked in seconds.
# Exit nonzero on any violation. Add a check every time a human review catches
# something new — that's how the harness learns.
set -uo pipefail
fail=0
say(){ printf '%s\n' "$*"; }
viol(){ say "❌ $1"; fail=1; }
ok(){ say "✔  $1"; }

# 1. No EF Core anywhere (ADR-0001)
if grep -rIl --include='*.csproj' 'Microsoft.EntityFrameworkCore' src tests 2>/dev/null | grep -q .; then
  viol "EF Core reference found — ADR-0001 forbids it"
else ok "no EF Core"; fi

# 2. No raw DateTime.UtcNow/Now outside the Clock abstraction (testability)
hits=$(grep -rn --include='*.cs' -E 'DateTime(OffsetJson)?\.(UtcNow|Now)' src 2>/dev/null | grep -v 'Common/Clock' || true)
if [ -n "$hits" ]; then viol "raw system clock usage:"; say "$hits"; else ok "clock abstraction respected"; fi

# 3. No interpolated/concatenated SQL (injection + review risk)
hits=$(grep -rn --include='*.cs' -E '(QueryAsync|ExecuteAsync|QuerySingle|QueryFirst)[^;]*\$"' src 2>/dev/null || true)
if [ -n "$hits" ]; then viol "interpolated string passed to Dapper — parameterize:"; say "$hits"; else ok "SQL parameterized"; fi

# 4. Applied migrations are immutable (compare against main)
if git rev-parse --verify origin/main >/dev/null 2>&1; then
  changed=$(git diff --name-only origin/main -- db/migrations/ | while read -r f; do
    git cat-file -e "origin/main:$f" 2>/dev/null && echo "$f"; done)
  if [ -n "$changed" ]; then viol "existing migration(s) modified — add a new file instead:"; say "$changed";
  else ok "migrations additive"; fi
fi

# 5. Integration tests must not mock the repository layer
hits=$(grep -rn --include='*.cs' -E 'Mock<.*Repository|Substitute\.For<.*Repository' tests/Api.IntegrationTests 2>/dev/null || true)
if [ -n "$hits" ]; then viol "mocked repository in integration tests — use real Postgres:"; say "$hits"; else ok "integration tests use real DB"; fi

# 6. Every AC in the spec has a test named for it
if [ -d tests/Api.IntegrationTests ]; then
  for n in $(grep -oE '^\- \*\*AC[0-9]+' docs/spec-taskmanager.md | grep -oE '[0-9]+'); do
    grep -rqE "AC${n}_" tests/Api.IntegrationTests --include='*.cs' \
      && ok "AC$n test present" || say "⚠  AC$n has no test yet (fine mid-build, blocker at submission)"
  done
fi

# 7. Console.WriteLine ban (Serilog only)
hits=$(grep -rn --include='*.cs' 'Console.WriteLine' src 2>/dev/null || true)
if [ -n "$hits" ]; then viol "Console.WriteLine found — use Serilog:"; say "$hits"; else ok "structured logging only"; fi

[ $fail -eq 0 ] && say "── guardrails clean ──" || say "── guardrails FAILED ──"
exit $fail
