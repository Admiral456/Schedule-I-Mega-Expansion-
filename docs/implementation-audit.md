# Harborline – audit implementace

**Kontrolovaný stav:** větev `feat/harborline-foundation`, commit `5f862a62030fd78dcbe5a61939794a194c2672c8`, vývojová verze 0.3.1.
**Metoda:** kontrola C# implementace, S1API Forked 3.2.2 public source/API, konfiguračních dat a automatických testů. Audit nebyl proveden uvnitř Schedule I hry.

## Co se nově skutečně implementuje přes nativní API

| Oblast | Kód | Co dělá | Ověření |
|---|---|---|---|
| NPC – první vlna | `HarborlineNpcRoster.cs` | 11 fyzických S1API NPC: 2 dealeři, 8 zákazníků a 1 supplier Portside Supply | Čeká na spuštění ve hře |
| Dealeři | `WithDealerDefaults` | PlayerDealer, poplatek, provize, pravidla kvality, počitadlo smluv a nativní dealer události | Čeká na nábor a obchodní test |
| Zákazníci | `WithCustomerDefaults` | Osm individuálních rozpočtů, frekvencí/časů objednávek, standardů, affinities, policejních šancí a dependence | Čeká na test objednávek |
| Vzhled | `Appearance.GenerateRandomAppearance()` + `Build()` | Náhodný vzhled vytvořený herním generátorem při OnCreated | Čeká na render/save-load test |
| Portside Supply | `HarborlineDeadDropSupplier` | Nativní Supplier NPC s limity objednávek $25–$8,000 a 14 existujícími item ID pro dead-drop/meeting-shop flow; není to samostatný storefront ani synchronizovaný sklad Hardware Store | Čeká na ověření native supplier/dead-drop nabídky a doručení ve hře |
| NPC scheduler | `WithSchedule`, `Schedule.Enable()` | Zákaznické čekání na handover je automatické pro `IsCustomer=true`; zastaralé `EnsureDealSignal()` jsme odstranili. Dealeři používají automatické `DealerAttendDealBehaviour` v cílové herní verzi. `HandleDeal()` je zastaralé a `NPCSchedule.InitializeActions()` v S1API 3.2.2 není dostupné. | Základní zákaznické handover chování poskytuje hra; pěší trasy a návštěvy budov čekají na ověřené navmesh body a nativní budovy |
| Sortiment | `HarborlineShopCatalog.cs` | 14 existujících vanilla item definitions přidá do nativního Hardware Store | Čeká na kontrolu shop UI a nákupu |
| Ceny | `StorableItemDefinition.BasePurchasePrice` | Nastaví cenu na existující nativní definici předmětu; `Shop.AddItem` používá tuto cenu | Čeká na herní nákupní test; cena je sdílená napříč obchody používajícími stejné ID |
| Fyzická čtvrť/sklad | `HarborlinePhysicalBuilder.cs`, `HarborlineProceduralBuildingFactory.cs` | Dockyard geometrie zůstává prototyp; sklad používá S1MAPI BuildingBuilder s TerrainClearer/FlattenTerrain, InteriorBuilder vizuální dekorací a primitive fallbackem. Harbor Market zkouší původní Docks clothing-store render shell | Umístění, vstup, navigace i vizuální zdroje dosud neověřeny ve hře |

### První nativní NPC

**Dealeři**
- Mason Reed – PlayerDealer, nábor $2,500, provize 18 %.
- Sierra Voss – PlayerDealer, nábor $4,000, provize 15 %.

Oba mají nativní dealer defaults, omezení kvality, vlastní completed-deals proměnnou a reakce na události náboru, smlouvy a doporučení.

**Zákazníci**
- Caleb Price – $180–$420, 1–2 objednávky týdně, pondělí 06:30, Moderate.
- Nina Brooks – $450–$850, 2–3 objednávky týdně, středa 12:00, High.
- Rafael Cole – $800–$1,600, 1–3 objednávky týdně, pátek 19:30, VeryHigh.
- Olivia Hayes – $260–$560, 1–3 objednávky týdně, úterý 09:00, Moderate.
- Ethan Ward – $600–$1,100, 2–4 objednávky týdně, čtvrtek 17:40, High.
- Maya Bennett – $120–$300, 1–2 objednávky týdně, sobota 11:30, Low.
- Lucas Foster – $900–$1,750, 1–2 objednávky týdně, neděle 20:10, VeryHigh.
- Ava Collins – $350–$720, 2–3 objednávky týdně, pátek 15:15, High.

Všech osm profilů navíc nastavuje individuální policejní šanci, dependence a drogové afinity v `WithCustomerDefaults`.

