# Visual-Spec: test.blender-figure

Art: character
Status: im-spiel
Stil: hausstil
Weltgröße: 112 × 112 (Frame 104 × 160, Ursprung am Fußpunkt)
Akzentfarbe: Karminrot an einer kleinen Kugel auf der Brust
Lore: [Art Direction §5b Figurenrichtung](../../docs/current/VISUAL-ART-DIRECTION.md)

## Merkmale
- Prüffigur für den Produktionsweg aus 6.3, keine Spielfigur: Körper, Kopf, Beine und ein Stab mit Klinge, prozedural in Blender (`tools/visuals/blender/test_figure.py`).
- Acht Richtungen aus einem Modell durch Drehen, Farb- und Normal-Durchgang, gemeinsamer Zuschnitt je Animation.
- Erscheint nur in automatischen Bildtests (`arena_blender_figure`).

## Silhouette
Zylinderkörper mit Kugelkopf, Stab an der rechten Seite; jede Richtung zeigt dieselbe Figur gedreht.

## Animationen
- `idle`: leichtes Wippen, Schleife, 8 Frames bei 8 fps.
