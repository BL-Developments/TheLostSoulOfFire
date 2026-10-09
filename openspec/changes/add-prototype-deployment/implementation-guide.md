# Implementierungsleitfaden

Dieser Leitfaden konkretisiert `design.md` und `specs/prototype-distribution/spec.md`. Arbeite `tasks.md` in Reihenfolge ab. Alle Pfade sind relativ zur Repository-Wurzel. Dieser Change ist noch nicht implementiert; Befehle und Ausgabeformate hier sind Soll-Vorgaben, keine bereits geprüften Ergebnisse.

## A. Umfang und Einstieg

Lies zuerst `AGENTS.md`, Proposal, Design, Spec, diesen Leitfaden und Tasks. Prüfe `git status` vor Änderungen; laufende Feature-Arbeit nicht überschreiben. Keine Subagenten erforderlich.

Erste Lieferstufe: lokale Paketierung und Build-Workflow. Zweite Lieferstufe: Upload und Dokumentation. Dritte Lieferstufe: echter Test und Veröffentlichung mit Betreiber. Fehlende itch.io-Zugangsdaten blockieren nur die dritte Stufe. Nicht für Infrastrukturzugänge die gesamte Implementierung anhalten.

Dateiplan (neu, sofern zwischenzeitlich nicht vorhanden):

| Datei | Verantwortung |
|---|---|
| `tools/deployment/Package-Prototype.ps1` | Publish, Paket-Metadaten, ZIP, SHA-256 |
| `tools/deployment/Test-PrototypePackage.ps1` | Ein entpacktes Paket ohne Spielstart prüfen |
| `tools/deployment/Test-DeploymentScripts.ps1` | Selbstständige Regressionstests mit temporären Dateien, ohne zusätzliche Testbibliothek |
| `tools/deployment/Upload-Prototype.ps1` | Vorhandenes ZIP prüfen, entpacken, butler aufrufen; niemals bauen |
| `.github/workflows/prototype-build.yml` | Tests und Kandidatenpaket erstellen |
| `.github/workflows/prototype-upload.yml` | Ausgewählten erfolgreichen Build herunterladen und hochladen |
| `docs/deployment/README.md` | Betreiberablauf, Variablen/Secrets, Fehlerbehebung, Rücknahme |
| `docs/deployment/TESTER-README.md` | Vorlage für die Anleitung im ZIP |
| `docs/deployment/SMOKE-TEST.md` | Ausfüllbare Abnahmevorlage, keine erfundenen Ergebnisse |
| `README.md` | Kurzer Link auf Deployment-Anleitung |

`artifacts/` ist bereits ignoriert. Keine neue Ignore-Regel nötig. Keine Änderungen an Gameplay, DeveloperStartOptions, bestehender CI oder TargetFramework. Keine Paket-Upgrades und keine Umstellung auf andere MonoGame-Targets. Falls Publish die Registry nicht kopiert, ausschließlich am bestehenden `None Include="Content/Visuals/registry.json"` zusätzlich `CopyToPublishDirectory="PreserveNewest"` setzen. Weitere Abweichungen erst mit konkretem Buildfehler begründen.

## B. Paket-Schnittstelle

PowerShell 7.5 oder neuer verwenden (die Paket- und Upload-Skripte verwenden `ConvertFrom-Json -DateKind String`). Package-Prototype hat genau den Pflichtparameter `[string]$Version`; Repository-Wurzel aus `$PSScriptRoot/../..` ableiten, nicht aus dem aktuellen Arbeitsverzeichnis. Betrieb nur unter Windows. Am Skriptanfang `$ErrorActionPreference = 'Stop'` und StrictMode setzen. Bei jedem nativen Programm sofort `$LASTEXITCODE` prüfen und bei ungleich 0 `throw` verwenden; ErrorActionPreference allein erkennt nicht jeden externen Fehler.

Version: maximal 64 Zeichen, Muster `\A[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?\z`. Beispiele: `0.1.0-prototype.1` gültig; `v0.1.0`, `../bad`, Leerzeichen, Zeilenumbrüche ungültig. Vor Dateisystemänderungen validieren. Keine Version automatisch raten.

