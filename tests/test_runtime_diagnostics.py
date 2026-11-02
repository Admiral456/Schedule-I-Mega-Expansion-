import json
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


class RuntimeDiagnosticTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.project_path = ROOT / "src" / "ScheduleIMegaExpansion.csproj"
        cls.project = ET.parse(cls.project_path).getroot()
        cls.manifest = json.loads((ROOT / "manifest.json").read_text(encoding="utf-8"))
        cls.audit = json.loads((ROOT / "world" / "harborline" / "implementation-status.json").read_text(encoding="utf-8"))
        cls.core = (ROOT / "src" / "Core.cs").read_text(encoding="utf-8")
        cls.exporter = (ROOT / "src" / "Diagnostics" / "WorldSnapshotExporter.cs").read_text(encoding="utf-8")
        cls.scanner = (ROOT / "src" / "Diagnostics" / "NativeApiSurfaceScanner.cs").read_text(encoding="utf-8")
        cls.prefabs = (ROOT / "src" / "Diagnostics" / "NativePrefabCatalogExporter.cs").read_text(encoding="utf-8")
        cls.installer = (ROOT / "src" / "install.ps1").read_text(encoding="utf-8")
        cls.physical = (ROOT / "src" / "Runtime" / "HarborlinePhysicalBuilder.cs").read_text(encoding="utf-8")
        cls.root_readme = (ROOT / "README.md").read_text(encoding="utf-8")
        cls.src_readme = (ROOT / "src" / "README.md").read_text(encoding="utf-8")

    def test_runtime_project_targets_net6_and_matches_manifest(self):
        target = self.project.find("./PropertyGroup/TargetFramework")
        version = self.project.find("./PropertyGroup/Version")
        self.assertIsNotNone(target)
        self.assertEqual(target.text, "net6.0")
        self.assertIsNotNone(version)
        self.assertEqual(version.text, self.manifest["version_number"])

    def test_runtime_compiles_without_bundling_game_assemblies(self):
        self.assertEqual(self.project.findall(".//Reference"), [])
        packages = {
            element.attrib.get("Include"): element.attrib.get("Version")
            for element in self.project.findall(".//PackageReference")
        }
        self.assertEqual(packages.get("LavaGang.MelonLoader"), "0.7.3")
        self.assertNotIn("S1API.Forked", packages)
        self.assertNotIn("S1MAPI", packages)

    def test_core_uses_native_save_and_scene_lifecycle(self):
        self.assertIn('GetType("S1API.Lifecycle.GameLifecycle", false)', self.core)
        for event_name in ("OnPreLoad", "OnLoadComplete", "OnPreSceneChange"):
            self.assertIn('"' + event_name + '"', self.core)
        self.assertIn("HarborlinePhysicalBuilder.TryAutoGenerate(", self.core)
        self.assertIn("HarborlinePhysicalBuilder.ResetForNewSave();", self.core)
        self.assertIn("private void OnPreSceneChange()", self.core)
        self.assertIn("_saveLoaded = true", self.core)

    def test_player_safety_gate_uses_real_s1api_player_registry(self):
        self.assertIn("IsConfirmedSinglePlayer(out int playerCount)", self.physical)
        self.assertIn('FindType("S1API.Entities.Player")', self.physical)
        self.assertIn('GetField("All"', self.physical)
        self.assertIn("return playerCount == 1;", self.physical)
        self.assertNotIn("IsMultiplayerSession", self.physical)

    def test_automatic_placement_uses_documented_property_wrapper_members(self):
        self.assertIn('FindType("S1API.Property.PropertyManager")', self.physical)
        self.assertIn("GetAllProperties", self.physical)
        self.assertIn('"PropertyName"', self.physical)
        self.assertIn('"Docks Warehouse"', self.physical)
        self.assertIn('"ExteriorSpawnPosition"', self.physical)
        self.assertIn("TryFindClearPlacement(", self.physical)

    def test_placement_checks_occupancy_ground_slope_and_water_hints(self):
        self.assertIn('method.Name == "CheckBox"', self.physical)
        self.assertIn('method.Name == "Raycast"', self.physical)
        self.assertIn("normalY < 0.92f", self.physical)
        self.assertIn("heights.Max() - heights.Min() > 1.5f", self.physical)
        self.assertIn("IsLikelyWaterCollider", self.physical)
        self.assertIn('"water"', self.physical)
        self.assertIn('"ocean"', self.physical)

    def test_physical_generation_is_transactional_and_cleans_up(self):
        self.assertIn("PendingRootName", self.physical)
        self.assertIn("CleanupActiveRoot()", self.physical)
        self.assertIn("CleanupGeneratedRoot(RootName)", self.physical)
        self.assertIn("DestroyGameObject(", self.physical)
        self.assertIn('SetProperty(rootObject, "name", RootName)', self.physical)
        self.assertIn("pending geometry was rolled back", self.physical)

    def test_physical_slice_is_unity_geometry_not_a_native_business(self):
        self.assertIn("CreatePrimitive", self.physical)
        self.assertIn('FindType("UnityEngine.GameObject")', self.physical)
        self.assertIn('FindType("UnityEngine.MeshRenderer")', self.physical)
        self.assertIn("BuildWarehouse()", self.physical)
        self.assertIn("BuildContainers()", self.physical)
        self.assertIn("BuildCrane()", self.physical)
        self.assertIn("This stage is physical geometry only", self.physical)
        self.assertNotIn("F6", self.physical)
        self.assertNotIn("F7", self.physical)

    def test_registry_diagnostics_read_native_registries_and_export_real_filenames(self):
        for token in (
            'FindLoadedType("S1API.Map.Building")',
            'FindLoadedType("S1API.Property.PropertyManager")',
            'FindLoadedType("S1API.Property.BusinessManager")',
            'FindLoadedType("S1API.Map.Region")',
        ):
            self.assertIn(token, self.exporter)
        self.assertIn("NativeApiSurfaceScanner.Export(", self.exporter)
        self.assertIn("NativePrefabCatalogExporter.Export(", self.exporter)
        self.assertIn("native-prefabs.csv", self.exporter)
        self.assertIn("native-game-api-surface.txt", self.scanner)
        for filename in (
            "native-regions.csv",
            "native-buildings.csv",
            "native-properties.csv",
            "native-businesses.csv",
            "standalone-phone-map-pois.csv",
        ):
            self.assertIn(filename, self.exporter)

    def test_prefab_survey_never_instantiates_prefabs_and_marks_unknown_network_status(self):
        self.assertIn("S1MAPI.Core.PrefabRef", self.prefabs)
        self.assertIn('"Find"', self.prefabs)
        self.assertNotIn(".Instantiate(", self.prefabs)
        self.assertNotIn(".InstantiateNetworked(", self.prefabs)
        self.assertIn('"unknown-do-not-spawn"', self.prefabs)
        self.assertIn('"unknown"', self.prefabs)

    def test_metadata_scanner_covers_s1mapi_and_fishnet(self):
        self.assertIn('name.IndexOf("S1MAPI"', self.scanner)
        self.assertIn('name.IndexOf("FishNet"', self.scanner)
        self.assertIn('"NavigationBuilder"', self.scanner)
        self.assertIn('"NetworkObject"', self.scanner)
        self.assertIn('"PrefabPlacer"', self.scanner)
        self.assertIn("METADATA_ONLY", self.scanner)

    def test_registry_exporters_do_not_call_game_mutation_apis(self):
        diagnostic_helpers = self.exporter + self.scanner + self.prefabs
        self.assertNotIn(".SetOwned(", diagnostic_helpers)
        self.assertNotIn(".SetPrice(", diagnostic_helpers)
        self.assertNotIn(".Instantiate(", diagnostic_helpers)
        self.assertNotIn(".InstantiateNetworked(", diagnostic_helpers)
        self.assertNotIn(".Spawn(", diagnostic_helpers)

    def test_thunderstore_manifest_and_package_layout_are_consistent(self):
        self.assertEqual(self.manifest["name"], "ScheduleIMegaExpansion")
        self.assertEqual(self.manifest["version_number"], "0.2.0")
        dependencies = set(self.manifest["dependencies"])
        self.assertIn("LavaGang-MelonLoader-0.7.3", dependencies)
        self.assertIn("ifBars-S1API_Forked-3.2.2", dependencies)
        self.assertIn("ifBars-S1MAPI-2.0.1", dependencies)
        workflow = (ROOT / ".github" / "workflows" / "build-runtime.yml").read_text(encoding="utf-8")
        self.assertIn("package/Mods/", workflow)
        self.assertIn("ScheduleIMegaExpansion-0.2.0.zip", workflow)
        for required in (
            "Mods/ScheduleIMegaExpansion.dll",
            "manifest.json",
            "README.md",
            "LICENSE",
        ):
            self.assertIn(required, workflow)

    def test_installer_has_no_manual_hotkey_instructions(self):
        self.assertIn("Schedule I.exe", self.installer)
        self.assertIn("ScheduleIMegaExpansion.dll", self.installer)
        self.assertIn("Copy-Item", self.installer)
        self.assertNotIn("F6", self.installer)
        self.assertNotIn("F7", self.installer)

    def test_docs_do_not_claim_full_implementation_or_read_only_total_mod(self):
        self.assertNotIn("diagnostický runtime nic nemění", self.root_readme)
        self.assertIn("nejsou v runtime implementované", self.root_readme.lower())
        self.assertIn("does not yet register a native Building", self.src_readme)

    def test_audit_matrix_covers_all_requested_systems(self):
        ids = {item["id"] for item in self.audit["requirements"]}
        required = {
            "runtime.bootstrap", "diagnostics.registries", "diagnostics.prefabs",
            "world.automatic_geometry", "world.placement", "world.roads_navigation",
            "buildings.interiors", "doors.interactions", "properties.realtor",
            "map.icons", "npc.population", "npc.appearance", "npc.customer_profiles",
            "npc.schedules", "shops.online_orders", "police.integration",
            "multiplayer", "persistence", "runtime.test",
        }
        self.assertTrue(required.issubset(ids))
        valid_statuses = set(self.audit["statusLegend"])
        for item in self.audit["requirements"]:
            self.assertIn(item["status"], valid_statuses)
            self.assertTrue(item["evidence"], item["id"])
            self.assertTrue(item["remaining"], item["id"])

    def test_no_manual_hotkey_instructions_remain(self):
        for source in (self.root_readme, self.src_readme, self.installer):
            self.assertNotIn("F6", source)
            self.assertNotIn("F7", source)


if __name__ == "__main__":
    unittest.main()
