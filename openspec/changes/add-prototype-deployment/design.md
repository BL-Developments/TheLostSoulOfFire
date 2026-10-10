## Context

Das Projekt verwendet .NET 9 und MonoGame DesktopGL. Die bestehende CI baut und testet auf Windows und Linux, erzeugt aber kein Distributionspaket. Feature-Entwicklung läuft parallel; das Deployment soll keine Gameplay-Änderungen benötigen.

## Goals / Non-Goals

**Goals:** Versionierte Windows-x64-ZIPs ohne separat zu installierende .NET-Laufzeit; manuell ausgelöste Paketierung; nachvollziehbare Freigabe eines geprüften Pakets für einen geschlossenen Testerkreis.

**Non-Goals:** Browser, Backend, Installer, Auto-Updater, Code-Signing, weitere Betriebssysteme und automatische Veröffentlichung jedes Commits.

## Decisions

1. Ein PowerShell-Skript kapselt `dotnet publish` mit Release, `win-x64` und `--self-contained true`. Der vollständige Publish-Ordner einschließlich Content und nativer Bibliotheken wird gepackt. Single-file, Trimming und AOT bleiben zunächst deaktiviert, um zusätzliche Laufzeitvarianten zu vermeiden. Derselbe Einstieg dient lokal und in CI zur wiederholbaren Paketierung; Byte-Identität wird nicht versprochen.
2. Ein eigener Workflow erzeugt über `workflow_dispatch` mit validierter Versionsangabe ein ZIP, SHA-256-Prüfsumme und Metadaten mit Quell-Commit, Version und Build-Zeit. Eingaben werden als Umgebungsvariablen übergeben und vor Nutzung in Pfaden validiert. Tests und Publish müssen erfolgreich sein; fehlende EXE, Laufzeit oder Inhalte verhindern ein erfolgreiches Paket. Ein Tag-Trigger ist eine spätere Erweiterung.
3. Ein getrennter manuell gestarteter Upload-Workflow übernimmt ein vorhandenes Artefakt anhand der Build-Run-ID, prüft seine Prüfsumme und lädt dessen entpackten Inhalt mit butler in den Kanal `windows-prototype`. Er baut das Spiel nicht neu. Alternativ bleibt der dokumentierte manuelle ZIP-Upload möglich. GitHub Releases sind optional für längere Aufbewahrung; CI-Artefakte allein sind wegen ihrer Ablaufzeit kein dauerhafter Download-Ort.
4. Die itch.io-Seite wird vor dem ersten Upload auf Restricted mit Passwort oder expliziten Testzugängen eingerichtet. Zielprojekt und butler-Version werden konfiguriert, der API-Schlüssel liegt ausschließlich als Secret vor. Der Paket-Workflow benötigt keine Upload-Zugangsdaten. Vor externem Upload bestätigt der Betreiber den Smoke-Test genau dieses Pakets.
5. Die Testeranleitung enthält normale EXE-Startschritte, Steuerung, bekannte Probleme und Feedback mit Version, Reproduktionsschritten und Systemangaben. Kein neuer Developer-Startbereich ist erforderlich.

## Risks / Trade-offs

- Fehlende Content-Dateien oder native Bibliotheken → Paketprüfung plus echter Start auf einem zweiten Windows-Rechner ohne SDK und separate .NET-Installation.
- Erfolgreicher CI-Build beweist keine funktionierende Grafik oder Audio → Start, Arena, Audio und Einstellungen manuell am entpackten Paket prüfen und Ergebnis dokumentieren.
- Unsigned EXE kann Windows-Warnungen auslösen → in Testerhinweisen sachlich erwähnen; Signierung später separat bewerten.
- Zugangsdaten fehlen → Paketierung bleibt nutzbar; Upload erst nach Konfiguration.
- Ablaufende Build-Artefakte → Aufbewahrungsdauer dokumentieren und veröffentlichte ZIPs samt Prüfsumme für Rückkehr zu älteren Versionen sichern.
- Bewegliche Paketversionen (`3.8.*`) → tatsächlich verwendete SDK-/Paketversionen im Buildnachweis erfassen; keine Paket-Upgrades oder Pinning in diesem Change; Byte-Reproduzierbarkeit ist nicht zugesagt.

## Migration Plan

1. Skript, Workflows und Dokumentation in eigenem PR liefern; bestehende CI beibehalten.
2. Kandidatenpaket erzeugen, auf zweitem Rechner prüfen und Ergebnis mit Version/Commit festhalten.
3. itch.io-Projekt konfigurieren und dasselbe Paket bewusst veröffentlichen.
4. Bei Fehlern den vorherigen gesicherten Build erneut hochladen; für den ersten Release Download vorübergehend deaktivieren. Kein Datenmigrationsbedarf.

## Open Questions

- itch.io-Konto/Projekt-Slug und Testzugänge: vor dem ersten Upload festzulegen; blockiert die Paketierung nicht.
- Verfügbarer zweiter Windows-Testrechner und Feedback-Kanal: vor der ersten Testerfreigabe festzulegen.

## References

- https://docs.monogame.net/articles/getting_started/packaging_games.html
- https://itch.io/docs/creators/access-control
- https://itch.io/docs/butler/pushing.html

## Verbindliche Implementierungsdetails

Der [Implementierungsleitfaden](implementation-guide.md) legt Dateipfade, Parameter, Versionierung, JSON-Schema, Prüfschritte und Workflow-Reihenfolge fest. Bei der Umsetzung zuerst diesen Leitfaden lesen und dann die referenzierten Abschnitte pro Task abarbeiten.

- Build-Kandidaten können von ausgewählten Branches erzeugt werden; externer Upload akzeptiert ausschließlich erfolgreiche manuelle Läufe von `prototype-build.yml` auf `main` aus diesem Repository.
- Die Abnahme bezieht sich auf eine konkrete ZIP-Prüfsumme, die als Upload-Eingabe nochmals verglichen wird.
- Lokale Release-Paketierung verlangt einen sauberen Git-Arbeitsbaum, um die Commit-Zuordnung eindeutig zu halten. Ungültige Versionen und vorhandene Zielordner werden vorher abgewiesen.
- GitHub CLI übernimmt den Download; keine zusätzliche Drittanbieter-Action. butler-Version und Archivhash werden als Konfiguration hinterlegt.
- Die Skriptumsetzung benötigt noch kein itch.io-Konto. Externe Einrichtung und Zweitrechner-Abnahme sind ausdrücklich als Betreiberaufgaben markiert und bleiben bis zur tatsächlichen Durchführung offen.
