# Visual-Spec: enemy.devourer

Art: character
Status: im-spiel
Stil: ludo
Weltgröße: 174 × 174, wächst je gefangener Seele um 3,5 %
Akzentfarbe: gedämpftes Violett der gefangenen Seelen in der Rumpföffnung
Lore: [Figurenblatt Devourer](../../docs/current/characters/devourer.md)

## Merkmale
- Groß, breit, schwer, gebeugt; schwerer Mantel, vorn aufgerissen.
- Rumpföffnung als rissige Kante mit Licht, kein Maul und keine Zähne.
- Gefangene Seelen als kleine Lichter im Rumpf.

## Silhouette
Das breiteste Profil aller Figuren; Kopf tief zwischen den Schultern, Arme vor dem Rumpf.

## Animationen
- `idle`: schweres Atmen, Schleife.
- `move`: langsam und beharrlich, Schleife.
- `slam`: Telegraph 0,88 s, Schlag 0,18 s, Erholung 0,78 s; einmalig.
- `devour`: Verschlingen einer Seele, 1,1 s, Schleife bis zum Ende.

## Effekte
- `fx.soul-release`: befreite Seelen beim Tod (Tod 0,82 s)
