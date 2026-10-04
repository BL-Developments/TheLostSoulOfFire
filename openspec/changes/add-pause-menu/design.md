## Context

`MenuController` führt Seitenstapel, Auswahl und Reveal-Sperre und ist frei von MonoGame; `Open()` legt fest das Hauptmenü als Wurzel. `GameWorld.UpdateMenu` wertet Maus und Tastatur für das Titelmenü aus, `CinematicPresentation` zeichnet es. `Game1` beendet die Anwendung bei gehaltenem `Escape` in jedem Zustand außer auf Einstellungsseiten. `GameWorld.Update` tickt jedes Frame Präsentation, Audio, Effekte und die Simulation der aktuellen Phase; eine Pause gibt es nicht. `ResetFullRun` setzt einen Lauf vollständig auf die Titelkarte zurück.

## Goals / Non-Goals

**Goals:**
- Escape-Semantik vollständig im testbaren `MenuController` bündeln.
- Titel- und Pausenmenü teilen Eingabe, Seiten, Einstellungen und Zeichenroutine.
- Pause friert die Simulation ein, ohne einzelne Systeme anzufassen.

**Non-Goals:**
- Automatisches Pausieren bei Fokusverlust.
- Gamepad-Bedienung; der Gamepad-Back-Knopf behält seine bisherige Wirkung.
- Echte Errungenschaften und Statistiken, Spielstände.

## Decisions

### Zweite `MenuController`-Instanz mit eigener Wurzelseite
`Open(MenuPage root)` nimmt die Wurzelseite entgegen; `GameWorld` hält `_menu` (Wurzel `Main`) und `_pauseMenu` (Wurzel `Pause`) mit demselben `GameSettings`. Alternative: ein Controller, der je nach Phase umschaltet. Verworfen, weil Seitenstapel und Reveal-Timer sich dann gegenseitig überschreiben könnten.

### Escape als Controller-Aktion
`MenuController.HandleEscape()` liefert ein `MenuActionResult`: Unterseite → eine Ebene zurück; Wurzel `Pause` → `Resume`; Wurzel `Main` → Beenden-Abfrage öffnen. `OpenQuitConfirmation()` öffnet das Hauptmenü direkt mit Abfrage (Titelkarte). Dadurch sind alle Escape-Regeln ohne MonoGame testbar. `Game1` wertet `Escape` nicht mehr aus; `GameWorld` reagiert nur auf `WasKeyPressed`, damit gehaltenes `Escape` nicht mehrfach wirkt.

### Neue Seiten und Ergebnisse
`Pause` (`FORTSETZEN`, `EINSTELLUNGEN`, `ERRUNGENSCHAFTEN UND STATISTIKEN`, `BEENDEN`), `PauseQuit` (`ZURÜCK ZUM HAUPTMENÜ`, `ZURÜCK ZUM DESKTOP`, `ZURÜCK`) und `QuitConfirm` (`JA`, `NEIN`, mit Frage als Seitentext). `MenuPage` erhält einen optionalen `Prompt`. Neue Ergebnisse `Resume` und `QuitToMainMenu`; `JA` und `ZURÜCK ZUM DESKTOP` liefern das bestehende `Quit`. Der Hauptmenü-Eintrag `BEENDEN` öffnet `QuitConfirm`, der Pausen-Eintrag `BEENDEN` (`PauseQuit`-Id) öffnet `PauseQuit`.

### Gemeinsame Menüeingabe
Hover, Pfeiltasten, Wertänderung, Klick und `Confirm` aus `UpdateMenu` werden in `UpdateMenuInput(MenuController, …)` gezogen; die doppelt geführte Liste der Wertzeilen wird `MenuController.IsValueEntry`.

### Pausenweiche am Anfang von `GameWorld.Update`
Ist `_pauseMenu.IsOpen`, tickt `Update` nur Menü und Audio und kehrt dann zurück, bevor Präsentationszeit, Effekte, Partikel, Kamera oder Phasenlogik fortschreiten. Das Öffnen geschieht ebenfalls vor allen Ticks und beendet das Frame. Das Fortsetzen beendet das Frame ebenfalls, deshalb sieht `ScytheCombat` den bestätigenden Klick nie als neuen Tastendruck. Alternative: Zeitskala 0. Verworfen, weil einige Systeme Eingaben unabhängig von der Zeit auswerten.

### Darstellung
`CinematicPresentation.DrawPauseMenu` zeichnet am Ende von `DrawHud` (über HUD und Debug-Overlay) Schleier 0,62, Letterbox, Zierlinie, `PAUSIERT` und die Liste. `DrawMenuList` erhält die Atemzeit als Parameter (Titel: `_titleTime`, Pause: `OpenTimer`, weil `_titleTime` außerhalb der Titelphase steht) und zeichnet einen vorhandenen `Prompt` über den Einträgen. Hit-Boxen bleiben in `GetMenuEntryBounds` mit denselben Konstanten.

### Audio
`AudioDirector.SetPaused(bool)` pausiert bzw. setzt laufende Effektinstanzen fort und multipliziert Musik und Ambience im Mix mit 0,4. `StopEffects()` verwirft Effekte vor der Rückkehr ins Hauptmenü.

### Rückkehr ins Hauptmenü
`ResetFullRun` und anschließend `_menu.Open()`; die Titeldarstellung blendet wie beim Start ein, das Menü ist sofort geöffnet.

## Risks / Trade-offs

- [Escape beendet nicht mehr sofort] → Beenden über Abfrage oder Pausenmenü ist weiterhin mit zwei Eingaben möglich; Gamepad-Back bleibt unverändert.
- [Zeitabhängige Effekte, die außerhalb von `GameWorld.Update` ticken] → Ein Screenshot vor und nach einer Pause von mehreren Sekunden muss dasselbe Bild zeigen.
- [Offener Change `add-initial-settings-menu` ändert dieselbe Anforderung] → Dieser Change formuliert die Anforderung vollständig; nach jenem archivieren.
