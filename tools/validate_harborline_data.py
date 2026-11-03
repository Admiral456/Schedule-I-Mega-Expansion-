from __future__ import annotations
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def load(rel: str):
    with (ROOT / rel).open(encoding="utf-8") as f:
        return json.load(f)

def validate() -> list[str]:
    errors: list[str] = []
    anchors = load("world/harborline/anchors.json")
    status = load("world/harborline/implementation-status.json")
    roster = load("packs/Harborline/roster.json")
    designs = load("packs/Harborline/customer-designs.json")
    integration = load("world/integrations/daffys-hills.json")

    anchor_ids = [a["id"] for a in anchors["anchors"]]
    if len(anchor_ids) != len(set(anchor_ids)):
        errors.append("Harborline anchor IDs must be unique")
    if any(a.get("coordinate") is not None for a in anchors["anchors"]):
        errors.append("Unsurveyed Harborline anchors must not contain invented coordinates")
    if anchors.get("nativeRegion") != "Docks":
        errors.append("Harborline must remain a named sub-district of native Docks")

    native_customers = roster["nativeRuntimeInventory"]["customers"]
    if len(native_customers) != 8:
        errors.append("Expected exactly eight documented native customer NPCs in 0.3.1 baseline")
    if len(set(native_customers)) != len(native_customers):
        errors.append("Native customer names must be unique")
    if len(designs["profiles"]) != 22:
        errors.append("Expected 22 additional proposed profiles")
    profile_ids = [x["id"] for x in designs["profiles"]]
    profile_names = [x["name"] for x in designs["profiles"]]
    if len(profile_ids) != len(set(profile_ids)) or len(profile_names) != len(set(profile_names)):
        errors.append("Additional customer IDs and names must be unique")
    for item in designs["profiles"]:
        p = item["profile"]
        if not 0 <= p["policeCallChance"] <= 1:
            errors.append(f"{item['id']}: policeCallChance must be in [0,1]")
        if not 0 <= p["addictionLevel"] <= 1:
            errors.append(f"{item['id']}: addictionLevel must be in [0,1]")
        if any(not 0 <= v <= 100 for v in p["affinities"].values()):
            errors.append(f"{item['id']}: affinities must be in [0,100]")
        if p["preferredProduct"] not in p["affinities"]:
            errors.append(f"{item['id']}: preferredProduct missing from affinities")
        if item["status"] != "planned_not_runtime_registered":
            errors.append(f"{item['id']}: proposed profiles must remain clearly marked as planned")

    if integration["integrationPolicy"]["bundled"] or integration["integrationPolicy"]["requiredDependency"]:
        errors.append("Daffy's Hills must remain optional and unbundled")
    if integration["integrationPolicy"]["copyAssets"] or integration["integrationPolicy"]["modifyExternalFiles"]:
        errors.append("Daffy's Hills assets/files must not be copied or modified")

    ids = [x["id"] for x in status["implementedButRuntimeUnverified"] + status["plannedOrBlocked"]]
    if len(ids) != len(set(ids)):
        errors.append("Implementation status IDs must be unique")
    return errors

if __name__ == "__main__":
    problems = validate()
    if problems:
        for e in problems:
            print(f"FAIL: {e}")
        raise SystemExit(1)
    print("OK: Harborline data, roster and integration rules are internally consistent.")
