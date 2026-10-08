# Visual-Spec: fx.resonance-activate

Art: effect
Status: im-spiel
Stil: hausstil
Weltgröße: 256 × 256
Akzentfarbe: Death-Flame-Violett bis fast Weiß; kein Orange, Grün oder Blau
Lore: [Art Direction §7 und Symbolsatz](../../docs/current/VISUAL-ART-DIRECTION.md#11-symbolsatz--working-canon)

## Merkmale
- Aktivierung der Resonance um den Spieler.
- Folgt der Death-Flame-Verlaufstabelle (dunkles Violett → Violett → fast Weiß).
- Bedeckt den Frame nie flächig; kein eingebackener Hintergrund.

## Silhouette
Klar als Effekt dieser Quelle lesbar, verdeckt keine Figur und keinen Telegraph.

## Animationen
- `default`: einmalig, 16 Frames bei 24 fps; Resonance hält 10 s.

## Herstellung
Erzeugt von `tools/visuals/vfx_kit.py --only resonance_activate` aus Form- und Rauschfeldern, gefärbt
über die Death-Flame-Verlaufstabelle. Die Sheets sind vorgemultipliert und decken weniger, als
sie leuchten (heiße Stellen addieren Licht); Import mit `PremultiplyAlpha=False`.