Hodnoty jsou uložené v nativním CustomerDataBuilder. Policejní šance a dependence se nastavují individuálně. Zákaznické handover chování se aktivuje automaticky přes `IsCustomer`; `EnsureDealSignal()` je v aktuálním API no-op a v kódu se nepoužívá. Kompletní 30členný zákaznický roster zatím dokončený není.

### Sortiment a cenová vrstva nativního Hardware Store

| Předmět / item ID | Cena |
|---|---:|
| Plastic Pot / `plasticpot` | $25 |
| Moisture-Preserving Pot / `moisturepreservingpot` | $65 |
| Air Pot / `airpot` | $145 |
| Suspension Rack / `suspensionrack` | $50 |
| Halogen Grow Light / `halogengrowlight` | $50 |
| LED Grow Light / `ledgrowlight` | $95 |
| Full Spectrum Grow Light / `fullspectrumgrowlight` | $235 |
| Drying Rack / `dryingrack` | $300 |
| Mixing Station / `mixingstation` | $575 |
| Chemistry Station / `chemistrystation` | $1,150 |
| Lab Oven / `laboven` | $1,150 |
| Mixing Station MK2 / `mixingstationmk2` | $2,250 |
| Brick Press / `brickpress` | $900 |
| Cauldron / `cauldron` | $3,500 |

Kód používá `ItemManager.GetDefinition` a upravuje `StorableItemDefinition.BasePurchasePrice`, tedy skutečný zdroj ceny, ze kterého nativní obchod počítá nákup. Poté ověří existenci položky v Hardware Store a případně zavolá `Shop.AddItem(item)`. Neznámé item IDs přeskočí. **Pozor:** tato cena patří sdílené definici předmětu, takže ji použijí všechny nativní obchody prodávající stejné item ID. S1API 3.2.2 parametr `Shop.AddItem(item, customPrice)` ve zdrojovém kódu ignoruje; mod na něj již nespoléhá.

### Procedurální budova a focused API scan

`HarborlineProceduralBuildingFactory.cs` vytváří sklad přes S1MAPI `BuildingBuilder`/`BuildingConfig`, před stavbou zkouší `TerrainClearer.ClearAroundBuilding` a `FlattenTerrain()`, následně přidává statické mesh dekorace přes S1MAPI `InteriorBuilder` (desk, chair, cabinet, locker, fridge, plant), posuvné dveře přes `AddSlidingDoors` a vnitřní pathfinding přes `NavigationBuilder`. Při chybě se zachová původní geometrický fallback. Vnitřní dekorace jsou vizuální, nikoli funkční pokladna/sklad. Vytvořená budova není automaticky registrovanou nativní Property/Business nebo novým Shop. `HarborlineIntegrationApiScanner.cs` rozšiřuje read-only diagnostiku o přesné podpisy metod/událostí pro Realtor, Property, Business, dveře, NPC movement, A* graphy, Navigation, road, seat-set, transport a map API. `NativeScenePrefabExporter.cs` hledá přesné vanilla hierarchy paths. `NativeTerrainSurveyExporter.cs` čte terénní rozměry, vrstvy a výškovou mřížku. `NativeRoadGraphExporter.cs` exportuje všechny uzly z `Map/Pathfinding/RoadNodes` včetně world/local pozic.

### Nová diagnostika obchodů a položek

`WorldSnapshotExporter` nyní čte přes `ShopManager.GetAllShops()` a `ItemManager.GetAllItemDefinitions()` tři soubory: `native-shops.csv`, `native-shop-items.csv` a `native-items.csv`. Samostatný `NativeLawSnapshotExporter` přidává šest read-only souborů: `native-law-controller.csv`, `native-law-manager.csv`, `native-law-checkpoint-locations.csv`, `native-law-checkpoints.csv`, `native-law-foot-patrol-routes.csv` a `native-law-vehicle-patrol-routes.csv`. Jde pouze o čtení nativních registrů; export nespouští hlídky, nepovoluje checkpointy a nemění herní stav.

### Rozšířené používání vanilla mapových zdrojů podle Daffy's Hills

`HarborlineNativeVisualReuse.cs` nyní používá původní Docks clothing-store render mesh jako vizuální shell Harbor Market, Suburbia streetlight pro lampy, Uptown OutdoorBench pro vzhled laviček a vanilla Bus Stop pro vzhled zastávky. Kopírují se jen MeshFilter/MeshRenderer/Light; neklonují se collidery, scripts, seat sets, doors ani NetworkObjects. Při chybě je vlastní geometrický fallback. Vizuální lavička není sedátko, autobusová zastávka nezíská dopravní logiku a Harbor Market není funkční Shop.

