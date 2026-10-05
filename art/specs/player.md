# Visual-Spec: player

Art: character
Status: im-spiel
Stil: hausstil
Weltgröße: 100 × 100
Akzentfarbe: Karminrot des Kompassdeckels; Death Flame als Emission am Kern
Lore: [Figurenblatt Protagonist](../../docs/current/characters/protagonist.md)

## Merkmale
- Mensch zuerst: lesbares Gesicht (Brauen, dunkle Augen), Hände und Beine.
- Langer, asymmetrischer dunkler Wollmantel mit Stehkragen, auf der Waffenseite länger; Ledergürtel; Brustriemen mit Kompass, karminroter Deckel.
- Sense (S-Stiel, Klinge mit Rückensporn, Manschette mit Death-Flame-Kern, Dorn) und Soul Cannon (Reliquienkasten mit Gitterkammer und gerippter Mündung, auf dem Rücken) sind Teil der Frames.
- Schwacher violetter Kern unter dem Brustbein (vom Spiel gezeichnet).

## Silhouette
Schlank und mittelgroß; Sensenklinge vor dem Körper oder über der linken Schulter, Cannon-Mündung über der rechten, schräger Mantelsaum. Aus allen acht Richtungen dieselbe Figur.

## Herstellung
3D-Pfad B: MPFB2-Körper mit Kleidung, Sense und Cannon per Skript (`tools/visuals/blender/build_player.py`), Toon-Rampe mit Pinselrauschen und Konturhülle, 8 Richtungen mit Normalmap (`render_directions.py`, 320 px auf 3,2 m, 35°), gepackt mit `pack_sheets.py --pixels-per-unit 1.5`. Die Füße stehen auf der Spielposition.

## Animationen
- `idle`: Ruhe, Schleife; Sense waagerecht quer vor dem Körper, Klinge links oben.
- `move`: Laufen, Schleife; Sense schräg vor der Brust, Klinge über der Schulter; schreitet nach Strecke voran (Clip-Fortschritt `distance`, ein Zyklus je 180 Weltpixel).
- `swing1`: erster Sensenhieb, 120° von links nach rechts, einmalig; Bild für Bild nach dem Fortschritt des Angriffs.
- `swing2`: zweiter Hieb, 140° zurück, einmalig.
- `swing3`: dritter Hieb mit Ausholen und Drehung des Oberkörpers, 198°, einmalig.
- `aim`: Soul Cannon in der rechten Hand erhoben, Sense in der linken, Schleife.

## Effekte
- `fx.scythe-slash-01`: erster Sensenhieb, 0,205 s, Treffer bei 0,062 s
- `fx.scythe-slash-02`: zweiter Sensenhieb, 0,255 s, Treffer bei 0,085 s
- `fx.scythe-cleave`: dritter Hieb mit Drehung, 0,42 s, Treffer bei 0,155 s
- `fx.dash-ignition`: Ignition Dash, 0,14 s
- `fx.cannon-charge-loop`: Aufladen der Soul Cannon, bis 1,2 s
- `fx.cannon-muzzle-full`: Mündungsfeuer bei voller Ladung
- `fx.cannon-projectile-full`: Geschoss der Soul Cannon
