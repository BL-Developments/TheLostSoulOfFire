# Bewertung: Ausgangsstand Ludo-Arena und Formen-Ufer

- **Datum:** 05.10.2026
- **Commit:** `47c0ea1` (Branch `docs/visual-vertical-slice`)
- **Befehl:** `dotnet run --project src/TheLostSoulOfFire -- --slice-visual-test`
- **Bewertet:** alle 25 Aufnahmen der Bildreihe; Ufer = Formenkomposition aus `PrologueEnvironment`, Arena = Ludo-Grafik vom 29.08.2026
- **Rubrik:** [QUALITY-RUBRIC.md](../QUALITY-RUBRIC.md); Punkte F4 und F5 entfallen, weil die Stil-Bibel (Aufgabe 7.5) noch fehlt.
- **Zweck:** Probebewertung (Aufgabe 5.3). Sie zeigt den Abstand zum Ziel und testet die Rubrik.

## Messwerte

`tools/visuals/value_distribution.py … --rect 240 200 1440 680` auf die Überblicksaufnahmen:

| Aufnahme | Wertstufen 1–7 in % | W1 (≥ 60 %) | W2 (≤ 3 %) | W4 Boden | W6 größte schwarze Fläche |
| --- | --- | --- | --- | --- | --- |
| `shore_overview` | 97,8 · 2,2 · 0 · 0 · 0 · 0 · 0 | 100 % – erfüllt | 0 % – erfüllt | Stufe 1 – nicht erfüllt | 5.296 px – erfüllt |
| `arena_overview` | 97,5 · 2,4 · 0,1 · 0 · 0 · 0 · 0 | 100 % – erfüllt | 0 % – erfüllt | Stufe 1 – nicht erfüllt | 810.416 px, etwa 55 Spielerfiguren – nicht erfüllt |

W1 ist in beiden Fällen nur formal erfüllt: Fast das ganze Bild liegt in der
dunkelsten Stufe, es gibt keine Wertverteilung. Die Ludo-Bodenplatte ist in der
Mitte nahezu schwarz (Quellbild: Mittelwert RGB 4/4/6, Höchstwert 8).

## Ufer (Formen)

| Nr. | Wertung | Begründung | Beleg |
| --- | --- | --- | --- |
| A1 | nicht erfüllt | Graue Rechtecke und Linien lesen sich als Prototyp, nicht als das Spiel. | `shore_overview` |
| A2 | teilweise | Bank, Koffer und Anzeigetafel sind als kleine Formen da, aber ohne Material; die Geschichte trägt erst die Erzählzeile „A BENCH A DEPARTURE BOARD NOBODY CAME BACK“. | `shore_soul_sense_trace` |
| A3 | nicht erfüllt | Keine schräg aus dem Wasser ragende Fallblattanzeige; die Tafel ist ein kleines Rechteck. | `shore_overview` |
| A4 | teilweise | Nichts widerspricht der Epoche, aber es gibt zu wenige Gegenstände, um sie zu zeigen. | `shore_overview` |
| A5 | erfüllt | Keine erkennbare Referenz. | `shore_overview` |
| B1 | nicht erfüllt | Eine flache Bodenplatte mit Plattenraster; Höhe und Überlagerung fehlen bis auf zwei Vordergrundbalken. | `shore_overview` |
| B2 | teilweise | Nur Kern des Spielers und Warden-Marke sind hell; eine Lichthierarchie fehlt. | `shore_overview` |
| B3 | nicht erfüllt | Keine Atmosphäre, kein Wasser, keine Tiefe. | `shore_overview` |
| B4 | teilweise | Die Warden-Marke im Osten zeigt den Ausgang, ist aber klein. | `shore_overview` |
| B5 | teilweise | Spieler mit Schattenkreis, Props ohne Kontaktschatten. | `shore_soul_sense_trace` |
| C1 | erfüllt | Ein Hollow und der Spieler sind auf dem gleichmäßigen Boden klar zu sehen. | `shore_first_hollow` |
| C2 | nicht erfüllt | Gesicht und Hände des Spielers sind auf Spielgröße nicht lesbar (Ludo-Sprite). | `shore_first_hollow` |
| C3 | entfällt | Keine Occluder im Kampfbereich. | – |
| C4 | siehe Arena | Gleiche Spielergrafik. | – |
| C5 | entfällt | Keine Kampfeffekte am Ufer aufgenommen. | – |
| D1 | nicht erfüllt | Gleichmäßiges Dunkel ohne Lichtinseln. | `shore_overview` |
| D2 | teilweise | Risse als schwarze Linien, sonst kein Verfall. | `shore_overview` |
| D3 | erfüllt | Bank und Koffer. | `shore_soul_sense_trace` |
| D4–D8 | erfüllt | Kein Blut, kein Körperhorror, keine Knochen, keine Folter, keine Höllenzeichen. | alle |
| D9 | nicht erfüllt | Keine Bedrohung durch Größe oder Stille; der Raum wirkt leer, nicht still. | `shore_overview` |
| D10 | erfüllt | Der Hollow trägt eine Maske. | `shore_first_hollow` |
| L1 | nicht erfüllt | Kein Schlüssellicht. | `shore_overview` |
| L2 | erfüllt | Höchstens zwei Lichtpunkte. | `shore_overview` |
| L3 | teilweise | Der Spielerkern ist der hellste Punkt; die Spur ist erst mit Soul Sense hell. | `shore_soul_sense_trace` |
| L4 | erfüllt | Keine warmen Lampen. | `shore_overview` |
| W1–W6 | siehe Messwerte | W4 nicht erfüllt; W3 entfällt (nichts in Stufe 7); W5 teilweise. | `shore_overview` |
| E1 | erfüllt | Warden-Marke und Spur sind violett. | `shore_soul_sense_trace` |
| E2 | entfällt | Keine Life Flame. | – |
| E3 | erfüllt | Die Warden-Marke ist eine senkrechte, ruhige Flamme in einer Eisenfassung (S8, S9). | `shore_overview` |
| E4 | erfüllt | Kaum Emission. | `shore_overview` |
| F1 | nicht erfüllt | Vektorformen statt gemalter Grafik. | `shore_overview` |
| F2 | nicht erfüllt | Kein gerichtetes Licht. | `shore_overview` |
| F3 | entfällt | Umgebung ohne Figurenakzent. | – |
| F4, F5 | entfällt | Stil-Bibel fehlt noch. | – |

