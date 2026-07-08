# Automation layer — every manual gate from EXECUTION-WALKTHROUGH.md as one command.
.PHONY: gate guard test unit integration web-test smoke drill slice adr pr log

## The whole pre-PR gate: guardrails + full test pyramid + lint
gate: guard test web-check
	@echo "✅ GATE PASSED — run 'make pr' or /finish-pr"

## Static guardrails (the catch-lists, mechanized)
guard:
	@bash scripts/guardrails.sh

test:
	dotnet test --nologo

unit:
	dotnet test tests/Api.UnitTests --nologo

integration:
	dotnet test tests/Api.IntegrationTests --nologo

web-check:
	@if [ -d web ]; then cd web && npm run lint && npm run test --if-present && npm run build; else echo "(web not built yet — skipped)"; fi

## Boot the stack and verify health, then tear down
smoke:
	docker compose up -d --build
	@bash scripts/wait-healthy.sh
	docker compose down

## The final clean-clone drill, timed (run from anywhere inside the repo)
drill:
	@bash scripts/clean-clone-drill.sh

## Start a slice: branch + AI-DEVELOPMENT.md stub in one step
## usage: make slice NAME=transitions-ac4-ac8
slice:
	git checkout -b slice/$(NAME)
	@bash scripts/log-entry.sh "slice/$(NAME)"
	@echo "→ now run: claude, then /implement-slice $(NAME)"

## Create the PR with gh, body from template, request Copilot review
pr:
	@bash scripts/open-pr.sh

## Step 0 automated: toolchain + daemon + auth checks, image pre-pulls
preflight:
	@bash scripts/preflight.sh

## Step 1 automated: harness-first first commit + gh repo creation (refuses if code exists)
genesis:
	@bash scripts/genesis.sh

## Run the next unmerged slice from prompts/ (branch → log stub → claude session → gate → pr)
next:
	@bash scripts/next-slice.sh

## Run a specific slice: make run PROMPT=prompts/30-transitions.md
run:
	@bash scripts/slice-runner.sh $(PROMPT)
