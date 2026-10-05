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
- `aim`: Soul Cannon in der rechten Hand erhoben, Sense in der linken, Schleife (Zustand `Charging`).
- `cannon_draw`: Griff über die Schulter zur Kanone auf dem Rücken, Schwung nach vorn in den Anschlag; einmalig, nach dem Fortschritt des Zustands `Drawing` (0,16 s).
- `cannon_fire`: Rückstoß (Mündung hoch, Schulter zurück, Knie federn), dann Kanone zurück auf den Rücken und Hand zurück an die Sense; einmalig, nach dem Fortschritt von `Returning` (0,28 s).
- `dash`: tiefer Ausfallschritt in Dash-Richtung, Sense eng geführt; einmalig, nach dem Dash-Fortschritt (0,14 s).
- `hit`: Kopf und Brust schnappen zurück, Knie geben nach; einmalig, nach dem Treffer-Aufblitzen (0,14 s).
- `death`: Rückstoß, Knie knicken ein, die Sense fällt, Sturz nach vorn; einmalig ab dem tödlichen Treffer, 1,33 s, hält das letzte Bild. Die Death Flame nimmt danach den liegenden Körper.

## Effekte
- `fx.scythe-slash-01`: erster Sensenhieb, 0,205 s, Treffer bei 0,062 s
- `fx.scythe-slash-02`: zweiter Sensenhieb, 0,255 s, Treffer bei 0,085 s
- `fx.scythe-cleave`: dritter Hieb mit Drehung, 0,42 s, Treffer bei 0,155 s
- `fx.dash-ignition`: Ignition Dash, 0,14 s
- `fx.cannon-charge-loop`: Aufladen der Soul Cannon, bis 1,2 s
- `fx.cannon-muzzle-full`: Mündungsfeuer bei voller Ladung
- `fx.cannon-projectile-full`: Geschoss der Soul Cannon
