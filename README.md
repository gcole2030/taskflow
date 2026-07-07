# Task Management System — AI-Native Development Runbook

This is the runbook I follow to build this project with AI agents doing the implementation
under my direction. I wrote it before writing any application code, because that's the
point: the process is designed first, committed first, and the code is produced through it.

My working principles, in one paragraph: I treat AI agents like a fast team of mid-level
engineers with no memory between sessions. You don't get quality out of a team like that
by typing better prompts — you get it with a written contract, encoded conventions, and
gated reviews. So the spec defines *done*, the skill defines *how we do things here*, the
commands define *the unit of work*, and CI plus review rules define *what's allowed to
merge*. Everything below is just the disciplined execution of that idea.

Key files: [`docs/spec-taskmanager.md`](docs/spec-taskmanager.md) (the contract) ·
[`CLAUDE.md`](CLAUDE.md) / [`AGENTS.md`](AGENTS.md) (standing agent instructions) ·
[`.claude/skills/vertical-slice`](.claude/skills/vertical-slice/SKILL.md) (conventions as code) ·
[`.claude/commands`](.claude/commands/) (the workflow verbs) ·
[`Makefile`](Makefile) + [`scripts/`](scripts/) (mechanized gates) ·
[`docs/AI-DEVELOPMENT.md`](docs/AI-DEVELOPMENT.md) (the session-by-session process log).

---

## How the automation is layered

The whole runbook below can be driven with five commands. The prompts I feed the agent
are versioned files in [`prompts/`](prompts/) — the process is reproducible from the
repo itself, not from my shell history.

```bash
make preflight   # Step 0: toolchain, docker daemon, gh auth, image pre-pulls
make genesis     # Step 1: harness-first first commit + repo creation
                 #         (refuses to run if application code already exists)
make next        # Steps 3–6: runs the next unmerged slice — branch, log stub,
                 #         Claude Code session seeded with the versioned prompt,
                 #         gate on exit, PR on my confirmation
make drill       # final gate: timed clean-clone boot + full test run
```

What `make next` deliberately does NOT do: it never runs the agent headless, never
opens a PR without my confirmation, and never merges. The three points where judgment
lives — the session itself, the gate review, the merge — stay manual. Automation here
removes ceremony, not oversight.

---

## Step 0 — Machine prerequisites

`make preflight` verifies all of this; listed here for transparency:

```bash
dotnet --version          # 10.0.x SDK
docker --version && docker compose version
docker run --rm hello-world               # daemon works — Testcontainers depends on it
node --version            # 22.x
gh --version && gh auth status
claude --version
```

It also pre-pulls images so builds aren't waiting on downloads mid-session:

```bash
docker pull postgres:16-alpine
docker pull mcr.microsoft.com/dotnet/sdk:10.0
docker pull mcr.microsoft.com/dotnet/aspnet:10.0
docker pull node:22-alpine
```

## Step 1 — Repo genesis: harness before code

The first commit contains zero application code — only the harness: spec, agent
instructions, skill, commands, permissions/hooks, CI, migration, compose files. I do
this deliberately so the git history shows the process existed before the code did.

```bash
git init -b main
git add -A
git commit -m "chore: AI-native development harness (spec, skill, commands, hooks, CI)"
gh repo create --private --source=. --push
```

This runs on a free GitHub plan, private, so branch protection isn't GitHub-enforced —
the equivalent discipline is process: every change goes through a PR, `make gate` and
CI must be green, and nothing merges red. Every PR also gets an independent AI review
before merge: `/review-slice` puts the agent in reviewer role against
[`.github/ai-reviewer-instructions.md`](.github/ai-reviewer-instructions.md) — a
different instruction file than the implementer follows — and its verdict is pasted
into the PR body under "## AI review".

## Step 2 — Prove the harness before using it

First Claude Code session, before any real work:

```
Read CLAUDE.md and the vertical-slice skill, then tell me in 5 bullets what the
workflow rules are and what you're not allowed to do. Don't touch any files.
```

If the playback misses a rule (test-first, no EF Core, conventional commits, ask before
anything destructive, log every session), the instructions are ambiguous — I fix the
wording now and commit the fix. Harness bugs are bugs. I also confirm `/implement-slice`,
`/review-slice`, `/finish-pr`, and `/write-adr` show up in the command list.

## Step 3 — Bootstrap slice → PR #1

Prompt:

