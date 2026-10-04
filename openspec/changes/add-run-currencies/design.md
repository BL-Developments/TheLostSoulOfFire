## Context

`main` hat einen Hub (Aschenvorhalle, Tür I offen) und eine Arena mit vier Wellen. Tod bietet mit `R` einen Neuversuch derselben Arena, der Abschluss führt mit `R` zurück zur Titelkarte. Es gibt keine Levels, keinen Bosssieg und keinen Rückweg zur Homebase, also keinen der Sicherungsorte aus #52. Persistiert wird bisher nur `settings.json` über `GameSettingsStore` (JSON, Schreiben über Temp-Datei).

Die Regeln stammen aus #52: Geld aus Kisten und NPC-Prämien, Glut beim Besiegen genau einmal, Erlösung ohne zweite Gutschrift, Run- und gesicherte Bestände, Verlust beider Run-Bestände bei Niederlage, kostenloser Basisvorrat, keine Mitnahme gesicherter Glut. Björn hat am 04.10. bestätigt: Arena-Abschluss sichert vorläufig, Kisten sind die erste Geldquelle (nach jeder Welle außer der letzten, sie verschwinden nach dem Öffnen), Arbeitsnamen sind Geld und Glut.

## Goals / Non-Goals

**Goals:**

- Ein spielbarer Kreislauf Verdienen → Verlieren oder Sichern → Anzeigen im Hub, der über Neustarts erhalten bleibt.
- Ein Kontenmodell ohne MonoGame-Abhängigkeit, das spätere Ausgaben (#80, #82, #83, #87, #88) und Reisepunkte (#74) ohne Umbau tragen kann.
- Deterministische Unit-Tests für Gutschrift, Doppelereignisse, Ausgeben, Niederlage, Sichern, Laden.

**Non-Goals:**

- Teilsicherung, Quote, Extraktion und Reisepunkte (#53, #18, #74).
- Ausgabestellen: Fähigkeiten, Händler, Schmied, Skilltrees.
- NPC-Killprämien für Geld (#78, #85), Koop-Besitz (#58), Ultimate (#13), Meta-Level (#89).
- Gutschrift im Prolog. Der Prolog ist kein Run; dort gibt es weder Glut noch Währungs-HUD.

## Decisions

**Kontenmodell `CurrencyWallet` als reine Klasse.** Zwei Währungen (`Currency.Geld`, `Currency.Glut`) mit je `Run` und `Secured` als `int`. Operationen: `BeginRun(starterGlut)` setzt beide Run-Bestände (Geld 0, Glut = Basisvorrat), `Credit(currency, amount)`, `TrySpendRun(currency, amount)` (false ohne Änderung bei zu wenig Mitteln oder `amount <= 0`), `LoseRun()` und `SecureAllRun()`. Ganzzahlen statt `float`, damit es keine Rundungsfragen gibt, bevor #53 eine Quote festlegt. Alternative: Werte direkt in `Player` führen wie Resonance. Verworfen, weil Konten Run und Profil überdauern und nicht an die Spielfigur gehören.

**Einmalige Gutschrift über ein Flag am Gegner.** `Enemy` erhält `GlutReward` (virtuell je Typ, Werte aus `GameBalance`) und ein privates `_rewardCredited`. `GameWorld.ApplyEnemyDamage` erkennt bereits den Übergang lebendig → besiegt; dort ruft es `enemy.TryClaimReward(out int glut)` auf, das genau einmal `true` liefert. Damit kann weder ein zweiter Treffer im selben Frame noch eine Detonation eine zweite Gutschrift auslösen. Alternative: Gutschrift beim Entfernen aus `_enemies`. Verworfen, weil die Todesdarstellung dazwischenliegt und der Beschluss „beim Besiegen“ verlangt.

**Glutfunke nur als Darstellung.** Ein kurzer Partikelflug (Vorlage: Soul-Residue-Flug, `GameBalance.SoulResidueTravelTime`) vom Gegner zum Spieler. Die Gutschrift ist zu diesem Zeitpunkt schon gebucht. Der Funke nutzt eine eigene glühende Farbe, damit er sich vom weißen Soul Release unterscheidet (#73: Erlösung visuell getrennt von der Gutschrift).

**Run-Grenzen.** Run-Start: `EnterArena` und `ResetEncounter` rufen `BeginRun`. Niederlage: Wechsel des Spielers in den Todeszustand ruft `LoseRun`. Abschluss: Übergang nach `ArenaLoopState.Complete` ruft `SecureAllRun` und speichert das Profil. Der Developer-Start in die Arena läuft über denselben Run-Start.

**Kisten.** Nach dem Leeren der Wellen 1 bis 3 erscheint je eine Kiste an einer für jede Welle eigenen festen Position, damit ungeöffnete Kisten nicht übereinanderliegen; nach Welle 4 folgt direkt der Abschluss. Interaktion mit `E` in ihrer Zone öffnet sie einmalig, schreibt `GameBalance.ChestGeld` gut und entfernt sie nach einer kurzen Öffnungsdarstellung. Ungeöffnete Kisten bleiben bis zum Ende des Runs stehen und verfallen beim Abschluss oder Neuversuch. Alternative: automatisches Aufsammeln beim Drüberlaufen. Verworfen, weil `E` schon die Interaktion im Hub ist und Kisten später als Erkundungsziel taugen sollen.

**Profil `PlayerProfile` und `PlayerProfileStore`.** JSON mit `Version = 1`, `SecuredGeld`, `SecuredGlut` unter `%LocalAppData%/TheLostSoulOfFire/profile.json`. Schreiben über Temp-Datei und Ersetzen wie bei den Einstellungen. Fehlende Datei, unlesbares JSON, unbekannte Version oder negative Werte ergeben ein leeres Profil; eine ungültige Datei wird vor dem Überschreiben als `profile.json.invalid` beiseitegelegt, damit nichts stillschweigend verloren geht. Das Profil wird nur beim Sichern geschrieben, nicht bei jeder Gutschrift. Der Pfad ist injizierbar, damit Tests eigene Dateien nutzen.

**HUD.** Im Run zeigt `HudRenderer` unter der Gesundheit zwei Zeilen `GELD <n>` und `GLUT <n>` (Run-Bestände). Im Hub zeigt eine zurückhaltende Anzeige `GESICHERT · GELD <n> · GLUT <n>`. Der Abschlusszustand der Arena nennt die eben gesicherten Beträge. Bei einer Gutschrift pulsiert die betroffene Zeile kurz.

**Arbeitswerte in `GameBalance`.** Basisvorrat Glut 10; Glut je Hollow 3, Burning 5, Devourer 12; 25 Geld je Kiste. Reine Balancewerte, im Code als Arbeitswerte kommentiert.

## Risks / Trade-offs

- [Vollständige Sicherung beim Abschluss macht Risiko kaum spürbar] → Ausdrücklich Platzhalter; #53 ersetzt die Regel, das Modell trennt `SecureAllRun` schon von einer späteren `SecurePartial`.
- [Neuversuch nach Tod setzt Glut auf den Basisvorrat zurück und verwirft Geld] → Entspricht #52; mit dem echten Niederlage-Rückweg (#68) wird der Neuversuch ohnehin ersetzt.
- [Entwicklertests schreiben in das echte Profil] → Pfad ist injizierbar; die automatisierten Läufe (`--audio-*-test`, `--antechamber-visual-test`, `--currency-visual-test`) verwenden ein Profil im Temp-Verzeichnis, das beim Beenden gelöscht wird. `--currency-visual-test` spielt einen Arena-Run mit zwei geöffneten und einer ungeöffneten Kiste durch, nimmt HUD, Kiste, Abschluss und Hub auf und prüft die gesicherten Bestände.
- [Spätere Koop-Entscheidung (#58) braucht Konten pro Spieler] → Das Profil ist einer Person zugeordnet; ein Team-Konto ließe sich als zweites `CurrencyWallet` ergänzen, ohne die Regeln zu ändern.

## Open Questions

- Sicherungsquote, Teilsicherung und Rundung (#53).
- Ob gesicherte Glut beim Abschluss künftig vollständig oder nur teilweise gutgeschrieben wird, hängt ebenfalls an #53.
- Endgültige Namen und Lore der Währungen bleiben offen; `Geld` und `Glut` sind Arbeitsnamen.
