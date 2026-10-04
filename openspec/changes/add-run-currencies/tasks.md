## 1. Produktregeln nachziehen

- [x] 1.1 Beschluss aus #52 und die Platzhalterregeln dieses Changes (Arena-Abschluss sichert vollständig, Kisten nach jeder Welle außer der letzten als erste Geldquelle, Arbeitsnamen Geld und Glut) datiert in `docs/current/DECISION-LOG.md` eintragen und die Zeilen „Ressourcen“ und „Niederlage“ in `docs/current/GAME-RULES.md` anpassen.

## 2. Kontenmodell und Profil

- [x] 2.1 `Currency` und `CurrencyWallet` (Run/Gesichert, `BeginRun`, `Credit`, `TrySpendRun`, `LoseRun`, `SecureAllRun`) ohne MonoGame-Abhängigkeit anlegen; Unit-Tests für getrennte Konten, abgelehnte und erfolgreiche Abbuchung, Null- und Negativbeträge, Niederlage und Sicherung.
- [x] 2.2 `PlayerProfile` (Version 1) und `PlayerProfileStore` mit injizierbarem Pfad, atomarem Schreiben und Beiseitelegen ungültiger Dateien anlegen; Unit-Tests für fehlende, beschädigte, falsch versionierte und negative Profile sowie Speichern und Laden.
- [x] 2.3 Arbeitswerte in `GameBalance` ergänzen (Basisvorrat Glut, Glut je Gegnertyp, Betrag je Kiste) und als Arbeitswerte kommentieren.

## 3. Verdienen, Verlieren, Sichern

- [x] 3.1 `Enemy.GlutReward` je Typ und `TryClaimReward` ergänzen; in `GameWorld.ApplyEnemyDamage` gutschreiben, nur in der Arena; Unit-Test, dass Mehrfachtreffer und Detonation keine zweite Gutschrift erzeugen.
- [x] 3.2 Run-Grenzen anbinden: `BeginRun` in `EnterArena`, `ResetEncounter` und Developer-Arena-Start, `LoseRun` beim Spielertod, `SecureAllRun` plus Profilspeichern beim Wechsel nach `Complete`; Profil in `Game1`/`GameWorld` beim Start laden.
- [x] 3.3 Kisten nach den Wellen 1 bis 3 mit Interaktionszone, `E`-Aufforderung, einmaliger Geldgutschrift und Entfernen nach dem Öffnen; ungeöffnete verfallen beim Run-Ende.
- [x] 3.4 Automatisierte Testläufe (`--audio-*-test`, `--antechamber-visual-test`, neu `--currency-visual-test`) auf ein Profil im Temp-Verzeichnis umstellen, das beim Beenden gelöscht wird.
- [x] 3.5 Pause `Intermission` nach den Wellen 1 bis 3 mit Auslösezone in der Arenamitte; `E` startet die nächste Welle, Kisten haben Vorrang; automatisierte Läufe lösen die Welle selbst aus.

## 4. Darstellung

- [x] 4.1 Glutfunke vom Gegner zum Spieler mit eigener Glutfarbe, unabhängig von der Gutschrift.
- [x] 4.2 `HudRenderer`: Zeilen `GELD <n>` und `GLUT <n>` mit kurzem Puls bei Gutschrift; Abschlusszustand nennt die gesicherten Beträge.
- [x] 4.3 Hub-Anzeige `GESICHERT · GELD <n> · GLUT <n>`; native Screenshots von Kampf-HUD, Kisten, Abschluss und Hub über `--currency-visual-test` prüfen.

## 5. Gesamtabnahme

- [x] 5.1 `openspec validate add-run-currencies --strict` und `dotnet test` erfolgreich ausführen.
- [x] 5.2 Manuell anspielen mit `dotnet run --project src/TheLostSoulOfFire -- --dev --start arena` (Glut beim Besiegen, Kisten nach den Wellen 1 bis 3 und ihr Verschwinden, Pause und Wellenstart mit `E` in der Mitte, Tod leert den Run-Bestand, Abschluss sichert) und danach `dotnet run --project src/TheLostSoulOfFire -- --dev --start hub` (gesicherte Bestände nach Neustart sichtbar).
