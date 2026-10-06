# Visual-Spec: fx.death-flame-loop

Art: effect
Status: im-spiel
Stil: hausstil
Weltgröße: 128 × 128
Akzentfarbe: Death-Flame-Violett bis fast Weiß; kein Orange, Grün oder Blau
Lore: [Art Direction §7 und Symbolsatz](../../docs/current/VISUAL-ART-DIRECTION.md#11-symbolsatz--working-canon)

## Merkmale
- Ruhige Death Flame am Spieler in Inszenierungen.
- Folgt der Death-Flame-Verlaufstabelle (dunkles Violett → Violett → fast Weiß).
- Bedeckt den Frame nie flächig; kein eingebackener Hintergrund.

## Silhouette
Klar als Effekt dieser Quelle lesbar, verdeckt keine Figur und keinen Telegraph.

## Animationen
- `default`: Schleife, 16 Frames bei 16 fps (das Rauschen läuft genau eine Periode pro Durchlauf).

## Herstellung
Erzeugt von `tools/visuals/vfx_kit.py --only death_flame` aus Form- und Rauschfeldern, gefärbt
über die Death-Flame-Verlaufstabelle. Die Sheets sind vorgemultipliert und decken weniger, als
sie leuchten (heiße Stellen addieren Licht); Import mit `PremultiplyAlpha=False`.
