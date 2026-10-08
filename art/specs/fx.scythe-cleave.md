# Visual-Spec: fx.scythe-cleave

Art: effect
Status: im-spiel
Stil: ludo
Weltgröße: 256 × 256
Akzentfarbe: Death-Flame-Violett bis fast Weiß; kein Orange, Grün oder Blau
Lore: [Art Direction §7 und Symbolsatz](../../docs/current/VISUAL-ART-DIRECTION.md#11-symbolsatz--working-canon)

## Merkmale
- Dritter Hieb mit voller Drehung, schwerster Bogen.
- Folgt der Death-Flame-Verlaufstabelle (dunkles Violett → Violett → fast Weiß).
- Bedeckt den Frame nie flächig; kein eingebackener Hintergrund.

## Silhouette
Klar als Effekt dieser Quelle lesbar, verdeckt keine Figur und keinen Telegraph.

## Animationen
- `default`: 0,42 s, Treffer bei 0,155 s.

## Einsatz
Nur noch Rückfall: Mit gerenderter Spielfigur und geladenem Death-Flame-Shader zieht die Sense
ihren Hieb als Flammenband entlang der Klinge (`ScytheCombat.DrawFlameSlash`), dieses Sheet
bleibt dann aus.
