# Visual-Spec: environment.shore

Art: environment
Status: im-spiel
Stil: hausstil
Weltgröße: 1800 × 1000 (zwei Kacheln zu 1350 × 1500 Pixeln)
Akzentfarbe: keine; Schiefergrau-Blau, Salzweiß, verblichenes Bahnhofsgrün; Violett nur an der Warden-Marke
Lore: [Region-Vertrag Prolog, Abschnitt Ufer](../../docs/current/regions/prologue.md)

## Merkmale
- Bahnsteig des Seebahnhofs in Draufsicht, halb im dunklen Wasser; die Plattform deckt die begehbare Fläche (110, 125)–(1690, 875).
- Bank zur Wasserseite und Koffer an der Spur (805, 520); die schräge Fallblattanzeige ragt im Norden aus dem Wasser.
- Gusseiserne Dachstützen an der Nordkante, Salzkrusten, Teerlinien, Nebel über dem Wasser.
- Zusammengesetzt mit `tools/visuals/compose_platform.py` aus gemaltem Wasser und gemaltem Bahnsteigbelag (FLUX.2 [klein] 4B, Gouache-Stil B), damit die Bahnsteigkante exakt auf der begehbaren Fläche liegt. Bank, Koffer, Anzeigetafel, Stützen, Laterne und Poller sind eigene Props.

## Silhouette
Lange Plattform mit Granitkante, umgeben von Wasser; die Anzeigetafel als schräge Landmarke.

## Animationen
- `default`: Standbild, gekachelt.
