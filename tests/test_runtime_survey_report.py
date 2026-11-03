import csv
import tempfile
import unittest
import zipfile
from pathlib import Path

from tools.analyze_runtime_survey import analyze_input, render_markdown


class RuntimeSurveyReportTests(unittest.TestCase):
    def _write_csv(self, directory: Path, name: str, headers: list[str], rows: list[list[str]]) -> None:
        with (directory / name).open("w", encoding="utf-8", newline="") as stream:
            writer = csv.writer(stream)
            writer.writerow(headers)
            writer.writerows(rows)

    def _sample(self, directory: Path) -> None:
        (directory / "summary.txt").write_text(
            "NativeRegionValues=Northtown, Westville, Downtown, Docks, Suburbia, Uptown\n",
            encoding="utf-8",
        )
        self._write_csv(directory, "native-regions.csv", ["regionName", "enumValue"], [
            ["Docks", "3"], ["Downtown", "2"]
        ])
        self._write_csv(directory, "native-properties.csv",
            ["propertyName", "propertyCode", "price", "isOwned", "exteriorSpawnPosition", "interiorSpawnPosition", "npcSpawnPosition"],
            [["Docks Warehouse", "DocksWarehouse", "120000", "false", "(12, 0, 34)", "(12, 1, 34)", "(13, 0, 34)"]])
        self._write_csv(directory, "native-shops.csv", ["shopName", "listingCount"], [
            ["Hardware Store", "14"], ["Shack", "8"]
        ])
        self._write_csv(directory, "native-shop-items.csv",
            ["shopName", "itemId", "itemName", "category", "basePurchasePrice"],
            [["Hardware Store", "fertilizer", "Fertilizer", "Supply", "20"]])
        self._write_csv(directory, "native-law-foot-patrol-routes",
            ["index", "runtimeType", "footPatrolRouteName", "publicValues"], [])
        self._write_csv(directory, "native-law-vehicle-patrol-routes.csv",
            ["index", "runtimeType", "vehiclePatrolRouteName", "publicValues"], [["0", "Route", "Docks Patrol", "{}"]])

    def test_analyze_directory_extracts_native_anchor_and_stock(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self._sample(root)
            report = analyze_input(root)
            self.assertEqual(report["docksWarehouseAnchor"]["exteriorSpawnPosition"], "(12, 0, 34)")
            self.assertEqual(len(report["hardwareStoreListings"]), 1)
            self.assertIn("Docks", [row["regionName"] for row in report["regions"]])
            self.assertIn("Docks Warehouse", render_markdown(report))

    def test_analyze_zip_extracts_nested_diagnostics(self):
        with tempfile.TemporaryDirectory() as temp:
            base = Path(temp)
            diagnostic = base / "Diagnostics" / "2026-11-03"
            diagnostic.mkdir(parents=True)
            self._sample(diagnostic)
            archive = base / "survey.zip"
            with zipfile.ZipFile(archive, "w") as zf:
                for file in diagnostic.iterdir():
                    zf.write(file, f"Diagnostics/2026-11-03/{file.name}")
            report = analyze_input(archive)
            self.assertEqual(report["docksWarehouseAnchor"]["propertyName"], "Docks Warehouse")

if __name__ == "__main__":
    unittest.main()
