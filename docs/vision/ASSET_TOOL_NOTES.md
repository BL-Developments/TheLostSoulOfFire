# Asset Tool Decision Notes — August 2026

This file is deliberately practical rather than permanent canon.

## Recommended Starting Choice: Ludo

For the immediate prototype sprint, start by testing **Ludo**.

Reason:

- one workflow covers sprites, animated sprite sheets, VFX-style assets, audio, and other game assets
- API/MCP support makes an agent-assisted workflow possible
- export formats include sprite sheets / per-frame assets
- it reduces context switching while Codex is implementing

Use the first paid tier that gives enough generations for the sprint; do not commit to an annual plan before testing the exact Soulfire prompts.

## PixelLab

Choose PixelLab instead or alongside Ludo if the project moves strongly toward true pixel-art production.

Particularly useful for:

- pixel-art characters
- attack animations
- rotations
- top-down tiles
- API-driven asset generation

## Scenario

Scenario becomes especially valuable after you have curated a small set of approved Soulfire assets.

Then:

1. collect strong Player/enemy/environment/VFX references
2. train a custom style model
3. use the trained style for consistent expansion
4. generate region props, concepts, variations, and future asset families

This fits the long-term AI-assisted world-generation vision very well.

## Pixelpart

Pixelpart is worth testing as a non-AI VFX authoring/export tool.

It can create particle effects visually and export sprite sheets/image sequences. This may be useful when AI produces the concept/look but you want precise timing and looping.

Because the game uses MonoGame, exported PNG sprite sheets are more useful than depending on an engine-specific plugin.

## Decision

Immediate MVP:
**Ludo + ordinary PNG sprite sheets + MonoGame runtime particles.**

If pixel-art consistency becomes the dominant problem:
**PixelLab.**

Once the game's own art style is established:
**Scenario custom style training.**

For hand-tuned procedural effect authoring:
**Pixelpart.**
