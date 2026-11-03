# Schedule I Mega Expansion

> **Stav: rozpracovaný základ Harborline.** Tento repozitář je zdrojem herního rozšíření, nikoli webového pomocníka Schedule-I.

Cílem je přirozeně rozšířit Hyland Point o nové obyvatelné a funkční oblasti. Vývoj staví na existujících systémech Schedule I: skutečných NPC, zákaznické ekonomice, rozvrzích, budovách, interakcích, ukládání a multiplayerové synchronizaci. Vlastní AI obyvatel ani druhý systém ekonomiky vytvářet nebudeme.

## Principy

1. **Nejdřív znovu použít hru.** Preferujeme existující prefaby, budovy, nábytek, světla, silniční prvky a herní interakce. Vlastní geometrii použijeme jen tam, kde ve hře vhodný prvek neexistuje.
2. **NPC jako data.** Harborline už obsahuje první runtime vlnu jedenácti nativních S1API NPC: dva PlayerDealers, osm zákazníků a jeden nativní supplier Portside Supply. Dalších 31 postav z cílového rosteru je stále plánovaných; systém se rozšiřuje postupně po ověření ve hře.
3. **Žádné vymyšlené souřadnice.** Prototyp používá pozici existující Docks Warehouse a fyzikální kontroly Unity. Přesné hranice Docks a výsledné umístění zatím nejsou ověřené v cílové hře.
4. **Host je autorita.** Síťové změny a stav světa musí respektovat síťové/ukládací mechanismy hry a S1API.
5. **Po malých ověřitelných krocích.** Nejdřív funkční Harborline, až potom další provincie a rozšiřování mobilní sítě.

## První oblast: Harborline

Harborline je pojmenovaná podčtvrť původního herního regionu **Docks**; nevytváříme nepodporovanou novou hodnotu region enum. Plánované dominanty: Portside Supply, Harbor Freight Warehouse, Harbor Market, Harbor Auto Works, Harbor Offices, Harbor Power Plant, Pier Promenade a policejní kontrolní bod. Seznam je v `world/harborline/anchors.json`; dosud neobsahuje herní souřadnice a není určen k přímému spawnu.

NPC role a cílové počty jsou v `packs/Harborline/roster.json`: nově 42 fyzických NPC, z toho zhruba 18 současně aktivních, 30 zákaznických profilů a 2 dealeři. Harborline je rozdělený do pěti propojených podčástí. Detail rozšíření je v `world/harborline/expansion-plan.json`.

## První nativní herní obsah

- `HarborlineNpcRoster.cs` registruje dva fyzické dealery (Mason Reed, Sierra Voss) přes `IsDealer` a `WithDealerDefaults`, včetně náborového poplatku, provize, kvality a nativních událostí náboru/smluv.
- Portside Supply je samostatný nativní Supplier NPC, který je vložen do existujícího supplier/dead-drop/meeting-shop toku hry. Těchto 14 item ID se předává do nativního supplier katalogu, ale neznamená to nový samostatný fyzický obchod ani hotovou Hyland Network online objednávkovou aplikaci. Zásoby supplieru nejsou prokazatelně sdílené s Hardware Store. Vlastní online storefront a jednotná skladová evidence zůstávají nedokončené.

