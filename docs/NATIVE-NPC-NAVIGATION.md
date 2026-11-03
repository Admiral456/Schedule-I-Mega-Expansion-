# Nativní navigace a denní rozvrhy NPC

## Základní pravidlo

Mega Expansion nebude implementovat vlastní chodící AI, vlastní pathfinder ani vlastní správu laviček/dveří. Fyzické postavy jsou S1API NPC a jejich pohyb musí řídit existující Schedule I komponenty. S1API slouží jako stabilní rozhraní k hernímu scheduleru.

## Použité herní systémy

- `NPCPrefabBuilder.WithSchedule(...)` – deklaruje akce při konfiguraci prefabů. Rozvrh se nesestavuje průběžně vlastním update loopem.
- `PrefabScheduleBuilder.SitAtSeatSet(...)` – vyhledá reálný `AvatarSeatSet`, přidá nativní `NPCEvent_Sit` a použije herní pohyb k dosažení místa.
- `PrefabScheduleBuilder.WalkTo(...)` – nativní `NPCSignal_WalkToLocation`; čas v této API metodě je v minutách od půlnoci (0–1439).
- `PrefabScheduleBuilder.StayInBuilding(...)` – nativní `NPCEvent_StayInBuilding`; použijeme až pro budovu registrovanou v herním Building registry.
- `NPC.Schedule.Enable()` – zapne nativní scheduler NPC.
- `NPC.IsCustomer = true` – v Schedule I 0.4.6+ S1API automaticky doplňuje zákaznické deal-attendance chování. `EnsureDealSignal()` je jen zastaralá kompatibilní no-op.
- Dealeři používají nativní `DealerAttendDealBehaviour` a HomeEvent. Supplier používá nativní supplier meeting/phone flow a nemá vlastní toulavou rutinu.

## První pilot: Maya Bennett

V `HarborlineNpcRoster.cs` je při `ConfigurePrefab` definována nativní akce:

- seat set: `Map/Hyland Point/Region_Docks/WaterFront/OutdoorBench (1)`
- začátek: 14:30 (HHMM formát pro `SitAtSeatSet`)
- délka: 30 herních minut
- teleport při přeskočení akce: vypnutý

To znamená, že Maya musí dojít na skutečné herní sedací místo. Mega Expansion nevytváří lavičku, neřídí její agent a neprovádí warp.

## Diagnostika skutečné navigace

Po načtení save se vytváří:
- `native-navmesh-anchor.csv` – Docks Warehouse anchor a nejbližší bod herního NavMesh.
- `harborline-spawn-navigation.csv` – 11 plánovaných NPC bodů, projekce přes `NavMesh.SamplePosition` a dosažitelnost přes `NavMesh.CalculatePath`.
- `native-seat-sets.csv` – skutečné herní `AvatarSeatSet` objekty, plná cesta ve stromu scény, poloha, NavMesh projekce a dosažitelnost z Docks Warehouse.

Export je read-only. Nepřidává NavMesh, neupravuje sedací místa ani neposouvá NPC. Ochranná pojistka pro Pier Expansion/UME může způsobit, že plánované body nejsou dostupné; v takovém případě diagnostika tuto skutečnost zaznamená.

## Pravidla pro rozšíření dalších tras

1. Použít pouze pozice a seat sety přítomné v aktuální instalaci hry.
2. Pro každý nový `WalkTo` ověřit, že source i destination jsou na herním NavMesh a cesta vrací `PathComplete`.
3. Pro `SitAtSeatSet` používat plnou hierarchickou cestu, pokud jsou názvy sedacích setů duplicitní.
4. `StayInBuilding` přidat až pro skutečnou nativně registrovanou budovu. Procedurální Unity shell není registrovaná budova a sám o sobě není použitelný cíl `StayInBuilding`.
5. Žádné vlastní `NavMeshAgent.SetDestination`, ruční interpolace pozic, teleporty pro běžný pohyb nebo paralelní schedule manager.
6. Nové trasy ověřit v čistém save, po opětovném načtení a v kooperaci.

## Verze S1API

Projekt je připnutý na S1API Forked 3.2.3 – compile-time NuGet a Thunderstore runtime dependency. Vydání 3.2.3 opravuje tvorbu odpovídajících behavior stacků a schedule komponent na co-op klientech a předinicializační přiřazení seat setů. To je relevantní pro síťové NPC a `SitAtSeatSet`, ale stále to nenahrazuje test našeho modu v běžící hře.

## Stav ověření

- Zdrojová definice pilotní akce: hotová.
- S1API 3.2.3 kompilace: ověřuje GitHub Actions.
- Runtime přesun Mayi na lavičku: čeká na herní test.
- Zbývajících 7 zákazníků: čekají na konkrétní seat-sety a trasy z diagnostického exportu.
- Návštěvy nových Harborline interiérů: čekají na registraci nativních Building/Property, funkční dveře a ověřený interiérový path.
- Co-op: rozvrhy používají S1API síťový prefab tok; náš společný běh čeká na herní test.
