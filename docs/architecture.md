# Architektura rozšíření

## Základní rozhodnutí

Rozšíření nebude nahrazovat systémy hry. Přidá do existujícího Hyland Pointu nové herní objekty, místa a definice NPC a připojí je k původní ekonomice a interakcím.

### NPC – Personnel nad S1API

- Každá nová postava bude definována v Personnel packu pod `UserData/Personnel/Packs/Harborline/manifest.json`.
- `autoRegister: true` použijeme pro automatickou registraci postav.
- Personnel vytváří skutečné S1API NPC; nepoužijeme náhražky ve formě statických modelů ani vlastní paralelní AI.
- Vzhled navrhneme v Personify a exportujeme do packu.
- Zákaznické/dealerské vlastnosti, inventář, vztahy a kontaktní texty budou v manifestu. Konkrétní pole budou převzata z aktuální Pack Format dokumentace a ověřena na testovacím NPC.
- Stabilní `id` každé postavy je povinné. Po vydání se nemění, protože slouží jako identita v uložené hře.
- V kooperaci musí mít všichni hráči stejný Personnel pack.

### Měření souřadnic – bez odhadů

Hra nemá v běžném UI přehledné Unity World souřadnice. Použijeme nástroje Personnel přímo ve hře:

1. Postavit se na přesné místo spawnu a spustit `personnel spawn`. Výstup vložit do NPC `spawn`.
2. Postavit se na další bod trasy a spustit `personnel pos HH:MM` pro samostatný bod.
3. Pro celý den použít `personnel route HH:MM` a následně `personnel route show`.
4. Zkontrolovat názvy budov a regionů na mapě hry; trasu uložit až po fyzické kontrole průchodnosti.

Souřadnice z mapy 2D pomocníka se nesmějí převádět ani kopírovat do Unity X/Y/Z. Jsou v jiném souřadnicovém prostoru.

### Budovy a prostředí – reuse first

Pořadí implementace pro každý objekt:

1. Najít vhodný existující objekt/prefab v načteném světě Schedule I.
2. Pokud existuje odpovídající objekt, znovu ho použít nebo vytvořit jeho runtime instanci; zachovat materiály a herní styl.
3. Pro statické dekorace interiéru použít existující herní nábytek/mesh.
4. Pokud vhodný prvek neexistuje, vytvořit vlastní doplněk přes S1MAPI.
5. Interaktivní/NetworkObject prefaby musí být spawnované se správným síťovým režimem. U S1MAPI je například ATM, interaktivní dveře a úložné kontejnery síťový obsah; dekorativní nábytek je odlišný případ.

S1MAPI `BuildingBuilder` je záložní nástroj pro nové konstrukce, ne výchozí způsob, jak modelovat každou budovu. Prioritou je znovupoužití originálních herních prvků.

## Harborline – funkční zóny

| Zóna | Hlavní obsah | Herní vazby |
|---|---|---|
| Přístav a nákladní plocha | mola, lodní rampa, kontejnery, jeřáby, parkoviště | pěší trasy, nákladní doprava, kontroly |
| Portside Supply | prodejna zásobovacího materiálu | původní obchodní/inventářový systém |
| Harbor Freight Warehouse | skladové haly a nakládací zóna | vlastnictví, sklad, dodávky |
| Harbor Market | stánky a tržnice | zákazníci, prodejci, denní rozvrhy |
| Harbor Auto Works | autoservis a tuning | existující vozidla a servisní interakce, pokud jsou dostupné |
| Harbor Offices | kanceláře a obchodní zázemí | realitní makléř / business registr |
| Harbor Power Plant | průmyslový areál | kontrakty a logistika |
| Pier Promenade | promenáda, lavičky, osvětlení, turisté a rybáři | ambientní NPC a večerní rozvrhy |
| Police Checkpoint | kontrolní stanoviště a patrol point | původní police/crime reakce; bez vlastní paralelní policie |

Každá lokalita v `anchors.json` má zatím prázdné souřadnice. To je záměr: musí se zaměřit v herním světě.

## NPC plán

První cílová sada je 18 fyzických obyvatel rozdělených mezi pracovní směny, promenádu, tržnici, servis a bezpečnost. Cílem je nejvýše přibližně 10–12 současně aktivních postav v záběru; skutečný limit se určí měřením FPS, paměti a doby generování mugshotů.

Z nich bude vybráno 14 jedinečných zákaznických profilů s rozdílným rozpočtem, frekvencí objednávek a preferencemi. Jeden nový dealer bude zvláštní kontakt. Tyto role nejsou samostatné AI typy: budou kombinovat fyzický NPC rozvrh s nativními Personnel customer/dealer bloky.

## Integrace dalších systémů

- **Business/property:** ověřit, které typy budov lze přidat do existujícího systému vlastnictví. Nevytvářet vlastní nabídku nemovitostí, pokud jde rozšířit původní seznam.
- **Obchod:** připojit Portside Supply k původnímu shop/inventory systému. Mobilní online objednávky budou další vrstva stejného katalogu, ne samostatný sklad.
- **Policie:** používat původní reakce na podezření, hledání a zásahy. Nové checkpointy a patrol body rozšíří prostor, ale nevytvoří novou AI policistů.
- **Hyland Network:** až po funkčním NPC a obchodu. Aplikace bude číst stejné zákaznické, objednávkové a reputační záznamy.

## Fáze realizace

1. Zaměřit vstup, silnice, stavební parcely a hlavní budovy v Harborline.
2. Zprovoznit jeden testovací objekt a ověřit, že se načítá, je viditelný a multiplayerově bezpečný.
3. Vygenerovat první 3–5 NPC přes Personnel; ověřit vzhled, spawn, trasu, ukládání a zákaznickou interakci.
4. Rozšířit roster na cílové role; zkontrolovat výkon a navmesh.
5. Přidat obchody, vlastnictví, policejní body a zakázky přes nativní systémy.
6. Teprve potom začít Ridgeview a Lakeside.

## Zdroje / API

- Personnel packy a herní souřadnice: https://docs.doodesch.de/mods/personnel/
- S1API: https://ifbars.github.io/S1API/
- S1MAPI BuildingBuilder: https://ifbars.github.io/S1MAPI/api/S1MAPI.Building.BuildingBuilder.html
- S1MAPI Building Guide: https://ifbars.github.io/S1MAPI/docs/building.html
- Referenční mapová data v odděleném projektu `Admiral456/Schedule-I` slouží jen jako 2D orientace; zatím neobsahují zaměřenou geometrii budov ani silnic.
