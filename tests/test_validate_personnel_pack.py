import unittest

from tools.validate_personnel_pack import validate_manifest


class PersonnelManifestValidationTests(unittest.TestCase):
    def valid_manifest(self):
        return {
            "name": "Harborline test",
            "author": "Schedule I Mega Expansion",
            "schemaVersion": 3,
            "autoRegister": True,
            "npcs": [
                {
                    "id": "harborline_test_worker",
                    "name": "Test Worker",
                    "spawn": {
                        "x": 12.5,
                        "y": 0.0,
                        "z": -8.25,
                        "region": "Docks",
                        "physical": True,
                    },
                    "schedule": [
                        {
                            "type": "walkTo",
                            "time": "07:30",
                            "position": [12.5, 0.0, -8.25],
                        }
                    ],
                },
                {
                    "id": "harborline_test_contact",
                    "name": "Test Contact",
                    "contact": {},
                },
            ],
        }

    def test_accepts_measured_physical_npc_and_contact(self):
        errors, counts = validate_manifest(self.valid_manifest())
        self.assertEqual(errors, [])
        self.assertEqual(counts, {"npcs": 2, "physical": 1, "contact_only": 1})

    def test_rejects_unmeasured_spawn_coordinates(self):
        manifest = self.valid_manifest()
        manifest["npcs"][0]["spawn"]["x"] = None
        errors, _ = validate_manifest(manifest)
        self.assertTrue(any("spawn.x" in error for error in errors))

    def test_rejects_invalid_schedule_time_and_route(self):
        manifest = self.valid_manifest()
        manifest["npcs"][0]["schedule"][0]["time"] = "25:70"
        manifest["npcs"][0]["schedule"][0]["position"] = [1, None]
        errors, _ = validate_manifest(manifest)
        self.assertTrue(any("HH:MM" in error for error in errors))
        self.assertTrue(any("position" in error for error in errors))

    def test_rejects_duplicate_or_missing_ids(self):
        manifest = self.valid_manifest()
        manifest["npcs"][1]["id"] = manifest["npcs"][0]["id"]
        errors, _ = validate_manifest(manifest)
        self.assertTrue(any("duplicates" in error for error in errors))

        manifest["npcs"][1].pop("id")
        errors, _ = validate_manifest(manifest)
        self.assertTrue(any("stable explicit id" in error for error in errors))

    def test_rejects_non_automatic_pack(self):
        manifest = self.valid_manifest()
        manifest["autoRegister"] = False
        errors, _ = validate_manifest(manifest)
        self.assertTrue(any("autoRegister" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
