## 1. Trainingspuppe

- [x] 1.1 Werte der Puppe in `GameBalance` (Leben 1000, Radius, Auffüllpause 2,5 s, Anzeigedauer 0,9 s) statt der ungenutzten Konstante `DummyMaxHealth`.
- [x] 1.2 `TrainingDummy`: fester Ankerpunkt, kein Angriff, Schaden bis höchstens 1 Leben ohne Rückstoß, Summe und letzter Treffer, aufsteigende Zahlen in drei Spuren, Auffüllen nach der Pause, keine Glut.
- [x] 1.3 Darstellung mit Pfahl, Strohkörper, Schatten, Lebensbalken, `SUMME <n>` und Zahlen (Kerntreffer hervorgehoben).
- [x] 1.4 Unit-Tests: Schaden und Summe, nie besiegt und keine Glut, Auffüllen erst nach trefferfreier Pause, Rückstoß verschiebt nicht.

## 2. Sandbox

- [x] 2.1 `SandboxEnemyKind.TrainingDummy` mit Bezeichnung `TRAININGSPUPPE`, Radius, Typprüfung und Erzeugung; der Menüeintrag entsteht aus der Typliste.

## 3. Abnahme

- [x] 3.1 README um die Trainingspuppe ergänzen.
- [x] 3.2 `openspec validate add-training-dummy --strict` und `dotnet test` erfolgreich ausführen.
- [x] 3.3 Manuell anspielen mit `dotnet run --project src/TheLostSoulOfFire -- --dev --start sandbox --strength 40` (`F`, `TRAININGSPUPPE`, Menü schließen, Kombo auf die Puppe: Zahlen 50, 62, 100 und `SUMME 212`, nach 2,5 s voller Balken).
