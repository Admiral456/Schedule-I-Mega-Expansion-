# Runtime world survey

The first runtime build is a read-only diagnostic mod. It discovers S1API at runtime and exports the registries exposed by the installed Schedule I/S1API build. It does not need Schedule I game assemblies to compile. The .NET 6 build and ZIP artifact have now succeeded in GitHub Actions; the mod has not yet been run inside the game.

## Build and install

1. Build the project in src with the .NET 6 SDK, as described in src/README.md, or download the GitHub Actions artifact named ScheduleIMegaExpansion-runtime-diagnostic.
2. Copy ScheduleIMegaExpansion.dll into Schedule I/Mods. The install.ps1 helper can copy it for you.
3. Start the game and load a normal save. The report is automatically written after the native S1API save-load lifecycle signals completion.

## Output

Schedule I/UserData/ScheduleIMegaExpansion/Diagnostics/<timestamp>/

- native-regions.csv: the exact Region enum names and numeric values from the installed S1API.
- native-buildings.csv: names registered as enterable native game buildings.
- native-properties.csv: property name/code/price/ownership and exposed spawn locations.
- native-businesses.csv: business name/code/price/ownership and exposed spawn locations.
- standalone-phone-map-pois.csv: phone-map POI markers, if supported by the installed S1API build.
- native-game-api-surface.txt: metadata-only report of relevant game map, region, property and business types/member signatures.
- summary.txt: assembly version, counts and errors.

## Safety

- The build does not spawn or move anything.
- It does not alter ownership, prices, stock, NPCs, map markers or save data.
- It does not infer world coordinates from the concept map.
- Standalone POIs are not equivalent to native shop/business/property map pins.
- S1API's Region enum exposes the original six game regions. Harborline is therefore planned as a physical subdistrict inside Docks, not a fabricated enum value. Its eventual coordinates still need to be checked against the Docks region/navigation boundaries.
- A successful export only inventories native registries; it does not prove that custom property registration is possible through public API.
