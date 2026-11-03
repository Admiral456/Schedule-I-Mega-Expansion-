from __future__ import annotations

import argparse
import csv
import json
import tempfile
import zipfile
from pathlib import Path, PurePosixPath
from typing import Any


CSV_FILES = {
    "regions": "native-regions.csv",
    "buildings": "native-buildings.csv",
    "properties": "native-properties.csv",
    "businesses": "native-businesses.csv",
    "shops": "native-shops.csv",
    "shop_items": "native-shop-items.csv",
    "items": "native-items.csv",
    "map_pois": "standalone-phone-map-pois.csv",
    "prefabs": "native-prefabs.csv",
    "law_controller": "native-law-controller.csv",
    "law_manager": "native-law-manager.csv",
    "law_checkpoint_locations": "native-law-checkpoint-locations.csv",
    "law_checkpoints": "native-law-checkpoints.csv",
    "law_foot_patrols": "native-law-foot-patrol-routes.csv",
    "law_vehicle_patrols": "native-law-vehicle-patrol-routes.csv",
}


def _safe_extract(zip_path: Path, destination: Path) -> None:
    """Extract a diagnostics ZIP while rejecting absolute paths and traversal."""
    with zipfile.ZipFile(zip_path) as archive:
        for member in archive.infolist():
            normalized = member.filename.replace("\\", "/")
            target = PurePosixPath(normalized)
            if target.is_absolute() or ".." in target.parts:
                raise ValueError(f"Unsafe ZIP member path: {member.filename}")
            if not normalized:
                continue
        archive.extractall(destination)


def _find_summary(root: Path) -> Path:
    direct = root / "summary.txt"
    if direct.is_file():
        return direct
    candidates = list(root.rglob("summary.txt"))
    if not candidates:
        raise FileNotFoundError(f"No summary.txt found under {root}")
    return max(candidates, key=lambda item: item.stat().st_mtime)


def _read_summary(path: Path) -> dict[str, str]:
    values: dict[str, str] = {}
    for line in path.read_text(encoding="utf-8-sig", errors="replace").splitlines():
        if "=" not in line:
            continue
        key, value = line.split("=", 1)
        values[key.strip()] = value.strip()
    return values


def _read_csv(path: Path, warnings: list[str]) -> list[dict[str, str]]:
    if not path.is_file():
        warnings.append(f"Missing optional export: {path.name}")
        return []
    try:
        with path.open("r", encoding="utf-8-sig", newline="") as stream:
            return list(csv.DictReader(stream))
    except (OSError, csv.Error, UnicodeError) as exc:
        warnings.append(f"Could not read {path.name}: {type(exc).__name__}: {exc}")
        return []


def analyze_diagnostics_dir(directory: Path) -> dict[str, Any]:
    directory = directory.resolve()
    summary_path = _find_summary(directory)
    report_root = summary_path.parent
    warnings: list[str] = []
    summary = _read_summary(summary_path)
    exports = {
        key: _read_csv(report_root / filename, warnings)
        for key, filename in CSV_FILES.items()
    }

    docks = [
        row for row in exports["properties"]
        if row.get("propertyName", "").strip().casefold() == "docks warehouse"
    ]
    if len(docks) == 0:
        warnings.append("Docks Warehouse property was not found in native-properties.csv.")
    elif len(docks) > 1:
        warnings.append("Multiple Docks Warehouse property records were exported; inspect the raw CSV.")

    regions = [row.get("regionName", "") for row in exports["regions"]]
    if "Docks" not in regions:
        warnings.append("The native Region export does not contain an exact 'Docks' entry.")

    hardware_listings = [
        row for row in exports["shop_items"]
        if row.get("shopName", "").strip().casefold() == "hardware store"
    ]
    if not any(row.get("shopName", "").strip().casefold() == "hardware store" for row in exports["shops"]):
        warnings.append("The native shop export does not contain Hardware Store.")

    report: dict[str, Any] = {
        "sourceDirectory": report_root.name,
        "summary": summary,
        "counts": {key: len(rows) for key, rows in exports.items()},
        "regions": exports["regions"],
        "buildings": exports["buildings"],
        "properties": exports["properties"],
        "businesses": exports["businesses"],
        "shops": exports["shops"],
        "hardwareStoreListings": hardware_listings,
        "items": exports["items"],
        "mapPOIs": exports["map_pois"],
        "prefabs": exports["prefabs"],
        "law": {
            "controller": exports["law_controller"],
            "manager": exports["law_manager"],
            "checkpointLocations": exports["law_checkpoint_locations"],
            "checkpoints": exports["law_checkpoints"],
            "footPatrolRoutes": exports["law_foot_patrols"],
            "vehiclePatrolRoutes": exports["law_vehicle_patrols"],
        },
        "docksWarehouseAnchor": docks[0] if len(docks) == 1 else None,
        "warnings": warnings,
        "safetyNote": (
            "This is a read-only summary of exported game data. It does not calculate or assign "
            "Harborline coordinates, declare a property purchasable, change police coverage, "
            "or prove that a prefab is safe to spawn."
        ),
    }
    return report


