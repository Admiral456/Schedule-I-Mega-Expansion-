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
        cls.npcs = (ROOT / "src" / "Runtime" / "HarborlineNpcRoster.cs").read_text(encoding="utf-8")
        cls.shop = (ROOT / "src" / "Runtime" / "HarborlineShopCatalog.cs").read_text(encoding="utf-8")
        cls.build_workflow = (ROOT / ".github" / "workflows" / "build-runtime.yml").read_text(encoding="utf-8")
        cls.root_readme = (ROOT / "README.md").read_text(encoding="utf-8")
        cls.src_readme = (ROOT / "src" / "README.md").read_text(encoding="utf-8")

    def test_runtime_project_targets_net6_and_matches_manifest(self):
        target = self.project.find("./PropertyGroup/TargetFramework")
        version = self.project.find("./PropertyGroup/Version")
        self.assertIsNotNone(target)
        self.assertEqual(target.text, "net6.0")
        self.assertIsNotNone(version)
        self.assertEqual(version.text, self.manifest["version_number"])
        self.assertEqual(version.text, "0.3.1")

    def test_runtime_uses_official_s1api_package_without_bundling_game_assemblies(self):
        self.assertEqual(self.project.findall(".//Reference"), [])
        packages = {
            element.attrib.get("Include"): element.attrib.get("Version")
            for element in self.project.findall(".//PackageReference")
        }
        self.assertEqual(packages.get("LavaGang.MelonLoader"), "0.7.3")
        self.assertEqual(packages.get("S1API.Forked"), "3.2.2")
        self.assertNotIn("RefGen.Schedule-I.Il2Cpp", packages)
        self.assertNotIn("Il2CppInterop.Common", packages)
        self.assertNotIn("S1MAPI", packages)
        self.assertIn('PackageReference Include="S1API.Forked" Version="3.2.2"', self.build_workflow + self.project.read_text() if hasattr(self.project, "read_text") else (ROOT / "src" / "ScheduleIMegaExpansion.csproj").read_text(encoding="utf-8"))

    def test_core_uses_native_save_and_scene_lifecycle(self):
        self.assertIn('GetType("S1API.Lifecycle.GameLifecycle", false)', self.core)
        for event_name in ("OnPreLoad", "OnLoadComplete", "OnPreSceneChange"):
            self.assertIn('"' + event_name + '"', self.core)
        self.assertIn("HarborlinePhysicalBuilder.TryAutoGenerate(", self.core)
        self.assertIn("HarborlinePhysicalBuilder.ResetForNewSave();", self.core)
        self.assertIn("private void OnPreSceneChange()", self.core)
        self.assertIn("_saveLoaded = true", self.core)
        self.assertIn("HarborlineGameplayBootstrap.TryActivate(", self.core)
        self.assertIn("if (_saveLoaded && !_gameplayInitialized)", self.core)
        self.assertNotIn("if (_saveLoaded && _harborlineGenerated && !_gameplayInitialized)", self.core)
        self.assertIn("_gameplayInitialized", self.core)

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
        self.assertIn("temporaryReportPath", self.physical)
        self.assertIn("placementRegionVerified = false", self.physical)
        self.assertIn('if (cube != null && !parented)', self.physical)
        self.assertIn("DestroyGameObject(cube)", self.physical)

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
        self.assertIn("WriteShopsAndItems(outputDirectory, summary, warn)", self.exporter)
        self.assertIn("NativePrefabCatalogExporter.Export(", self.exporter)
        self.assertIn("native-prefabs.csv", self.prefabs)
        self.assertIn("native-game-api-surface.txt", self.scanner)
        for filename in (
            "native-regions.csv",
            "native-buildings.csv",
            "native-properties.csv",
            "native-businesses.csv",
            "native-shops.csv",
            "native-shop-items.csv",
            "native-items.csv",
            "standalone-phone-map-pois.csv",
            "native-law-controller.csv",
            "native-law-manager.csv",
            "native-law-checkpoint-locations.csv",
            "native-law-checkpoints.csv",
            "native-law-foot-patrol-routes.csv",
            "native-law-vehicle-patrol-routes.csv",
        ):
            self.assertIn(filename, self.exporter)

    def test_prefab_survey_never_instantiates_prefabs_and_marks_unknown_network_status(self):
        self.assertIn("S1MAPI.Core.PrefabRef", self.prefabs)
        self.assertIn('"Find"', self.prefabs)
        self.assertNotIn(".Instantiate(", self.prefabs)
        self.assertNotIn(".InstantiateNetworked(", self.prefabs)
        self.assertIn('"unknown-do-not-spawn"', self.prefabs)
        self.assertIn('"unknown"', self.prefabs)
        self.assertIn("ReadComponentTypes(prefab, out bool componentScanComplete)", self.prefabs)
        self.assertIn("bool networkStatusKnown = componentScanComplete", self.prefabs)
        self.assertIn("Partial enumeration must never be treated", self.prefabs)

    def test_metadata_scanner_covers_s1mapi_and_fishnet(self):
        self.assertIn('name.IndexOf("S1MAPI"', self.scanner)
        self.assertIn('name.IndexOf("FishNet"', self.scanner)
        self.assertIn('"NavigationBuilder"', self.scanner)
        self.assertIn('"NetworkObject"', self.scanner)
        self.assertIn('"PrefabPlacer"', self.scanner)
        self.assertIn("METADATA_ONLY", self.scanner)
        law_diagnostics = (ROOT / "src" / "Diagnostics" / "NativeLawSnapshotExporter.cs").read_text(encoding="utf-8")
        self.assertIn("S1API.Law.CheckpointManager", law_diagnostics)
        self.assertIn("GetAllCheckpointInfo", law_diagnostics)
        self.assertIn("GetAllFootPatrolRoutes", law_diagnostics)
        self.assertIn("GetAllVehiclePatrolRoutes", law_diagnostics)
        self.assertIn("NativeLawSnapshotMode=READ_ONLY", law_diagnostics)
        self.assertNotIn("SetCheckpointEnabled(", law_diagnostics)
        self.assertNotIn("StartFootPatrol(", law_diagnostics)

    def test_registry_exporters_do_not_call_game_mutation_apis(self):
        diagnostic_helpers = self.exporter + self.scanner + self.prefabs
        self.assertNotIn(".SetOwned(", diagnostic_helpers)
        self.assertNotIn(".SetPrice(", diagnostic_helpers)
        self.assertNotIn(".Instantiate(", diagnostic_helpers)
        self.assertNotIn(".InstantiateNetworked(", diagnostic_helpers)
        self.assertNotIn(".Spawn(", diagnostic_helpers)

    def test_native_npc_pilot_uses_s1api_lifecycle_and_real_roles(self):
        for npc_id in (
            "harborline_dealer_mason",
            "harborline_dealer_sierra",
            "harborline_customer_caleb",
            "harborline_customer_nina",
            "harborline_customer_rafael",
            "harborline_customer_olivia", "harborline_customer_ethan",
            "harborline_customer_maya", "harborline_customer_lucas",
            "harborline_customer_ava",
        ):
            self.assertIn(npc_id, self.npcs)
        self.assertEqual(self.npcs.count("public override bool IsDealer => true;"), 2)
        self.assertEqual(self.npcs.count("public override bool IsCustomer => true;"), 8)
        self.assertIn("WithDealerDefaults(", self.npcs)
        self.assertIn("WithCustomerDefaults(", self.npcs)
        self.assertIn("WithAffinities(", self.npcs)
        self.assertIn("CustomerStandard.Moderate", self.npcs)
        self.assertIn("DrugType.Methamphetamine", self.npcs)
        self.assertIn("Appearance.GenerateRandomAppearance()", self.npcs)
        self.assertIn("Appearance.Build()", self.npcs)
        self.assertIn("Schedule.Enable()", self.npcs)
        self.assertEqual(self.npcs.count("builder.WithSchedule(plan => plan.EnsureDealSignal());"), 8)
        self.assertEqual(self.npcs.count("plan.EnsureDealSignal()"), 8)
        self.assertIn("NPC.CustomNpcsReady", self.npcs)
        self.assertIn("IsConfirmedSinglePlayerSession()", self.npcs)
        self.assertIn("NPC.Get(id)", self.npcs)
        self.assertEqual(self.npcs.count("new HarborlineCustomer"), 8)
        self.assertIn("deseti nativních S1API NPC", self.root_readme)
        self.assertIn("Dalších 32 postav", self.root_readme)
        self.assertIn("Osm fyzických zákaznických NPC", self.root_readme)
        self.assertIn("pilot 10 postav", self.src_readme)
        self.assertIn("zbylých 22 profilů", self.src_readme)
        self.assertIn("10 fyzických S1API NPC", (ROOT / "docs" / "implementation-audit.md").read_text(encoding="utf-8"))
        self.assertIn("slot > 9", self.npcs)
        self.assertIn("slot > 9", self.physical)
        self.assertIn("5 => -21.0f", self.physical)
        self.assertNotIn("InitializeActions()", self.npcs)
        self.assertNotIn("HandleDeal(900)", self.npcs)

    def test_shop_stock_uses_native_shared_item_prices_and_purchase_ui(self):
        self.assertIn('ShopManager.GetShopByName("Hardware Store")', self.shop)
        self.assertIn("ItemManager.GetDefinition(stock.Id)", self.shop)
        self.assertIn("item is not NativeStorableItemDefinition storable", self.shop)
        self.assertIn("storable.BasePurchasePrice = stock.Price", self.shop)
        self.assertIn("hardwareStore.AddItem(item)", self.shop)
        self.assertNotIn("hardwareStore.AddItem(item, stock.Price)", self.shop)
        self.assertIn("customPrice argument is currently ignored", self.shop)
        self.assertIn("shared item definition", self.shop)
        self.assertIn("hardwareStore.HasItem(item.ID)", self.shop)
        self.assertEqual(self.shop.count("new StockItem("), 14)
        self.assertIn("using NativeStorableItemDefinition = S1API.Items.Storable.StorableItemDefinition;", self.shop)
        for item_id in (
            "plasticpot", "moisturepreservingpot", "airpot",
            "suspensionrack", "halogengrowlight", "ledgrowlight",
            "fullspectrumgrowlight", "dryingrack", "mixingstation",
            "chemistrystation", "laboven", "mixingstationmk2",
            "brickpress", "cauldron",
        ):
            self.assertIn('"' + item_id + '"', self.shop)

    def test_thunderstore_manifest_and_package_layout_are_consistent(self):
        self.assertEqual(self.manifest["name"], "ScheduleIMegaExpansion")
        self.assertEqual(self.manifest["version_number"], "0.3.1")
        dependencies = set(self.manifest["dependencies"])
        self.assertIn("LavaGang-MelonLoader-0.7.3", dependencies)
        self.assertIn("ifBars-S1API_Forked-3.2.2", dependencies)
        self.assertIn("ifBars-S1MAPI-2.0.1", dependencies)
        workflow = self.build_workflow
        self.assertIn("package/Mods/", workflow)
        self.assertIn("ScheduleIMegaExpansion-0.3.1.zip", workflow)
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
        self.assertIn("Unity primitiva nejsou nativní Schedule I Building", self.src_readme)

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
        by_id = {item["id"]: item for item in self.audit["requirements"]}
        self.assertEqual(by_id["npc.population"]["status"], "implemented_code_unverified_runtime")
        self.assertEqual(by_id["npc.customer_profiles"]["status"], "implemented_code_unverified_runtime")
        self.assertEqual(by_id["shops.native_stock"]["status"], "implemented_code_unverified_runtime")
        self.assertEqual(by_id["shops.online_orders"]["status"], "not_implemented")
        self.assertEqual(by_id["properties.realtor"]["status"], "blocked_unverified_api")
        self.assertEqual(by_id["map.icons"]["status"], "not_implemented")
        self.assertEqual(by_id["police.integration"]["status"], "not_implemented")
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
