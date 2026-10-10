## Context

Die Regeln stehen in `docs/current/ECONOMY.md` (#53): drei Entscheidungen am Reisepunkt, feste Quote 50 %, Abrundung je Währung, höchstens eine Teilsicherung je Reisepunkt, Extraktion sichert alles und beendet den Run, Niederlage verliert nur den Run-Bestand. Die Arena hat zehn Wellen, Kisten nach den Wellen 3, 6 und 9 und eine Pause nach jeder Welle außer der letzten.

## Decisions

- **Platzierung:** Ein Reisepunkt in der Pause nach Welle 5 (Entscheidung Björn, 06.10.2026). Er steht oben mittig in der Kampffläche, außerhalb der Reichweite der Kisten und der Auslösezone in der Mitte. Ohne eigene Grafik zeichnet ihn ein Platzhalter aus Stein, Ring und kaltem Seelenlicht.
- **Bedienung:** `E` in Reichweite öffnet das Menü; Kisten haben Vorrang, danach der Reisepunkt, danach die Auslösezone. Im Menü wählen `W`/`S` oder Pfeiltasten, `1` bis `3` wählen direkt, `E`, `Enter` oder `Leertaste` bestätigen, `Esc` schließt ohne Entscheidung. Die Welt steht still, solange das Menü offen ist.
- **Vorschau:** Das Menü zeigt je Option, was gesichert wird und was im Run bleibt, dazu die bereits gesicherten Bestände (Teil von #73).
- **Weiter:** Teilsichern und weiter sowie Weiter ohne Sichern starten sofort die nächste Welle, wie eine Weiterreise ins nächste Level.
- **Extrahieren:** Wechselt mit `GameFlowRules.ExtractToHub` in den Hub. Der Hub zeigt einige Sekunden lang, was extrahiert wurde, über der vorhandenen Zeile mit den gesicherten Beständen. Der nächste Arena-Run beginnt bei Welle 1.
- **Einmaligkeit:** `TravelPoint.TryDecide` nimmt nur die erste Entscheidung an. Doppelte Tastendrücke oder ein erneutes Öffnen können nicht zweimal sichern.
- **Persistenz:** Teilsicherung und Extraktion speichern das Profil sofort, damit eine spätere Niederlage nichts mehr davon nehmen kann.

## Non-Goals

- Zweiter Reisepunkt, Levels, Bosssieg mit Rückkehr (#14, #66, #27).
- Koop-Zustimmung beider Spieler (#59).
- Eigene Grafik für den Reisepunkt.