`NativeScenePrefabExporter` ukládá zdrojové prefab paths a hierarchii do `native-scene-prefabs.csv`; `NativeRoadGraphExporter` exportuje jednotlivé RoadNodes do `native-road-nodes.csv`; `NativeTerrainSurveyExporter` exportuje terrain dimensions/layers/height samples do tří CSV. Všechny exportéry pouze čtou běžící scénu.

## Co zůstává nedokončené

| Oblast | Stav | Co chybí |
|---|---|---|
| Cílových 42 NPC | Částečně | Implementováno 11; zbývá 31 |
| 30 zákaznických profilů | Částečně | Implementováno 8; zbývá 22 |
| Denní režimy NPC | Částečně implementováno | Zákaznický handover řeší automaticky herní `IsCustomer` chování a dealeři používají automatické herní dealer chování; chybí ověřené navmesh trasy, body a native building visits |
| Harborline Supply – fyzická provozovna | Nedokončeno | Nativní supplier/dead-drop kontakt Portside Supply je kódově připraven; fyzická budova ještě není registrovaná jako nativní Shop/Business a nemá funkční pokladnu |
| Online objednávky | Nedokončeno | Supplier NPC využívá dead-drop/meeting-shop flow; samostatná Hyland Network aplikace a plně funkční online objednávky nejsou implementované |
| Koupitelné podniky / Realtor | Blokováno neověřeným API | `PropertyManager` a `BusinessManager` zpřístupňují existující záznamy; ověřený aditivní registrační hook pro novou Property/Business není znám |
| Nové mapové ikony | Nedokončeno | Chybí ověřený map registry/projekce a vlastnické změny |
| Procedurální sklad/interiér/dveře | Kód implementován, runtime neověřen | S1MAPI BuildingBuilder, TerrainClearer/FlattenTerrain, InteriorBuilder statické dekorace, AddSlidingDoors a vnitřní NavigationBuilder jsou zapojené v kódu, ale jejich výsledek není ve hře otestovaný. Harbor Market znovu používá jen render mesh Docks obchodu; nic z jeho interakcí není klonováno. Není to registrace nativní Property/Business |
| Policie | Nedokončeno | Není připojený nativní dispatch/patrol/checkpoint flow |
| Save geometrie | Nedokončeno | Fyzická geometrie je zatím session-local |
| Multiplayer | Zablokováno | NPC a geometrie jsou v této pilotní fázi omezené na single-player; host/client/late join nebyl testován |

## Bezpečnost a integrace

- S1API automaticky registruje custom NPC prefaby. Kód čeká na `NPC.CustomNpcsReady` a nepokouší se o ruční legacy preregistraci.
- Každé NPC má stabilní ID; před vytvořením se kontroluje `NPC.Get(id)`.
- NPC spawn je navázán na stejnou vybranou pozici geometrie a používá nativní region `Docks`.
- Multi-player gate zůstává fail-closed, dokud neproběhne test server-authority a late join.
- Obchod upravuje jen existující Hardware Store. Nezakládá vlastní nákupní rozhraní ani falešné produkty.
- Nové Realtor listings nebudou vytvářeny zápisem do interních seznamů bez ověřené nativní registrační cesty.

## Ověření před vydáním

1. Build proti S1API 3.2.2 a odpovídajícím Schedule I IL2CPP references musí projít v CI.
2. V cílové hře ověřit spawn všech 11 NPC (2 dealeři, 8 zákazníků, 1 supplier), jejich komponenty, vzhled, uložení a další načtení.
3. Ověřit, že Hardware Store zobrazuje všech 14 položek s uvedenými cenami a nákup skutečně odečítá peníze a vydává item.
4. Získat runtime diagnostiky pro Docks property/regions a nativní PropertyManager, BusinessManager, map, shop a building registry.
5. Teprve poté implementovat další NPC, vlastní prodejnu, telefonní objednávky, Realtor listings a jejich propojení se stejnou inventurou.

**Závěr:** PR #13 nyní obsahuje 11 nativních S1API NPC (2 dealeři, 8 zákazníků, 1 supplier), rozšíření Hardware Store a nativní telefonní/delivery supplier základ. Sdílená skladovost, nový fyzický obchod, Realtor business inventory, plné osídlení 42 NPC a další district systémy zůstávají před námi. Nejde ještě o hotový Harborline: nový Realtor business inventory, samostatná Harborline prodejna, online objednávky, plné osídlení 42 NPC a další district systémy zůstávají před námi.