# PRIVATE PREP DOC — do not commit to the submission repo
### Two things live here: (A) the day-of execution budget for the build, and
### (B) the review walkthrough script (~12 min narration + Q&A).
### The public process documentation is the repo README — this file is for your desk.

---

# PART A — DAY-OF EXECUTION BUDGET

Total: ~6 hours wall-clock. When a phase blows its budget, invoke the cut order —
never grind past a gate.

| Phase | Budget | Hard gate |
|---|---|---|
| 0 — Repo genesis (harness-first commit, review workflow live) | 20 min | repo shows harness-only history |
| 1 — Bootstrap slice → PR #1 | 60–75 min | `make gate` + `make smoke` green, PR merged |
| 2 — Core slices → PRs #2–#4 (+ ADR-0003 after PR #3) | 2.5–3 h | per-PR: red→green in CI, AI review resolved, log entry |
| 3 — Frontend → PR #5 | 90 min HARD CAP | clean `docker compose up` → usable UI < 2 min |
| 4 — Packaging → PR #6 | 45 min | `make drill` passes from clean clone |
| Rehearse Part B against the real repo | 20 min | — |

**Cut order when behind (in this exact sequence):**
1. AC11 overdue filter (defer, note in README)
2. Frontend audit-timeline page → 3. priority badge → 4. status filter
   (list + create + transitions are the frontend floor)
Never cut: tests for shipped code, the process log, the per-PR gates.

**Failure modes to avoid:**
- Feature-complete app with an anemic process trail — the process IS the deliverable
- Claiming green without a CI run to point at
- Scope creep (auth, boards, websockets) — contradicts "efficient, short amount of time"
- One giant commit or batched slices — small PRs are the methodology
- Forgetting the "Human corrections" line in log entries — an empty corrections column
  reads as "didn't review"

---

# PART B — WALKTHROUGH SCRIPT — "How I built this with AI"
### A first-person, timed script for the review: ~12 minutes of narration + Q&A.
### Screen-share the repo, nothing else.

---

## The one idea everything hangs on (memorize this paragraph)

> "I treat AI agents the way I'd treat a very fast team of mid-level engineers who
> have no memory between meetings. You don't get quality out of a team like that by
> typing harder — you get it with a written contract, encoded conventions, and gated
> reviews. So before any code existed, I built the harness: a spec that defines *done*,
> a skill that defines *how we do things here*, commands that define *the unit of work*,
> and CI plus review rules that define *what's allowed to merge*. Then I directed
> agents through it and corrected them on the record. The app is the output; the
> harness is the work."

Every question they ask, you answer from that paragraph and then point at a file.

---

## BEAT 1 — Open with the git history, not the app (90 sec)

**Do:** `git log --oneline --reverse | head -15` on screen.

**Say:** "Before I show you the running app, I want to show you the first commit —
because it has no application code in it. It's the harness: the spec, the agent
instructions, a skill, three commands, CI, and the database migration. I committed the
*process* first, deliberately, so the history itself proves the code was born inside
the process rather than the process being decorated on afterward."

**Why this works (for the emulator):** it inverts the expected demo order and
immediately signals you understood what was being graded.

---

## BEAT 2 — The contract: spec as the agent's definition of done (2 min)

**Do:** open `docs/spec-taskmanager.md`, scroll to §5.

**Say:** "The assignment was one sentence, so my first act was to expand it into a
contract: eleven Given/When/Then acceptance criteria, a state machine, and — just as
important — section 9, the list of things I deliberately did *not* build. Agents scope-creep
by default; a written not-list is how you get 'efficient' out of them. Each AC becomes
exactly one integration test against real Postgres, named after the AC — so 'accurate'
isn't my opinion, it's a green CI run you can audit."

**Point at:** one AC (AC6 — the illegal TODO→DONE transition is a nice concrete one),
then the matching test name in the repo: `AC6_Todo_Directly_To_Done_Returns409`.

---

## BEAT 3 — Skills, instructions, commands (3 min — this is the core, their words)

**Do:** open these three in order, ~1 minute each.

1. **`CLAUDE.md`** — "These are the standing *instructions* — loaded into every agent
   session. Stack decisions, workflow rules, and the guardrails: test-first, never claim
   green without running, ask before anything destructive. AGENTS.md mirrors it in the
   open standard so a second agent — other coding agents — obeys the same contract."

2. **`.claude/skills/vertical-slice/SKILL.md`** — "This is a *skill* — team conventions
   as code. Folder layout, Dapper patterns, the non-negotiable order of work: read the AC,
   write the failing test, migrate, implement, format, prove green. On a real team this is
   the document that makes ten agents produce code that looks like one engineer wrote it.
   I never had to re-explain conventions in a prompt; the skill triggers itself."

3. **`.claude/commands/implement-slice.md`** — "And these are *commands* — the unit of
   work. `/implement-slice transitions AC4-AC7` gives the agent a bounded, repeatable
   procedure: branch, red test shown to me, implement, green run shown to me, commit,
   log the session. `/finish-pr` gates the exit. The agent never free-styles the workflow."

