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
- native-shops.csv – all loaded native shop names and listing counts.
- native-shop-items.csv – each native shop listing, item ID/name/category and shared base purchase price.
- native-items.csv – item registry definitions with category, base purchase price, resale multiplier and stack limit.
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
- The geometry prototype remains single-player-only. The five Harborline NPC classes are derived from S1API.Entities.NPC and are eligible for S1API's own save-loader/FishNet prefab registration path; the optional fallback registrar is single-player-only. Actual host/client spawn, coordinates, late join and save/load still require a runtime test.

## Native integration limits

S1API's public PropertyManager and BusinessManager methods inspected so far enumerate existing records. No supported public hook to register a new Realtor property/business has been confirmed. Normal-map projection, map icons, road/navmesh extension, native doors, shops, online orders, daily NPC routes and police dispatch remain unimplemented.

See docs/implementation-audit.md and world/harborline/implementation-status.json for the requirement-by-requirement status.


## Automatický přehled exportu

Po zkopírování celé složky diagnostiky nebo jejího ZIPu lze soubory shrnout bez ručního otevírání jednotlivých CSV:

```bash
python tools/analyze_runtime_survey.py "/path/to/Diagnostics/<timestamp>" --markdown runtime-survey-report.md --json runtime-survey-report.json
```

Nebo přímo ze ZIPu:

```bash
python tools/analyze_runtime_survey.py "./ScheduleIMegaExpansion-Diagnostics.zip" --markdown runtime-survey-report.md
```

Report vypíše přesný exportovaný záznam Docks Warehouse, nativní regiony, existující obchody a katalog, nemovitosti/podniky, policejní checkpointy/trasy a prefab kandidáty. Nástroj je read-only: nevymýšlí souřadnice, nevytváří nové Realtor nabídky a neoznačuje neověřený prefab za bezpečný pro spawn.