```
/implement-slice bootstrap — solution skeleton

Create the .NET 10 solution per CLAUDE.md and the vertical-slice skill:

1. taskman.sln with:
   - src/Api (minimal API): Serilog console logging, NpgsqlDataSource from
     ConnectionStrings__Db, DbUp running db/migrations/*.sql as embedded resources
     on startup, /healthz (no DB) and /readyz (SELECT 1), Common/ with a Db helper,
     enum text handler, ProblemDetails helpers, and Domain/ containing TaskStatus +
     Priority enums and a pure static StateMachine exposing CanTransition(from, to)
     and LegalTargets(from) per spec §3.
   - tests/Api.UnitTests (xUnit): exhaustive table-driven tests for StateMachine —
     every legal transition true, every illegal pair false, terminal states have
     zero legal targets.
   - tests/Api.IntegrationTests (xUnit + Testcontainers): a PostgresFixture
     collection fixture starting postgres:16-alpine, running the DbUp migrations,
     a WebApplicationFactory wired to the container, truncation reset between test
     classes, and one smoke test: GET /readyz returns 200.
2. Make `docker compose up` bring up db+api with /readyz healthy.
3. Show me the `dotnet test` output proving both test projects run and pass.
Branch slice/bootstrap, conventional commits, AI-DEVELOPMENT.md entry.
```

My review checklist while it works — the mistakes I specifically watch for:

- EF Core sneaking into a csproj (ADR-0001 forbids it)
- migrations read from a disk path instead of embedded resources (breaks in the container)
- `/readyz` returning 200 without actually touching the database
- integration tests invoking handlers directly instead of going through HTTP

Gate before the PR:

```bash
make gate     # guardrails + unit + integration (+ web once it exists)
make smoke    # compose up, health-wait, down
```

Then `/finish-pr` → `make pr` → wait for CI. Review findings get fed
back to the agent verbatim: *"The reviewer says: <paste>. Evaluate whether it's right; fix or
rebut with reasoning."* The rebuttals stay in the PR thread. Squash-merge, log elapsed
time in the process log.

## Step 4 — Core slices, test-first → PRs #2–#4

The test pyramid for this codebase:

- **Unit tests** (fast, zero I/O): the pure StateMachine, validators, any extracted pure
  logic. Time-based rules use an injected clock — a validator calling `DateTime.UtcNow`
  directly is untestable and gets rejected.
- **Integration tests** (Testcontainers, real Postgres, real HTTP pipeline): exactly one
  per acceptance criterion, named `AC<N>_...` so the spec-to-test mapping is auditable.
- **No mocked-database middle layer.** A mocked repository in an integration test defeats
  the reason this stack exists; `make guard` flags it mechanically.

### PR #2 — create & get

```
/implement-slice create-and-get AC1 AC2 AC3

Scope: POST /api/v1/tasks (Idempotency-Key via the idempotency_keys table, UUIDv7 ids,
CREATED event in the SAME transaction as the insert), GET /api/v1/tasks/{id}
(404 problem+json when absent).
Unit tests: CreateTaskValidator — title required/length, description length, due_date
not in the past via an injected clock.
Integration tests: AC1, AC2, AC3 — write them FIRST, show me the red dotnet test
output, then implement to green.
```

Watching for: idempotency done as read-then-insert with no handling of the unique-violation
race (the PK on `key` is the guard — the code must catch that path), and the event insert
drifting outside the transaction.

### PR #3 — transitions & events

```
/implement-slice transitions AC4 AC5 AC6 AC7 AC8

Scope: POST /api/v1/tasks/{id}/transitions with body { "to": ..., "metadata": {...} },
enforced by Domain.StateMachine only — no duplicated transition logic in SQL or the
endpoint; 409 problem+json naming the illegal transition; STATUS_CHANGED event with
from/to/metadata in the same transaction; GET /api/v1/tasks/{id}/events chronological.
Red first: AC4–AC8 integration tests, show me the failing run.
```

Watching for: transition rules re-implemented inline (drift risk — one source of truth),
400 where the spec says 409, events queried without a deterministic ORDER BY.

Immediately after merge, while context is hot:

```
/write-adr state machine enforced in domain code, mirrored by DB CHECK constraints
```

### PR #4 — list, patch, filters

```
/implement-slice list-patch AC9 AC10 AC11

Scope: PATCH /api/v1/tasks/{id} for title/description/priority/due_date (partial —
absent fields untouched; updated_at advances; UPDATED event carrying a jsonb diff of
changed fields). GET /api/v1/tasks with status/priority/overdue filters, page/pageSize
(defaults 1/20, cap 100), X-Total-Count from a windowed count.
Overdue means due_date < today AND status NOT IN (DONE, CANCELLED).
Unit tests: the filter builder if extracted; the PATCH field-merge logic.
Integration: AC9, AC10, AC11 red-first.
```

