## Context

Die Sandbox läuft als Phase `Arena` mit dem Kennzeichen `_sandboxActive`. Pausen- und Charaktermenü frieren das Spiel über `IsGamePaused` ein; `GameWorld.Update` lässt dann nur Menü und Audio-Mix laufen, `AudioDirector.SetPaused` senkt Musik und hält Effekte an. `F` ist bisher nirgends belegt, `F1` bis `F11` sind Debug- und Fenstertasten.

## Goals / Non-Goals

**Goals:**

- Ein Menü, das Werte- und Aktionseinträge in zwei Abschnitten aufnehmen kann, ohne dass die Folge-Changes es umbauen müssen.
- Gleiches Anhalteverhalten wie Pausen- und Charaktermenü.
- Das Feld bleibt sichtbar, damit Spawns und Werte direkt beurteilt werden können.

**Non-Goals:**

- Konkrete Einträge (Folge-Changes), Gamepad-Bedienung.

## Decisions

**Eigenes kleines Modell.** `DevMenu` hält offen/zu, Öffnungszeit, die Einträge (`Id`, Abschnitt, Art `Value` oder `Action`, Beschriftung) und die Auswahl. Es kennt weder MonoGame noch Spiellogik; `GameWorld` bildet Tasten darauf ab und entscheidet in `AdjustDevEntry` und `ActivateDevEntry`, was ein Eintrag tut, und in `DevEntryValue`, welchen Wert er zeigt. Alternative: `MenuController` wiederverwenden. Verworfen, weil dessen Seitenstapel und Einstellungswerte an `GameSettings` hängen.

**Eine Liste über beide Abschnitte.** Abschnitte sind nur Überschriften; `W`/`S` laufen durch alle Einträge, ohne am Rand umzubrechen. So bleiben `A`/`D` für Werte frei und es gibt keine zweite Navigationsebene. Die Einträge stehen in `SandboxDevMenuEntries.All` in Anzeigereihenfolge und müssen nach Abschnitt gruppiert sein; das Modell prüft das.

**Auswahl bleibt erhalten.** Wer mehrere Gegner nacheinander spawnt, soll nicht jedes Mal neu navigieren. Anders als das Charaktermenü setzt das Dev-Menü die Auswahl beim Öffnen deshalb nicht zurück.

**Tafel statt Vollbild.** `DevMenuRenderer` zeichnet einen leichten Schleier und eine Tafel links unter dem Lebensbalken. Trefferflächen für die Maus entstehen aus derselben Anordnung wie das Zeichnen. Ein Klick auf einen Werteintrag ändert ihn: linke Hälfte verringert, rechte erhöht.

**Tasten.** `F` öffnet nur in der Sandbox und nur, wenn kein anderes Menü offen ist; die Prüfung steht hinter den Abfragen für Pausen- und Charaktermenü. Ist das Dev-Menü offen, behandelt `GameWorld.Update` nur noch das Dev-Menü, sodass `Escape` und `Tab` dort keine anderen Menüs öffnen.
