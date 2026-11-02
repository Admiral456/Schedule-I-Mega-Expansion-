# Harborline runtime – implementace a diagnostika

Tento projekt je **runtime prototyp**, nikoli hotové rozšíření Harborline. Obsahuje automatickou tvorbu session-local geometrie z Unity primitiv a samostatné read-only diagnostické exportéry.

## Co je implementováno v kódu

Po události S1API GameLifecycle.OnLoadComplete:
- exportuje nativní regiony, enterable buildings, properties, businesses a podporované standalone phone-map POIs;
- zapisuje metadata API surface z Assembly-CSharp, S1API, S1MAPI a FishNet;
- volá pouze PrefabRef.Find() pro kandidátní prefab references a zapisuje komponenty/network status do native-prefabs.csv;
- pokouší se v single-player najít záznam Docks Warehouse přes S1API.Property.PropertyManager.GetAllProperties() a jeho PropertyName/ExteriorSpawnPosition;
- vyhledává okolní plochu přes Unity Physics.CheckBox a více raycastů, kontroluje sklon/výškový rozdíl a heuristicky odmítá známé water colliders;
- po úspěchu vytváří geometrii z GameObject.CreatePrimitive(Cube) – přístavní plochu, silnici, skladový shell, kontejnery, jeřáb, světla, molo a jednoduché props.

Generování se povolí pouze tehdy, když S1API.Entities.Player.All obsahuje právě jednoho hráče. Ve více hráčích nebo při neznámém stavu player registry se nespouští.

## Co to NEZNAMENÁ

- Unity primitiva nejsou nativní Schedule I Building, Property, Business ani Shop.
- Sklad nemá skutečné nativní dveře, obchodní pokladnu ani funkční inventář.
- Není napojená normální herní mapa ani mapové ikony.
- Neexistují vlastní Harborline NPC třídy ani volání NPC appearance/customer/dealer/schedule API.
- Neexistují online objednávky, Realtor záznamy, policejní hlídky/checkpointy ani synchronizace geometrie.
- Unity objekty jsou session-local; nejsou serializované v save.
- Umístění je heuristika kolem Docks Warehouse; hranice Docks, výsledná souřadnice a kolize ještě nebyly ověřené v cílové hře.

## Diagnostické soubory

Výstup je v Schedule I/UserData/ScheduleIMegaExpansion/Diagnostics/<timestamp>/:
- summary.txt
- native-regions.csv
- native-buildings.csv
- native-properties.csv
- native-businesses.csv
- standalone-phone-map-pois.csv
- native-game-api-surface.txt
- native-prefabs.csv

Zvolená pozice geometrie se zapisuje do UserData/ScheduleIMegaExpansion/Harborline/last-generation.json.

## Build a instalace

~~~powershell
dotnet restore .\ScheduleIMegaExpansion.csproj
dotnet build .\ScheduleIMegaExpansion.csproj -c Release --no-restore
~~~

Požadované runtime dependencies: MelonLoader 0.7.3+, S1API Forked 3.2.2+ a S1MAPI 2.0.1+. Thunderstore manifest je v kořeni repozitáře. CI vytváří ScheduleIMegaExpansion-0.2.0.zip.

## Auditní status

Viz docs/implementation-audit.md a world/harborline/implementation-status.json. Testy ověřují zdrojové invarianty a konfiguraci, nikoli běh v Unity/IL2CPP. Plné ověření vyžaduje spuštění v cílové instalaci Schedule I a kontrolu runtime reportu.
~~~
