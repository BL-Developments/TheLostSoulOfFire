# Visual Max tools

Asset inventory commands are offline and use only the standard Python library.

```bash
python3 tools/visual-max/visual_assets.py inventory
python3 tools/visual-max/visual_assets.py validate
python3 tools/visual-max/visual_assets.py self-test
```

`inventory` derives runtime paths and sprite metadata from `ArtAssets.cs`, checks
that each loaded texture is registered in `Content.mgcb`, verifies the locked
source hashes, and writes a reviewable JSON inventory. `validate` checks only
new versioned work beneath `art/visual-max/`; it intentionally does not alter
the historical Ludo delivery audit or its fixed 116-file claim.

Native visual review (requires a DesktopGL graphics host and a Release build):

```bash
bash tools/visual-max/capture-renderer-review.sh
```

This is the capture runner currently present on `prototype/design-polish`. Older Astra script and handoff references describe files not present on this branch. See the [current product handoff](../../docs/current/HANDOFF.md) and the [screenshot workflow](../../docs/visual-max/SCREENSHOT-WORKFLOW.md).
