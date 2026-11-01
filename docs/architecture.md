# Architektura Harborline — rozšíření skutečného světa

## Zásada
Harborline nesmí být samostatná obrazovka, oddělená mapa ani sada dekorací mimo svět hry. Musí jít o fyzické rozšíření Hyland Pointu, propojené s Docks, silnicemi, navigací, mapou telefonu, budovami a původními herními systémy.

## Původní region hry

Harborline je pojmenovaná podčtvrť fyzicky rozšířená v rámci původního regionu `Docks`. Aktuální S1API `Region` enum obsahuje pouze `Northtown`, `Westville`, `Downtown`, `Docks`, `Suburbia` a `Uptown`. Nepoužívat vymyšlené `Region.Harborline`; nové NPC nastavovat přes existující `Region.Docks`. Souřadnice oblasti musí být zaměřeny a potvrzeny v rámci hranic Docks.

## NPC: původní systém postav
- Vytvářet fyzické postavy jako S1API.Entities.NPC, tedy přes NPC prefab, schedule, customer/dealer, save/load a síťovací cestu Schedule I.
- Vzhled generovat výhradně nativním avatar systémem přes S1API.Entities.NPCAppearance.GenerateRandomAppearance() a následné Build().
- Nepoužívat vlastní generátor obličejů/těl/oblečení ani Personnel jako povinný framework pro spawny. S1API je API vrstva, která zpřístupňuje herní NPC/avatar systémy.
- Náhodný vzhled se vytvoří jednou na hostu, uloží s NPC a synchronizuje do klientů. Při načtení save, připojení klienta nebo změně scény se nesmí znovu losovat.
- Kontrola opakování smí porovnávat jen skutečně vyřešené nativní AvatarSettings; při shodě se znovu zavolá nativní randomizer. Žádné ručně sestavené kombinace vlastních assetů.
- Zákaznické a dealerské chování musí používat nativní customer/dealer komponenty a ekonomiku.

Reference: https://ifbars.github.io/S1API/docs/custom-npcs.html a https://ifbars.github.io/S1API/api/S1API.Entities.NPCAppearance.html.

## Fyzická oblast a mapa
1. Změřit skutečný prostor navazující na Docks. Nevytvářet souřadnice převodem pixelů z návrhové mapy.
2. Rozšířit existující herní geometrii/scene: terén, pobřeží, chodníky, silnice, napojení na Docks, kolize a navigační síť.
3. Rozšířit stejnou mapu používanou hrou o odpovídající výřez/oblast a správný world-to-map převod. Nová část musí být viditelná při běžném otevření mapy; samostatná mapka nebo sada POI ikon není náhrada.
4. Zaregistrovat oblast v nativním region/map systému tak, aby v ní fungovala NPC navigace, policejní logika a další regionální systémy. Přesný hook/identifikátor je release gate, dokud nebude potvrzen v cílovém herním buildu.
5. S1API.Map.MapPOI použít pouze pro samostatné veřejné body zájmu, které nemají vlastní nativní budovu/nemovitost. POI nesmí předstírat vlastnictví ani nahradit obchodní či obytnou značku.

## Ikony a vlastnictví
- Obchod se registruje jako skutečný herní obchod/budova; jeho ikonu vybírá nativní mapový klasifikátor hry.
- Nemovitost/obchod určený k nákupu musí být registrovaný v původním property/real-estate systému. Stav vlastnictví je jediný zdroj pravdy.
- Po nákupu musí původní mapový renderer změnit značku do nativního stavu vlastněné nemovitosti (včetně domečku, pokud jej hra pro daný typ používá).
- Nepřidávat vlastní sprite ani ručně přebarvené ikony, pokud pro daný typ existuje nativní ikona.
- MapPOI je obecný bod zájmu na mapě telefonu; jeho existence sama o sobě nedělá z objektu Property nebo Business.
- Veřejné body jako Pier Promenade či policejní kontrolní bod mohou mít nativní POI; nemají se tvářit jako koupitelné budovy.

Důležité: veřejný wrapper PropertyManager umožňuje prohlížet existující vlastnosti a jejich stav. Samotná existence wrapperu není důkaz, že lze libovolně přidat novou Property. Připojení nových budov do Realtor/property registru musí být ověřeno v aktuální hře předtím, než se jejich ikony označí jako hotové.

## Budovy, dveře a vnitřní vybavení
- Přednostně použít původní budovy/prefaby, které už obsahují správné vstupy, kolize, interiéry a komponenty.
- Když neexistuje vhodná budova, S1MAPI BuildingBuilder může dodat geometrii; samotný shell ale není automaticky herní Building, Property, Shop ani interaktivní interiér.
- Funkční dveře musí být původní interaktivní door prefab s nativním controllerem a NetworkObject, vytvořený serverem přes síťovou cestu. Ověřit ruční otevření/zavření, proximity chování, NPC průchod, save/load a host/client.
- BuildingBuilder.AddSlidingDoors() je možné použít jen po testu: aktuální dokumentace uvádí server-side spawn a známé omezení automatického otevírání/zavírání na klientech. Pro produkční dveře se nesmí ignorovat.
- Přidat kolize, průchozí otvory a navigační propojení dveří; NPC musí opravdu vejít dovnitř a použít StayInBuilding/nativní schedule akci.
- Dekorativní nábytek lze vytvořit přes InteriorBuilder; funkční předměty (pokladna, sklad, regály, prodejní interakce, pracovní stanice, automaty, dveře) musí být skutečné nativní interaktivní objekty s jejich původním gameplay komponentem a multiplayer synchronizací.
- V interiéru každé budovy musí být konkrétní vybavení odpovídající funkci. Prázdná skořápka budovy se nepovažuje za hotovou.

Reference: https://ifbars.github.io/S1MAPI/docs/building.html, https://ifbars.github.io/S1API/api/S1API.Map.Building.html, https://ifbars.github.io/S1API/api/S1API.Property.BaseProperty.html.

## Multiplayer a kompatibilita
- Host/server vytváří NPC, budovy, dveře a vlastnický stav; klienti obdrží stejné objekty přes FishNet.
- Každý síťový prefab s NetworkObject se spawnuje pouze přes nativní síťovou cestu.
- Používat stabilní namespaced ID, ukládání nativním save systémem a kontrolu, aby se objekty při reloadu nezdvojily.
- Nepřepisovat globální vanilla singletony; Harmony hooky jen tam, kde není veřejný extension point, a s detekcí verze/bezpečným fallbackem.
- Volitelné integrace jiných modů musí být oddělené. Bez daného modu má Harborline bezpečně fungovat v základním režimu.
- Kompatibilita se nesmí deklarovat jako hotová, dokud neproběhne test host + klient, pozdní připojení, opětovné načtení a test s reprezentativní sadou modů.

## Brány před vydáním
1. Ověřená pozice a mapový převod celé oblasti.
2. Nativní region registrace, navigace a napojení na Docks.
3. Ověřené nativní building/property/business registration a správné mapové ikony.
4. Ověřené interaktivní dveře a průchod NPC.
5. Funkční obsah interiérů (ne pouze dekorace).
6. Nativní náhodný vzhled NPC, persistence a kooperace.
7. Host/client test a kontrola výkonu při postupném navýšení populace.

Dokud některá brána neprojde, záznam zůstává not-verified a nesmí se prezentovat jako hotová funkce.