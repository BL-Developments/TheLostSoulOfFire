# NEANDERTHAL MODE v2 — Repo-Aware MVP Build Plan

**STATUS: RECOMMENDED FOR CURRENT MAIN**

This plan is tailored to the current `main` branch of `bjsc-dev/TheLostSoulOfFire`.

Current repository reality:

- The actual MonoGame game code is still essentially the default starter.
- `Game1.cs` contains only basic MonoGame setup, Escape handling, and CornflowerBlue clear.
- `Content/` currently only contains `Content.mgcb`.
- The `.claude`, `.codex`, and `openspec` folders are agent/workflow scaffolding, not an existing gameplay architecture.
- There are no existing gameplay systems that need to be preserved.

Therefore the MVP can be built directly on top of the current codebase.

The human does not need to review code.

The human reviews the **game**.

---

# Rule 0 — Protect Main

Do not work directly on `main`.

Create:

```bash
git checkout main
git pull
git checkout -b prototype/soulfire-mvp
```

Before Codex starts:

```bash
dotnet restore
dotnet build
dotnet run --project src/TheLostSoulOfFire
```

Confirm the current starter launches.

Then commit the specification packages if they are not already committed.

This is the clean rollback point.

---

# Rule 1 — Do Not Touch Agent Scaffolding Without Need

The repository already contains:

- `.codex/`
- `.claude/`
- `openspec/`

These are not part of the actual game architecture.

Codex should not reorganize, delete, or redesign them.

Instruction:

> Leave `.codex`, `.claude`, and `openspec` unchanged unless a direct requirement of the MVP makes a change unavoidable.

The implementation should primarily occur in:

```text
src/TheLostSoulOfFire/
```

and the new:

```text
docs/mvp/
docs/vision/
```

---

# Rule 2 — Codex Owns Code, Human Owns Feel

Human responsibilities:

- launch game
- play
- judge feel
- generate/select assets
- describe problems in normal language
- approve checkpoints

Codex responsibilities:

- architecture
- implementation
- build
- compile fixes
- runtime fixes
- asset integration
- tuning
- commits when explicitly requested

Do not manually review code unless something genuinely requires investigation.

---

# Rule 3 — Start Codex With This Prompt

```text
Read docs/mvp/CODEX_AUTOPILOT.md and all authoritative docs/mvp specifications.

Implement the MVP on the current repository branch.

Important repository-specific rules:
- The existing gameplay code is only a MonoGame starter, so you may build the required game structure directly.
- Preserve the existing C#/.NET 9/MonoGame DesktopGL project.
- Leave .codex, .claude, and openspec unchanged unless strictly necessary.
- Do not introduce speculative architecture.
- Use placeholders whenever final assets are unavailable.
- Build and verify continuously.

Work through CODEX_EXECUTION_PLAN.md sequentially.

Stop after Phase 04 and report that the first playable checkpoint is ready.
Do not commit unless I ask.
```

Why stop at Phase 04?

Because the first useful human review is:

> Does moving, aiming, dashing and swinging the Scythe already feel good?

Reviewing earlier than that provides little value.

---

# CHECKPOINT A — PLAYER FEEL

Codex completes:

- Foundation
- Player movement
- Dash
- Scythe

Human launches:

```bash
dotnet run --project src/TheLostSoulOfFire
```

Ignore code.

Play for 2–5 minutes.

Judge only:

- Is movement responsive?
- Does mouse aim feel natural?
- Is Dash satisfying?
- Does Scythe read `SWISH → SWISH → BOOM`?
- Does the Player already feel fun to move?

## If Bad

Speak naturally.

Examples:

```text
Movement feels floaty. Make acceleration effectively immediate and movement more direct.
Do not change the control scheme.
```

```text
Dash feels weak. I want Space to feel explosive immediately.
Tune distance, duration and VFX without adding new mechanics.
```

```text
Soul Cleave feels too similar to the first two hits.
Make the third hit substantially heavier through hitstop, knockback, trail and impact.
```

Codex fixes.

Run again.

Repeat until:

> GEIL

Then tell Codex:

```text
Checkpoint A approved.
Run build verification and commit the current state with a concise descriptive commit message.
Then continue through Phase 08 and stop.
```

---

# CHECKPOINT B — CORE SOUL COMBAT

Codex completes through Phase 08:

- Hollow
- Soul system
- Soul Release
- Soul Sense
- Soul Cannon

Human plays.

Judge:

