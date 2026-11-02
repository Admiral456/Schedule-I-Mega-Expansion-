# Runtime survey and physical prototype

The current build is a mixed runtime mod:
- diagnostic exporters are read-only and inventory installed native registries/API signatures;
- the automatic Harborline prototype creates session-local Unity geometry in a confirmed single-player session.

The .NET 6 build and CI tests validate compilation, package layout and source-level invariants. They do **not** prove that the mod runs correctly in Schedule I. No user game runtime report has been inspected yet.

## Install and run

1. Install MelonLoader 0.7.3+, S1API Forked 3.2.2+ and S1MAPI 2.0.1+.
2. Copy ScheduleIMegaExpansion.dll to Schedule I/Mods or use src/install.ps1.
3. Start a single-player save.
4. After the native S1API OnLoadComplete lifecycle event, the mod exports diagnostics and attempts automatic geometry generation.
5. Inspect logs and the generated reports. If no safe placement is found, the builder logs a warning and does not create geometry.

## Diagnostic output

Schedule I/UserData/ScheduleIMegaExpansion/Diagnostics/<timestamp>/

- summary.txt – API version, export counts and errors.
- native-regions.csv – installed Region enum values.
- native-buildings.csv – names from S1API Building lookup.
- native-properties.csv – existing property names/codes/prices/ownership/spawn positions.
- native-businesses.csv – existing business records and ownership.
- standalone-phone-map-pois.csv – standalone phone-map POIs if available.
- native-game-api-surface.txt – metadata-only signatures from Assembly-CSharp, S1API, S1MAPI and FishNet.
- native-prefabs.csv – candidate prefab references found through S1MAPI PrefabRef.Find() and the component/network inspection status.

The chosen prototype geometry anchor is recorded in Schedule I/UserData/ScheduleIMegaExpansion/Harborline/last-generation.json.

## Runtime safety

- The physical builder only proceeds when S1API.Entities.Player.All contains exactly one player.
- It anchors from the native Docks Warehouse property position, not a 2D concept map.
- It scans nearby candidate sites and requires collision-free footprint checks plus multiple ground raycasts.
- It rejects known water collider names/tags/layers, but this is a heuristic and does not guarantee detection of unnamed water surfaces.
- The generated objects are session-local Unity primitives. They are not native registered buildings and are not saved into the game's save.
- Multiplayer, networked prefab instantiation and custom NPC spawning are intentionally not enabled.

## Native integration limits

S1API's public PropertyManager and BusinessManager methods inspected so far enumerate existing records. No supported public hook to register a new Realtor property/business has been confirmed. Normal-map projection, map icons, road/navmesh extension, native doors, shops, online orders, NPC behavior and police dispatch remain unimplemented.

See docs/implementation-audit.md and world/harborline/implementation-status.json for the requirement-by-requirement status.
