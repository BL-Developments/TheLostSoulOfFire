## Context

`RunAbilities` verwaltet zwei Slots, Abklingzeiten und Effekte. `TryCast` prüft Zustand, Abklingzeit, volles Leben und blockierten Rückstoßsprung und bucht danach die Kosten über `CurrencyWallet.TrySpendRun(Currency.Glut, …)`. `GameWorld.CanChooseAbilities` erlaubt die Auswahl nur in der Vorkammer sowie in Arena-Intro und Wellenpause. Die Sandbox läuft als `GamePhase.Arena` mit dem Zustand `Combat` und dem Flag `_sandboxActive`; sie schreibt keine Glut gut und zeigt kein Währungs-HUD. `UpdateAbilities` simuliert Effekte schon im Zustand `Combat`, also auch in der Sandbox. `ZWEITER ATEM` und `Player.Heal` begrenzen auf `GameBalance.PlayerMaxHealth`, obwohl das Dev-Menü das Maximalleben in der Sandbox ändern kann.

## Goals / Non-Goals

**Goals:**

- Alle sechs Fähigkeiten in der Sandbox auswählen und wirken, mit echten Abklingzeiten.
- Arena und Vorkammer bleiben unverändert.

**Non-Goals:**

- Abklingzeiten abschalten, Fähigkeiten ins Dev-Menü verlegen, neue Fähigkeiten oder Balancing.
- Glut in der Sandbox einführen.

## Decisions

**Auswahl jederzeit in der Sandbox.** `CanChooseAbilities` erlaubt in der Sandbox die Auswahl im Zustand `Combat`, sonst gelten die bisherigen Bedingungen. Die Auswahl friert die Welt schon heute über `IsGamePaused` ein, so dass mitten im Kampf nichts weiterläuft. Das Dev-Menü wird vor der Fähigkeitsauswahl behandelt; beide schließen sich gegenseitig aus, `F` öffnet nichts, solange die Auswahl offen ist.

**Kosten als Parameter statt Sandbox-Wallet.** `TryCast` bekommt einen Parameter `chargeCost` (Standard `true`); `GameWorld` übergibt in der Sandbox `false`. Alternative wäre, der Sandbox endlos Glut ins Wallet zu legen; das würde aber Glut-Anzeige und Gutschriften wieder ins Spiel bringen, die `add-sandbox-start` bewusst ausschließt. Die Prüfreihenfolge bleibt gleich, nur die Buchung entfällt.

**Heilgrenze am Spieler.** `Player.Heal` und die Vollleben-Prüfung von `ZWEITER ATEM` lesen `player.MaxHealth`. Außerhalb der Sandbox ist das 100, das Verhalten dort bleibt gleich.

**Neustart.** `ResetSandbox` ruft `_abilities.Clear(_player)`, wie es Arena-Neustart und Niederlage tun; die Slots bleiben erhalten.
