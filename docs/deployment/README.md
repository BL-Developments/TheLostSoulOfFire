# Windows-Prototyp: Betrieb und Veröffentlichung

Der Kandidaten-Workflow erzeugt ein Windows-x64-ZIP als GitHub Actions Artefakt. Die Erstellung baut die ausgewählte Quellrevision, führt die bestehenden Tests aus und fügt die mitgelieferte .NET-Laufzeit, Content, Metadaten und eine SHA-256-Prüfsumme hinzu. Sie veröffentlicht nichts auf itch.io.

## Lokal einen Kandidaten erstellen

Voraussetzungen: Windows x64, PowerShell 7.5 oder neuer, .NET 9 SDK, Git und ein sauberer Git-Arbeitsbaum. Die Veröffentlichung lehnt nicht committete oder unversionierte Dateien und bereits vorhandene Versionsordner ab. Zuerst alle Quelländerungen im vorgesehenen Commit sichern.

```powershell
pwsh -NoProfile -File tools/deployment/Test-DeploymentScripts.ps1
dotnet test tests/TheLostSoulOfFire.Tests/TheLostSoulOfFire.Tests.csproj --configuration Release
pwsh -NoProfile -File tools/deployment/Package-Prototype.ps1 -Version 0.1.0-prototype.1
```

Die Ausgabe liegt unter artifacts/prototype/<version>/release. Das ZIP heißt TheLostSoulOfFire-<version>-win-x64.zip; die gleichnamige SHA256-Datei enthält den Hash. build-info.json protokolliert Version, vollen Quell-Commit, UTC-Bauzeit, RID und Konfiguration. dotnet-info.txt und packages.txt dokumentieren Buildumgebung und aufgelöste Abhängigkeiten. Das Verzeichnis staging enthält den Paketinhalt zur lokalen Kontrolle. Artefakte gehören nicht ins Git.

## GitHub Actions Kandidat

1. Workflowdatei .github/workflows/prototype-build.yml in den Default-Branch integrieren. GitHub bietet workflow_dispatch erst an, wenn der Workflow dort verfügbar ist.
2. Actions → Prototype build → Run workflow öffnen und eine freie Version im Format N.N.N oder N.N.N-suffix eintragen (zum Beispiel 0.1.0-prototype.1).
3. Nur einen erfolgreichen Lauf als Kandidat verwenden. Summary, Commit, Run-ID und ZIP-Prüfsumme mit build-info.json und .sha256 abgleichen.
4. Artifact prototype-win-x64 herunterladen und das ZIP entpacken. Vor der ersten Freigabe muss der Smoke-Test auf einem zweiten Windows-Rechner ohne SDK oder separate .NET-Laufzeit bestanden sein. Formular: SMOKE-TEST.md.
5. Das Artefakt läuft nach 30 Tagen ab. Ein freigegebenes ZIP samt SHA256-Datei vor Ablauf in einem privaten, versionierten Projektarchiv sichern. Den Quell-Commit in Git behalten.

Der Kandidaten-Workflow kann von einer gewählten Revision manuell gestartet werden. Der Upload-Workflow akzeptiert später nur erfolgreiche manuelle Kandidatenläufe von main im selben Repository.

## itch.io einmalig einrichten

1. Ein neues Projekt oder eine für Tester freigegebene itch.io-Seite anlegen. In Visibility & access Restricted wählen und ein Passwort oder ausdrückliche Testzugänge einrichten.
2. Repository Variables eintragen:
   - ITCH_PROJECT: owner/slug in Kleinbuchstaben, passend zur Seite.
   - BUTLER_VERSION: geprüfte stabile numerische Version von butler im Format x.y.z.
   - BUTLER_SHA256: SHA256 des dazugehörigen offiziellen Windows-amd64-butler-Archivs.
3. Repository Secret BUTLER_API_KEY aus den itch.io API keys hinzufügen. Den Schlüssel nicht als Variable, Workfloweingabe, Argument, Datei oder Logzeile ablegen.
4. Für butler wird das offizielle Archiv von https://broth.itch.zone/butler/windows-amd64/<version>/archive/default bezogen und mit BUTLER_SHA256 verglichen. Version und Hash müssen aus dem tatsächlichen Archiv stammen; kein LATEST-Ersatz.
5. Im Browser Restricted-Zugriff als nicht eingeloggter/uneingeladener Nutzer testen und Testzugang verifizieren. Der Workflow fragt bei jedem Upload ausdrücklich die Bestätigung der Zugriffsbeschränkung ab; er kann diese Seite nicht selbst auslesen.

## Freigegebenen Kandidaten hochladen

1. Im Smoke-Testformular Version, Run-ID, Commit und ZIP-SHA256 des bestandenen Kandidaten notieren.
2. Actions → Prototype upload → Run workflow auswählen. build_run_id und den geprüften zip_sha256 eingeben; smoke_test_passed und restricted_access_confirmed beide aktivieren.
3. Uploads funktionieren nur nach Integration des Upload-Workflows in main. Er muss den ausgewählten, erfolgreichen Prototype-build-Lauf verifizieren, genau dessen Artefakt laden und SHA256 sowie Metadaten vor butler push abgleichen.
4. Nach erfolgreichem Workflow mit einem Testzugang den Download auf itch.io abrufen und prüfen. Das ist die tatsächliche Freigabe; ein erfolgreicher CI-Upload allein belegt keinen spielbaren Build.

Wenn Zugangsdaten oder ein ITCH_PROJECT noch fehlen, lässt sich der Kandidaten-Workflow weiterhin nutzen. Lade nach bestandenem Smoke-Test das gesicherte ZIP manuell auf der itch.io-Seite hoch; dokumentiere dabei denselben Hash und Commit. Kein lokales Paket neu bauen, wenn du genau den abgenommenen Kandidaten freigeben willst.

## Fehler, Aufbewahrung und Rücknahme

- Fehlgeschlagener Build: kein Kandidatenartefakt freigeben. Fehler in Workflow-Logs untersuchen, beheben und mit neuer Versionsnummer starten.
- ZIP-Prüfsumme oder Quell-Commit stimmen nicht: Upload abbrechen. Den Kandidaten frisch aus dem angegebenen Build-Run herunterladen; keine andere ZIP einsetzen.
- Actions-Artefakt abgelaufen: nur aus privatem Projektarchiv wiederherstellen. Der Upload-Workflow wechselt nicht automatisch auf den neuesten Lauf.
- Fehlerhafter itch.io-Build: vorher gesicherten, bereits geprüften Kandidaten erneut in den Kanal windows-prototype hochladen. Bei der ersten Veröffentlichung ohne früheren Kandidaten den Download vorübergehend deaktivieren/entfernen und den Eintrag aktualisieren.
- Bei butler-Fehlern Workflow-Fehler beheben und erneut ausdrücklich starten. Das Paket wird dabei nicht neu gebaut und ein fehlgeschlagener Upload erhält keinen automatischen Retry.

## Hinweise für Tester

Das Spiel startet nach dem Entpacken über TheLostSoulOfFire.exe. Testerhinweise und Steuerung liegen auch im ZIP unter TESTER-README.md. Sammle Rückmeldungen mit Version, Windows-Version und Reproduktionsschritten.
