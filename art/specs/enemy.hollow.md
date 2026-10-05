# Visual-Spec: enemy.hollow

Art: character
Status: im-spiel
Stil: hausstil
Weltgröße: 112 × 112
Akzentfarbe: Violett des Kerns, nur unter Soul Sense; die Maske ist ein Wertakzent
Lore: [Figurenblatt Hollow](../../docs/current/characters/hollow.md)

## Merkmale
- Groß, dünn, zu lange Arme, leicht gebeugt.
- Zerrissenes langes Kleidungsstück in Anthrazit mit Rest von Kragen oder Knopfleiste.
- Glatte, gebrochen-weiße Maske ohne Gesichtszüge.
- Ohne Soul Sense kaum Violett.

## Silhouette
Hoch und schmal mit hängenden Armen, kleiner heller Kopf, unten in Stoffbahnen ausfransend.

## Herstellung
3D-Pfad B wie der Spieler (`tools/visuals/blender/build_hollow.py`): MPFB2-Körper, groß und dünn, Arme über die Längen-Targets verlängert; Gewand aus dem Körper und ein bis zum Boden in Bahnen ausfransender Rock; Halbschale als Maske; Kragen und Knopfleiste. Der Kern in der Brust ist nicht im Modell, das Spiel zeichnet ihn unter Soul Sense auf Brusthöhe.

## Animationen
- `idle`: Ruhe, leichtes Schwanken, die Maske bleibt still, Schleife.
- `move`: steifer Gang mit wenig Knie, die Arme hängen nach; die Pausen kommen aus dem Verhalten (Zustand Pause zeigt `idle`), Schleife nach Strecke (92 Weltpixel je Zyklus).
- `swipe`: 7 Bilder Ausholen, Maske neigt sich zum Ziel (Telegraph 0,42 s), dann 3 Bilder weiter Griff (0,13 s); einmalig, 18 Bilder/s.

## Effekte
- `fx.core-hit`: Treffer auf den Kern
- `fx.soul-release`: die Seele nach dem Tod (Tod 0,62 s)
