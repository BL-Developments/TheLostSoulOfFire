## Context

Auf `main` führt der Ablauf Titel → Prolog → Aschenvorhalle → Tor → Arena. `SoulFurnaceAntechamber` ist ein einzelner 1500×820-Raum mit Spawn links und einem großen Tor rechts. `GameWorld` prüft `IsPlayerAtGate`, zeigt `E  ENTER THE SOUL FURNACE` und startet per `E` die Phase `EnteringArena`. `PixelText` kennt nur Großbuchstaben, Ziffern und `: - / . %`.

Björn hat entschieden: Hub = Aschenvorhalle, das Tor entfällt, die Arena liegt hinter der bereits offenen Tür I, und versiegelte Türen zeigen `SEALED · DEFEAT THE PREVIOUS GUARDIAN`.

## Goals / Non-Goals

**Goals:**

- Sieben lesbare, grafisch schlichte Biom-Türen im Hub.
- Tür I übernimmt den kompletten bisherigen Arena-Eintritt.
- Versiegelte Türen sind klar als solche erkennbar und reagieren nicht mit einem Übergang.
- Ein Sperrzustand pro Tür, der später von außen gesetzt werden kann.

**Non-Goals:**

- Freischaltung durch Endbosssiege, Speicherstand oder Meta-Fortschritt (#26, #27).
- Biom-Namen, Biom-Inhalte oder weitere Level hinter den Türen II bis VII.
- Neue Sprite-Assets; die Türen werden wie der restliche Raum mit Primitiven gezeichnet.

## Decisions

### Türen an der Nordwand, Raum wächst nach oben

Die sieben Türen stehen nebeneinander an der Nordwand, von links nach rechts `I` bis `VII`. Damit passen sie ohne Gänge oder Kollisionen in den bestehenden Rechteckraum. Der Raum wird dafür nach oben vergrößert, damit Türen, Säulen und Kohlebecken nicht kollidieren; die bewegliche Fläche bleibt ein einzelnes Rechteck und schließt direkt unter den Türen an. Konkrete Maße werden bei der Umsetzung anhand von Screenshots festgelegt.

Alternative: Türen verteilt auf mehrere Wände. Verworfen, weil die Reihenfolge I–VII in einer Reihe am schnellsten lesbar ist.

### Tür als kleines Datenobjekt

`SoulFurnaceAntechamber` ersetzt `Gate`/`InteractionZone` durch eine Liste von sieben Türen mit Index, Rechteck, Interaktionszone und `IsSealed`. Tür I ist offen, II–VII sind versiegelt. Eine Abfrage liefert die Tür, in deren Zone der Spieler steht (höchstens eine; Zonen überlappen nicht).

Der Sperrzustand wird vorerst im Konstruktor festgelegt. Die spätere Freischaltung kann ihn von außen setzen, ohne Raum oder Darstellung zu ändern.

### Tür I übernimmt den Arena-Eintritt unverändert

Die Phase `EnteringArena` und ihre Sequenz aus Öffnen, Kamera-Vorschub, Licht und Audio bleiben erhalten und beziehen sich auf Tür I statt auf das Tor. `GameFlowRules.EnterGate`/`FinishGateTransition` werden sprachlich auf Türen umbenannt; das Verhalten bleibt gleich. Der Einblendetext `THE GATE AWAKENS` wird angepasst (Default: `THE DOOR AWAKENS`).

### Hinweise

- Offene Tür I: `E  ENTER BIOME I` im bestehenden Prompt-Stil.
- Versiegelte Tür: `SEALED · DEFEAT THE PREVIOUS GUARDIAN` im selben Panel, gedämpfter Rahmen, ohne `E`-Symbol. `E` an einer versiegelten Tür bleibt wirkungslos.
- Jede Tür trägt ihre römische Ziffer über dem Bogen; versiegelte Türen erhalten ein Siegel-Symbol und dunklere Farben.

`PixelText` erhält dafür einen Glyph für `·`.

## Risks / Trade-offs

- [Längerer Hinweistext passt bei kleinen Fenstern nicht ins feste Panel] → Panelbreite aus der Textbreite berechnen.
- [Bestehende Screenshots und Visual-Test referenzieren das Tor] → Visual-Test auf Tür I umstellen und Screenshots neu erzeugen.
- [Größerer Raum verändert Kamera- und Spawn-Eindruck] → Spawn und Kameraführung bei der Umsetzung per Screenshot prüfen.
