# Visual-Spec: prop.training-dummy

Art: prop
Status: im-spiel
Stil: hausstil
Weltgröße: aus dem Render (Fußpunkt der Eisenplatte)
Akzentfarbe: Stroh und Holz; der Kern violett mit Kreide markiert
Lore: [VISUAL-ART-DIRECTION §11, Wardens](../../docs/current/VISUAL-ART-DIRECTION.md)

## Merkmale
- Übungspfahl der Wardens in der Sandbox: Holzpfosten in eisernem Fuß, Querholz als Arme, Körper aus gebundenem Stroh, Kapuze aus Sackleinen, der Kern auf der Brust violett angekreidet.
- Gebaut mit `tools/visuals/blender/build_dummy.py` (Spielkamera, 1,5 px je Welteinheit), registriert mit `tools/visuals/place_pieces.py`.

## Silhouette
Schlanker Pfahl mit Querholz und rundem Strohkörper.

## Animationen
- `default`: Standbild; bei einem Treffer wankt der Pfahl kurz (Laufzeit).
