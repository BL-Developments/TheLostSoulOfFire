## 1. Paketprüfer und Paketierung

Vor Beginn `implementation-guide.md` vollständig lesen. Referenzen A–H beziehen sich auf dessen Abschnitte. Ein Task gilt erst nach seinem angegebenen Nachweis als erledigt; nicht ausgeführte Betriebsaufgaben offen lassen.

- [x] 1.1 `tools/deployment/Test-PrototypePackage.ps1` gemäß C anlegen. Nachweis: Pflichtdateien, Registry-/Metadaten-JSON, Runtimeconfig und Windows-x64-Native-Pfade werden geprüft; kein Spielstart.
- [x] 1.2 `tools/deployment/Package-Prototype.ps1` gemäß B anlegen: Version validieren, vorhandenes Ziel ablehnen, Clean-Tree/Commit ermitteln, Publish mit allen festgelegten Flags ausführen. Nachweis: native Fehlercodes werden übernommen und keine früheren Builds überschrieben.
- [x] 1.3 Paketierung vervollständigen: Metadaten, SDK-/Paketnachweis, Testerdatei, Prüferaufruf, ZIP ohne Zusatzordner und SHA-256-Datei. Nachweis: Ausgabe entspricht exakt B; Erfolg erst nach vollständig erzeugtem Paket.
- [x] 1.4 `docs/deployment/TESTER-README.md` schreiben und in Paketierung einbinden. Nachweis: Start, aktuelle Steuerung, bekannte Probleme, Version aus build-info.json und Feedback an Testlink-Absender vorhanden; keine erfundenen Kontaktdaten.
- [x] 1.5 `tools/deployment/Test-DeploymentScripts.ps1` für die Paketfälle aus F erstellen und ausführen. Nachweis: fehlende Runtime/Shader, kaputtes JSON, falsche Architektur, ungültige Version und vorhandener Zielordner werden korrekt abgewiesen.

## 2. Kandidaten-Workflow

- [x] 2.1 `.github/workflows/prototype-build.yml` genau nach D anlegen. Nachweis: manueller Versionseingang, Windows/pwsh, vorhandene Action-Majors, .NET 9, Skripttests und dotnet test vor Paketierung, Artefakt nur bei Erfolg.
- [x] 2.2 `docs/deployment/README.md` um lokale Befehle, Clean-Tree-Voraussetzung, Ausgaben, 30-Tage-Aufbewahrung und Default-Branch-Voraussetzung für workflow_dispatch ergänzen; im Projekt-README verlinken.
- [x] 2.3 Lokale Skripttests, bestehende Tests und reale Windows-Paketierung an einem sauberen Commit ausführen. ZIP neu entpacken und Paketprüfer darauf anwenden. Nachweis: Befehle/Exitcodes, Version/Commit und tatsächliche Prüfergebnisse dokumentiert; dies ersetzt nicht die Zweitrechner-Abnahme.

Lokaler Nachweis am 2026-10-10 (Windows x64, PowerShell 7.6.5): `Test-DeploymentScripts.ps1` bestand mit 27 Assertions und Exitcode 0 auch unter dem GitHub-pwsh-Exitcode-Wrapper. `dotnet test tests/TheLostSoulOfFire.Tests/TheLostSoulOfFire.Tests.csproj --configuration Release` bestand mit 312/312 Tests und Exitcode 0. `Package-Prototype.ps1 -Version 0.1.0-prototype.1` lief am sauberen Commit `19dc99449a77d6983aa3077f8ec70d992f000453` erfolgreich (Exitcode 0); das ZIP `TheLostSoulOfFire-0.1.0-prototype.1-win-x64.zip` hat SHA-256 `390cd6408dcce4f910b66aea3ff3841f56a303cc03649c26578d7fde83c6234f`. Das ZIP wurde separat entpackt; `Test-PrototypePackage.ps1` bestand am entpackten Verzeichnis, `TheLostSoulOfFire.exe` lag direkt an der Wurzel, und interne/externe Metadaten sowie `.sha256` stimmten überein. Ein zuvor absichtlich bestehender Versionsordner wurde vom Paketierungsskript abgewiesen; ein fehlgeschlagener `dotnet publish` gab Exitcode 1 weiter. Kein Spielstart oder Zweitrechner-Test ist durch diesen Nachweis abgedeckt.

