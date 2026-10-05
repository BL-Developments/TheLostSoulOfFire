# Qualitätsrubrik für Bildreihen

Mit dieser Rubrik bewertet ein Agent die Bildreihe aus `--slice-visual-test` und
jede spätere Grafik, bevor der Owner sie sieht. Die Bewertung ist eine
Empfehlung, kein Testkriterium; entscheiden tut der Owner.

Grundlage:
- [VISUAL-ART-DIRECTION.md](../../docs/current/VISUAL-ART-DIRECTION.md) §8 (Qualitätsbar), §5a/§5b (Bildsprache, Figuren), §10 Düsternis-Charta, §11 Symbolsatz, §12 Epochenregel;
- die Region-Verträge unter [`docs/current/regions/`](../../docs/current/regions/) und die Figurenblätter unter [`docs/current/characters/`](../../docs/current/characters/);
- die Stil-Bibel `art/production/STYLE-BIBLE.md`, sobald sie mit Aufgabe 7.5 vorliegt. Bis dahin entfallen die Punkte, die nur die Stil-Bibel prüfen kann (in der Tabelle mit „Stil-Bibel“ markiert).

## Bewertung je Punkt

Jeder Punkt bekommt genau eine Wertung:

| Wertung | Bedeutung |
| --- | --- |
| **erfüllt** | Die Frage ist am Bild ohne Zweifel mit Ja zu beantworten. |
| **teilweise** | Ja mit klarer Einschränkung; die Einschränkung steht in der Begründung. |
| **nicht erfüllt** | Nein. |
| **entfällt** | Die Frage betrifft dieses Bild nicht (z. B. Life Flame, wenn keine zu sehen ist). |

Zu jeder Wertung gehören eine Begründung in einem Satz und der Name der Aufnahme
als Beleg (z. B. `arena_overview`).

## Punkte

### A. Ort und Geschichte

| Nr. | Frage | Quelle |
| --- | --- | --- |
| A1 | Ist das Bild ohne HUD als The Lost Soul of Fire erkennbar? | §8 |
| A2 | **Erzählt das Bild die menschliche Geschichte seines Ortes?** Sind die Pflicht-Elemente des Region-Vertrags lesbar (Ufer: Bahnsteigkante, Bank, Koffer, Fallblattanzeige; Gießhalle: Ofen, Gießbett, Werkzeug an Haken, Spinde)? | §8, Region-Vertrag §8 |
| A3 | Ist die Signature Landmark des Region-Vertrags zu sehen und vom Kampfbereich aus lesbar? | Region-Vertrag Feld 15 |
| A4 | Passen alle Gegenstände zur Bau- und Ereignisepoche der Region (E1–E9)? | §12 |
| A5 | Wirkt keine Referenz so direkt, dass das Bild wie Fan-Art eines anderen Spiels aussieht? | §8, §2 |

### B. Raum und Komposition

| Nr. | Frage | Quelle |
| --- | --- | --- |
| B1 | Wirkt der Raum ohne VFX räumlich und komponiert (Ebenen, Höhe, Überlagerung)? | §8, §4 |
| B2 | Führt eine klare Wert-, Größen- und Lichthierarchie den Blick? | §8 |
| B3 | Vertieft Atmosphäre die Entfernung, statt alles gleichmäßig zu verschleiern? | §8 |
| B4 | Sind Ausgang und Hauptweg erkennbar? | §5, Room Grammar |
| B5 | Haben Figuren und Props sichtbaren Bodenkontakt (Schatten, Fußpunkt)? | §2 |

### C. Lesbarkeit im Kampf

| Nr. | Frage | Quelle |
| --- | --- | --- |
| C1 | Sind Spieler, Gegner und Telegraphe gleichzeitig lesbar? | §8 |
| C2 | Bleiben Gesicht, Hände und Waffenführung trotz Licht und Effekten lesbar? | §5a, §5b |
| C3 | Verdeckt kein Vordergrund oder Occluder Spieler, Gegner oder Telegraph, ohne durchscheinend zu werden? | §4 |
| C4 | Ist der Spieler aus jeder der acht Richtungen dieselbe Figur (ein Rig, gleiche Proportionen)? | §5b |
| C5 | Unterscheiden sich Effekte klar nach Quelle und Gewicht (drei Hiebe, Dash, Core-Treffer, Soul Release)? | §6, §7 |

