# Harborline runtime prototype

This is the first compiled physical runtime prototype for Schedule I Mega Expansion. It includes a read-only native registry survey and an opt-in physical dockyard geometry slice.

## What it does

After a save finishes loading, the mod discovers S1API dynamically and exports native registries:
- registered enterable buildings (S1API.Map.Building.GetAll);
- native properties and ownership state (S1API.Property.PropertyManager.GetAllProperties);
- native businesses and ownership state (S1API.Property.BusinessManager.GetAllBusinesses);
- exact native Region enum values;
- standalone phone-map POIs, when the installed S1API assembly exposes that optional API;
- a metadata-only report of native map, region, property, business, NPC, avatar, schedule, door, navigation and law-enforcement types and member signatures.

The diagnostic survey does not mutate native registries. The separate F7 prototype action creates real Unity GameObjects with renderers and primitive colliders. It does not yet register a native Building, Property, Business or Shop, and it does not create native doors, NPCs, map icons, inventories or police behaviors.

## Build

Requirements:
- Windows or another platform with the .NET 6 SDK;
- no game assemblies are needed at compile time.

From this folder:

```powershell
dotnet restore .\ScheduleIMegaExpansion.csproj
dotnet build .\ScheduleIMegaExpansion.csproj -c Release --no-restore
```

The GitHub Actions workflow also builds this project and uploads a ZIP artifact named ScheduleIMegaExpansion-harborline-prototype.

## Install

Copy bin/Release/net6.0/ScheduleIMegaExpansion.dll into the game's Mods folder, or run:

```powershell
.\install.ps1 -GameDir "C:\Program Files (x86)\Steam\steamapps\common\Schedule I"
```

The game must already have MelonLoader and S1API installed. The mod discovers S1API dynamically at runtime and uses its save lifecycle to enable the F6/F7 tools.

## Diagnostic output

After loading a normal save, the registry survey is written to:

```
Schedule I/UserData/ScheduleIMegaExpansion/Diagnostics/<timestamp>/
```

Files:
- summary.txt
- native-regions.csv
- native-buildings.csv
- native-properties.csv
- native-businesses.csv
- standalone-phone-map-pois.csv
- native-game-api-surface.txt

## Why this is needed

The public S1API property and business managers expose existing collections for reading. The installed runtime API scan is used to check whether the target game build exposes an additive registration path for new Property/Business records; do not assume one exists. The diagnostic snapshot records the actual native names, codes, prices, ownership state and available spawn points so the next implementation can target real game systems instead of inventing identifiers.

MapPOI is a standalone phone-map marker API. It is not equivalent to a native shop/property pin or ownership marker.

## Physical Harborline prototype (single-player only)

After loading into a save, stand at the intended Harborline connection point inside the existing Docks area and face toward the open space where the prototype should be placed.

- Press **F6** to capture your in-game position and facing direction.
- Press **F7** to create the physical dockyard slice.

The anchor is saved to:

```
Schedule I/UserData/ScheduleIMegaExpansion/Harborline/harborline-anchor.json
```

The geometry is placed in front of the captured anchor and consists of visible Unity cube meshes with primitive colliders: ground slab, road and lane markings, walk-in warehouse shell and interior props, shipping containers, crane structure, lamps, quay, pier and cargo props. The warehouse front has a doorway opening so the interior can be entered.

Important limits:
- F7 builds a physical geometry prototype only. It is not yet a native game Building/Property/Business/Shop.
- The current prototype is intentionally single-player only. It is not replicated to multiplayer clients.
- The generated geometry is session-local and is not saved into the vanilla save.
- Native doors, NPCs, functional business systems, Realtor registration, map projection/icons, police dispatch and multiplayer replication are not yet implemented.
- Capture the anchor in Docks and leave enough clear space in front of it. The prototype is large and can overlap existing structures if placed carelessly.
- Reload the save to reset the one-build-per-session guard.