Watching for: dynamically built SQL that concatenates values instead of parameters
(`make guard` catches the obvious cases; I read the filter builder line by line anyway),
pagination done in memory instead of LIMIT/OFFSET, and the overdue filter forgetting the
terminal-status exclusion — AC11 exists precisely to test that.

Per-PR gate, no exceptions: red run visible in the first commit → green in CI → AI
review resolved fix-or-rebut → process log entry with elapsed time → squash-merge. Three
separate PRs, never batched — small slices are the whole methodology.

## Step 5 — Frontend slice → PR #5 (90-minute cap)

```
/implement-slice web — Next.js frontend per spec §6

Scaffold in web/: Next.js App Router, TypeScript, output:'standalone', TanStack Query.
- tasks list: status filter tabs, priority badge, overdue highlight
- create form: client mirrors server rules but ALWAYS renders API field errors inline
- status actions: buttons driven by a legalTransitions map identical to spec §3;
  illegal targets disabled
- detail page: audit timeline from /events
Testing: Vitest + React Testing Library for exactly two things — the legalTransitions
map (table-driven mirror of the backend StateMachine) and create-form rendering of a
mocked 400 problem+json. Add npm run test to CI. Wire into compose via Dockerfile.web.
```

If the cap gets tight, I cut from the bottom: timeline page first, then priority badge,
then the filter. List + create + transitions are the floor. Watching for: the frontend
inventing its own transition rules that drift from spec §3 (the mirrored map plus its
test is the guard), errors swallowed into a generic toast, and a missing
`output:'standalone'` (the Dockerfile depends on it).

Gate:

```bash
cd web && npm run lint && npm run test && npm run build && cd ..
docker compose down -v && docker compose up -d --build
# http://localhost:3000 — create a task, walk TODO→IN_PROGRESS→DONE, timeline shows 3 events
```

## Step 6 — Packaging → PR #6

```
Final packaging PR on branch chore/packaging:
1. Rewrite README.md as the product README: 3-command quickstart, Mermaid architecture
   diagram, link to the spec, a "Test strategy" section (unit: pure domain · integration:
   one test per AC on real Postgres · frontend: transition map + error rendering), a
   "Deliberate scope cuts" section from spec §9 with one-line rationale each, and a
   "How this was built" section pointing at CLAUDE.md, the skill, the commands, and
   docs/AI-DEVELOPMENT.md.
2. Generate CHANGELOG.md from the conventional commit history.
3. Add a summary to the top of AI-DEVELOPMENT.md: slices shipped, total elapsed, and
   each human correction in one line.
Do not modify application code in this PR.
```

Final gate — the clean-clone drill, timed:

```bash
make drill    # clones fresh into /tmp, compose up, health-checks, full test run
```

Target: healthy in under two minutes post-pull. If it fails, the fix is one more PR —
a `fix: works from clean clone` commit with a log entry beats a repo that only runs on
my machine.

## Step 7 — Definition of complete

- [ ] First commit is harness-only; the log shows process before code
- [ ] 6+ merged PRs, each with: spec ACs in the body, red-then-green commits, CI checks, an AI review thread resolved fix-or-rebut
- [ ] Full pyramid green in CI: StateMachine + validator unit tests, 11 AC integration tests on real Postgres, 2 frontend tests, compose smoke
- [ ] AC test names map 1:1 to spec §5
- [ ] `docs/AI-DEVELOPMENT.md` has an entry per session with elapsed times and the corrections I made
- [ ] ADRs 0001–0003 recorded
- [ ] `make drill` passes from a clean clone
- [ ] Nothing exists outside spec §6; everything in §9 is absent and documented

## Troubleshooting notes (from experience)

1. **Testcontainers can't reach Docker** → `export DOCKER_HOST=unix:///var/run/docker.sock`,
   or enable the default Docker socket in Docker Desktop. CI's ubuntu-latest just works.
2. **.NET 10 base image tag doesn't resolve** → check the current tag suffix on Docker Hub
   (e.g. `10.0-noble`), fix both stages of `Dockerfile.api`, commit as `fix(docker):`.
3. **DbUp finds zero scripts inside the container** → the .sql files aren't embedded
   resources; the csproj needs `<EmbeddedResource Include="../../db/migrations/*.sql" />`.
4. **Next standalone container 404s static assets** → the COPY lines for `.next/static`
   and `public` are missing; compare against `Dockerfile.web`.

## The rule I hold myself to

Anything that goes wrong mid-build is not a detour. It gets captured in the process log
and fixed through the agent on a branch, on the record. A development log with real
corrections in it is worth more than a spotless one — the corrections are where the
engineering actually happened.