**Größte Mängel:**
1. Kein gemalter Ort: Bahnsteig, Wasser und Anzeigetafel fehlen als Bild. Die Geschichte steht nur im Text.
2. Kein Licht und keine Wertstufen: Alles liegt in Stufe 1, der Boden ist zu dunkel (W4).
3. Kein Raum: Ohne Höhe, Überlagerung und Atmosphäre wirkt das Ufer wie ein Kampfbrett.

## Arena (Ludo)

| Nr. | Wertung | Begründung | Beleg |
| --- | --- | --- | --- |
| A1 | teilweise | Öfen, Bögen und Rohre am Rand lesen sich als Soulfire Gothic; die schwarze Mitte wirkt wie ein Loch. | `arena_overview` |
| A2 | teilweise | Industrie ist erkennbar, Menschen nicht: keine Werkzeuge, Spinde oder Werkbänke. | `arena_overview` |
| A3 | nicht erfüllt | Keine Rosette aus dem Schwungrad und kein geneigter Hochofen; die Öfen stehen gleichförmig in einer Reihe. | `arena_overview` |
| A4 | teilweise | Industriezeitlich, aber violette Kristalle in Öfen und Wänden lesen sich als Fantasy-Zierde. | `arena_overview` |
| A5 | erfüllt | Keine erkennbare Referenz. | `arena_overview` |
| B1 | teilweise | Die Ränder haben Höhe und Überlagerung, die Kampffläche ist leer und flach. | `arena_overview` |
| B2 | nicht erfüllt | Der Blick bleibt an den hellen Rändern hängen, die Mitte hat keinen Wert. | `arena_overview` |
| B3 | nicht erfüllt | Keine Atmosphäre, die Tiefe staffelt. | `arena_overview` |
| B4 | teilweise | Das Gitter im Süden ist als Tor lesbar. | `arena_overview` |
| B5 | nicht erfüllt | Der Schattenkreis des Spielers verschwindet auf dem schwarzen Boden. | `arena_idle_s` |
| C1 | teilweise | Hollows sind vor allem über Maske und Kern lesbar, ihre Körper versinken im Boden; die Telegraph-Bögen sind klar. | `arena_hollow_swipe_telegraph` |
| C2 | nicht erfüllt | Der Spielerkörper ist kaum zu sehen; Sense und Cannon dominieren, ein Gesicht fehlt. | `arena_idle_*` |
| C3 | erfüllt | Der Säulen-Dummy wird durchscheinend, der Spieler bleibt sichtbar. | `arena_occluder` |
| C4 | nicht erfüllt | Die acht Richtungen sind getrennte Zeichnungen; in der Seitenansicht trägt die Figur Fledermausflügel (Verstoß gegen §5b); die Waffen drehen als eigene Sprites. | `arena_idle_e`, `arena_idle_w` |
| C5 | teilweise | Die drei Hiebe steigern sich in Größe und Helligkeit, Hieb 1 und 2 sind sich aber ähnlich; Dash-Zündung und Core-Treffer sind klein. | `arena_slash_1`–`3`, `arena_dash_ignition`, `arena_core_hit` |
| D1 | teilweise | Lichtinseln nur an den Ofenschlitzen am Rand. | `arena_overview` |
| D2 | erfüllt | Risse, Rost und Bruch am Rand. | `arena_overview` |
| D3 | nicht erfüllt | Kein Gegenstand zeigt Menschen. | `arena_overview` |
| D4–D8 | erfüllt | Nichts davon im Bild. | alle |
| D9 | teilweise | Die Öfen haben Größe, die Stille fehlt durch verstreute Lichtpunkte. | `arena_overview` |
| D10 | erfüllt | Hollows mit Maske. | `arena_hollow_swipe_telegraph` |
| L1 | nicht erfüllt | Kein durchgehendes Schlüssellicht. | `arena_overview` |
| L2 | teilweise | Viele kleine violette Punkte statt weniger Inseln. | `arena_overview` |
| L3 | nicht erfüllt | Die hellsten Stellen sind Zierkristalle am Rand. | `arena_overview` |
| L4 | erfüllt | Keine warmen Lampen. | `arena_overview` |
| W1–W6 | siehe Messwerte | W4, W5 und W6 nicht erfüllt; W3 erfüllt (Stufe 7 nur an Kern und Effekten). | `arena_overview` |
| E1 | erfüllt | Alle Death-Flame-Effekte sind violett (Farbbudget-Test: höchstens 1,75 % Blau). | `arena_slash_*`, `arena_soul_release` |
| E2 | entfällt | Die Life Flame erscheint erst nach Welle 10. | – |
| E3 | entfällt | Keine Warden-Zeichen. | – |
| E4 | teilweise | Violette Kristalle in den Wänden konkurrieren mit dem Violett des Kampfes. | `arena_overview` |
| F1 | nicht erfüllt | Pixel-Look mit harten Kanten (Beschluss vom 02.10.2026: gemalt, hochaufgelöst). | `arena_overview` |
| F2 | nicht erfüllt | Licht ohne gemeinsame Richtung. | `arena_overview` |
| F3 | nicht erfüllt | Spieler mit Flügeln und vielen Farben statt einem Akzent (Figurenblatt: Kompass, Karminrot). | `arena_idle_e` |
| F4, F5 | entfällt | Stil-Bibel fehlt noch. | – |

