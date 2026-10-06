# Visual-Spec: fx.dash-ignition

Art: effect
Status: im-spiel
Stil: hausstil
Weltgröße: 128 × 128
Akzentfarbe: Death-Flame-Violett bis fast Weiß; kein Orange, Grün oder Blau
Lore: [Art Direction §7 und Symbolsatz](../../docs/current/VISUAL-ART-DIRECTION.md#11-symbolsatz--working-canon)

## Merkmale
- Zündung des Ignition Dash an den Füßen.
- Folgt der Death-Flame-Verlaufstabelle (dunkles Violett → Violett → fast Weiß).
- Bedeckt den Frame nie flächig; kein eingebackener Hintergrund.

## Silhouette
Klar als Effekt dieser Quelle lesbar, verdeckt keine Figur und keinen Telegraph.

## Animationen
- `default`: Dash 0,14 s, Unverwundbarkeit 0,18 s.

## Herstellung
Erzeugt von `tools/visuals/vfx_kit.py --only dash_ignition` aus Form- und Rauschfeldern, gefärbt
über die Death-Flame-Verlaufstabelle. Die Sheets sind vorgemultipliert und decken weniger, als
sie leuchten (heiße Stellen addieren Licht); Import mit `PremultiplyAlpha=False`.