## 3. Upload-Implementierung

- [x] 3.1 `tools/deployment/Upload-Prototype.ps1` gemäß E implementieren. Nachweis: ZIP-/Hash-/Commit-/Metadatenprüfung und sichere Extraktion vor butler; Paket wird weder gebaut noch gestartet.
- [x] 3.2 Upload-Fälle aus F mit lokalem butler-Stub zu Test-DeploymentScripts ergänzen. Nachweis: Erfolg ruft exakt den richtigen Kanal auf; Manipulation, Mehrdeutigkeit, Traversal und butler-Fehler führen zu Fehlerstatus; keine echten Secrets oder Uploads verwenden.
- [x] 3.3 `.github/workflows/prototype-upload.yml` gemäß E anlegen, einschließlich main-Beschränkung, Run-Herkunft, Hash-/Testbestätigung, Serialisierung, gh-Download und geprüftem butler-Download. Nachweis: keine Quelle außerhalb eines erfolgreichen main-Builds akzeptiert; keine Secrets im Build-Workflow.
- [x] 3.4 Run-Herkunftsprüfung mit JSON-Fixtures für erlaubten Run, falschen Branch, falschen Workflow und fehlgeschlagenen Run prüfen. Nachweis: dieselbe Prüflogik wird von Test und Workflow verwendet, ohne Netzwerkzugriff in den Tests.
- [x] 3.5 Betreiberanleitung nach E/G vervollständigen: alle Inputs, drei Variablen, Secret, Restricted-Seite, manueller ZIP-Upload, abgelaufene Artefakte, Sicherung und Rollback. Nachweis: konkrete Befehle und kein stiller LATEST-/Neubau-Fallback.
- [x] 3.6 `docs/deployment/SMOKE-TEST.md` gemäß G anlegen. Nachweis: alle Identitäts-/System-/Prüffelder vorhanden und noch ohne behauptete Testergebnisse.

## 4. Betreiberaufgaben und echte Abnahme

Diese Aufgaben benötigen reale Zugänge oder einen zweiten Rechner. Bei fehlender Voraussetzung Grund dokumentieren und Checkbox offen lassen; Implementierung aus 1–3 trotzdem abschließen. Ein teilweiser PR erhält keine OpenSpec-Archive-Zeile.

- [ ] 4.1 Nach Verfügbarkeit des Build-Workflows im Default-Branch einen Kandidaten auf main auslösen und Ergebnis herunterladen. Nachweis: erfolgreicher Run mit ID, Commit, Version und ZIP-Prüfsumme; keine erfundenen CI-Ergebnisse.
- [ ] 4.2 Mit Betreiber Restricted-Projekt, Testzugänge, `ITCH_PROJECT`, `BUTLER_VERSION`, `BUTLER_SHA256` und `BUTLER_API_KEY` einrichten. Nachweis: Werte tatsächlich konfiguriert; Secret nicht in Dokumentation oder Logs kopieren.
- [ ] 4.3 Genau das Kandidaten-ZIP auf zweitem Windows-Rechner ohne SDK/separate .NET-Installation testen und Abnahmeformular ausfüllen. Nachweis: normaler Start, Arena, Grafik/Shader, Audio, Einstellungen; Fehler verhindern Freigabe.
- [ ] 4.4 Upload mit derselben Run-ID und abgenommenem ZIP-Hash auslösen, Download über Testerzugang prüfen und ZIP/Hash vor Artefaktablauf sichern. Nachweis: reale Upload-/Download-Ergebnisse und Wiederherstellungsdateien vorhanden.
- [ ] 4.5 Abschließendes Anspielen dokumentieren: `dotnet run --project src/TheLostSoulOfFire -- --dev --start arena --wave 1` und im entpackten Paket `./TheLostSoulOfFire.exe --dev --start arena --wave 1`. Nachweis: Arena startet; dieser zusätzliche Check ersetzt weder normalen Start noch Zweitrechner-Test aus 4.3.
