# Visual-Spec: player

Art: character
Status: im-spiel
Stil: ludo
Weltgröße: 100 × 100
Akzentfarbe: Karminrot des Kompassdeckels; Death Flame als Emission am Kern
Lore: [Figurenblatt Protagonist](../../docs/current/characters/protagonist.md)

## Merkmale
- Mensch zuerst: lesbares Gesicht, Hände und Beine.
- Langer, asymmetrischer dunkler Mantel, Brustriemen, Kompass mit karminrotem Deckel am Riemen.
- Sense und Soul Cannon sind Teil der Figur; die Ludo-Grafik zeichnet sie noch getrennt.
- Schwacher violetter Kern unter dem Brustbein.

## Silhouette
Schlank und mittelgroß; Sensenklinge über einer Schulter, Cannon-Mündung über der anderen, schräger Mantelsaum. Aus allen acht Richtungen dieselbe Figur.

## Animationen
- `idle`: Ruhe, Schleife; Waffe in Kampfhaltung quer vor dem Körper.
- `move`: Laufen, Schleife; folgt künftig der Strecke (Clip-Fortschritt `distance`).

## Effekte
- `fx.scythe-slash-01`: erster Sensenhieb, 0,205 s, Treffer bei 0,062 s
- `fx.scythe-slash-02`: zweiter Sensenhieb, 0,255 s, Treffer bei 0,085 s
- `fx.scythe-cleave`: dritter Hieb mit Drehung, 0,42 s, Treffer bei 0,155 s
- `fx.dash-ignition`: Ignition Dash, 0,14 s
- `fx.cannon-charge-loop`: Aufladen der Soul Cannon, bis 1,2 s
- `fx.cannon-muzzle-full`: Mündungsfeuer bei voller Ladung
- `fx.cannon-projectile-full`: Geschoss der Soul Cannon
