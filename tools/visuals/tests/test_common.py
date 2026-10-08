"""Run with: tools/visuals/.venv/bin/python -m unittest discover tools/visuals/tests"""
from __future__ import annotations

import json
import os
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import visuals_common as common  # noqa: E402


class ManifestTests(unittest.TestCase):
    def setUp(self) -> None:
        self.directory = tempfile.TemporaryDirectory()
        self.root = Path(self.directory.name)
        self.manifest = self.root / "manifest.json"
        self.output = self.root / "probe.png"
        self.output.write_bytes(b"probe")

    def tearDown(self) -> None:
        self.directory.cleanup()

    def test_probe_call_writes_a_complete_entry(self) -> None:
        step = common.Step(
            visual_id="test.probe", step="probe", tool={"name": "unittest", "version": "1"},
            model=common.model_info("z-image-turbo"), prompt="probe", seed=7, outputs=[self.output])
        with common.record_step(step, self.manifest):
            pass

        entries = json.loads(self.manifest.read_text())["entries"]
        self.assertEqual(1, len(entries))
        self.assertEqual([], common.validate_entry(entries[0]))
        self.assertEqual(0.0, entries[0]["cost_eur"])
        self.assertEqual(common.sha256(self.output), entries[0]["outputs"][0]["sha256"])

    def test_incomplete_entry_is_rejected(self) -> None:
        with self.assertRaises(ValueError):
            common.append_entry({"step": "probe"}, self.manifest)

    def test_forbidden_model_stops_the_script(self) -> None:
        with self.assertRaises(SystemExit):
            common.model_info("flux2-klein-9b")


class CostBrakeTests(unittest.TestCase):
    def setUp(self) -> None:
        self.saved = os.environ.get(common.BUDGET_VARIABLE)
        self.directory = tempfile.TemporaryDirectory()
        self.manifest = Path(self.directory.name) / "manifest.json"

    def tearDown(self) -> None:
        if self.saved is None:
            os.environ.pop(common.BUDGET_VARIABLE, None)
        else:
            os.environ[common.BUDGET_VARIABLE] = self.saved
        self.directory.cleanup()

    def test_refuses_without_budget(self) -> None:
        os.environ.pop(common.BUDGET_VARIABLE, None)
        with self.assertRaises(common.PaidCallRefused):
            common.authorize_paid_call("fal.ai", 0.01, self.manifest)

    def test_refuses_with_budget_zero(self) -> None:
        os.environ[common.BUDGET_VARIABLE] = "0"
        with self.assertRaises(common.PaidCallRefused):
            common.authorize_paid_call("fal.ai", 0.01, self.manifest)

    def test_refuses_a_call_over_the_limit(self) -> None:
        os.environ[common.BUDGET_VARIABLE] = "5"
        self.manifest.write_text(json.dumps({"version": 1, "entries": [{"cost_eur": 4.5}]}))
        with self.assertRaises(common.PaidCallRefused):
            common.authorize_paid_call("meshy", 1.0, self.manifest)

    def test_allows_a_call_inside_an_opened_budget(self) -> None:
        os.environ[common.BUDGET_VARIABLE] = "5"
        self.manifest.write_text(json.dumps({"version": 1, "entries": [{"cost_eur": 1.0}]}))
        common.authorize_paid_call("meshy", 1.0, self.manifest)


if __name__ == "__main__":
    unittest.main()
