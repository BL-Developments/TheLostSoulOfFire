## Context

Die Sandbox spawnt bisher nur über die Debug-Tasten `F2` bis `F4` (fester Versatz rechts vom Spieler). Arena-Wellen nutzen `ArenaEnemyKind` und feste Spawnpunkte am Arenarand. Das Dev-Menü ruft für Aktionen `ActivateDevEntry` und für die Anzeige `DevEntryValue` auf.

## Goals / Non-Goals

**Goals:**

- Jeder Gegnertyp lässt sich aus dem Menü spawnen, gut sichtbar und nie direkt auf dem Spieler.
- Neue Gegnertypen erscheinen im Menü, ohne das Menü anzupassen.
- Ein Eintrag räumt das Feld.

**Non-Goals:**

- Trainingspuppe (`add-training-dummy`), Anzahl oder Ort frei wählen, Gegnerwerte ändern.

## Decisions

**Eigene Typliste `SandboxEnemyKind` statt `ArenaEnemyKind`.** Die Sandbox soll auch Typen spawnen, die in keiner Welle vorkommen (die Trainingspuppe im nächsten Change). `SandboxEnemyKind` hält alle spawnbaren Typen; `SandboxSpawner` liefert Bezeichnung, Radius, Typprüfung und Erzeugung. `SandboxDevMenuEntries` erzeugt daraus mit `Enum.GetValues` je einen Aktionseintrag `spawn:<Typ>` und hängt `ALLE GEGNER ENTFERNEN` an.

**Spawnposition.** `SandboxSpawner.ChoosePosition` setzt den Gegner 260 Pixel vom Spieler entfernt, in einem Winkel, der sich pro Spawn um den goldenen Winkel weiterdreht. So landen Gegner rundum verteilt und nicht übereinander. Die Position wird mit Gegnerradius und kleinem Rand in die Kampfgrenzen geklemmt. Wäre sie danach näher als 160 Pixel am Spieler (an einer Wand), werden der Reihe nach andere Richtungen probiert. Die Funktion ist zustandslos und testbar.

**Menü bleibt offen.** Spawnen schließt das Menü nicht; die Zähler in der Wertespalte zeigen sofort, was auf dem Feld ist. Da das Spiel angehalten ist, greifen gespawnte Gegner erst nach dem Schließen an.

**Entfernen ohne Belohnung.** `ALLE GEGNER ENTFERNEN` leert Gegner- und Seelenliste direkt, statt Schaden zuzufügen. So entstehen keine Seelen, keine Todesanimationen und keine Gutschriften; nur eine kleine Todesflamme markiert die Stellen.