Zielstruktur für Version V:

```text
artifacts/prototype/V/
  staging/                          # kompletter Inhalt des Publish-Ordners + Paketdokumente
  release/
    TheLostSoulOfFire-V-win-x64.zip
    TheLostSoulOfFire-V-win-x64.zip.sha256
    build-info.json                 # identische Kopie des Dokuments im ZIP
    dotnet-info.txt
    packages.txt
```

Existiert `artifacts/prototype/V` bereits, verständlich abbrechen. Kein implizites Löschen oder Überschreiben früherer Builds, kein Force-Parameter. Bei Fehlversuch Reste nennen; neuer Versuch mit neuer Versionskennung oder bewusstem manuellen Aufräumen. Immer nur `release/*` als GitHub-Artefakt hochladen, niemals staging oder das Repository.

Vor Publish: `git rev-parse HEAD`, `git status --porcelain --untracked-files=normal`. Uncommittierte/untracked Quelldateien verhindern Release-Paketierung; ignorierte Build-Ausgaben zählen nicht. Dies dokumentieren, damit der Agent zuerst Implementierung fertigstellt und einen abgestimmten Commit verwendet. Die Planungsdateien sind derzeit noch uncommitted: daraus keinen falschen erfolgreichen Pakettest ableiten. Keine automatischen Commits im Paketskript.

Publish-Befehl (Argumente im Skript als Array übergeben):

```powershell
dotnet publish src/TheLostSoulOfFire/TheLostSoulOfFire.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:PublishAot=false -p:PublishReadyToRun=false -p:TieredCompilation=false -o <absoluter-staging-pfad>
```

Kein `--no-restore`: vorheriges Test-Restore hat noch nicht zwingend win-x64-Runtime-Pakete. Projektpfad absolut auflösen. Restore/Publish keine Ausgabeumleitung geben, die Fehler verschluckt. Vollständige Publish-Ausgabe verwenden, keine DLLs einzeln zusammensuchen. Version dient Paketkennzeichnung; Assembly-Version muss nicht geändert werden.

Danach `dotnet --info` und `dotnet list <projekt> package --include-transitive` erfolgreich erfassen, als Dateien in release speichern. Bestehende `3.8.*`-Referenzen nicht im Rahmen dieses Changes pinnen: aufgelöste Versionen dokumentieren; Byte-Reproduzierbarkeit ist nicht zugesagt.

`build-info.json` in staging und release, JSON über `ConvertTo-Json` erzeugen:

```json
{
  "schemaVersion": 1,
  "version": "0.1.0-prototype.1",
  "sourceCommit": "<vollstaendiger 40-stelliger Git-SHA>",
  "builtAtUtc": "<ISO-8601 UTC>",
  "runtimeIdentifier": "win-x64",
  "configuration": "Release",
  "selfContained": true
}
```

Tester-Vorlage als `TESTER-README.md` nach staging kopieren. Vorlage verweist für Version/Commit auf build-info.json; keine Platzhalter für erfundene Kontakte. Feedback zunächst an die Person, die den Testlink verteilt hat. Steuerung mit aktuellem README und Code abgleichen, insbesondere Escape öffnet im Spiel das Pausenmenü.

Paketprüfung auf staging ausführen. ZIP über .NET `System.IO.Compression.ZipFile.CreateFromDirectory` erstellen, mit `includeBaseDirectory=false`: EXE und Content stehen direkt im ZIP-Wurzelverzeichnis. Dadurch wird die zusätzliche Versionsordner-Ebene vermieden. Prüfsummendatei als genau eine Zeile `<64 lowercase hex><zwei Leerzeichen><ZIP-Dateiname>` schreiben, UTF-8 ohne BOM. Erfolg erst nach vollständigem ZIP und Prüfsumme melden.

## C. Paketprüfer

`Test-PrototypePackage.ps1 -PackageDirectory <pfad>`: exit 0 bei Erfolg, sonst throw/ungleich 0 mit Namen der fehlenden oder ungültigen Datei. Nicht das Spiel starten und keine Dateien verändern.

Pflichtdateien, jeweils nicht leer:

