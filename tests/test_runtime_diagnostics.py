import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


class RuntimeDiagnosticTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.project_path = ROOT / "src" / "ScheduleIMegaExpansion.csproj"
        cls.project = ET.parse(cls.project_path).getroot()
        cls.core = (ROOT / "src" / "Core.cs").read_text(encoding="utf-8")
        cls.exporter = (ROOT / "src" / "Diagnostics" / "WorldSnapshotExporter.cs").read_text(encoding="utf-8")
        cls.scanner = (ROOT / "src" / "Diagnostics" / "NativeApiSurfaceScanner.cs").read_text(encoding="utf-8")
        cls.setup = (ROOT / "src" / "setup.ps1").read_text(encoding="utf-8")

    def test_runtime_project_targets_il2cpp_net6(self):
        target = self.project.find("./PropertyGroup/TargetFramework")
        self.assertIsNotNone(target)
        self.assertEqual(target.text, "net6.0")

    def test_runtime_dependencies_are_pinned(self):
        refs = {
            element.attrib.get("Include"): element.attrib.get("Version")
            for element in self.project.findall("./ItemGroup/PackageReference")
        }
        self.assertEqual(refs.get("S1API.Forked"), "3.1.6")
        self.assertEqual(refs.get("LavaGang.MelonLoader"), "0.7.3")

    def test_diagnostic_runs_after_native_world_load(self):
        self.assertIn("GameLifecycle.OnLoadComplete += OnLoadComplete", self.core)
        self.assertIn("WorldSnapshotExporter.Export(", self.core)

    def test_exporter_reads_required_native_registries(self):
        self.assertIn("Building.GetAll()", self.exporter)
        self.assertIn("PropertyManager.GetAllProperties()", self.exporter)
        self.assertIn("BusinessManager.GetAllBusinesses()", self.exporter)
        self.assertIn("Enum.GetValues(typeof(Region))", self.exporter)
        self.assertIn("NativeApiSurfaceScanner.Export(", self.exporter)

    def test_diagnostic_never_changes_native_property_or_business_state(self):
        combined = self.core + self.exporter + self.scanner
        self.assertNotIn(".SetOwned(", combined)
        self.assertNotIn(".SetPrice(", combined)
        self.assertNotIn(".InstantiateNetworked(", combined)
        self.assertNotIn(".Spawn(", combined)
        self.assertNotIn(".Invoke(", self.scanner)

    def test_setup_requires_real_game_and_il2cpp_assemblies(self):
        self.assertIn("Schedule I.exe", self.setup)
        self.assertIn("Assembly-CSharp.dll", self.setup)
        self.assertIn("Il2CppInterop.Runtime.dll", self.setup)


if __name__ == "__main__":
    unittest.main()
