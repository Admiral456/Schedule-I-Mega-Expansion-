# Runtime diagnostic build

This is the first compiled-mod foundation for Schedule I Mega Expansion. It is intentionally READ-ONLY.

## What it does

After a save finishes loading, the mod exports native registries exposed by S1API:
- registered enterable buildings (Building.GetAll);
- native properties and ownership state (PropertyManager.GetAllProperties);
- native businesses and ownership state (BusinessManager.GetAllBusinesses);
- exact native Region enum values;
- standalone phone-map POIs, when the installed S1API assembly exposes that optional API.

It does not create or remove buildings, NPCs, POIs, properties or doors. It does not change ownership, prices, inventory, map icons or save data.

## Build

Requirements:
- Windows;
- Schedule I installed on the IL2CPP Steam build;
- MelonLoader 0.7.3 or newer;
- S1API Forked installed in the game.

From this folder:

```powershell
.\setup.ps1 -GameDir "C:\Program Files (x86)\Steam\steamapps\common\Schedule I"
dotnet build .\ScheduleIMegaExpansion.csproj -c Release
```

The compile-time S1API package is pinned to 3.1.6, the currently published NuGet package used by this source. Test the resulting DLL with the installed S1API release before distributing it.

Copy bin/Release/net6.0/ScheduleIMegaExpansion.dll into the game's Mods folder.

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

## Why this is needed

The public S1API property and business managers expose existing collections for reading, but do not expose a public method to add a new Property/Business. The diagnostic snapshot records the actual native IDs, names, prices, ownership state and available spawn points so the next implementation can target real game systems instead of inventing identifiers.

MapPOI is a standalone phone-map marker API. It is not equivalent to a native shop/property pin or ownership marker.
