# ADR-0001: Dapper + Npgsql instead of EF Core
Date: 2026-07-06 · Status: Accepted

## Context
The domain is two tables with an append-only event stream and transactional
task+event writes (spec AC1, AC4, AC12-adjacent invariant). The assignment weights
efficiency and transparency of process; reviewers must be able to read every query.

## Decision
Dapper over Npgsql with hand-written parameterized SQL, SQL colocated with each
vertical slice. DbUp for plain-SQL versioned migrations run at API startup.

## Consequences
+ Every query is visible and reviewable; no LINQ translation surprises.
+ Transaction boundaries (task + event in one tx) are explicit.
+ Migrations are the same SQL Postgres runs — no model drift.
− No change tracking; PATCH semantics are hand-rolled (acceptable at this size).
− More boilerplate per slice; mitigated by the vertical-slice skill templates.
Revisit if the model grows past ~10 aggregates or needs complex object graphs.
