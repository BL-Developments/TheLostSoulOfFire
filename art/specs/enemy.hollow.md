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
- `swipe`: 7 Bilder Ausholen, Maske neigt sich zum Ziel (Telegraph 0,42 s), dann 3 Bilder weiter Griff (0,13 s); einmalig, Bild für Bild nach den Zeitgebern von Telegraph und Swipe.
- `recover`: Erholung nach dem Griff (0,48 s): der Arm schwingt zurück, der Körper richtet sich in die gebeugte Haltung auf.
- `hit`: Treffer-Aufblitzen (0,1 s, Kern 0,16 s): Kopf ruckt zurück, Schultern heben sich, Arme zucken; nur außerhalb von Angriff, Erholung und Taumeln.
- `stagger`: volle Kanone auf den Kern (1,15 s): zurückgeworfen mit ausgebreiteten Armen, dann benommenes Schwanken, dann wieder gebeugt.
- `death`: Sterben (0,62 s): der Schlag, dann knicken die Knie ein, der Körper sackt nach vorn, die Maske löst sich und fällt mit dem Gesicht nach oben vor ihm auf den Boden; nach drei Vierteln übernimmt die Auflösung die letzte Pose.

## Effekte
- `fx.core-hit`: Treffer auf den Kern
- `fx.soul-release`: die Seele nach dem Tod (Tod 0,62 s)