- Hollow attack readable?
- Soul Core obvious in Soul Sense?
- Soul Release emotionally different from enemy death?
- Cannon visibly heavy?
- Full charge unmistakable?
- Cannon vs Scythe clearly different?

Do not inspect implementation.

If something feels wrong, describe the symptom.

Examples:

```text
Soul Sense currently just feels like a purple filter.
Make the normal world more suppressed and Soul information much more dominant according to the spec.
```

```text
The Cannon is mechanically working but does not feel heavy.
Improve recoil, charge readability, audio/VFX timing and impact without changing the mechanic.
```

When good:

```text
Checkpoint B approved.
Build and verify, commit this state, then continue through Phase 12 and stop.
```

---

# CHECKPOINT C — ENEMY ECOSYSTEM + RESONANCE

Codex completes:

- Burning
- Devourer
- Resonance
- Waves / complete combat loop

Human plays a full run.

Judge:

### Burning
- Is Charge obvious?
- Is Cannon detonation satisfying?

### Devourer
- Can you immediately see when it wants a Soul?
- Does it create actual pressure?
- Does Cannon extraction make sense?

### Resonance
- Is filling it understandable?
- Is activation a payoff?
- Are you noticeably faster/more mobile?
- Is it strong without becoming stupidly overpowered?

### Waves
- Does difficulty escalate naturally?
- Can you realistically reach Resonance?

If good:

```text
Checkpoint C approved.
Build and verify, commit this state, then continue through Phase 17.
Stop before the final commit and report that the polish/final-verification checkpoint is ready.
```

---

# CHECKPOINT D — FINAL GAME

Codex completes:

- Arena art pass
- VFX
- Audio
- Ending
- Debug
- Hardening
- Acceptance verification

Now play the complete prototype.

Judge only the product.

Questions:

- Would I show this to someone?
- Is the Player fun immediately?
- Is the visual identity readable?
- Do Scythe and Cannon feel different?
- Does Soul Sense matter?
- Do Soul Releases feel special?
- Does Devourer create interesting pressure?
- Is Resonance a payoff?
- Does the orange Flame of Life ending land?
- Any obvious crash/blocker?

If yes:

```text
Final checkpoint approved.
Run the complete acceptance criteria again.
Run dotnet restore and dotnet build.
Fix any remaining required failures.
Then commit all final changes with message:
Complete Soulfire MVP prototype
```

Optionally push:

```bash
git push -u origin prototype/soulfire-mvp
```

---

# VFX Side Quest While Codex Works

Use:

```text
docs/vision/VFX_RAPID_PRODUCTION.md
```

Generate assets in the prescribed order.

Place generated assets under:

```text
src/TheLostSoulOfFire/Content/Textures/Effects/
```

Do not worry about integrating them manually.

Tell Codex:

```text
I added new generated VFX assets under src/TheLostSoulOfFire/Content/Textures/Effects.

Read docs/vision/VFX_RAPID_PRODUCTION.md.
Integrate each useful asset into its intended effect.

Do not change locked gameplay behavior.
Keep procedural particles, hitstop, camera shake, overlays and afterimages in code.
Build and verify afterwards.
```

---

# The Entire Human Loop

```text
CODEX BUILDS
    ↓
GAME RUN
    ↓
PLAY
    ↓
GOOD?
 ┌───────┴───────┐
YES              NO
 ↓                ↓
COMMIT        SAY WHAT
 ↓            FEELS WRONG
NEXT              ↓
             CODEX FIXES
                  ↓
                PLAY
```

You are allowed to say:

```text
I do not know exactly what is wrong, but this feels weak.
Compare it against the authoritative specs and improve the tunable game-feel values and feedback.
Do not invent a new mechanic.
```

That is enough.

---

# Emergency Rollback

If Codex seriously wrecks something after an approved checkpoint:

```bash
git log --oneline
```

Find the last good checkpoint commit.

Then either ask Codex:

```text
The last approved state was commit <hash>.
The current changes broke the game.
Inspect the diff since that commit and restore the intended behavior without removing later requirements.
```

Or, if you intentionally want to throw away everything after it:

```bash
git reset --hard <hash>
```

Only use hard reset when you intentionally want to discard later changes.

---

# Why This Works on Current Main

The current game project has almost no existing gameplay code.

There is no existing:

- Player system
- combat system
- enemy hierarchy
- world manager
- animation system
- VFX framework
- Soul system
- progression system
- arena system

So Codex is not being asked to perform a dangerous rewrite.

It is effectively being asked to build the first real game implementation from a clean MonoGame starter.

That is close to the ideal repository state for this workflow.