- `TheLostSoulOfFire.exe`, `.dll`, `.deps.json`, `.runtimeconfig.json` (jeweils gleicher Basename)
- `coreclr.dll`, `hostfxr.dll`, `hostpolicy.dll`, `System.Private.CoreLib.dll`
- `MonoGame.Framework.dll`, `MonoGame.Extended.dll`
- `SDL2.dll` und `openal.dll`: jeweils im Wurzelverzeichnis ODER unter `runtimes/win-x64/native/`; beliebige andere RID-Verzeichnisse erfüllen die Prüfung nicht
- `Content/Visuals/registry.json`, als JSON parsebar
- `Content/Audio/Sfx/scythe_swing_1.xnb`
- `Content/Textures/Weapons/scythe_physical_256.xnb`
- `Content/Effects/SceneGrade.xnb`, `Content/Effects/SpriteLit.xnb`, `Content/Effects/Dissolve.xnb`, `Content/Effects/DeathFlame.xnb`
- `Content/Fonts/ui.xnb`
- `build-info.json`, `TESTER-README.md`

Die Content-Pfade sind aus dem aktuellen Content.mgcb abgeleitet. Die Liste ist ein Frühwarnsystem; sie beweist nicht, dass alle Assets zur Laufzeit funktionieren. Vollständiges Kopieren + zweiter Rechner bleiben erforderlich. Metadaten gegen obiges Schema prüfen: Version, SHA, UTC-Zeitstempel, RID, Release und selfContained. Runtimeconfig auf parsebares JSON und Self-contained-Konfiguration prüfen (includedFrameworks statt externer framework/frameworks-Anforderung).

## D. Build-Workflow

Datei `prototype-build.yml`, Anzeigename `Prototype build`:

- Nur `workflow_dispatch`; required string input `version`, Vorschlagswert `0.1.0-prototype.1`.
- Ein Windows-Job `windows-latest`, `defaults.run.shell: pwsh`, Timeout 30 Minuten; `permissions: contents: read`.
- Checkout und setup-dotnet in denselben Major-Versionen wie vorhandene CI (`checkout@v7`, `setup-dotnet@v6`, SDK `9.0.x`). Keine neuen Drittanbieter-Actions nötig.
- Schritte in Reihenfolge: Checkout; SDK; `Test-DeploymentScripts.ps1`; `dotnet test tests/TheLostSoulOfFire.Tests/TheLostSoulOfFire.Tests.csproj -c Release`; Package-Prototype; upload-artifact.
- Dispatch-Version über `env: RELEASE_VERSION: ${{ inputs.version }}` setzen, dann `-Version $env:RELEASE_VERSION` übergeben. Keine GitHub-Ausdrücke direkt in PowerShell-Quelltext interpolieren.
- `actions/upload-artifact@v6`, fester Artefaktname `prototype-win-x64`, `path: artifacts/prototype/<validierte Version>/release/`, `if-no-files-found: error`, `retention-days: 30`. Nur im Erfolgsfall ausführen; kein `always()` für Release-Artefakte.
- Summary: Version, github.sha, Run-ID, Hinweis 'Kandidat; noch nicht veröffentlicht oder auf zweitem Rechner abgenommen'.
- workflow_dispatch wird in GitHub erst angeboten, wenn die Workflow-Datei im Default-Branch vorhanden ist. Im Betreiberleitfaden erwähnen; bis dahin lokale Skriptprüfung, kein erfundener Remote-Lauf.

## E. Upload-Skript und Workflow

`Upload-Prototype.ps1` hat Pflichtparameter `ArtifactDirectory`, `ExpectedSourceCommit`, `ExpectedZipSha256`, `ItchProject`, `ButlerPath`. Es baut nichts, lädt butler nicht selbst herunter und startet keine EXE aus dem Spielpaket.

