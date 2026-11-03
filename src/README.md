# Harborline runtime – první nativní herní vlna

Tento projekt je stále rozpracované rozšíření. Unity primitiva nejsou nativní Schedule I Building, Property, Business ani Shop. Vedle fyzického Unity prototypu už obsahuje první skutečné herní integrace přes S1API 3.2.2. Zdrojové ověření zatím nenahrazuje herní test.

## Implementováno ve zdrojovém kódu

### Nativní NPC – pilot 5 postav
- **Mason Reed** a **Sierra Voss**: fyzické postavy s rolí PlayerDealer. Používají nativní poplatek za nábor, provizi, pravidla kvality, uložení počtu obchodů, zamčený vztah s možností přímého oslovení a nativní události náboru/smluv.
- **Caleb Price**, **Nina Brooks**, **Rafael Cole**: fyzičtí zákazníci s různým rozpočtem, počtem objednávek týdně, preferovaným dnem a časem, standardy, přístupem, šancí zavolat policii, závislostí a drogovými afinitami.
- Vzhled se vytváří herním generátorem přes `Appearance.GenerateRandomAppearance()` a aplikuje přes `Appearance.Build()`.
- Nativní schedule engine se zapíná přes `Schedule.Enable()`. Konkrétní chození po čtvrti zatím není nastavené, protože nemáme ověřené navmesh trasy a funkční budovy.
- Prefaby se registrují automaticky přes S1API/FishNet. Fallback registrar čeká na `NPC.CustomNpcsReady`, ověřenou pozici a single-player session; ID jsou stabilní a kontrolují se proti `NPC.Get(id)`, aby se nevytvářely duplicity.

### Nativní obchod a ceník
`HarborlineShopCatalog` upravuje katalog skutečného **Hardware Store**. Nepoužívá vlastní falešné menu: získá existující `Shop` přes `ShopManager`, načte originální `StorableItemDefinition` z `ItemManager`, nastaví `BasePurchasePrice` na cenovou hodnotu katalogu a přes `Shop.AddItem` zajistí nativní listing. Před přidáním kontroluje duplicitní položky. Neexistující ID v konkrétním herním buildu přeskočí. S1API 3.2.2 parametr `Shop.AddItem(item, customPrice)` ignoruje, proto se používá skutečná cenová vlastnost definice.

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

- Zbývajících 37 fyzických NPC z cíle 42 a zbylých 27 profilů ze zamýšlených 30.
- Denní směny dělníků, trasy řidičů, rybářské a turistické chování. Nativní scheduler je zapnutý, ale bez ověřených bodů a dveří nelze tvrdit, že NPC chodí po čtvrti.
- Samostatný Harborline Supply jako nová fyzická provozovna. Aktuální funkční integrace doplňuje existující Hardware Store, protože S1API veřejně neposkytuje registraci nového Shop.
- Nové koupitelné podniky a bydlení v Realtoru. Veřejný `PropertyManager` a `BusinessManager` mají čtecí API pro existující záznamy, ale zatím nebyl ověřen bezpečný přidávací hook v cílové verzi Schedule I. Nezabíráme ani nepřepisujeme cizí nemovitosti.
- Online objednávky přes telefon se sdíleným inventářem, mapa/ikony, nativní budovy/interiéry, policejní hlídky, ukládání nové geometrie a multiplayer.

## Ověření a sestavení

- Build cílí na .NET 6 / Schedule I IL2CPP.
- Je vyžadován MelonLoader 0.7.3, S1API Forked 3.2.2 a S1MAPI 2.0.1.
- Pro lokální build stačí `dotnet restore` a `dotnet build`; projekt používá oficiální NuGet balíček `S1API.Forked` 3.2.2. Runtime mod manager stáhne S1API samostatně podle manifestu.
- Kompilace a statické testy neprokazují, že NPC, spawn body a změna nabídky skutečně fungují ve hře. Je potřeba otestovat instalaci v cílové verzi Schedule I.

Diagnostické soubory se ukládají do `UserData/ScheduleIMegaExpansion/Diagnostics/`; fyzická poloha Harborline do `UserData/ScheduleIMegaExpansion/Harborline/last-generation.json`.