# Daffy's Hills – technická studie reference pro Harborline

**Účel:** využít zkušenosti z existujícího rozšíření mapy Schedule I jako technický referenční bod, nikoli přebírat jeho implementaci.

## Zdroj a rozsah analýzy

Uživatel poskytl archiv DaffysHills 1.2. Archiv obsahoval pouze kompilovanou DaffysHills.dll (bez zdrojových souborů a bez samostatného balíčku textur/modelů). Byla proto provedena statická inspekce dostupných metadat a řetězců DLL. Názvy metod a tříd níže jsou vodítka k architektuře; samy o sobě nedokazují přesné chování ani správné použití v aktuální verzi hry.

Veřejná stránka modu: https://www.nexusmods.com/schedule1/mods/2618. Verze 1.2 je označena datem aktualizace 23. září 2026. Popis uvádí novou přibližně 720m silnici připojenou na původní slepý konec, 32 domů, 43 zákazníků, klepání na domovní dveře, denní návštěvy, autobusové zastávky, lavičky, vozidla, terénní úpravy a nový koupitelný objekt. Polyfill changelog (https://thunderstore.io/c/schedule-i/p/DooDesch/Polyfill/changelog/) zmiňuje opravy kompatibility DaffysHills s verzí hry 0.4.7 dne 24. října 2026 včetně pohybu NPC po silnici a chování kabiny.

## Co si bereme jako architektonickou inspiraci

### 1. Mapa je skutečný prostor, ne sada ikon

Rozšíření musí upravit nebo navázat na reálný herní svět: terén, průjezdnou silnici, chodníky, napojení na stávající komunikaci, obyvatelné budovy a NPC trasy. Samostatné objekty nebo body v telefonu nejsou náhradou za rozšíření mapy.

**Rozhodnutí pro Harborline:** rozšiřovat existující oblast Docks, nikoli vytvářet izolovanou mapovou vrstvu. Zachovat požadavek na skutečné napojení na původní cestní síť. Před umístěním finální geometrie je potřeba v běžící hře ověřit hranici regionu, výšku povrchu, pobřeží a dostupný prostor.

### 2. Budovy a dveře musí být součástí herního pohybu

Daffy's Hills podle popisu používá dveře, za které mohou obyvatelé chodit domů a které hráč může zaklepat. Nestačí vizuální fasáda ani dveřní kvádr.

**Rozhodnutí pro Harborline:** pro první sklad Portside Supply použít S1MAPI BuildingBuilder namísto dosavadní čistě kvádrové skořápky. Přes S1MAPI se pokusíme vytvořit podlahu, zdi, střechu, osvětlení a síťově vytvářené posuvné dveře. NavigationBuilder poskytuje vlastní A* navigaci interiéru a přechod mezi exteriérovým NavMesh a vnitřním pathfindingem; nenahrazuje však celoměstskou silniční síť. Viz https://ifbars.github.io/S1MAPI/api/S1MAPI.Building.BuildingBuilder.html a https://ifbars.github.io/S1MAPI/api/S1MAPI.Building.html.

### 3. Obyvatelé potřebují vlastní rutinu a vztah k místu

Počet NPC není sám o sobě měřítkem funkční čtvrti. Důležité jsou zákaznické profily, domovy, časy odchodů/příchodů, návštěvy ostatních obyvatel, obchodní interakce a dosažitelné cíle.

**Rozhodnutí pro Harborline:** používat skutečné S1API NPC a aktuální plánovací akce. Aktuální S1API uvádí, že EnsureDealSignal() je zachováno pouze kvůli kompatibilitě a od verze hry 0.4.6 nedělá nic; zákaznické čekání na deal se aktivuje automaticky přes IsCustomer. Volání bylo proto odstraněno. Další denní pohyb budeme přidávat až s naměřenými body a zaregistrovanými budovami. Viz https://ifbars.github.io/S1API/api/S1API.Entities.NPCSchedule.html.

