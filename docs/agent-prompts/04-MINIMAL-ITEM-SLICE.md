# Prompt 04 — Minimal Soul Echo Item Slice

You are implementing the first run-build choices for The Lost Soul of Fire.

This is not a request for a full inventory system. Deliver six playable, lore-compatible modifiers and one choice moment inside the approved playable slice.

## Outcome

After a major encounter or story beat, offer three Soul Echoes from a pool of six. The selected Echo changes the remainder of the run enough that the player notices it immediately.

The owner must be able to decide whether item choices belong in the game’s core loop.

## Required reading

Read:

- `AGENTS.md`;
- `docs/current/`;
- current combat and co-op handoff;
- Item/Combat sections of the current vision;
- actual Player, weapon, Resonance and Soul code.

## Lore rule

The item must not be an intact Soul consumed as fuel.

Use a working concept such as **Soul Echo**:

> A stable behavioral or emotional imprint left in the Death Layer after a Soul has genuinely passed on.

The final name remains tunable. Make the distinction visible in code and UI.

## First six Echoes

Implement a balanced first set. Use these as starting hypotheses and improve them if play evidence supports a clearer set:

1. **Echo of Severance** — a successful reaction counter widens the next Scythe arc or cuts through one additional target.
2. **Echo of Momentum** — perfect dodge grants a brief movement/attack transition benefit without permanent speed inflation.
3. **Echo of Still Breath** — holding Soul Sense under danger strengthens one precise Core opportunity rather than generic damage.
4. **Echo of Recoil** — full Soul Cannon recoil becomes a tactical movement option and gains a follow-up window.
5. **Echo of Shelter** — releasing a Soul creates a brief cooperative stabilization field.
6. **Echo of Volatile Residue** — Soul Residue may be detonated for immediate area control instead of being added to Resonance.

For Volatile Residue, detonate residue or excess Death-Flame energy only. Do not destroy the released conscious Soul.

## System scope

Create the smallest data-driven structure that supports:

- stable ID;
- display name;
- one-sentence effect;
- optional numeric tuning values;
- acquisition;
- active run collection;
- hooks at the few relevant combat events;
- reset at run restart;
- two-player ownership rule.

Do not build:

- equipment slots;
- rarity tiers;
- shops;
- crafting;
- currencies;
- procedural affix generation;
- permanent unlocks;
- save persistence;
- a generic scripting language.

## Co-op rule

Prototype team-shared Echoes first so both Players benefit and no one waits through a private inventory screen. If an Echo affects one Player, make selection and ownership explicit.

The choice screen must work with mouse/keyboard and gamepad, remain short and readable, and pause or secure the encounter safely.

## Verification

- deterministic acquisition scenario for each Echo;
- focused behavior check for each modifier;
- solo and two-player choice flow;
- reset removes run Echoes;
- visual capture of the choice and at least three visible effects;
- no regression to Soul Release or Resonance accounting.

## Acceptance gate

- Six Echoes work.
- At least four change a decision or timing pattern, not only a number.
- Volatile Residue respects Soul lore.
- Choice takes under 20 seconds for a new player to understand.
- UI remains readable in solo and co-op.
- Existing combat scenarios still pass.
- Owner can name at least two Echoes they would want to pick again.

## Final handoff

Provide:

- IMPLEMENTED ECHOES
- WHAT EACH CHANGES
- SOLO / COOP OWNERSHIP
- UI FLOW
- VERIFICATION
- BALANCE ASSUMPTIONS
- OWNER DECISION: APPROVE / REVISE / REJECT
- RECOMMENDED SECOND SET ONLY IF THE FIRST SET IS APPROVED