Osm fyzických zákaznických NPC (Caleb Price, Nina Brooks, Rafael Cole, Olivia Hayes, Ethan Ward, Maya Bennett, Lucas Foster a Ava Collins) má individuální rozpočty, týdenní počet objednávek, den/čas, standardy, šanci zavolat policii, závislost a afinity k typům drog.
- Vzhled se vytváří herním generátorem přes `Appearance.GenerateRandomAppearance()` a `Appearance.Build()`. Zákaznické čekání na obchodní handover se v aktuální hře nastavuje automaticky přes `IsCustomer`; `EnsureDealSignal()` je od hry 0.4.6 pouze zastaralý no-op, proto ho nepoužíváme. Scheduler se zapíná přes `Schedule.Enable()`. Pěší trasy a návštěvy budov zatím čekají na zaměření navmeshe.
- `HarborlineShopCatalog.cs` upravuje ceny 14 existujících vanilla předmětů přímo v nativním `StorableItemDefinition.BasePurchasePrice` a zajišťuje jejich listing v existujícím Hardware Store přes `Shop.AddItem`. Používá originální nákupní UI. Cena definice je sdílená napříč všemi obchody používajícími stejné item ID; nejde o per-shop cenu.
- Vlastní NPC typy jsou definované přes S1API a používají jeho nativní prefab, save/load a FishNet pipeline. Doplňkový fallback registrar je omezený na single-player, aby nevytvářel duplicitní NPC mimo ověřený serverový tok. Runtime ve hře zatím nebyl ověřen; denní chození po čtvrti zatím není nakonfigurované.

## Zatím blokované / další fáze

- Zbývajících 31 NPC, rozšíření na 42 fyzických postav a dalších 22 zákaznických profilů do cílového počtu 30.
- Nativní denní trasy a návštěvy budov – zatím nejsou zaměřené a otestované navmesh body a interiéry.
- Vlastní fyzický Harborline obchod a online objednávky přes telefon se společným inventářem. Aktuální implementace rozšiřuje skutečný vanilla Hardware Store, nikoli novou provozovnu.
- Nové koupitelné podniky a byty přes Realtor. Veřejné S1API PropertyManager/BusinessManager pouze čtou existující registrace; přidávací hook není zatím prokázán. Nepřepisujeme cizí nemovitosti ani nefalšujeme realitní nabídky.
- Nativní mapové ikony, funkční interiérové objekty a otestované dveřní interakce, policejní dispatch/hlídky, ukládání geometrie a multiplayer.
- Detailní kontrakt a testovací brány: `docs/npc-generation-and-multiplayer.md`.

## Technologická vrstva

