# Visual-Spec: test.lit-figure

Art: sprite
Status: im-spiel
Stil: hausstil
Weltgröße: 112 × 112
Akzentfarbe: keine; neutrales Grau, damit nur das Licht wirkt
Lore: [Art Direction §5a Bildsprache](../../docs/current/VISUAL-ART-DIRECTION.md)

## Merkmale
- Prüffigur für `SpriteLit.fx`, keine Spielfigur: graue Kapsel mit Kopf.
- Gleiche Farbe in beiden Clips; nur die Normal-Map unterscheidet sich.
- Erscheint nur in automatischen Bildtests.

## Silhouette
Ellipse mit Kreis darüber, in Figurengröße.

## Animationen
- `flat`: flache Normal-Map; muss neben einer Death Flame genauso aussehen wie ohne Beleuchtung, abgesehen vom gleichmäßigen Licht.
- `tilted`: gewölbte Normal-Map; die der Death Flame zugewandte Seite wird violett aufgehellt, die abgewandte nicht.
