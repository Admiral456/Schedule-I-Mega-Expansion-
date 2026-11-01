#!/usr/bin/env python3
"""Validate project JSON schemas and cross-file world/business/NPC/dependency references."""
from __future__ import annotations

import json
import sys
from pathlib import Path
from jsonschema import Draft202012Validator

ROOT = Path(__file__).resolve().parents[1]

def load(relative: str):
    path = ROOT / relative
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:
        raise SystemExit(f"Could not load {relative}: {exc}") from exc

def require(condition: bool, message: str):
    if not condition:
        raise SystemExit(f"PROJECT DATA ERROR: {message}")

def validate_schema(data_path: str, schema_path: str):
    data = load(data_path)
    schema = load(schema_path)
    validator = Draft202012Validator(schema, format_checker=Draft202012Validator.FORMAT_CHECKER)
    errors = sorted(
        validator.iter_errors(data),
        key=lambda error: (list(error.path), error.message),
    )
    if errors:
        for error in errors:
            location = ".".join(str(part) for part in error.path) or "<root>"
            print(f"{data_path} [{location}]: {error.message}", file=sys.stderr)
        raise SystemExit(1)
    print(f"schema ok: {data_path}")

def main():
    # Every JSON file in the repository must be syntactically valid.
    for path in ROOT.rglob("*.json"):
        if ".git" not in path.parts:
            json.loads(path.read_text(encoding="utf-8"))

    validate_schema("data/provinces.json", "schemas/provinces.schema.json")
    validate_schema("data/businesses.json", "schemas/businesses.schema.json")
    validate_schema("data/npc_profiles.json", "schemas/npc_profiles.schema.json")
    validate_schema("data/dependency-baseline.json", "schemas/dependency-baseline.schema.json")

    provinces_doc = load("data/provinces.json")
    businesses_doc = load("data/businesses.json")
    npcs_doc = load("data/npc_profiles.json")
    dependency_doc = load("data/dependency-baseline.json")

    provinces = provinces_doc["provinces"]
    province_ids = {item["id"] for item in provinces}
    require(len(province_ids) == 3, "exactly three unique expansion provinces are required")

    province_pois = {
        item["id"]: set(item.get("poiIds", []))
        for item in provinces
    }

    business_ids = [item["id"] for item in businesses_doc["businesses"]]
    listing_ids = [
        item.get("realtorListingId")
        for item in businesses_doc["businesses"]
        if item.get("purchasable")
    ]
    require(len(business_ids) == len(set(business_ids)), "business IDs must be unique")
    require(len(listing_ids) == len(set(listing_ids)), "Realtor listing IDs must be unique")

    for business in businesses_doc["businesses"]:
        province_id = business["provinceId"]
        require(province_id in province_ids, f"{business['id']} references unknown province {province_id}")
        require(
            business["physicalPoiId"] in province_pois[province_id],
            f"{business['id']} physical POI is missing from {province_id}.poiIds",
        )
        if business.get("purchasable"):
            require(bool(business.get("realtorListingId")), f"{business['id']} is purchasable but missing Realtor listing")
        if business.get("onlinePhoneAppId"):
            require(bool(business.get("sharedInventoryId")), f"{business['id']} has online shop but no shared inventory")

    portside = next(
        (item for item in businesses_doc["businesses"] if item["id"] == "sime.business.harborline.portside_supply"),
        None,
    )
    require(portside is not None, "Portside Supply business definition is required")
    require(bool(portside.get("onlinePhoneAppId")), "Portside Supply must expose a phone app")
    require(bool(portside.get("sharedInventoryId")), "Portside Supply online and physical sales must share inventory")

    npc_ids = [item["id"] for item in npcs_doc["profiles"]]
    require(len(npc_ids) == len(set(npc_ids)), "NPC IDs must be unique")
    for npc in npcs_doc["profiles"]:
        require(npc["provinceId"] in province_ids, f"{npc['id']} references unknown province")
        sensitivity = npc["preferences"]["priceSensitivity"]
        require(0.0 <= sensitivity <= 1.0, f"{npc['id']} priceSensitivity must be between 0 and 1")

    dependencies = dependency_doc["dependencies"]
    dependency_ids = [item["id"] for item in dependencies]
    require(len(dependency_ids) == len(set(dependency_ids)), "dependency IDs must be unique")
    require(dependency_doc["gameTarget"]["runtime"] == "IL2CPP", "project baseline must remain Steam Default / IL2CPP")

    expected = {
        "ifbars.s1api": ("3.2.2", "net6.0"),
        "ifbars.s1mapi": ("2.0.1", "net6.0"),
        "ifbars.steamnetworklib": ("1.6.0", "netstandard2.1"),
    }
    actual = {item["id"]: item for item in dependencies}
    for dependency_id, (version, target_framework) in expected.items():
        require(dependency_id in actual, f"missing pinned dependency {dependency_id}")
        require(actual[dependency_id]["version"] == version, f"{dependency_id} version differs from the reviewed baseline")
        require(actual[dependency_id]["targetFramework"] == target_framework, f"{dependency_id} target framework differs from the reviewed baseline")

    print("Project data validation passed: provinces, Realtor listings, shared shop stock, NPC references and IL2CPP dependency pins are consistent.")

if __name__ == "__main__":
    main()