### 4. Koupitelný objekt potřebuje úplný nativní nákupní cyklus

Statické řetězce v DLL obsahují vodítka k napojení realitního makléře a existujících voleb dialogu, například názvy ShouldChoiceBeShown, ModifyChoiceText, CheckChoice, ChoiceCallback, AgentShow, AgentText, AgentCheck a AgentPick. Dále obsahují názvy jako Property, SetOwned, BuyingProperty a SetOwned_Server. To naznačuje, že autor řešil vlastní Property, dialog/volbu u realitního makléře a serverové vlastnictví. Jde o indicie ze jmen v DLL, nikoli o ověřený recept ani o zdrojový kód.

**Rozhodnutí pro Harborline:** přidat do diagnostiky přesný metadata scan tříd a metod realitního makléře, Property/Business registrů, vlastnictví, dveří, pohybu, cest a mapových markerů v nainstalované verzi hry. Vlastnictví nepředstírat vložením objektu do soukromého seznamu. Nabídku v Realtor označíme za hotovou teprve tehdy, když funguje zobrazení, nákupní transakce, vlastnictví na serveru, uložení/načtení, interiér a mapová značka.

### 5. Ověřování kompatibility je nezbytné

Veřejný changelog Polyfill uvádí, že DaffysHills potřeboval po změně hry opravy kompatibility 0.4.7; 24. října 2026 byly uvedeny opravy pohybu obyvatel po silnici a chyb kabiny. To potvrzuje, že i fungující rozšíření světa může být citlivé na interní API hry. Neznamená to, že máme přidat Polyfill jako závislost ani že máme přebírat starší pohybové hooky.

**Rozhodnutí pro Harborline:** používat současné veřejné S1API/S1MAPI API, reflexi omezit na zjišťování dostupných podpisů a pro neznámou verzi mít bezpečný fallback. Testy sestavení nenahrazují test ve hře.

## Další technické indicie z DLL string table

Při druhém průchodu celým string table se objevily další konkrétní stopy (stále nejde o dekompilovaný zdrojový kód):

- **Původní objekty mapy:** cesty jako `Map/Hyland Point/Region_Docks/Clothing store with interior`, `Region_Suburbia/Green House`, původní domy v Suburbia/Westville, `Region_Downtown/Boutique Store`, gas station, `Region_Suburbia/StreetLight`, `Region_Uptown/Pier/Seats/OutdoorBench` a `Map/Hyland Point/Bus stops/Bus Stop`. Z toho vznikl vlastní katalog `world/harborline/vanilla-prefab-reuse.json`.
- **Silniční síť:** string table zmiňuje `Map/Pathfinding/RoadNodes`, AstarPath, RecastGraph, PointGraph, Seeker, road points on navmesh, valid links a testy úplných tras z města přes nový úsek až ke vchodovým dveřím. Daffy tedy nekontroloval jen to, jestli silnice vizuálně existuje; ověřoval dosažitelnost.
- **Terén:** DLL obsahuje kroky pro výškový profil, vzorkování terénu, heightmapu, alphamap vrstvy, stromy a kolize. Nová diagnostika proto exportuje rozměry Terrain, vrstvy materiálů a mřížku 5×5 výškových vzorků pro každý aktivní Terrain.
- **NPC a rezidence:** texty ukazují domovní rutiny, návštěvy, odchody domů a kontroly cest k domovním dveřím. Vlastní plánování NPC má být navázané na existující Building/door registry a ověřenou navigaci, ne jen na přidání souřadnic do seznamu.
- **Realtor:** string table obsahuje čtyři jména hooků `ShouldChoiceBeShown`, `ModifyChoiceText`, `CheckChoice`, `ChoiceCallback` a další pojmenované wrappery `AgentShow`, `AgentText`, `AgentCheck`, `AgentPick`. Objevují se také `AddComponent<Property>`, `SetOwned`, `SetOwned_Server`, `BuyingCabin` a název seznamu `Properties`. To je vodítko k tomu, kde autor napojil koupi nemovitosti a vlastnictví. Bez přesných typů, signatur a serializačních polí cílové hry to ale není bezpečný návod k opakování.
- **Testy:** DLL obsahuje výstupy vlastního drive-line self-testu, testů cest od města ke dveřím a kontroly colliderů/terénních kroků. Je to dobrý standard pro náš budoucí road self-test.

