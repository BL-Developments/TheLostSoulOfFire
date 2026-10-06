# Visual-Spec: enemy.devourer

Art: character
Status: im-spiel
Stil: hausstil
Weltgröße: aus dem Render (1,5 Texturpixel je Welteinheit, Fußpunkt je Clip), wächst je gefangener Seele um 3,5 %
Akzentfarbe: gedämpftes Violett der gefangenen Seelen in der Rumpföffnung
Lore: [Figurenblatt Devourer](../../docs/current/characters/devourer.md)

## Merkmale
- Groß, breit, schwer, gebeugt; schwerer Mantel, vorn aufgerissen.
- Rumpföffnung als rissige Kante mit Licht, kein Maul und keine Zähne.
- Gefangene Seelen als kleine Lichter im Rumpf.

## Silhouette
Das breiteste Profil aller Figuren; Kopf tief zwischen den Schultern, Arme vor dem Rumpf.

## Animationen
- `idle`: schweres Atmen, die Seelen im Rumpf kreisen, Schleife.
- `move`: langsam und beharrlich, das Gewicht rollt von Seite zu Seite; Schleife nach Strecke (110 Welteinheiten je Zyklus).
- `slam`: neun Bilder Ankündigung (Arme und Oberkörper heben sich, die Öffnung zeigt sich, 0,88 s), drei Bilder Schlag (0,18 s); Bild für Bild nach den Zeitgebern.
- `recover`: Erholung nach dem Schlag (0,78 s): tief gebeugt, er stemmt sich wieder hoch.
- `devour`: Verschlingen (1,1 s): er beugt sich über die Seele, die Öffnung weitet sich, die Arme greifen hinab.
- `stagger`: Taumeln (volle Kanone 1,4 s, unterbrochenes Verschlingen 0,38 s): zurückgeworfen, Arme weit, dann sammelt er sich.
- `hit`: Treffer-Aufblitzen.
- `death`: Sterben (0,82 s): das Gefängnis bricht auf, er sinkt auf die Knie; nach drei Vierteln übernimmt die Auflösung, alle Seelen kommen frei.

## Effekte
- `fx.soul-release`: befreite Seelen beim Tod (Tod 0,82 s)

## Herstellung
- MPFB2-Figur mit Masse-Targets (Rumpfbreite und -tiefe, Schultern, Arme, Nacken), schwerer Mantel als Stoffhülle und langer Rock, hoher Kragen; vorn aufgerissen zur Rumpföffnung: Hohlraum mit violettem Grund, gezackter glasiger Rand, drei gefangene Seelen.
- Knochen `maw` (Weite der Öffnung) und `souls` (Kreisen der Seelen) in jeder Aktion verschlüsselt.
- `tools/visuals/blender/build_devourer.py`, gerendert mit `render_directions.py` (460 px, 4,6 m), gepackt mit `pack_sheets.py --pixels-per-unit 1.5`. Rumpf und gehaltene Seelen zeichnet das Spiel bei der gerenderten Figur auf Schusshöhe.
