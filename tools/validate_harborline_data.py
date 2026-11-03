from __future__ import annotations

import json
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]


def load(relative_path: str) -> dict[str, Any]:
    with (ROOT / relative_path).open(encoding="utf-8") as stream:
        return json.load(stream)


def validate() -> list[str]:
    """Validate design data only; never claim that these records are live game entities."""
    errors: list[str] = []
    anchors = load("world/harborline/anchors.json")
    status = load("world/harborline/implementation-status.json")
    roster = load("packs/Harborline/roster.json")
    designs = load("packs/Harborline/customer-designs.json")
    integration = load("world/integrations/daffys-hills.json")
    pier_integration = load("world/integrations/pier-expansion.json")
    ume_integration = load("world/integrations/ume-serena-flats.json")
    map_guard = load("world/integrations/map-expansion-guard.json")
    native_integration = load("world/harborline/native-integration.json")

    # Harborline remains a named extension of the vanilla Docks region.
    site_ids = [site["id"] for site in anchors.get("sites", [])]
    if not site_ids or len(site_ids) != len(set(site_ids)):
        errors.append("Harborline site IDs must be present and unique")
    if anchors.get("nativeRegion") != "Docks":
        errors.append("Harborline NPCs/sites must use the native Docks region")
    if anchors.get("status") != "survey-required":
        errors.append("Unsurveyed Harborline anchors must remain marked survey-required")
    for group_name in ("sites", "connections", "environmentAnchors"):
        for item in anchors.get(group_name, []):
            if item.get("surveyStatus", "not-surveyed") == "not-surveyed":
                for key in ("position", "rotationY", "anchor", "coordinates"):
                    if item.get(key) is not None:
                        errors.append(f"{group_name}/{item.get('id')}: unsurveyed {key} must be null")

    # The roster contains targets, not a guarantee that all NPCs are already spawned.
    groups = roster.get("groups", [])
    group_ids = [group.get("idPrefix") for group in groups]
    group_total = sum(int(group.get("targetCount", 0)) for group in groups)
    if len(group_ids) != len(set(group_ids)):
        errors.append("NPC group idPrefix values must be unique")
    npc_target = roster.get("npcTarget", {})
    if group_total != npc_target.get("physicalNpcCount"):
        errors.append("NPC group target sum must equal npcTarget.physicalNpcCount")
    if npc_target.get("uniqueCustomerProfiles") != len(designs.get("profiles", [])):
        errors.append("NPC target profile count must match customer-designs.json")
    if npc_target.get("newDealers") != len(roster.get("dealerProfiles", [])):
        errors.append("Dealer target count must match the dealer roster")
    if roster.get("nativeRegion") != "Docks":
        errors.append("NPC roster must use the native Docks region")

    # Customer profiles are design-only. Their product/effect values still require mapping to the target build.
    profiles = designs.get("profiles", [])
    profile_ids = [profile.get("id") for profile in profiles]
    if len(profiles) != 30:
        errors.append("Expected 30 customer design profiles")
    if len(profile_ids) != len(set(profile_ids)):
        errors.append("Customer profile IDs must be unique")
    for profile in profiles:
        pid = profile.get("id", "<missing-id>")
        if profile.get("mappingStatus", "").startswith("design-only") is False:
            errors.append(f"{pid}: design profile must remain marked design-only until mapped")
        if not profile.get("likesProducts"):
            errors.append(f"{pid}: at least one preferred product family is required")
        if not 1 <= int(profile.get("ordersPerWeek", 0)) <= 7:
            errors.append(f"{pid}: ordersPerWeek must be between 1 and 7")
        if profile.get("preferredDay") not in {
            "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
        }:
            errors.append(f"{pid}: preferredDay is invalid")
        if not isinstance(profile.get("preferredTime"), str) or ":" not in profile["preferredTime"]:
            errors.append(f"{pid}: preferredTime must be a clock time")

    # Daffy's Hills is an optional third-party mod. Never bundle, alter or reuse it.
    policy = integration.get("integrationPolicy", {})
    if policy.get("bundled") or policy.get("requiredDependency"):
        errors.append("Daffy's Hills must remain optional and unbundled")
    if policy.get("copyAssets") or policy.get("modifyExternalFiles") or policy.get("copyBinary"):
        errors.append("Daffy's Hills binaries/assets/source must not be copied or modified")
    if integration.get("version") != "1.2":
        errors.append("The documented Daffy's Hills reference version must remain 1.2")

    # Optional external map expansions: never bundle, modify, or reuse third-party files/assets.
    for name, record in (
        ("Pier Expansion", pier_integration),
        ("Unofficial Map Enhancer", ume_integration),
    ):
        policy = record.get("integrationPolicy", {})
        if not record.get("sourcePage"):
            errors.append(f"{name} profile must include its public source page")
        if not policy.get("optionalCompanion") or policy.get("bundled") or policy.get("requiredDependency"):
            errors.append(f"{name} must remain an optional, unbundled companion")
        if policy.get("copyBinary") or policy.get("copyAssets") or policy.get("modifyExternalFiles"):
            errors.append(f"{name} files/assets must not be copied or modified")
        if not policy.get("runtimePlacementGuardRequired"):
            errors.append(f"{name} must require the external-map placement guard")

    pier_files = pier_integration.get("archiveInspection", {}).get("files", [])
    if len(pier_files) != 1 or pier_files[0].get("path") != "PierExpansion.dll":
        errors.append("Pier Expansion inspected archive must remain documented as one DLL only")
    if pier_files and pier_files[0].get("sha256") != "e4d8d17c4e00d5e9bd1da9eb1a818252d4a546b38299717828c06ce7be85dc34":
        errors.append("Pier Expansion archive SHA-256 does not match the inspected user archive")
    if pier_integration.get("archiveLabelVersion") != "1.2.1" or pier_integration.get("authorPublishedVersion") != "1.2.0":
        errors.append("Pier Expansion archive-label/public-page version discrepancy must remain explicit")

    ume_install = ume_integration.get("installationFactsByAuthorDescription", {})
    if len(ume_install.get("replacesOriginalGameAssetFiles", [])) != 5:
        errors.append("UME profile must document all five original game asset replacement files")
    if ume_install.get("gameAssetReplacementVersion") != "0.4.5f2":
        errors.append("UME replacement asset version must remain pinned to the author's 0.4.5f2 description")
    if ume_integration.get("archiveInspection", {}).get("providedByUser") is not False:
        errors.append("UME archive inspection must remain explicitly unverified until the archive is supplied")

    # Keep the runtime detector synchronized with the machine-readable guard manifest.
    detected_mod_files = map_guard.get("detectedModFiles", [])
    if not detected_mod_files or len(detected_mod_files) != len(set(detected_mod_files)):
        errors.append("External map guard DLL names must be present and unique")
    builder_source = (ROOT / "src/Runtime/HarborlinePhysicalBuilder.cs").read_text(encoding="utf-8")
    for file_name in detected_mod_files:
        if f'"{file_name}"' not in builder_source:
            errors.append(f"Runtime placement guard does not detect {file_name}")
    guard_behavior = map_guard.get("behavior", {})
    if not guard_behavior.get("pauseAutomaticHarborlineGeometry") or not guard_behavior.get("pauseHarborlineNpcPositionResolution"):
        errors.append("External map guard must pause both Harborline geometry and NPC position assignment")

    if native_integration.get("regionIntegration", {}).get("harborlineParentNativeRegion") != "Docks":
        errors.append("Native integration contract must map Harborline to Docks")

    # Implementation status IDs are the canonical requirement identifiers.
    requirements = status.get("requirements", [])
    requirement_ids = [requirement.get("id") for requirement in requirements]
    if not requirement_ids or len(requirement_ids) != len(set(requirement_ids)):
        errors.append("Implementation requirement IDs must be present and unique")
    if not status.get("overallVerdict"):
        errors.append("Implementation status must include an overall verdict")

    return errors


if __name__ == "__main__":
    problems = validate()
    if problems:
        for problem in problems:
            print(f"FAIL: {problem}")
        raise SystemExit(1)
    print("OK: Harborline design contracts, survey safety, roster, and integration boundaries are consistent.")