Ablauf: genau ein ZIP im Artefakt verlangen; zugehörige .sha256 nach Format aus B lesen; Dateiname muss exakt passen, ohne Verzeichnisteile. Tatsächlichen ZIP-Hash sowohl mit Datei als auch mit ExpectedZipSha256 vergleichen. Letzterer stammt aus der Abnahme des Operators. JSON neben ZIP prüfen, sourceCommit muss ExpectedSourceCommit entsprechen. In neuen GUID-Unterordner unter artifacts/prototype-upload entpacken; vor Extraktion ZIP-Einträge ablehnen, deren normalisierter Zielpfad außerhalb liegt (absolute Pfade und `..`-Traversal). Paketprüfung C aufrufen; innere und äußere build-info.json müssen identisch sein. `ItchProject` auf `\A[a-z0-9][a-z0-9-]*/[a-z0-9][a-z0-9-]*\z` begrenzen. Fehlender BUTLER_API_KEY oder ButlerPath verhindert Upload. Den Schlüssel nie ausgeben.

Aufruf über Argumentarray: `& $ButlerPath push <entpackter-ordner> "${ItchProject}:windows-prototype" --userversion <metadata.version>`; Exitcode prüfen. Protokoll: Version, Commit, ZIP-Hash, Ziel, Erfolg/Fehler ohne Secret. Kein stiller Retry oder Neubau bei Fehlern.

`prototype-upload.yml`, Anzeigename `Prototype upload`:

- Nur workflow_dispatch, Ausführung auf `main` beschränken (Job-Bedingung); Windows/pwsh, 20 Minuten; `permissions: contents: read, actions: read`.
- Inputs: `build_run_id` (string mit ausschließlich Ziffern), `zip_sha256` (64 Hexzeichen), `smoke_test_passed` und `restricted_access_confirmed` (boolean, default false). Vor jeglichem Upload beide Bestätigungen verlangen.
- Gleichzeitige Uploads über feste concurrency-Gruppe `prototype-itch-upload` serialisieren; `cancel-in-progress: false`.
- Repository-Variablen: `ITCH_PROJECT` (owner/slug), `BUTLER_VERSION` (numerische x.y.z), `BUTLER_SHA256` (Hash des Windows-butler-Archivs). Secret `BUTLER_API_KEY`. Fehlende Werte mit Konfigurationshinweis abbrechen, keine Dummy-Werte einsetzen.
- GitHub CLI nutzen (mit `gh --version` prüfen); `GH_TOKEN` nur in Metadaten-/Download-Schritten aus github.token setzen. Mit `gh api repos/$env:GITHUB_REPOSITORY/actions/runs/<run-id>` und `ConvertFrom-Json` Quelle prüfen: Status completed, conclusion success, event workflow_dispatch, head_branch main, path `.github/workflows/prototype-build.yml`, head_repository.full_name gleich aktuellem Repository. Bei Abweichung abbrechen.
- `gh run download <run-id> --repo <repository> --name prototype-win-x64 --dir <neuer-download-ordner>`; Exitcode prüfen. Kein Download des neuesten Runs, keine automatische Ersatzwahl bei abgelaufenem Artefakt.
- Checkout für Skripte auf main-Workflow-Revision belassen. Spielpaket niemals als Skriptquelle verwenden. expectedSourceCommit aus geprüftem Run head_sha, nicht aus aktuellem Checkout nehmen.
- butler-Windows-Archiv über `https://broth.itch.zone/butler/windows-amd64/<BUTLER_VERSION>/archive/default` beziehen. SHA-256 gegen BUTLER_SHA256 prüfen, nach runner.temp entpacken; `butler version` erfassen. Version/Hash werden beim Einrichten anhand des offiziellen Downloads festgelegt, nicht geraten. Kein LATEST als stiller Fallback.
- Upload-Skript aufrufen; BUTLER_API_KEY ausschließlich diesem Schritt über env zuführen. Für nicht erfolgreiche Herkunfts-/Prüfsummenprüfung kein butler push.

Der Hash schützt vor Verwechslung/Beschädigung; Herkunft wird separat über die Run-Prüfung abgesichert. Restricted-Sichtbarkeit wird bewusst vom Betreiber bestätigt; nicht behaupten, der Workflow habe sie automatisch bei itch.io geprüft.

## F. Verifikation ohne externe Veröffentlichung