def render_markdown(report: dict[str, Any]) -> str:
    counts = report["counts"]
    lines = [
        "# Schedule I runtime survey",
        "",
        f"Diagnostic folder: `{report['sourceDirectory']}`",
        "",
        "## Export counts",
        "",
        "| Export | Rows |",
        "|---|---:|",
    ]
    for key, value in counts.items():
        lines.append(f"| {key} | {value} |")

    lines.extend(["", "## Native region values", ""])
    if report["regions"]:
        for row in report["regions"]:
            lines.append(f"- {row.get('regionName', '?')} (enum {row.get('enumValue', '?')})")
    else:
        lines.append("No region records were exported.")

    lines.extend(["", "## Docks Warehouse anchor", ""])
    anchor = report["docksWarehouseAnchor"]
    if anchor:
        lines.extend([
            f"- Property code: {anchor.get('propertyCode', 'unknown')}",
            f"- Price: {anchor.get('price', 'unknown')}",
            f"- Owned: {anchor.get('isOwned', 'unknown')}",
            f"- Exterior spawn position: `{anchor.get('exteriorSpawnPosition', '')}`",
            f"- Interior spawn position: `{anchor.get('interiorSpawnPosition', '')}`",
            f"- NPC spawn position: `{anchor.get('npcSpawnPosition', '')}`",
        ])
    else:
        lines.append("Not found or ambiguous. Do not invent a substitute coordinate.")

    lines.extend(["", "## Existing native shops", ""])
    for row in report["shops"]:
        lines.append(f"- {row.get('shopName', 'Unknown shop')}: {row.get('listingCount', '?')} listings")
    if not report["shops"]:
        lines.append("No native shop rows were exported.")

    lines.extend(["", "### Hardware Store stock", ""])
    for row in report["hardwareStoreListings"]:
        lines.append(
            f"- {row.get('itemName', row.get('itemId', 'Unknown item'))} "
            f"(`{row.get('itemId', '?')}`) – base price {row.get('basePurchasePrice', '?')}"
        )
    if not report["hardwareStoreListings"]:
        lines.append("No Hardware Store listing rows were exported.")

    lines.extend(["", "## Native properties and businesses", ""])
    for row in report["properties"]:
        lines.append(
            f"- Property: {row.get('propertyName', '?')} | code {row.get('propertyCode', '?')} "
            f"| owned {row.get('isOwned', '?')} | exterior {row.get('exteriorSpawnPosition', '')}"
        )
    for row in report["businesses"]:
        lines.append(
            f"- Business: {row.get('businessName', '?')} | code {row.get('propertyCode', '?')} "
            f"| owned {row.get('isOwned', '?')} | exterior {row.get('exteriorSpawnPosition', '')}"
        )
    if not report["properties"] and not report["businesses"]:
        lines.append("No property or business records were exported.")

    lines.extend(["", "## Police and patrol survey", ""])
    law = report["law"]
    for key, rows in law.items():
        if isinstance(rows, list):
            lines.append(f"- {key}: {len(rows)} rows")
            for row in rows[:12]:
                label = row.get("checkpointName") or row.get("footPatrolRouteName") or row.get("vehiclePatrolRouteName") or row.get("checkpointLocation") or row.get("member") or row.get("runtimeType") or str(row)
                lines.append(f"  - {label}")
    lines.extend(["", "## Prefab candidates", ""])
    prefabs = report["prefabs"]
    if prefabs:
        for row in prefabs:
            lines.append(
                f"- {row.get('referenceName', '?')}: found={row.get('found', '?')}, "
                f"network={row.get('hasNetworkObject', 'unknown')}, "
                f"handling={row.get('recommendedSpawnHandling', 'unknown')}"
            )
    else:
        lines.append("No prefab candidates were exported.")

    lines.extend(["", "## Warnings", ""])
    if report["warnings"]:
        lines.extend(f"- {warning}" for warning in report["warnings"])
    else:
        lines.append("No missing or unreadable survey exports detected.")

    lines.extend(["", "## Interpretation limit", "", report["safetyNote"], ""])
    return "\n".join(lines)


def analyze_input(source: Path) -> dict[str, Any]:
    if source.is_dir():
        return analyze_diagnostics_dir(source)
    if source.is_file() and source.suffix.casefold() == ".zip":
        with tempfile.TemporaryDirectory(prefix="schedule-i-survey-") as temp:
            root = Path(temp)
            _safe_extract(source, root)
            return analyze_diagnostics_dir(root)
    raise FileNotFoundError(f"Expected a diagnostics directory or ZIP file: {source}")


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Summarize ScheduleIMegaExpansion read-only runtime diagnostic exports."
    )
    parser.add_argument("source", type=Path, help="Diagnostics folder or ZIP file")
    parser.add_argument("--markdown", type=Path, help="Write a Markdown report to this path")
    parser.add_argument("--json", dest="json_path", type=Path, help="Write a JSON report to this path")
    args = parser.parse_args()

    report = analyze_input(args.source)
    markdown = render_markdown(report)
    if args.markdown:
        args.markdown.write_text(markdown, encoding="utf-8")
    else:
        print(markdown)
    if args.json_path:
        args.json_path.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
