# Harborline – audit implementace

**Kontrolovaný stav:** větev `feat/harborline-foundation`, vývojová verze 0.3.1.
**Metoda:** kontrola C# implementace, S1API Forked 3.2.2 public source/API, konfiguračních dat a automatických testů. Audit nebyl proveden uvnitř Schedule I hry.

## Co se nově skutečně implementuje přes nativní API

| Oblast | Kód | Co dělá | Ověření |
|---|---|---|---|
| NPC – první vlna | `HarborlineNpcRoster.cs` | 5 fyzických S1API NPC: 2 dealeři, 3 zákazníci | Čeká na spuštění ve hře |
| Dealeři | `WithDealerDefaults` | PlayerDealer, poplatek, provize, pravidla kvality, počitadlo smluv a nativní dealer události | Čeká na nábor a obchodní test |
| Zákazníci | `WithCustomerDefaults` | Tři různé rozpočty, frekvence/čas objednávek, standardy, affinities, policejní šance a dependence | Čeká na test objednávek |
| Vzhled | `Appearance.GenerateRandomAppearance()` + `Build()` | Náhodný vzhled vytvořený herním generátorem při OnCreated | Čeká na render/save-load test |
| NPC scheduler | `Schedule.Enable()` | Zapne nativní scheduler komponentu | Denní trasy zatím nejsou nastavené |
| Sortiment | `HarborlineShopCatalog.cs` | 14 existujících vanilla item definitions přidá do nativního Hardware Store | Čeká na kontrolu shop UI a nákupu |
| Ceny | `StorableItemDefinition.BasePurchasePrice` | Nastaví cenu na existující nativní definici předmětu; `Shop.AddItem` používá tuto cenu | Čeká na herní nákupní test; cena je sdílená napříč obchody používajícími stejné ID |
| Fyzická čtvrť | `HarborlinePhysicalBuilder.cs` | Geometrický prototyp Unity primitiv | Umístění dosud neověřeno ve hře |

### První nativní NPC

**Dealeři**
- Mason Reed – PlayerDealer, nábor $2,500, provize 18 %.
- Sierra Voss – PlayerDealer, nábor $4,000, provize 15 %.

Oba mají nativní dealer defaults, omezení kvality, vlastní completed-deals proměnnou a reakce na události náboru, smlouvy a doporučení.

**Zákazníci**
- Caleb Price – $180–$420 týdenního rozpočtu, 1–2 objednávky týdně, pondělí 06:30, Moderate, afinita Marijuana +0.75 / Methamphetamine −0.25.
- Nina Brooks – $450–$850, 2–3 objednávky týdně, středa 12:00, High, afinita Methamphetamine +0.75 / Marijuana −0.30.
- Rafael Cole – $800–$1,600, 1–3 objednávky týdně, pátek 19:30, VeryHigh, afinita Cocaine +0.70 / Shrooms +0.35 / Methamphetamine −0.30.

Hodnoty jsou uložené v nativním CustomerDataBuilder. Policejní šance a dependence se nastavují individuálně. Kompletní 30členný zákaznický roster zatím dokončený není.

### Sortiment přidaný do nativního Hardware Store

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

## Co zůstává nedokončené

| Oblast | Stav | Co chybí |
|---|---|---|
| Cílových 42 NPC | Částečně | Implementováno 5; zbývá 37 |
| 30 zákaznických profilů | Částečně | Implementováno 3; zbývá 27 |
| Denní režimy NPC | Nedokončeno | Ověřené navmesh trasy, body a native building visits |
| Harborline Supply – nová provozovna | Nedokončeno | S1API veřejně poskytuje čtení existujících shopů a přidání itemu do nich; registrace nového Shop nebyla doložena |
| Online objednávky | Nedokončeno | Není propojená phone order cesta se stejným inventářem a skladovostí |
| Koupitelné podniky / Realtor | Blokováno neověřeným API | `PropertyManager` a `BusinessManager` zpřístupňují existující záznamy; ověřený aditivní registrační hook pro novou Property/Business není znám |
| Nové mapové ikony | Nedokončeno | Chybí ověřený map registry/projekce a vlastnické změny |
| Funkční budovy/interiéry/dveře | Nedokončeno | Současný shell není nativní building a není průchozí interiér |
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
2. V cílové hře ověřit spawn všech pěti NPC, jejich komponenty, vzhled, uložení a další načtení.
3. Ověřit, že Hardware Store zobrazuje všech 14 položek s uvedenými cenami a nákup skutečně odečítá peníze a vydává item.
4. Získat runtime diagnostiky pro Docks property/regions a nativní PropertyManager, BusinessManager, map, shop a building registry.
5. Teprve poté implementovat další NPC, vlastní prodejnu, telefonní objednávky, Realtor listings a jejich propojení se stejnou inventurou.

**Závěr:** PR #13 nyní obsahuje první nativní gameplay slice pro NPC, dealery a rozšíření existujícího obchodu. Nejde ještě o hotový Harborline: nový Realtor business inventory, samostatná Harborline prodejna, online objednávky, plné osídlení 42 NPC a další district systémy zůstávají před námi.