Test-DeploymentScripts nutzt eigene GUID-Verzeichnisse unter artifacts/deployment-tests und PowerShell-Assertions (throw bei falschem Ergebnis). Keine neue Pester-/NuGet-Abhängigkeit. Synthetische Dateien sind ausschließlich für Prüflogik, kein Beweis eines spielbaren Pakets. Für butler einen lokalen Stub verwenden, der Aufrufe protokolliert; niemals mit echtem Secret testen.

| Fall | Erwartung |
|---|---|
| vollständige synthetische Pflichtstruktur, gültige Metadaten | Paketprüfung erfolgreich |
| coreclr.dll oder SceneGrade.xnb fehlt | Paketprüfung schlägt fehl, nennt Datei |
| kaputtes Registry-JSON / selfContained=false | Paketprüfung schlägt fehl |
| SDL2 nur unter win-arm64 | Paketprüfung schlägt fehl |
| Version `../bad`, v-Präfix oder Newline | Package verwirft vor Publish/Dateianlage |
| bereits vorhandener Versionsordner | kein Überschreiben |
| passendes Artefakt, Metadaten, bestätigter Hash, Stub | genau ein push mit richtigem Ordner, Kanal und Version |
| verändertes ZIP oder falscher bestätigter Hash | kein Stub-Aufruf |
| falscher sourceCommit / abweichende innere Metadaten | kein Stub-Aufruf |
| zwei ZIPs oder ZIP mit Traversal | Abbruch vor Extraktion/Upload |
| butler-Stub beendet mit Fehlercode | Upload-Skript schlägt fehl |

Beim Testen der Versionseingaben sicherstellen, dass die Validierung vor Clean-Tree-Prüfung greift. Für vorhandenen Zielordner ebenfalls vor Publish prüfen. Ein realer Build muss zusätzlich dotnet test und Publish durchlaufen; danach ZIP separat entpacken und C erneut aufrufen.

Die Herkunftsprüfungen des Workflows anhand gespeicherter API-JSON-Beispiele für success/main, falschen Workflow, falschen Branch und failure prüfen, ohne Netzwerk oder Secrets. Dafür Prüfcode bei Bedarf als kleine Funktion/Skript in tools/deployment auslagern und dieselbe Implementierung im Workflow aufrufen; Tests nicht durch Nachbauen ihrer eigenen Prüflogik bestehen lassen.

## G. Abnahme, Restarbeit und Übergabe

SMOKE-TEST-Vorlage enthält: Version, Quell-SHA, Build-Run-ID, ZIP-SHA256, Datum, Tester, Windows-Version, GPU, Bestätigung keine separate .NET-/SDK-Installation, Start ohne Argumente, Arena, Grafik/Shader, Audio, Einstellungen, Ergebnis und Fehler. Bei fehlendem Rechner oder Zugang bleiben die zugehörigen Tasks offen, mit konkretem Grund.

Erst eingeschränkte Spielseite einrichten, dann Test/Hash bestätigen und Upload auslösen. Testdownload über tatsächlichen Testerzugang prüfen. Artefakt (ZIP plus Prüfsumme) vor Ablauf lokal aufbewahren. Rücknahme: denselben gesicherten vorherigen Kandidaten erneut hochladen oder bei Erstveröffentlichung Download entfernen/deaktivieren. Keine Löschung des Projekts erforderlich.

Ein Implementierungs-PR ist bei ausstehenden Betriebsaufgaben nur teilweise abgeschlossen. Dann gemäß AGENTS.md keine OpenSpec-Archive-Zeile setzen. Niemals erfolgreiche Zweitrechner-Tests, Uploads oder alle erledigten Tasks behaupten, wenn nur Skriptprüfungen bestanden wurden. Kein manuelles Archivieren nach Merge.

## H. Quellen für API-Details

- [MonoGame Packaging](https://docs.monogame.net/articles/getting_started/packaging_games.html)
- [GitHub CLI: run download](https://cli.github.com/manual/gh_run_download)
- [GitHub Workflow Runs REST API](https://docs.github.com/en/rest/actions/workflow-runs#get-a-workflow-run)
- [butler Installation](https://itch.io/docs/butler/installing.html)
- [butler push](https://itch.io/docs/butler/pushing.html)
- [itch.io Access Control](https://itch.io/docs/creators/access-control)
