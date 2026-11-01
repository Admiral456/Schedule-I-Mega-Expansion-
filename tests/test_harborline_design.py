import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def load_json(relative_path):
    with (ROOT / relative_path).open("r", encoding="utf-8") as handle:
        return json.load(handle)


class HarborlineDesignTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.anchors = load_json("world/harborline/anchors.json")
        cls.roster = load_json("packs/Harborline/roster.json")
        cls.customers = load_json("packs/Harborline/customer-designs.json")
        cls.native = load_json("world/harborline/native-integration.json")
        cls.expansion = load_json("world/harborline/expansion-plan.json")

    def test_customer_count_matches_roster_target(self):
        profiles = self.customers["profiles"]
        self.assertEqual(len(profiles), self.roster["customerProfileRules"]["targetCount"])
        self.assertEqual(len({profile["id"] for profile in profiles}), len(profiles))

    def test_each_customer_has_distinct_product_and_effect_preferences(self):
        signatures = set()
        for profile in self.customers["profiles"]:
            likes = set(profile["likesProducts"])
            dislikes = set(profile["dislikesProducts"])
            self.assertTrue(likes, profile["id"])
            self.assertTrue(dislikes, profile["id"])
            self.assertFalse(likes & dislikes, profile["id"])
            self.assertTrue(profile["preferredEffects"], profile["id"])
            signature = (
                tuple(sorted(likes)),
                tuple(sorted(dislikes)),
                tuple(sorted(profile["preferredEffects"])),
                tuple(sorted(profile["avoidEffects"])),
            )
            self.assertNotIn(signature, signatures, profile["id"])
            signatures.add(signature)

    def test_harborline_uses_native_docks_region_not_a_fake_enum(self):
        allowed = {"Northtown", "Westville", "Downtown", "Docks", "Suburbia", "Uptown"}
        self.assertEqual(self.roster["nativeRegion"], "Docks")
        self.assertEqual(self.anchors["nativeRegion"], "Docks")
        self.assertEqual(self.expansion["nativeRegion"], "Docks")
        self.assertEqual(self.native["regionIntegration"]["harborlineParentNativeRegion"], "Docks")
        self.assertTrue(self.native["regionIntegration"]["nativeRegionEnumVerified"])
        self.assertFalse(self.native["regionIntegration"]["customRegionEnumRequired"])
        self.assertEqual(self.native["worldMap"]["regionId"], "Docks")
        self.assertFalse(self.native["worldMap"]["requiresNativeRegionRegistration"])
        self.assertEqual(set(self.native["regionIntegration"]["availableNativeRegionValues"]), allowed)

    def test_native_npc_random_appearance_is_required(self):
        npc = self.roster["npcImplementation"]
        self.assertEqual(npc["appearanceApi"], "S1API.Entities.NPCAppearance.GenerateRandomAppearance()")
        self.assertFalse(npc["externalNpcPackFrameworkRequired"])
        self.assertFalse(self.roster["appearanceRules"]["usePersonnelFramework"])
        self.assertTrue(self.roster["appearanceRules"]["useNativeRandomizer"])

    def test_map_must_extend_real_game_world_and_normal_map(self):
        map_rules = self.native["worldMap"]
        self.assertTrue(map_rules["extendPlayableWorld"])
        self.assertTrue(map_rules["normalInGameMapVisible"])
        self.assertTrue(self.expansion["mapIntegration"]["sameGameMap"])
        self.assertTrue(self.expansion["mapIntegration"]["physicallyExtendPlayableWorld"])

    def test_map_icons_come_from_native_building_and_property_types(self):
        for site in self.anchors["sites"]:
            self.assertFalse(site["mapClassification"]["customSpriteOverride"], site["id"])
            self.assertEqual(site["mapClassification"]["nativeIconSource"], "native-game-building-property-map-classification")
            self.assertTrue(site["entrance"]["required"], site["id"])
            self.assertTrue(site["interior"]["required"], site["id"])
            self.assertTrue(site["interior"]["staticDecorationAloneIsInsufficient"], site["id"])

    def test_owned_property_icon_uses_real_ownership_state(self):
        for site in self.anchors["sites"]:
            if site.get("purchasableThroughExistingRealtor"):
                self.assertEqual(site["mapClassification"]["ownedIconRole"], "native-owned-property-icon")
        self.assertEqual(self.anchors["businessIntegration"]["listingSystem"], "existing-Realtor")

    def test_property_registration_gap_is_not_hidden(self):
        self.assertIs(self.native["propertyRegistration"]["customPropertyHookVerified"], False)

    def test_all_interactive_doors_are_networked_native_doors(self):
        for site in self.anchors["sites"]:
            self.assertEqual(site["entrance"]["doorSystem"], "native-interactive-door-controller")
            self.assertTrue(site["entrance"]["networked"])

    def test_mobile_and_physical_shop_share_catalog(self):
        for site_id in ("portside_supply", "harbor_market"):
            site = next(site for site in self.anchors["sites"] if site["id"] == site_id)
            commerce = site["commerce"]
            self.assertIn("in-person", commerce["channels"])
            self.assertIn("hyland-network-mobile", commerce["channels"])
            self.assertIn("shared-native", commerce["inventorySource"])

    def test_police_checks_use_three_unsurveyed_positions(self):
        police = self.anchors["policeInfrastructure"]
        self.assertTrue(police["doNotCreateSeparatePoliceAi"])
        self.assertEqual(len(police["checkpoints"]), 3)
        self.assertTrue(all(checkpoint["position"] is None for checkpoint in police["checkpoints"]))

    def test_unmeasured_world_anchors_are_not_marked_verified(self):
        entries = self.anchors["connections"] + self.anchors["sites"] + self.anchors["environmentAnchors"]
        for entry in entries:
            if entry.get("position") is None:
                self.assertEqual(entry["surveyStatus"], "not-surveyed", entry["id"])


if __name__ == "__main__":
    unittest.main()
