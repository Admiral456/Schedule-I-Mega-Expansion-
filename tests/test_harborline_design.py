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

    def test_purchasable_sites_are_registered_with_existing_realtor(self):
        site_by_id = {site["id"]: site for site in self.anchors["sites"]}
        integration = self.anchors["businessIntegration"]
        self.assertEqual(integration["listingSystem"], "existing-Realtor")
        self.assertFalse(integration["customRealtorUi"])
        for site_id in integration["purchasableSiteIds"]:
            self.assertIn(site_id, site_by_id)
            self.assertTrue(site_by_id[site_id]["purchasableThroughExistingRealtor"])

    def test_mobile_and_physical_shop_share_catalog(self):
        for site_id in ("portside_supply", "harbor_market"):
            site = next(site for site in self.anchors["sites"] if site["id"] == site_id)
            commerce = site["commerce"]
            self.assertIn("in-person", commerce["channels"])
            self.assertIn("hyland-network-mobile", commerce["channels"])
            self.assertIn("shared-native", commerce["inventorySource"])

    def test_police_checks_use_three_surveyed_positions(self):
        police = self.anchors["policeInfrastructure"]
        self.assertTrue(police["doNotCreateSeparatePoliceAi"])
        self.assertEqual(len(police["checkpoints"]), 3)
        self.assertTrue(all(checkpoint["position"] is None for checkpoint in police["checkpoints"]))

    def test_unmeasured_world_anchors_are_not_marked_verified(self):
        for entry in self.anchors["connections"] + self.anchors["sites"] + self.anchors["environmentAnchors"]:
            if entry.get("position") is None:
                self.assertEqual(entry["surveyStatus"], "not-surveyed", entry["id"])


if __name__ == "__main__":
    unittest.main()
