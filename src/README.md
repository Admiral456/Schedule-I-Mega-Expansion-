# Harborline runtime – první nativní herní vlna

Tento projekt je stále rozpracované rozšíření. Unity primitiva nejsou nativní Schedule I Building, Property, Business ani Shop. Vedle fyzického Unity prototypu už obsahuje první skutečné herní integrace přes S1API 3.2.3. Zdrojové ověření zatím nenahrazuje herní test.

## Implementováno ve zdrojovém kódu

### Nativní NPC – pilot 11 postav

- `HarborlineOnlineSupplier` je nativní `IsSupplier` NPC Portside Supply. `WithSupplierDefaults` napojuje 14 stejných storable item ID z `HarborlineShopCatalog` do nativního Supplier phone-shop katalogu; S1API vytváří vlastní stash/shop/delivery vehicle a základní herní delivery workflow. Supplier je skrytý během nečinnosti a používá nativní meeting flow. Společná jsou item ID a `BasePurchasePrice`; limitovaný fyzický sklad a synchronizované kusy mezi kanály zatím implementované nejsou. Veřejná `DeliveryRegistry` a `Delivery` API je pouze read-only.
- **Mason Reed** a **Sierra Voss**: fyzické postavy s rolí PlayerDealer. Používají nativní poplatek za nábor, provizi, pravidla kvality, uložení počtu obchodů, zamčený vztah s možností přímého oslovení a nativní události náboru/smluv.
- **Caleb Price**, **Nina Brooks**, **Rafael Cole**, **Olivia Hayes**, **Ethan Ward**, **Maya Bennett**, **Lucas Foster** a **Ava Collins**: fyzičtí zákazníci s individuálním rozpočtem, počtem objednávek týdně, preferovaným dnem a časem, standardy, přístupem, šancí zavolat policii, závislostí a drogovými afinitami.
- Vzhled se vytváří herním generátorem přes `Appearance.GenerateRandomAppearance()` a aplikuje přes `Appearance.Build()`.
- Všech osm zákazníků používá `IsCustomer = true`; v Schedule I 0.4.6+ S1API automaticky nastavuje zákaznické chování pro předávání objednávek. `EnsureDealSignal()` je zastaralá kompatibilní no-op metoda a už ji nepoužíváme. Dealeři používají nativní `DealerAttendDealBehaviour` a vlastní HomeEvent, nikoli náš ruční pohybový loop. `OnCreated` zapíná skutečný `NPC.Schedule.Enable()`.
- **Maya Bennett** má první nativní denní rutinu přes `NPCPrefabBuilder.WithSchedule` a `SitAtSeatSet`: ve 14:30 jde na existující sedací místo `Map/Hyland Point/Region_Docks/WaterFront/OutdoorBench (1)` a zůstane tam 30 herních minut. Polohu, cestu a místo řeší Schedule I/S1API; žádné vlastní `NavMeshAgent` řízení nepřidáváme. Zatím je to zdrojově implementovaný pilot – běh a dosah v konkrétním save čeká na ověření ve hře.
- Diagnostiky nově exportují `harborline-spawn-navigation.csv`, `native-seat-sets.csv` a `native-navmesh-anchor.csv`. Čtou herní `NavMesh.SamplePosition`, `NavMesh.CalculatePath` a reálné `AvatarSeatSet` objekty. Výstupy rozhodnou, které další trasy, místa k sezení a návštěvy budov jsou skutečně dosažitelné.
- Prefaby se registrují automaticky přes S1API/FishNet. Deset tříd NPC je odvozených ze `S1API.Entities.NPC` a používá jeho nativní save-loader/pre-registration tok; doplňkový fallback registrar čeká na `NPC.CustomNpcsReady`, ověřenou pozici a single-player session. ID jsou stabilní a kontrolují se proti `NPC.Get(id)`, aby se nevytvářely duplicity. Host/client spawn zatím nebyl ověřen ve hře.

