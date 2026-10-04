## 1. Produktregeln nachziehen

- [ ] 1.1 Beschluss aus #52 und die Platzhalterregeln dieses Changes (Arena-Abschluss sichert vollständig, Kisten nach jeder Welle außer der letzten als erste Geldquelle, Arbeitsnamen Geld und Glut) datiert in `docs/current/DECISION-LOG.md` eintragen und die Zeilen „Ressourcen“ und „Niederlage“ in `docs/current/GAME-RULES.md` anpassen.

## 2. Kontenmodell und Profil

- [ ] 2.1 `Currency` und `CurrencyWallet` (Run/Gesichert, `BeginRun`, `Credit`, `TrySpendRun`, `LoseRun`, `SecureAllRun`) ohne MonoGame-Abhängigkeit anlegen; Unit-Tests für getrennte Konten, abgelehnte und erfolgreiche Abbuchung, Null- und Negativbeträge, Niederlage und Sicherung.
- [ ] 2.2 `PlayerProfile` (Version 1) und `PlayerProfileStore` mit injizierbarem Pfad, atomarem Schreiben und Beiseitelegen ungültiger Dateien anlegen; Unit-Tests für fehlende, beschädigte, falsch versionierte und negative Profile sowie Speichern und Laden.
- [ ] 2.3 Arbeitswerte in `GameBalance` ergänzen (Basisvorrat Glut, Glut je Gegnertyp, Betrag je Kiste) und als Arbeitswerte kommentieren.

## 3. Verdienen, Verlieren, Sichern

- [ ] 3.1 `Enemy.GlutReward` je Typ und `TryClaimReward` ergänzen; in `GameWorld.ApplyEnemyDamage` gutschreiben, nur in der Arena; Unit-Test, dass Mehrfachtreffer und Detonation keine zweite Gutschrift erzeugen.
- [ ] 3.2 Run-Grenzen anbinden: `BeginRun` in `EnterArena`, `ResetEncounter` und Developer-Arena-Start, `LoseRun` beim Spielertod, `SecureAllRun` plus Profilspeichern beim Wechsel nach `Complete`; Profil in `Game1`/`GameWorld` beim Start laden.
- [ ] 3.3 Kisten nach den Wellen 1 bis 3 mit Interaktionszone, `E`-Aufforderung, einmaliger Geldgutschrift und Entfernen nach dem Öffnen; ungeöffnete verfallen beim Run-Ende.
- [ ] 3.4 Automatisierte Testläufe (`--audio-*-test`, `--antechamber-visual-test`) auf ein Profil im Temp-Verzeichnis umstellen.

## 4. Darstellung

- [ ] 4.1 Glutfunke vom Gegner zum Spieler mit eigener Glutfarbe, unabhängig von der Gutschrift.
- [ ] 4.2 `HudRenderer`: Zeilen `GELD <n>` und `GLUT <n>` mit kurzem Puls bei Gutschrift; Abschlusszustand nennt die gesicherten Beträge.
- [ ] 4.3 Hub-Anzeige `GESICHERT · GELD <n> · GLUT <n>`; native Screenshots von Kampf-HUD, Kisten, Abschluss und Hub prüfen.

## 5. Gesamtabnahme

- [ ] 5.1 `openspec validate add-run-currencies --strict` und `dotnet test` erfolgreich ausführen.
- [ ] 5.2 Manuell anspielen mit `dotnet run --project src/TheLostSoulOfFire -- --dev --start arena` (Glut beim Besiegen, Kisten nach den Wellen 1 bis 3 und ihr Verschwinden, Tod leert den Run-Bestand, Abschluss sichert) und danach `dotnet run --project src/TheLostSoulOfFire -- --dev --start hub` (gesicherte Bestände nach Neustart sichtbar).