**Also flash:** `.claude/settings.json` — "Permissions are allowlisted — the agent can
build, test, and commit freely but can't push, force-reset, or read secrets without me.
And a post-write hook auto-formats every file it touches, so style never reaches review."

---

## BEAT 4 — The evidence trail: one PR, end to end (2.5 min)

**Do:** open the transitions PR (your PR #3) in GitHub.

**Say, walking downward through the page:** "Here's one slice, end to end. The PR body
names the spec sections and the AC tests. First commit is the failing tests — you can
open the CI run on that commit and see red. Second commit turns them green. The
second Claude agent reviewed it under its own instruction file, which tells it to check the things that
actually matter in this domain — task and event written in one transaction, illegal
transitions returning 409, no mocked databases in integration tests. And here —"

**THE MONEY MOMENT — tell your correction story.** Open `docs/AI-DEVELOPMENT.md`, find
the entry where you rejected agent output. Template for the story:

> "In this session the agent [did X wrong — e.g., wrote the status update and the event
> insert as two separate calls / reached for EF Core / mocked the repository in an
> integration test]. It compiled and the tests it wrote passed — which is exactly why
> this matters. I caught it because [the skill/spec says Y]. I rejected it, pointed at
> the convention, and it corrected in one pass. That's the job now: the agent produces,
> the senior engineer holds the line on invariants the tests don't cover yet."

**Why this works:** every reviewer fears "AI slop merged blind." One documented,
specific correction disarms that fear more than any amount of green CI.

---

## BEAT 5 — Now, and only now, the running app (2 min)

**Do:** in a clean folder, live: `git clone <repo> && cd <repo> && docker compose up`.
While it builds, narrate; when healthy, open :3000, create a task, walk it
TODO → IN_PROGRESS → DONE, show the disabled illegal buttons, open the audit timeline.

**Say:** "Clean clone to working system, one command, under two minutes — that's the
whole deployment story. The assignment scoped deployment to Docker, so I resisted adding
cloud IaC; it would have been performative complexity against a brief that graded
efficiency. The UI enforces the same state machine the API does — the disabled buttons
you see are driven by the legal-transition map, and the timeline is the append-only
event stream from AC8."

---

## BEAT 6 — Close with the scale answer (60 sec)

**Say:** "What you're seeing is a one-project version of something that scales: the spec
becomes a per-feature spec process, the skill becomes a library of team conventions,
the commands become the paved road, and AI-DEVELOPMENT.md becomes normal PR hygiene.
Given a week I'd add the transactional outbox, auth, and projections — they're already
named in the cut list, which is where scope goes to be honest. Happy to go deeper on
any file."

---

## Q&A BANK (answer + file to open)

| Likely question | Answer in one breath | Then open |
|---|---|---|
| Why Dapper, not EF Core? | Two tables, transactional task+event invariant, reviewability — every query is readable SQL. Revisit past ~10 aggregates. | `docs/adr/0001` |
| Why vertical slices? | Agents work best with small bounded context: one folder + one skill + one spec section per task. Also PRs review as whole features. | `docs/adr/0002` |
| How do you stop AI hallucinating features? | The not-list (spec §9) plus CLAUDE.md rule: nothing without an AC. Scope creep becomes a contract violation, not a taste debate. | spec §9 |
| How do you know the tests are real? | Testcontainers against postgres:16, through the real HTTP pipeline; CI is the arbiter, and the compose smoke job boots the whole stack. | `.github/workflows/ci.yml` |
| What did the AI get wrong? | [Your correction story — never say "nothing"; "nothing" reads as "I didn't review".] | `AI-DEVELOPMENT.md` |
| What would you do differently? | Pick one honestly (e.g., "I'd write the frontend skill before Phase 3 — I re-prompted conventions I could have encoded"). | — |
| Where's the AI in the product? | The brief graded AI in the *process*. Product AI (NL task entry, summarization) is a clean extension point on the events stream. | — |
| Security? | Allowlisted agent permissions, secrets unreadable by hooks/agents, parameterized SQL only, containers run non-root. Auth is a documented cut. | `.claude/settings.json` |

---

## EMULATION CHECKLIST — to reproduce this shape on any project

1. **Expand the brief into a contract first** (ACs + a not-list). No code before it exists.
2. **Commit the harness before the app.** The history is the exam paper.
3. **Encode conventions once** (skill), **rules once** (CLAUDE.md/AGENTS.md), **workflow
   once** (commands). If you re-type it in a prompt twice, it belongs in a file.
4. **Make the agent show red before green.** Never accept "tests pass" without the run.
5. **Gate merges with a second AI + CI + yourself.** Different reviewers catch different sins.
6. **Log every session** — ask, output, correction, elapsed. The corrections are the résumé.
7. **Demo the history first, the app last.** The app proves it works; the history proves *you* work.
