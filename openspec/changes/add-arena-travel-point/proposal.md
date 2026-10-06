# Reisepunkt in der Arena

## Why

#53 hat Teilsicherung, Extraktion und Niederlage beschlossen (`docs/current/ECONOMY.md` auf `main`). Bisher sichert nur der Arena-Abschluss, und zwar alles. #74 verlangt die Entscheidung am Reisepunkt mit Persistenz und Grenzfällen. Levels gibt es noch nicht (#14, #66), deshalb steht der Reisepunkt vorläufig in der Arena.

## What Changes

- Nach Welle 5 erscheint in der Pause ein Reisepunkt (Björn, 06.10.2026: einer nach Welle 5, der zweite kommt mit echten Levels).
- `E` am Reisepunkt öffnet ein Menü mit drei Entscheidungen und einer Vorschau der Beträge: Teilsichern und weiter, Weiter ohne Sichern, Extrahieren.
- Teilsichern überträgt 50 % beider Run-Bestände, je Währung abgerundet, und speichert sofort. Extrahieren sichert alles, speichert und führt in den Hub.
- Je Reisepunkt ist genau eine Entscheidung möglich. Wer die Welle in der Arenamitte startet, ohne den Reisepunkt zu nutzen, geht weiter ohne Sichern.
- Der Arena-Abschluss nach Welle 10 sichert weiterhin alles und steht für den Bosssieg.
- Neuer automatischer Lauf `--travel-visual-test`.

## Capabilities

### New Capabilities

- `travel-points`: Reisepunkt mit Teilsicherung, Weiterreise und Extraktion.

## Impact

- `CurrencyWallet` (Teilsicherung, Vorschau), neue Klasse `TravelPoint`, `GameWorld.TravelPoint.cs`, Anbindung in `GameWorld.cs` und `GameWorld.Currency.cs`, `GameFlowRules.ExtractToHub`.
- Kein Koop: #59 ist offen, die Koop-Prüfung bleibt NOT_RUN.
