# Runtime diagnostic build

This is the first compiled-mod foundation for Schedule I Mega Expansion. It is intentionally READ-ONLY.

## What it does

After a save finishes loading, the mod discovers S1API dynamically and exports native registries:
- registered enterable buildings (S1API.Map.Building.GetAll);
- native properties and ownership state (S1API.Property.PropertyManager.GetAllProperties);
- native businesses and ownership state (S1API.Property.BusinessManager.GetAllBusinesses);
- exact native Region enum values;
- standalone phone-map POIs, when the installed S1API assembly exposes that optional API;
- a metadata-only report of native map, region, property, business, NPC, avatar, schedule, door, navigation and law-enforcement types and member signatures.

It does not create or remove buildings, NPCs, POIs, properties or doors. The expanded API scan still reads metadata only and never invokes discovered methods or property getters/setters. It does not change ownership, prices, inventory, map icons or save data.

## Build

Requirements:
- Windows or another platform with the .NET 6 SDK;
- no game assemblies are needed at compile time.

From this folder:

```powershell
dotnet restore .\ScheduleIMegaExpansion.csproj
dotnet build .\ScheduleIMegaExpansion.csproj -c Release --no-restore
```

The GitHub Actions workflow also builds this project and uploads a ZIP artifact named ScheduleIMegaExpansion-runtime-diagnostic.

## Install

Copy bin/Release/net6.0/ScheduleIMegaExpansion.dll into the game's Mods folder, or run:

```powershell
.\install.ps1 -GameDir "C:\Program Files (x86)\Steam\steamapps\common\Schedule I"
```

The game must already have MelonLoader and S1API installed.

## Diagnostic output

After loading a normal save, files are written to:

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
