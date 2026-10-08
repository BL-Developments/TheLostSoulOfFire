## Context

Die Arena läuft in `GameWorld` als Phase `Arena` mit dem Schleifenzustand `ArenaLoopState` (`Intro`, `Combat`, `Intermission`, `Transition`, `Complete`). `UpdateArenaLoop` startet Wellen, `UpdateReinforcements` lässt Schübe nachrücken, `GameWorld.Currency.cs` vergibt Glut, Truhen und den Run-Verlust. Der Developer-Mode (`developer-start-mode`) ruft beim Start `GameWorld.ApplyDeveloperStart` auf.

## Goals / Non-Goals

**Goals:**

- Ein Bereich, der aussieht und sich spielt wie die Arena, in dem aber nichts von selbst passiert.
- Nur per `--dev --start sandbox` erreichbar.
- Grundlage für das Dev-Menü und die Gegner-Spawns der Folge-Changes aus #104.

**Non-Goals:**

- Dev-Menü, Werte setzen, Gegnerauswahl und Trainingspuppe (eigene Changes).
- Zugang aus normalen Menüs, Speichern von Sandbox-Zuständen.

## Decisions

**Phase `Arena` mit Sandbox-Kennzeichen statt neuer Phase.** Zeichnen, Kamera, Licht, Kampf und Gegner hängen an `GamePhase.Arena`. Eine neue Phase müsste all diese Stellen erweitern. Stattdessen läuft die Sandbox als Phase `Arena` im Schleifenzustand `Combat` mit dem Kennzeichen `_sandboxActive`. Das Kennzeichen schaltet genau die Teile ab, die nicht gelten sollen: `UpdateArenaLoop` (Wellen, Nachschub, Abschluss), Glut, Run-Verlust, Truhen, `E`-Wellenstart und das Währungs-HUD. `ClearRunState` setzt es zurück, damit jeder Weg aus der Sandbox (etwa `Zum Hauptmenü` im Pausenmenü) den normalen Ablauf wiederherstellt. Alternative: eigene `GamePhase.Sandbox`. Verworfen, weil sie viele Arena-Abfragen verdoppeln würde.

**Neustart bleibt in der Sandbox.** `RetryCurrentEncounter` (`R` nach der Niederlage und `F8`) ruft in der Sandbox `ResetSandbox` statt `ResetEncounter` auf: Feld räumen, Spieler mit vollem Leben in die Mitte, kein Intro. Charakterwerte bleiben erhalten.

**Keine Währungen.** Glut und Run-Bestände würden in der Sandbox nur verwirren, und eine Niederlage soll nichts kosten. Gesicherte Bestände bleiben unberührt; die Charakterseite zeigt nur `GESICHERT`.

## Risks / Trade-offs

- [Neue Arena-Logik vergisst das Sandbox-Kennzeichen] → Die Arena-Teile, die das Kennzeichen prüfen, liegen gesammelt in `UpdateArenaLoop` und `GameWorld.Currency.cs`; neue Wellen- oder Belohnungslogik sollte dort andocken.