### Nativní obchod a ceník
`HarborlineShopCatalog` upravuje katalog skutečného **Hardware Store**. Nepoužívá vlastní falešné menu: získá existující `Shop` přes `ShopManager`, načte originální `StorableItemDefinition` z `ItemManager`, nastaví `BasePurchasePrice` na cenovou hodnotu katalogu a přes `Shop.AddItem` zajistí nativní listing. Před přidáním kontroluje duplicitní položky. Neexistující ID v konkrétním herním buildu přeskočí. S1API 3.2.3 parametr `Shop.AddItem(item, customPrice)` ignoruje, proto se používá skutečná cenová vlastnost definice.

| Předmět | ID | Cena |
|---|---|---:|
| Plastic Pot | `plasticpot` | $25 |
| Moisture-Preserving Pot | `moisturepreservingpot` | $65 |
| Air Pot | `airpot` | $145 |
| Suspension Rack | `suspensionrack` | $50 |
| Halogen Grow Light | `halogengrowlight` | $50 |
| LED Grow Light | `ledgrowlight` | $95 |
| Full Spectrum Grow Light | `fullspectrumgrowlight` | $235 |
| Drying Rack | `dryingrack` | $300 |
| Mixing Station | `mixingstation` | $575 |
| Chemistry Station | `chemistrystation` | $1,150 |
| Lab Oven | `laboven` | $1,150 |
| Mixing Station MK2 | `mixingstationmk2` | $2,250 |
| Brick Press | `brickpress` | $900 |
| Cauldron | `cauldron` | $3,500 |

Ceny níže jsou ceny nakonfigurované modem, nikoliv vanilla ceny hry. `BasePurchasePrice` je součást sdílené definice předmětu, takže změna platí všude, kde hra stejné item ID prodává; není to per-shop override. Položky se přidají pouze tehdy, pokud je herní registry skutečně obsahuje.

## Co ještě není hotové

- Zbývajících 31 fyzických NPC z cíle 42 a zbylých 22 profilů ze zamýšlených 30.
- Další denní trasy, příchody do budov, směny dělníků, řidiči, rybáři a turisté. Jeden zákazník má zdrojově definovanou nativní cestu k lavičce; další akce čekají na skutečné záznamy z NavMesh/seat-set diagnostiky a registraci nativních budov.
- Samostatný Harborline Supply jako nová fyzická provozovna. Aktuální funkční integrace doplňuje existující Hardware Store, protože S1API veřejně neposkytuje registraci nového Shop.
- Nové koupitelné podniky a bydlení v Realtoru. Veřejný `PropertyManager` a `BusinessManager` mají čtecí API pro existující záznamy, ale zatím nebyl ověřen bezpečný přidávací hook v cílové verzi Schedule I. Nezabíráme ani nepřepisujeme cizí nemovitosti.
- Online objednávky přes telefon se sdíleným inventářem, mapa/ikony, nativní budovy/interiéry, policejní hlídky, ukládání nové geometrie a multiplayer.

## Ověření a sestavení

- Build cílí na .NET 6 / Schedule I IL2CPP.
- Je vyžadován MelonLoader 0.7.3, S1API Forked 3.2.3 a S1MAPI 2.0.1.
- Pro lokální build stačí `dotnet restore` a `dotnet build`; projekt používá oficiální NuGet balíček `S1API.Forked` 3.2.3. Runtime mod manager stáhne S1API samostatně podle manifestu.
- Kompilace a statické testy neprokazují, že NPC, spawn body a změna nabídky skutečně fungují ve hře. Je potřeba otestovat instalaci v cílové verzi Schedule I.

Diagnostické soubory se ukládají do `UserData/ScheduleIMegaExpansion/Diagnostics/<timestamp>/`. Vedle regionů, budov, nemovitostí a podniků nyní vznikají také `native-shops.csv`, `native-shop-items.csv` a `native-items.csv`; obsahují skutečné názvy obchodů, jejich položky a základní ceny z item registry. Fyzická poloha Harborline se zapisuje do `UserData/ScheduleIMegaExpansion/Harborline/last-generation.json`.

The read-only diagnostics can also be summarized with `python tools/analyze_runtime_survey.py <diagnostics-folder-or-zip> --markdown report.md`.
