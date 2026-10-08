# Visual-Spec: pickup.lost-soul

Art: sprite
Status: im-spiel
Stil: hausstil
Weltgröße: 72 × 72 (Frame; das Licht selbst etwa 30 Einheiten)
Akzentfarbe: Seelenweiß mit violettem Saum
Lore: [Symbolsatz: Death Flame](../../docs/current/VISUAL-ART-DIRECTION.md#11-symbolsatz--working-canon)

## Merkmale
- Freie Seele: ruhiges, fast weißes Licht, keine Gestalt mit Gesicht.
- Pulsiert leicht (Laufzeit).

## Silhouette
Kleine runde Lichtform, nicht mit Effekten verwechselbar.

## Animationen
- `default`: Schleife, 12 Frames bei 12 fps: atmender Saum, kreisende Funken; zur Laufzeit zusätzlich pulsierend.

## Effekte
- `fx.soul-release`: Release der Seele, 1,25 s


## Herstellung
Erzeugt von `tools/visuals/vfx_kit.py --only lost_soul` (vorgemultipliert, Import mit `PremultiplyAlpha=False`).
