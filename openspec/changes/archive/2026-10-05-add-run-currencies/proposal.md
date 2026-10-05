## Why

Die Wirtschaftsregeln sind seit #52 beschlossen, im Spiel gibt es aber noch keine Währung. Ohne Konten lassen sich weder Fähigkeitskosten (#21), Händler und Schmied (#22) noch Skilltrees (#25) bauen, und die zentrale Entscheidung zwischen Ausgeben im Run und Sichern für später ist nicht spürbar.

## What Changes

- Zwei Währungen mit den Arbeitsnamen **Geld** und **Glut** (mystische Ressource). Jede hat einen Run-Bestand und einen gesicherten Bestand.
- Jeder besiegte Gegner schreibt genau einmal Glut gut, Menge je Gegnertyp. Ein Glutfunke fliegt vom Gegner zum Spieler; die Gutschrift hängt nicht vom Ende der Animation ab.
- Soul Release erzeugt weiter nur Resonance und keine Währung.
- Ein Run beginnt beim Betreten der Arena (oder bei einem Neuversuch) mit einem kleinen kostenlosen Glut-Basisvorrat. Gesicherte Glut wird nicht in den Run mitgenommen.
- Nach jeder Welle außer der letzten erscheint eine Kiste. Beim Öffnen mit `E` legt sie einmalig Geld in den Run-Bestand und verschwindet.
- Nach den Wellen 1 bis 3 pausiert die Arena. Die nächste Welle startet erst, wenn der Spieler in der markierten Zone in der Arenamitte `E` drückt; so bleibt Zeit für die Kiste.
- Niederlage leert beide Run-Bestände.
- Der Arena-Abschluss gilt vorläufig als Sicherungspunkt und überträgt beide Run-Bestände vollständig in den gesicherten Bestand. Diese Regel ist ein Platzhalter, bis #53 Teilsicherung und Quote festlegt.
- Der gesicherte Bestand wird in einem versionierten Profil gespeichert und beim Start geladen.
- Das HUD zeigt im Run beide Run-Bestände; im Hub werden die gesicherten Bestände angezeigt.
- Kontenmodell mit atomarem Ausgeben (keine negativen Werte, keine Doppelabbuchung) als Grundlage für spätere Ausgaben. In diesem Change gibt es noch nichts zu kaufen.

## Capabilities

### New Capabilities

- `run-currencies`: Geld und Glut, Run- und gesicherter Bestand, Gutschrift beim Besiegen, Basisvorrat, Kisten, Verlust bei Niederlage, vorläufige Sicherung beim Arena-Abschluss und HUD-Anzeige.
- `player-profile`: Versioniertes, atomar gespeichertes Profil mit den gesicherten Beständen und definiertem Verhalten bei fehlendem oder ungültigem Spielstand.

### Modified Capabilities

- `arena-showcase-flow`: Zwischen den Wellen gibt es eine Pause ohne Zeitlimit; die nächste Welle löst der Spieler in der Arenamitte aus.
- `soul-release`: Die Regel, dass verschlungene Seelen nichts einbringen, gilt nur noch für Resonance. Soul Release schreibt keine Währung gut.

## Impact

Betroffen sind `GameWorld` (Run-Start, Kill-Gutschrift in `ApplyEnemyDamage`, Kisten, Niederlage, Abschluss), `Enemy` und Unterklassen (Glutmenge je Typ), `GameBalance` (Arbeitswerte), `HudRenderer` und die Hub-Darstellung, ein neues Kontenmodell und ein neuer Profilspeicher nach dem Muster von `GameSettingsStore`. Neue Speicherdatei `profile.json` neben `settings.json`. `docs/current/GAME-RULES.md` und `DECISION-LOG.md` werden auf den Beschluss aus #52 nachgezogen. Kein Koop, keine Reisepunkte, keine Ausgabestellen; Resonance bleibt unverändert.
