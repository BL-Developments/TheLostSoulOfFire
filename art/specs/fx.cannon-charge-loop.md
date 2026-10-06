# Visual-Spec: fx.cannon-charge-loop

Art: effect
Status: im-spiel
Stil: hausstil
Weltgröße: 128 × 128
Akzentfarbe: Death-Flame-Violett bis fast Weiß; kein Orange, Grün oder Blau
Lore: [Art Direction §7 und Symbolsatz](../../docs/current/VISUAL-ART-DIRECTION.md#11-symbolsatz--working-canon)

## Merkmale
- Ladung an der Mündung der Soul Cannon, drei Stufen bis voll.
- Folgt der Death-Flame-Verlaufstabelle (dunkles Violett → Violett → fast Weiß).
- Bedeckt den Frame nie flächig; kein eingebackener Hintergrund.

## Silhouette
Klar als Effekt dieser Quelle lesbar, verdeckt keine Figur und keinen Telegraph.

## Animationen
- `default`: bis 1,2 s, Schleife.

## Herstellung
Erzeugt von `tools/visuals/vfx_kit.py --only cannon_charge` aus Form- und Rauschfeldern, gefärbt
über die Death-Flame-Verlaufstabelle. Die Sheets sind vorgemultipliert und decken weniger, als
sie leuchten (heiße Stellen addieren Licht); Import mit `PremultiplyAlpha=False`.