### D. Düsternis-Charta

Die Fragen D1–D10, L1–L4 und W1–W6 aus §10 gelten wörtlich. W1, W2, W4 und W6
misst das Skript `tools/visuals/value_distribution.py`; W3 und W5 beurteilt der
Agent am Bild.

```sh
python tools/visuals/value_distribution.py artifacts/screenshots/*slice_*overview*.png --rect 240 200 1440 680
```

`--rect` umreißt die Kampffläche in Ausgabepixeln (1920 × 1080); bei der Arena
und dem Ufer im Überblick passt der obige Wert.

### E. Flammen und Symbole

| Nr. | Frage | Quelle |
| --- | --- | --- |
| E1 | Ist jede Death Flame violett bis fast weiß, ohne Orange, Grün oder Blau (S1–S4)? | §11 |
| E2 | Ist eine Life Flame, falls sichtbar, warm orange, weich und einzeln (S5–S7)? | §11 |
| E3 | Sind Warden-Zeichen und Keeper-Ring, falls sichtbar, nach ihren Formregeln gezeichnet (S8–S14)? | §11 |
| E4 | Bleibt übernatürliche Emission ein Budget, das Formen nicht verwischt? | §5a, §7 |

### F. Stil

| Nr. | Frage | Quelle |
| --- | --- | --- |
| F1 | Ist das Bild gemalt und hochaufgelöst, ohne Pixelkanten, harte Vektorlinien im Kampf oder Formen-Platzhalter? | Beschluss 02.10.2026, §5a |
| F2 | Kommt das Licht erkennbar von einem Schlüssellicht oben links? | §5a |
| F3 | Bleibt jede Figur bei wenigen Materialfamilien und einem gesättigten Akzent (Figurenblatt)? | §5a, Figurenblätter |
| F4 | Stimmen Kamerawinkel, Figurenhöhe, Wertstufen und Farbskript des Ortes mit der Stil-Bibel überein? | Stil-Bibel |
| F5 | Hält sich das Bild an die Verbotsliste der Stil-Bibel? | Stil-Bibel |

## Anleitung für die Agentenbewertung

1. Bildreihe erzeugen: `dotnet run --project src/TheLostSoulOfFire -- --slice-visual-test`. Die Aufnahmen liegen danach unter `artifacts/screenshots/` mit `slice_<name>` im Dateinamen.
2. Werte messen: `tools/visuals/value_distribution.py` auf die Überblicksaufnahmen anwenden und die JSON-Ausgabe in die Bewertung übernehmen.
3. Jede Aufnahme ansehen, die Punkte A–F für den Ort (Ufer, Arena) bewerten und für C und E die passenden Einzelaufnahmen als Beleg nennen. Ein Punkt wird für einen Ort einmal bewertet, nicht je Aufnahme.
4. Die Bewertung nach `art/production/reviews/<datum>-<gegenstand>.md` schreiben, zum Beispiel `2026-10-05-ludo-baseline.md`. Aufbau:
   - Kopf: Datum, Commit, Befehl, bewertete Aufnahmen;
   - je Ort eine Tabelle `Nr. | Wertung | Begründung | Beleg`;
   - Messwerte des Skripts;
   - die drei wichtigsten Mängel je Ort, nach Wirkung geordnet;
   - Empfehlung in einem Satz: annehmen, überarbeiten (mit Hinweis) oder verwerfen.
5. Aufnahmen werden nicht eingecheckt (`artifacts/` ist ignoriert). Die Bewertung nennt den Commit, mit dem sie sich wieder erzeugen lassen.
6. Die Bewertung ist eine Empfehlung. Freigaben gibt nur der Owner.