- **MelonLoader + S1API Forked** – nativní NPC, zákaznická/dealerská ekonomika, vzhled, rozvrhy, předměty a obchody.
- **S1MAPI** – budovy a interiéry pouze pro prvky, které nelze vhodně znovu použít z původní hry.
- Projekt používá oficiální compile-time balíček `S1API.Forked` 3.2.2 z NuGetu. Hra načítá vlastní IL2CPP runtime sestavení S1API z deklarované Thunderstore závislosti; S1API DLL se do našeho ZIPu nepřibaluje. Viz [S1API Custom NPCs](https://ifbars.github.io/S1API/docs/custom-npcs.html), [NPCAppearance](https://ifbars.github.io/S1API/api/S1API.Entities.NPCAppearance.html) a [S1MAPI](https://ifbars.github.io/S1MAPI/).

## Instalace a závislosti

Pro mod manager je připraven manifest `manifest.json`. Balíček obsahuje samotný DLL a automaticky vyžaduje MelonLoader 0.7.3, S1API Forked 3.2.2 a S1MAPI 2.0.1. Všechny tři jsou samostatné knihovny; DLL modu je neobsahuje. Diagnostický export nyní navíc vypisuje `native-shops.csv`, `native-shop-items.csv`, `native-items.csv`, `native-law-*.csv`, `native-scene-prefabs.csv`, `native-terrain-surfaces.csv`, `native-terrain-layers.csv` a `native-terrain-height-samples.csv`. Exporty pomáhají ověřit skutečné herní prefab cesty, road-node graf, výškový profil terénu a policejní checkpointy konkrétní instalace.

## Aktuální stav a limity

- Runtime vytváří první fyzickou prototypovou část. Sklad Portside Supply se nově pokouší sestavit přes S1MAPI `BuildingBuilder` včetně podlahy, stěn, střechy, osvětlení a server-side posuvných dveří; při nekompatibilním API se použije původní Unity geometrický fallback. Základní volání budovy a vnitřní `NavigationBuilder` nejsou zatím ověřené v cílové hře. Po načtení save nezávisle zkouší aktivovat nativní NPC a katalog Hardware Store; katalog již není blokován tím, zda se podařilo vytvořit fyzickou geometrii. Vše je zatím ověřené pouze zdrojovou kontrolou, nikoli během v cílové hře.
- Umístění se odvozuje z nativní pozice Docks Warehouse a runtime fyzikálních kontrol. To ale samo o sobě nezaručuje, že kandidát leží uvnitř hranic Docks nebo že není nad vodou s neoznačeným colliderem.
- Vlastní Realtor/property registrace, mapové ikony, funkční interiérové předměty, nové fyzické obchody v Harborline, online objednávky, policie, ukládání geometrie a multiplayer nejsou v runtime implementované. S1MAPI budova je jen procedurální struktura; automaticky z ní nedělá nativní `Building`, `Property`, `Business` nebo `Shop`. Veřejné S1API PropertyManager a BusinessManager zatím pouze enumerují existující záznamy. Vanilla Hardware Store má připravený katalog 14 existujících položek; jejich nativní definice dostanou nastavené ceny. Změna ceny je globální pro stejné item ID, ne pouze pro Hardware Store.
- GitHub Actions sestavuje IL2CPP mod a balí Thunderstore ZIP. Statické testy a build neprokazují chování ve hře. Diagnostické exportéry pouze čtou registry, ale celý mod navíc vytváří session-local Unity geometrii.

## Struktura

- `docs/architecture.md` – technická architektura a pořadí implementace.
- `docs/prefab-reuse-rules.md` – pravidla pro opakované použití vanilla prefabů a multiplayer.
- `docs/harborline-survey-checklist.md` – přesný postup zaměření spawnu, tras, budov a checkpointů ve hře.
- `docs/npc-generation-and-multiplayer.md` – náhodné preference, nativní generování vzhledu, policie a multiplayer.
- `docs/native-map-buildings-interiors.md` – herní mapa, nativní ikony/vlastnictví, dveře a funkční interiéry.
- `docs/daffys-hills-reference-study.md` – technická studie rozšíření mapy Daffy's Hills; pouze architektonická inspirace, bez převzetí kódu či assetů.
- `world/harborline/native-integration.json` – nativní integrační pravidla a kontrolní brány.
- `src/` – IL2CPP runtime prototyp, automatická geometrie, bezpečné opětovné použití render meshů původní hry a read-only průzkum nativních registrů, terénu, road nodes a prefabů.
- `world/harborline/anchors.json` – seznam lokalit a míst k zaměření.
- `world/harborline/expansion-plan.json` – plán větší oblasti a pěti podčástí.
- `world/harborline/prefabs.json` – katalog kandidátních prefabů k ověření v cílové hře.
- `world/harborline/vanilla-prefab-reuse.json` – původní cesty k budovám, lampám, lavičkám, zastávkám a road-node grafu, které diagnostika ověřuje v načtené scéně.
- `world/harborline/implementation-status.json` a `docs/implementation-audit.md` – matice skutečného stavu každého požadavku.
- `packs/Harborline/roster.json` – role a cílové počty NPC.
- `packs/Harborline/customer-designs.json` – 30 návrhů zákaznických preferencí, které se před vydáním mapují na ověřená herní data.
- `tools/validate_personnel_pack.py` zůstává pouze pomocným validátorem cizích Personnel packů; není součástí plánovaného runtime.
- `tests/` – testy validátoru.

## Lokální kontrola

```bash
python -m unittest discover -s tests -v
```

Runtime mod se sestavuje jako .NET 6 projekt s oficiálním `S1API.Forked` NuGet 3.2.2 a MelonLoader. GitHub Actions provede restore, build a vytvoří ZIP artefakt. Runtime S1API zajišťuje deklarovaná závislost v manifestu.

Současný fyzický prototyp není finální Harborline. Kompletní audit: docs/implementation-audit.md. Strojově čitelný stav: world/harborline/implementation-status.json.
