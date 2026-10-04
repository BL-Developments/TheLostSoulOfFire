## Context

Das Pausenmenü (`pause-menu`) liegt als zweiter `MenuController` in `GameWorld`. Ist es offen, laufen in `GameWorld.Update` nur Menü und Audio-Mix weiter; alles andere bleibt stehen. `CinematicPresentation.DrawPauseMenu` zeichnet Schleier, Letterbox, Zierlinie, `PAUSIERT` und die Einträge. `AudioDirector.SetPaused` senkt Musik und Ambience und hält Effekte an.

Die Charakterwerte stehen seit `add-player-attributes` in `Player.Attributes` (`PlayerAttributes`: `Strength`, `AbilityPower`, `Armor`, dazu `StrengthMultiplier`, `AbilityPowerMultiplier`, `ArmorReduction`). Leben steht in `Player.Health` und `GameBalance.PlayerMaxHealth`. `Tab` ist bisher nirgends belegt.

## Goals / Non-Goals

**Goals:**

- Die Charakterwerte im Spiel sichtbar machen, mit ihrer Wirkung in verständlichen Prozentzahlen.
- Eine Reiterleiste, die Map und Skills später ohne Umbau aufnehmen kann.
- Dasselbe Pausenverhalten und derselbe Stil wie beim Pausenmenü, ohne die Pausenlogik zu verdoppeln.

**Non-Goals:**

- Inhalte für Map und Skills, Werte erhöhen, Ausrüstung oder Inventar.
- Gamepad-Bedienung und Tastenbelegung ändern (fehlt auch beim Pausenmenü).
- Währungen ausgeben oder umbuchen; die Seite zeigt sie nur an.

## Decisions

**Eigenes kleines Modell statt `MenuController`.** `MenuController` arbeitet mit einem Seitenstapel und senkrechten Einträgen. Das Charaktermenü braucht nur offen/zu, einen gewählten Reiter und eine Öffnungszeit für das Einblenden. Ein eigenes `CharacterMenu` (ohne MonoGame-Abhängigkeit, mit `Open`, `Close`, `SelectNext`, `SelectPrevious`, `Select(tab)`, `Update(dt)`) bleibt testbar und lässt `MenuController` unverändert. Alternative: Reiter als Seiten im `MenuController`. Verworfen, weil Reiter waagerecht wechseln und `Links`/`Rechts` dort schon Werte verstellen.

**Eine gemeinsame Pausenbedingung.** `GameWorld.Update` prüft heute `_pauseMenu.IsOpen`. Daraus wird eine Eigenschaft `IsGamePaused` (Pausenmenü oder Charaktermenü offen), die überall dort greift, wo heute das Pausenmenü abgefragt wird: Update-Stopp, ausgeblendete Story- und Hinweistexte, Screenshot-Kontext. `AudioDirector.SetPaused` wird beim Öffnen und Schließen beider Menüs genutzt.

**Tasten.** `Tab` öffnet in jeder Phase außer der Titelphase, wenn kein Menü offen ist. Im Charaktermenü schließen `Tab` und `Escape`; die schließende Taste löst keine Spielaktion aus, weil der Frame danach wie beim Fortsetzen der Pause endet. `Escape` öffnet in diesem Frame nicht zusätzlich das Pausenmenü. `Tab` im Pausenmenü bleibt wirkungslos, damit nie zwei Menüs übereinanderliegen. Reiterwechsel per `Links`/`Rechts`, `A`/`D` und Mausklick auf einen Reiter; am Rand wird nicht umgebrochen, damit die Reihenfolge eindeutig bleibt. Beim Öffnen ist immer `CHARAKTER` gewählt. Alternative: `Q`/`E` für Reiter wie in vielen Spielen. Verworfen, weil `A`/`D` und Pfeile schon die Menüsprache des Projekts sind.

**Charakterseite.** Eine zweispaltige Liste aus Bezeichnung und Wert, darunter je Wert eine kurze Wirkungszeile in gedämpfter Farbe:

| Bezeichnung | Wert | Wirkung |
| --- | --- | --- |
| LEBEN | `85 / 100` | – |
| STÄRKE | `10` | `WAFFENSCHADEN +0 %` |
| FÄHIGKEITSSTÄRKE | `10` | `FÄHIGKEITSSCHADEN +0 %` |
| RÜSTUNG | `10` | `SCHADENSVERRINGERUNG 17 %` |

Prozentwerte kommen aus `PlayerAttributes` (`(Multiplier − 1) × 100`, `ArmorReduction × 100`), gerundet auf ganze Zahlen, beim Schaden mit Vorzeichen. `PixelText` kennt bisher kein `+`; das Zeichen wird ergänzt. Damit stimmen Anzeige und Rechnung immer überein. Die Werte werden bei jedem Zeichnen aus dem Spieler gelesen, nicht beim Öffnen kopiert.

**Währungen auf der Charakterseite.** Unter den Werten folgt ein Abschnitt `WÄHRUNGEN` mit je einer Zeile für `GELD` und `GLUT`, gelesen aus `CurrencyWallet`. In der Arena nennt jede Zeile `IM LAUF <n>` und `GESICHERT <n>`, damit sichtbar ist, was bei einer Niederlage verloren ginge. Außerhalb der Arena gibt es keinen Run, der Run-Bestand wäre immer 0; dort steht nur `GESICHERT <n>`. Die Farben der Bezeichnungen folgen den Akzenten des Währungs-HUD, damit Geld und Glut überall gleich aussehen.

**Platzhalterseiten.** `MAP` und `SKILLS` sind wählbar und zeigen mittig einen gedämpften Hinweis `NOCH NICHT VERFÜGBAR`. Ihre Reiter stehen in der gedämpften Platzhalterfarbe wie Platzhaltereinträge im Hauptmenü.

**Darstellung.** Neue Methode `CinematicPresentation.DrawCharacterMenu` nutzt Schleier, Letterbox, Zierlinie und Einblendung des Pausenmenüs. Statt `PAUSIERT` steht oben die Reiterleiste; der gewählte Reiter trägt die Auswahlfarbe, das Atmen und die Auswahlmarkierung des Hauptmenüs, dazu eine Unterstreichung. Darunter liegt der Seiteninhalt. HUD und angehaltenes Spielbild bleiben unter dem Schleier sichtbar, Story- und Hinweistexte werden wie in der Pause ausgeblendet.

**Abnahme per Screenshot.** Der vorhandene Screenshot-Kontext erhält `character_<reiter>`, damit native Aufnahmen der drei Seiten eindeutig benannt sind.

## Risks / Trade-offs

- [`Tab` wechselt unter Windows mit `Alt` das Fenster] → `Alt+Tab` wird nicht abgefangen; ein kurzer Tab-Druck ohne `Alt` öffnet das Menü wie gewollt. Ein versehentlich geöffnetes Menü schließt sich mit `Tab`.
- [Leben ändert sich nicht, solange das Menü offen ist] → Gewollt, das Spiel steht. Die Anzeige liest trotzdem live, damit spätere Effekte ohne Umbau sichtbar werden.
- [Spätere Map- und Skill-Inhalte brauchen eigene Bedienung] → Jeder Reiter bekommt dann seine eigene Seitenlogik; die Reiterleiste bleibt gleich.

## Open Questions

- Ob das Menü sich künftig den zuletzt gewählten Reiter merkt, sobald Map und Skills Inhalte haben.
- Ob die Charakterseite später weitere Werte zeigt (Tempo, Resonance, Kanonenladung), wenn diese entwickelbar werden.
