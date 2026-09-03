# The Lost Soul of Fire — MVP Vision

**DESIGN STATUS: LOCKED**

## Purpose

The MVP is a short, highly focused playable proof of concept for **The Lost Soul of Fire**, a 2D top-down action roguelite built with C# and MonoGame.

The prototype must prove one thing:

> Fast Scythe combat, a heavy Soul Cannon, Soul Sense, releasing Lost Souls, protecting them from Devourers, building Resonance, and temporarily becoming a much more mobile Death-Flame-powered version of the Player can form a strong and visually distinctive combat loop.

The MVP is not intended to prove progression, procedural generation, content volume, narrative systems, or production-ready art.

## Core Fantasy

The Player is himself a Lost Soul who should already have passed on. For an unknown reason, he carries the rare **Flame of Death**.

The Flame of Death is not evil. Its purpose is separation and release. It breaks the connection between a Lost Soul and its distorted material manifestation so the Soul can finally leave the world.

The Player therefore does not simply kill monsters:

1. Defeat the hostile manifestation.
2. Expose the Lost Soul.
3. Protect it during the short release window.
4. Use the Flame of Death to release it.
5. Receive Soul Residue.
6. Build Resonance.
7. Activate an enhanced Death-Flame state.

## MVP Content

The MVP contains exactly:

- One hand-built arena: **Abandoned Soul Furnace**
- One Player
- One Scythe
- One Ignition Dash
- One physical Soul Cannon
- Soul Sense
- Soul Release
- Resonance / enhanced Death-Flame mode
- Hollow
- Burning
- Devourer
- Four escalating combat waves
- Player death and restart
- Prototype completion state

## Experience Target

A successful run should take only a few minutes.

The prototype should communicate:

- speed
- responsiveness
- strong combat impact
- a dark Gothic-industrial world
- violet-white Death Flame
- peaceful Soul Releases
- a meaningful contrast between fast Scythe combat and the heavy Soul Cannon
- a visible power spike when Resonance activates

## Primary References

References are directional only. Do not copy characters, assets, names, locations, or protected designs.

- **Wizard of Legend** — fast top-down spell/melee combat and movement
- **Ember Knights** — modern pixel-art combat readability and responsiveness
- **Hyper Light Drifter** — atmosphere, color discipline, world presentation
- **Hades** — readable action and run payoff
- **Children of Morta** — pixel art plus modern lighting/atmosphere
- **Enter the Gungeon** — arena encounters and readable enemy behavior

High-level tonal inspiration also comes from the contrast of grotesque Gothic shapes, Souls, dark humor/energy, fire-powered movement, and anime-style impact. The game must remain visually and narratively original.

## Design Pillars

### 1. Dark World. Bright Souls.
The environment is mostly black, charcoal, gray, and dark violet. Soul energy supplies the strongest color.

### 2. Quiet → Quiet → BOOM
The Player is calm and controlled between actions. Strong attacks are explosive and high-impact.

### 3. Game Feel Before Asset Complexity
Hitstop, trails, particles, recoil, impact frames, camera shake, audio, and strong silhouettes are more important than large animation sets.

### 4. Release, Not Consumption
The Player does not consume actual Souls. Successfully released Souls leave the world. Only **Soul Residue** remains and resonates with the Flame of Death.

### 5. Simple Implementation, Complete Design
Technical simplicity is encouraged. Redesigning a locked mechanic because another mechanic is easier to implement is forbidden.

## Out of Scope

Do not implement:

- procedural generation
- full roguelike run structure
- hub
- meta progression
- skill tree
- crafting
- inventory
- quests
- dialogue system
- save system
- multiple weapons
- boss
- multiplayer
- achievements
- localization
- complex settings
- complex shader pipeline
- ECS
- generic ability framework
- architecture for hypothetical future requirements

Missing out-of-scope features are not defects.
