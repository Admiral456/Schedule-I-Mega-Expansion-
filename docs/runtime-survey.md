# Runtime world survey

The first runtime build is a read-only diagnostic mod. It exports the real registries exposed by the currently installed Schedule I/S1API build. This is needed before implementing world coordinates, map bounds, building registration or real-estate entries.

## Run

1. Install MelonLoader and S1API Forked in Schedule I.
2. Build the project in src using the instructions in src/README.md.
3. Copy ScheduleIMegaExpansion.dll into Schedule I/Mods.
4. Start the game and load a normal save. The report is automatically written after the world load finishes.

## Output

Schedule I/UserData/ScheduleIMegaExpansion/Diagnostics/<timestamp>/

- native-regions.csv: the exact Region enum names and numeric values compiled from the S1API assembly.
- native-buildings.csv: names registered as enterable native game buildings.
- native-properties.csv: property name/code/price/ownership and exposed spawn locations.
- native-businesses.csv: business name/code/price/ownership and exposed spawn locations.
- standalone-phone-map-pois.csv: phone-map POI markers, if supported by the installed S1API build.
- summary.txt: assembly version, counts and errors.

## Safety

- The build does not spawn or move anything.
- It does not alter ownership, prices, stock, NPCs, map markers or save data.
- It does not infer world coordinates from the concept map.
- Standalone POIs are not equivalent to native shop/business/property map pins.
- S1API's Region enum currently exposes the original six game regions. Harborline is therefore planned as a physical subdistrict inside Docks, not a fabricated enum value. Its eventual coordinates still need to be checked against the Docks region/navigation boundaries.
- A successful export only inventories native registries; it does not prove that custom property registration is possible through public API.
