# Hinweise für Agenten

## Pull Requests mit OpenSpec-Change

Setzt ein PR einen OpenSpec-Change vollständig um, steht in seiner
Beschreibung eine eigene Zeile:

```
OpenSpec-Archive: <change-name>
```

Mehrere Changes werden durch Leerzeichen getrennt, in der Reihenfolge, in der
sie aufeinander aufbauen. Nach dem Merge archiviert der Workflow
`.github/workflows/openspec-archive.yml` genau diese Changes nacheinander und
öffnet dafür einen gemeinsamen Folge-PR `chore/archive-pr-<nummer>`. PRs, die einen
Change nur vorschlagen (Proposal, Design, Tasks) oder nur teilweise umsetzen,
bekommen die Zeile nicht. Archiviere Changes nach einem Merge nicht von Hand.
