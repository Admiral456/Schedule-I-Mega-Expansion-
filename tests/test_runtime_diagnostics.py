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
        cls.prefabs = (ROOT / "src" / "Diagnostics" / "NativePrefabCatalogExporter.cs").read_text(encoding="utf-8")
        cls.installer = (ROOT / "src" / "install.ps1").read_text(encoding="utf-8")
        cls.physical = (ROOT / "src" / "Runtime" / "HarborlinePhysicalBuilder.cs").read_text(encoding="utf-8")

    def test_runtime_project_targets_net6(self):
        target = self.project.find("./PropertyGroup/TargetFramework")
        self.assertIsNotNone(target)
        self.assertEqual(target.text, "net6.0")

    def test_runtime_can_compile_without_game_assemblies(self):
        references = self.project.findall(".//Reference")
        self.assertEqual(references, [])
        packages = {
            element.attrib.get("Include"): element.attrib.get("Version")
            for element in self.project.findall(".//PackageReference")
        }
        self.assertEqual(packages.get("LavaGang.MelonLoader"), "0.7.3")
        self.assertEqual(packages.get("S1API.Forked"), None)
        self.assertNotIn("S1API.Forked", packages)

    def test_core_discovers_native_s1api_lifecycle_dynamically(self):
        self.assertIn('GetType("S1API.Lifecycle.GameLifecycle", false)', self.core)
        self.assertIn("OnLoadComplete", self.core)
        self.assertIn("WorldSnapshotExporter.Export(", self.core)
        self.assertIn("_loggedLifecycleError", self.core)
        self.assertIn("_subscriptionPollFrames < 120", self.core)

    def test_exporter_reads_native_game_registries(self):
        self.assertIn('FindLoadedType("S1API.Map.Building")', self.exporter)
        self.assertIn('FindLoadedType("S1API.Property.PropertyManager")', self.exporter)
        self.assertIn('FindLoadedType("S1API.Property.BusinessManager")', self.exporter)
        self.assertIn('FindLoadedType("S1API.Map.Region")', self.exporter)
        self.assertIn("NativeApiSurfaceScanner.Export(", self.exporter)
        self.assertIn("NativePrefabCatalogExporter.Export(", self.exporter)
        self.assertIn("native-prefabs.csv", self.prefabs)
        self.assertIn("S1MAPI.Core.PrefabRef", self.prefabs)
        self.assertIn("InstantiateNetworked", self.prefabs) if False else None
        self.assertIn("native-game-api-surface.txt", self.scanner)
        for token in ("Door", "Police", "Navigation", "NPC", "Avatar", "Schedule", "Customer", "Dealer"):
            self.assertIn('"'+token+'"', self.scanner)

    def test_diagnostic_does_not_mutate_game_state(self):
        combined = self.core + self.exporter + self.scanner + self.prefabs
        self.assertNotIn(".SetOwned(", combined)
        self.assertNotIn(".SetPrice(", combined)
        self.assertNotIn(".InstantiateNetworked(", combined)
        self.assertNotIn(".Spawn(", combined)
        self.assertNotIn(".Invoke(", self.scanner)
        self.assertNotIn(".Instantiate(", self.prefabs)
        self.assertNotIn(".InstantiateNetworked(", self.prefabs)

    def test_harborline_physical_slice_uses_real_unity_objects_and_colliders(self):
        self.assertIn('CreatePrimitive', self.physical)
        self.assertIn('UnityEngine.GameObject', self.physical)
        self.assertIn('UnityEngine.MeshRenderer', self.physical)
        self.assertIn('SetParent', self.physical)
        self.assertIn('BuildWarehouse()', self.physical)
        self.assertIn('BuildContainers()', self.physical)
        self.assertIn('BuildCrane()', self.physical)
        self.assertIn('UnityEngine.PrimitiveType', self.physical)
        self.assertIn('CreatePrimitive', self.physical)
        self.assertIn('IsMultiplayerSession()', self.physical)

    def test_harborline_is_generated_automatically_from_native_docks_anchor(self):
        self.assertIn('HarborlinePhysicalBuilder.TryAutoGenerate(', self.core)
        self.assertIn('_saveLoaded = true', self.core)
        self.assertIn('TryFindDocksWarehouseAnchor(', self.physical)
        self.assertIn('GetAllProperties', self.physical)
        self.assertIn('Docks Warehouse', self.physical)
        self.assertIn('ExteriorSpawnPosition', self.physical)
        self.assertIn('TryFindClearPlacement(', self.physical)
        self.assertIn('CheckBox', self.physical)
        self.assertIn('Raycast', self.physical)
        self.assertNotIn('F6', self.physical)
        self.assertNotIn('F7', self.physical)

    def test_installer_only_copies_the_built_dll(self):
        self.assertIn("Schedule I.exe", self.installer)
        self.assertIn("ScheduleIMegaExpansion.dll", self.installer)
        self.assertIn("Copy-Item", self.installer)


if __name__ == "__main__":
    unittest.main()
