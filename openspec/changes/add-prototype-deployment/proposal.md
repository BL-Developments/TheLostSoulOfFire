## Why

Der spielbare Prototyp soll parallel zur Feature-Entwicklung an einen kleinen Testerkreis verteilt werden. Ein versioniertes Windows-Paket und ein bewusst ausgelöster Veröffentlichungsprozess ermöglichen Tests ohne Entwicklungsumgebung.

## What Changes

- Windows-x64-Release als ZIP mit .NET-Laufzeit, nativen Abhängigkeiten und allen Spielinhalten erzeugen.
- Einen manuell auslösbaren GitHub-Actions-Workflow für Prüfung, Paketierung und Download-Artefakt ergänzen.
- Einen getrennten, ausdrücklich gewählten Upload des geprüften Pakets auf eine eingeschränkte itch.io-Seite vorsehen.
- Version und Quell-Commit im Paket dokumentieren sowie Testeranleitung und Checkliste für einen zweiten Windows-Rechner bereitstellen.

## Capabilities

### New Capabilities
- `prototype-distribution`: Wiederholbare Windows-Paketierung, nachvollziehbare Builds und kontrollierte Verteilung an Tester.

### Modified Capabilities
Keine.

## Impact

Betroffen sind Release-Skripte, zwei neue Workflows unter `.github/workflows`, Paket-Metadaten und Deployment-/Tester-Dokumentation. Bestehende CI und Gameplay-Funktionen bleiben unabhängig. Der Upload benötigt ein itch.io-Projekt und einen als GitHub-Secret hinterlegten API-Schlüssel; reine Paketierung benötigt keinen itch.io-Zugang.

Browser-Portierung, Gameserver, Installer, automatischer Updater sowie Linux- und macOS-Releases sind nicht Bestandteil dieses Changes.

## Umsetzungseinstieg

Der konkrete Dateiplan und alle Schnittstellen stehen in [implementation-guide.md](implementation-guide.md). Die Aufgaben trennen automatisierbare Implementierung von echter Abnahme und Veröffentlichung durch den Betreiber.
