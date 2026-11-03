#!/usr/bin/env python3
"""Validate a Schedule I Personnel NPC pack manifest using only Python's stdlib."""

from __future__ import annotations

import argparse
import json
import math
import re
import sys
from pathlib import Path
from typing import Any

TIME_RE = re.compile(r"^(?:[01]\d|2[0-3]):[0-5]\d$")


def _is_number(value: Any) -> bool:
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)


def validate_manifest(data: Any) -> tuple[list[str], dict[str, int]]:
    errors: list[str] = []
    counts = {"npcs": 0, "physical": 0, "contact_only": 0}

    if not isinstance(data, dict):
        return ["manifest root must be a JSON object"], counts

    for field in ("name", "author"):
        if not isinstance(data.get(field), str) or not data[field].strip():
            errors.append(f"manifest.{field} must be a non-empty string")

    if data.get("autoRegister") is not True:
        errors.append("manifest.autoRegister must be true for automatic world registration")

    schema_version = data.get("schemaVersion")
    if schema_version is not None and (not isinstance(schema_version, int) or isinstance(schema_version, bool) or schema_version < 1):
        errors.append("manifest.schemaVersion must be a positive integer when provided")

    npcs = data.get("npcs")
    if not isinstance(npcs, list) or not npcs:
        errors.append("manifest.npcs must be a non-empty array")
        return errors, counts

    counts["npcs"] = len(npcs)
    seen_ids: set[str] = set()

    for index, npc in enumerate(npcs):
        prefix = f"npcs[{index}]"
        if not isinstance(npc, dict):
            errors.append(f"{prefix} must be a JSON object")
            continue

        name = npc.get("name")
        if not isinstance(name, str) or not name.strip():
            errors.append(f"{prefix}.name must be a non-empty string")

        npc_id = npc.get("id")
        if not isinstance(npc_id, str) or not npc_id.strip():
            errors.append(f"{prefix}.id is required; use a stable explicit id before publishing")
        elif npc_id in seen_ids:
            errors.append(f"{prefix}.id duplicates '{npc_id}'")
        else:
            seen_ids.add(npc_id)

        spawn = npc.get("spawn")
        physical = False
        if spawn is not None:
            if not isinstance(spawn, dict):
                errors.append(f"{prefix}.spawn must be an object when provided")
            else:
                physical = spawn.get("physical") is True
                if not isinstance(spawn.get("physical"), bool):
                    errors.append(f"{prefix}.spawn.physical must be explicitly true or false")

                if physical:
                    for axis in ("x", "y", "z"):
                        if not _is_number(spawn.get(axis)):
                            errors.append(f"{prefix}.spawn.{axis} must be a measured finite number")
                    region = spawn.get("region")
                    if not isinstance(region, str) or not region.strip():
                        errors.append(f"{prefix}.spawn.region must be a non-empty in-game region name")
                    counts["physical"] += 1
                else:
                    counts["contact_only"] += 1
        else:
            counts["contact_only"] += 1

        schedule = npc.get("schedule", [])
        if schedule is not None:
            if not isinstance(schedule, list):
                errors.append(f"{prefix}.schedule must be an array")
            else:
                for step_index, step in enumerate(schedule):
                    step_prefix = f"{prefix}.schedule[{step_index}]"
                    if not isinstance(step, dict):
                        errors.append(f"{step_prefix} must be an object")
                        continue

                    action_type = step.get("type")
                    if not isinstance(action_type, str) or not action_type.strip():
                        errors.append(f"{step_prefix}.type must be a non-empty string")

                    time_value = step.get("time")
                    if not isinstance(time_value, str) or not TIME_RE.fullmatch(time_value):
                        errors.append(f"{step_prefix}.time must use 24-hour HH:MM format")

                    if action_type == "walkTo":
                        position = step.get("position")
                        if not (
                            isinstance(position, list)
                            and len(position) == 3
                            and all(_is_number(value) for value in position)
                        ):
                            errors.append(f"{step_prefix}.position must be [x, y, z] with measured finite numbers")

                    if action_type == "stayInBuilding":
                        building = step.get("building")
                        if not isinstance(building, str) or not building.strip():
                            errors.append(f"{step_prefix}.building is required for stayInBuilding")

                    duration = step.get("duration")
                    if duration is not None and (
                        not isinstance(duration, (int, float))
                        or isinstance(duration, bool)
                        or not math.isfinite(duration)
                        or duration <= 0
                    ):
                        errors.append(f"{step_prefix}.duration must be a positive number when provided")

        for optional_object in ("appearance", "customer", "dealer", "supplier", "inventory", "relationships", "contact"):
            value = npc.get(optional_object)
            if value is not None and not isinstance(value, dict):
                errors.append(f"{prefix}.{optional_object} must be an object when provided")

    return errors, counts


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate a Personnel manifest before packaging.")
    parser.add_argument("manifest", type=Path, help="Path to manifest.json exported by Personify or authored from the Personnel Pack Format.")
    args = parser.parse_args()

    try:
        with args.manifest.open("r", encoding="utf-8") as handle:
            data = json.load(handle)
    except FileNotFoundError:
        print(f"ERROR: manifest not found: {args.manifest}", file=sys.stderr)
        return 2
    except json.JSONDecodeError as exc:
        print(f"ERROR: invalid JSON at line {exc.lineno}, column {exc.colno}: {exc.msg}", file=sys.stderr)
        return 2
    except OSError as exc:
        print(f"ERROR: cannot read {args.manifest}: {exc}", file=sys.stderr)
        return 2

    errors, counts = validate_manifest(data)
    if errors:
        for error in errors:
            print(f"ERROR: {error}")
        print(f"Invalid Personnel pack: {len(errors)} issue(s).", file=sys.stderr)
        return 1

    print(
        "Personnel pack OK: "
        f"{counts['npcs']} NPC(s), "
        f"{counts['physical']} physical, "
        f"{counts['contact_only']} contact-only."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
