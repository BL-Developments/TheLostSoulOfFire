# Visual-Spec: enemy.burning

Art: character
Status: im-spiel
Stil: hausstil
Weltgröße: aus dem Render (1,5 Texturpixel je Welteinheit, Fußpunkt je Clip)
Akzentfarbe: Violett der Risse und Flammen; kein Orange
Lore: [Figurenblatt Burning](../../docs/current/characters/burning.md)

## Merkmale
- Gedrungen, verkohlt, violette Risse, Death Flame schlägt aus den Rissen.
- Kopf mit zwei glühenden Rissen statt Augen.
- Kaum Kleidungsreste: Gürtel, Stiefel, angesengte Fetzen.

## Silhouette
Mittelgroß und kompakt, Kopf nach vorn geschoben, Flammenzungen an Rücken und Schultern.

## Animationen
- `idle`: unruhiges Federn auf den Fußballen, die Risse pulsieren, Schleife.
- `move`: tiefes, ruckartiges Pirschen, Schleife nach Strecke (90 Welteinheiten je Zyklus).
- `telegraph`: Stillstand vor dem Anlauf (0,62 s): er sinkt tief, zieht die Fäuste zurück, Risse und Flammenzungen lodern auf; nach dem Zeitgeber.
- `charge`: der explosive Sprint, weit vorgelehnt, Schleife.
- `recover`: nach dem Anlauf (1,0 s): er rutscht hoch, taumelt, keucht, das Glühen sinkt; nach dem Zeitgeber.
- `hit`: Treffer-Aufblitzen: Kopf und Brust zurück, kurzes Auflodern.
- `death`: Sterben (0,58 s): auf die Knie, die Flammen sinken, er sackt nach vorn; nach drei Vierteln übernimmt die Auflösung.

## Effekte
- `fx.burning-detonation`: Detonation durch vollen Soul-Cannon-Treffer im Anlauf

## Herstellung
- Rig und Körper aus MPFB2 (gedrungen, muskulös), verkohlte Toon-Haut mit Rissnetz, in dem violettes Death-Flame-Licht liegt; Flammenzungen an Schultern und Rücken als spitze, teils kantige Formen (S3, S4); kein Orange.
- Das Glühen ist eine Eigenschaft am Knochen `flames`, je Bild in jeder Aktion verschlüsselt und per Treiber an die Materialien gegeben; der Knochen trägt die Flammen, seine Skalierung lässt sie auflodern oder sinken.
- `tools/visuals/blender/build_burning.py`, gerendert mit `render_directions.py` (360 px, 3,6 m), gepackt mit `pack_sheets.py --pixels-per-unit 1.5`.
