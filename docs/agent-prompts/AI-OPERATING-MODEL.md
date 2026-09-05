# AI-First Operating Model

## Goal

The owner should spend most of their time expressing taste, intent and decisions. AI should inspect, implement, generate alternatives, test, capture evidence, update documentation and prepare the next decision.

## The durable context stack

Store thinking in the repository in four layers:

```text
AGENTS.md
  permanent rules for every coding session

docs/current/PRODUCT-TRUTH.md
  current canon, player fantasy, visual and combat pillars

docs/current/CURRENT-SLICE.md
  the one outcome currently being built and its acceptance gate

docs/current/DECISION-LOG.md
  owner approvals, rejections, taste observations and superseded ideas
```

Add `docs/current/HANDOFF.md` for exact implementation state, evidence and next action.

This follows the Codex model of repository-scoped persistent instructions through `AGENTS.md`. Keep the root file concise and link to the fuller context rather than placing the entire lore bible inside it.

## Owner workflow

For each slice, the owner supplies:

- desired feeling;
- references and what specifically matters about them;
- hard lore rules;
- known dislikes;
- one acceptance question.

The agent supplies:

- repository inspection;
- implementation;
- asset candidates;
- tuning;
- builds and checks;
- visual scenarios;
- screenshots/capture paths;
- current limitations;
- updated decision/handoff documents;
- one recommended next action.

The owner returns only:

- **APPROVE** and the strongest aspect;
- **REVISE** plus the largest mismatch;
- **REJECT** plus what felt wrong.

The agent then writes that feedback into `DECISION-LOG.md` and adjusts future work.

## Prompt design

Each long session prompt should specify:

1. **Outcome:** what reviewable product must exist at the end.
2. **Current truth:** files that are authoritative.
3. **Scope:** what may change.
4. **Non-goals:** systems that must not be built yet.
5. **Acceptance:** gameplay and visual evidence required.
6. **Autonomy:** continue through routine choices and document assumptions.
7. **Stop condition:** stop only at a real owner taste decision or external blocker.
8. **Handoff:** exact files and evidence to update.

Official OpenAI guidance recommends outcome-focused prompts with explicit success criteria, autonomy and verification, and documents that Codex loads repository instructions from `AGENTS.md` before work begins.

## AI visual loop

For visible changes:

```text
capture current baseline
        ↓
identify the three largest visible weaknesses
        ↓
implement one coherent hypothesis
        ↓
capture the same scenarios
        ↓
compare at native size and grayscale
        ↓
agent ranks remaining issues
        ↓
owner approves/revises/rejects
        ↓
decision becomes repository context
```

Avoid sessions that generate dozens of disconnected assets. Generate only the assets needed by the current playable slice, promote the best candidate and keep rejected candidates outside runtime Content.

## Session persistence

The agent should update `docs/current/HANDOFF.md` throughout the session, not only at the end. At minimum record:

- current commit and dirty state;
- completed changes;
- decisions and assumptions;
- build/run commands;
- screenshots and scenario names;
- known defects;
- exact next unfinished action.

If a task is interrupted, a new task reads `AGENTS.md`, `PRODUCT-TRUTH.md`, `CURRENT-SLICE.md`, `DECISION-LOG.md`, `HANDOFF.md` and git status before continuing. It must not restart completed work.

## Use of multiple AI agents

Parallel work is useful only when branches do not collide. Good parallel assignments:

- one agent implements gameplay;
- one audits visual captures;
- one creates asset briefs/candidates outside runtime Content;
- one checks lore/document consistency.

Avoid parallel edits to `GameWorld.cs`, `Player.cs` or the same asset manifest. Integrate through one owning agent.

## Quality rule

AI is allowed to make routine engineering and content choices. It is not allowed to decide whether the game feels right. That final aesthetic and product judgment remains the owner’s approval gate.

## Sources

- [Official Codex AGENTS.md documentation](https://learn.chatgpt.com/docs/agent-configuration/agents-md)
- [Official OpenAI model prompting guidance](https://developers.openai.com/api/docs/guides/latest-model)
