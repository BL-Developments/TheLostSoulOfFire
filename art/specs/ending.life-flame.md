# Visual-Spec: ending.life-flame

Art: sprite
Status: im-spiel
Stil: hausstil
Weltgröße: 128 × 128
Akzentfarbe: warmes Orange mit gelbweißem Kern (Life Flame)
Lore: [Symbolsatz: Life Flame](../../docs/current/VISUAL-ART-DIRECTION.md#11-symbolsatz--working-canon)

## Merkmale
- Die einzige Life Flame der Arena, nach der letzten Welle im kalten Ofen.
- Weich, rund, steigt nach oben; höchstens eine im Bild.

## Silhouette
Kleine aufrechte Flamme.

## Animationen
- `default`: Schleife, 16 Frames bei 16 fps; zur Laufzeit zusätzlich atmend.

## Herstellung
Erzeugt von `tools/visuals/vfx_kit.py --only life_flame` mit der warmen Verlaufstabelle (Glutrot, Orange, Gold, gelbweißer Kern); vorgemultipliert, Import mit `PremultiplyAlpha=False`.