## Nové použití těchto poznatků v Harborline

- `NativeScenePrefabExporter` hledá vybrané vanilla hierarchy paths v živé scéně; včetně původního Docks clothing store, domů, lamp, laviček, zastávky a `Map/Pathfinding/RoadNodes`. Exportuje názvy, souřadnice, měřítko a typy komponent.
- `HarborlineNativeVisualReuse` kopíruje jen `MeshFilter.sharedMesh`, `MeshRenderer.sharedMaterials` a základní nastavení Light komponent do nových lokálních GameObjectů. Neklonuje žádné síťové prefab objekty, interakční skripty ani collidery. Streetlight, promenade bench a bus stop visual mají vlastní geometry fallback, když konkrétní cesta ve scéně není.
- `NativeTerrainSurveyExporter` exportuje `native-terrain-surfaces.csv`, `native-terrain-layers.csv` a `native-terrain-height-samples.csv`. Získáme tak skutečné výšky a materiálové vrstvy, než začneme kreslit novou silnici nebo upravovat terén.
- `HarborlineIntegrationApiScanner` nyní sleduje také A* graph typy, road nodes, NPC movement, seat sets a transport/parking typy. Cílem je ověřit, jak bezpečně připojit novou trasu k již existujícímu grafu.

Tyto nové exportéry jsou read-only. Vlastní silniční síť, terénní úpravy, rezidenční routiny a Realtor transakce ještě nejsou hotové; nejdřív potřebujeme ověřené výstupy z cílové hry.

## Co je již převedeno do vlastního projektu

- Přidán HarborlineProceduralBuildingFactory, který přes reflexi hledá S1MAPI BuildingBuilder a BuildingConfig a sestavuje sklad; pokud API není dostupné nebo volání selže, vrací řízení původnímu geometrickému fallbacku.
- Továrna se nyní volá při generování skladu Portside Supply.
- Doplňkový scan HarborlineIntegrationApiScanner vypisuje přesné metadata podpisy pro realitního makléře, Property/Business, dveře, navigaci, silnice, mapu a vlastnictví. Je pouze čtecí.
- Odstraněna neúčinná zákaznická akce EnsureDealSignal().
- Nativní Portside Supply supplier je označován jako dead-drop/meeting supplier. Není vydáván za samostatný fyzický obchod ani za hotovou Hyland Network online objednávkovou aplikaci.

## Co zůstává nehotové

- Silniční síť a chodníky propojené s původní Docks navigací.
- Ověřené umístění a hranice čtvrti v běžící hře.
- Funkční interiérové předměty a dveřní interakce otestované hráčem i NPC.
- Domácí a pracovní rutiny, návštěvy, doprava a autobusová zastavení.
- Vlastní registrace a koupě budov přes skutečný Realtor flow.
- Mapové ikony v normální mapě.
- Samostatné kamenné obchody a online objednávky se společnou zásobou.
- Multiplayer, host-authoritative spawn a save/load vlastního světa.

## Licence a použití cizího modu

Tento archiv používáme jen jako referenci pro vlastní návrh. Z cizí DLL nepřebíráme kód, modely, textury, audio ani jiné assety a nevytváříme z ní odvozenou implementaci. Veškerá implementace Harborline zůstává samostatným kódem a používá veřejné API hry/S1API/S1MAPI. Pokud by bylo někdy potřeba použít konkrétní asset nebo část zdrojového kódu autora, je nutné nejprve získat výslovné svolení autora.
