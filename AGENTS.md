# Hinweise für Agenten

## Pull Requests mit OpenSpec-Change

Setzt ein PR einen OpenSpec-Change vollständig um, steht in seiner
Beschreibung eine eigene Zeile:

```
OpenSpec-Archive: <change-name>
```

Mehrere Changes werden durch Leerzeichen getrennt. Nach dem Merge archiviert
der Workflow `.github/workflows/openspec-archive.yml` genau diese Changes und
öffnet dafür einen Folge-PR `chore/archive-<change-name>`. PRs, die einen
Change nur vorschlagen (Proposal, Design, Tasks) oder nur teilweise umsetzen,
bekommen die Zeile nicht. Archiviere Changes nach einem Merge nicht von Hand.