**Größte Mängel:**
1. Die schwarze Kampffläche schluckt Figuren und Schatten (W4, W5, W6, B5); Lesbarkeit hängt allein an Masken, Kernen und Effekten.
2. Der Spieler verletzt §5b: Flügel, getrennt gezeichnete Richtungen, kaum lesbarer Körper (C2, C4, F3).
3. Kein Ort mit Geschichte: weder Landmarke noch menschliche Spuren der Gießhalle (A2, A3, D3).

## Empfehlung

Beide Orte **überarbeiten**: Die Formen am Ufer und die Ludo-Arena taugen als
Platzhalter für Spielbarkeit, nicht als Zielbild. Sie werden mit den Gruppen 8
bis 11 durch Grafik aus dem Hausstil ersetzt. Der erste Hebel ist die
Wertverteilung des Bodens (Stufe 2–3).

## Erfahrungen mit der Rubrik

- W1 sollte eine Untergrenze bekommen (z. B. höchstens 85 % in Stufe 1), sonst besteht ein fast schwarzes Bild. Vorschlag für die Stil-Bibel; die Charta bleibt bis zu einer Owner-Entscheidung unverändert.
- Die Bildreihe zeigt die Life Flame nicht, weil sie erst nach der letzten Welle erscheint. E2 lässt sich erst mit einer zusätzlichen Aufnahme prüfen.
