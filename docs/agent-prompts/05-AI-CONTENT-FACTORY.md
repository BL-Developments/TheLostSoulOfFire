# Prompt 05 — Build the AI-Assisted Content Factory

Run this session only after the owner has approved a Golden Combat Slice and at least one playable Death-Layer route. The factory must learn from an approved result, not invent the game’s identity from prose alone.

You are the owning tools and content-production agent.

## Outcome

Create a repository-native workflow that allows future AI sessions to propose, build, validate and present new Soulfire content while preserving the owner’s thinking and approval authority.

The workflow must produce useful assets and content briefs immediately. Do not build an abstract platform with no output.

## Required reading

Read:

- `AGENTS.md`;
- all `docs/current/`;
- approved Golden Slice and prologue handoffs;
- current World Grammar and Region Seeds;
- existing art manifests, asset audit and capture tooling;
- actual Content pipeline and runtime asset loader.

## Deliverables

Create:

### 1. Content request template

One structured Markdown/YAML-compatible request containing:

- human place;
- human event;
- emotional residue;
- contradiction;
- Soul distortion;
- visual motifs;
- material and palette rules;
- gameplay pressure;
- Soul Sense reveal;
- Release meaning;
- required asset list;
- reference to approved Golden Slice;
- explicit non-goals.

### 2. Region production pipeline

Document and automate where practical:

```text
owner seed
  → AI region brief
  → consistency review
  → room/encounter briefs
  → asset briefs
  → generated candidates outside Content
  → normalization/audit
  → selected promotion
  → build
  → deterministic captures
  → AI visual review
  → owner approval/rejection
  → decision log and handoff
```

### 3. Asset promotion contract

Define source, candidate, approved and runtime states. Record:

- prompt/source;
- dimensions and animation grid;
- intended anchor/socket;
- palette/material constraints;
- licensing/provenance where known;
- review captures;
- owner decision;
- runtime key.

Reuse and repair the existing visual asset tools rather than replacing them blindly.

### 4. Content validators

Add focused checks for:

- required files and manifests;
- sprite dimensions/frame grids;
- Content.mgcb registration;
- duplicate runtime IDs;
- missing provenance;
- references to nonexistent scripts/documents;
- scenario availability for promoted visible features.

### 5. One real production example

Use the pipeline to produce one bounded addition to the approved Death-Layer route, such as:

- one environmental prop family;
- one Soul Trace vignette;
- one encounter dressing variant;
- one non-critical enemy visual variant.

Generate candidates, select the strongest based on the current art rules, promote it, build it, capture it and prepare it for owner review.

## Agent roles

If collaboration/subagent tools are available, use parallel agents only for non-overlapping tasks:

- content brief and lore check;
- asset candidate preparation;
- runtime integration;
- visual evidence review.

One owning agent controls promotion and resolves conflicts.

## Autonomy rules

- Make routine production decisions without owner interruption.
- Never promote an asset that fails technical validation.
- Never mark aesthetic approval on behalf of the owner.
- Keep rejected candidates and reasons outside runtime Content.
- Update the decision log with every owner response.
- Keep the handoff usable after session interruption.

## Non-goals

- runtime generative AI;
- random unreviewed content shipping;
- AI-generated collision or encounter fairness without deterministic validation;
- a new editor;
- replacing MonoGame’s Content pipeline;
- generating an entire region in one unreviewed batch;
- autonomous lore canon changes.

## Acceptance gate

- A future agent can start from one owner seed and follow the documented pipeline.
- The pipeline produces one actual in-game addition.
- Asset/source/decision provenance is recorded.
- Validators catch at least missing registration, bad dimensions and broken documentation references.
- The game builds and the relevant scenario captures.
- The owner receives an APPROVE / REVISE / REJECT decision package.
- No important decision exists only in the chat.

## Final handoff

Provide:

- PIPELINE CREATED
- AUTOMATION ADDED
- REAL EXAMPLE PRODUCED
- ASSET PROVENANCE
- VALIDATION RESULTS
- IN-GAME CAPTURES
- OWNER DECISION REQUIRED
- NEXT CONTENT SEED RECOMMENDATION
