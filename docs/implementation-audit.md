# Harborline – audit implementace a propojení systémů

**Auditní základ:** větev `feat/harborline-foundation`, commit `ac46eb774a48bbfd044969c566e0e931b65ba110`. Kontrola zdrojového kódu, projektových dat, statických testů a veřejného zdrojového kódu S1API/S1MAPI. Audit nebyl spuštěn uvnitř uživatelovy instalace Schedule I.

## Výsledek

**Projekt není ve stavu plně funkčního rozšíření.** Je v něm skutečný MelonLoader runtime prototyp, automatická tvorba session-local geometrie přístavu a diagnostika nativních registrů/prefabů. Zásadní požadavky na NPC, Realtor, mapu, obchody, online objednávky, policii, nativní interakce, ukládání a multiplayer zatím nejsou napojené.

## Co je skutečně v kódu

| Oblast | Stav auditu | Co lze doložit |
|---|---|---|
| MelonLoader bootstrap | Kód existuje, runtime neověřen | `src/Core.cs` |
| Save/scene lifecycle | Kód existuje, runtime neověřen | `OnPreLoad`, `OnLoadComplete`, `OnPreSceneChange` |
| Automatická fyzická geometrie | Kód existuje, runtime neověřen | `HarborlinePhysicalBuilder` vytváří Unity primitiva s kolizemi |
| Kotva Docks Warehouse | API ověřené ve zdroji S1API; konkrétní záznam neověřen | `PropertyManager.GetAllProperties()`, `PropertyName`, `ExteriorSpawnPosition` |
| Kontrola terénu | Heuristika, runtime neověřen | `Physics.CheckBox`, raycast, sklon, výška a názvy/vrstvy vody |
| Multiplayerová ochrana | Opravena podle S1API wrapperu | `S1API.Entities.Player.All`; generování povolí pouze při přesně jednom hráči |
| Prefab katalog | Kód existuje, runtime neověřen | `PrefabRef.Find()`, `native-prefabs.csv` |
| API scanner | Kód existuje, runtime neověřen | Assembly-CSharp, S1API, S1MAPI, FishNet |

## Co není dokončeno

**Mapa a pozice:** žádné z osmi míst nemá zaměřené herní souřadnice. Neznáme ověřené hranice Docks, mapové bounds, world-to-map transform ani spojení s nativní silniční/navmesh sítí. Hledání volného terénu 42–104 metrů od Warehouse je pouze heuristika; samo o sobě nedokazuje, že výsledek leží uvnitř Docks.

**Budovy:** současná stavba je tvořena generickými Unity krychlemi. Není zaregistrovaná jako nativní Building, Property, Business nebo Shop a nemá produkční prefabový interiér.

**NPC:** žádná nová NPC třída ani volání `NPCPrefabBuilder` v runtime neexistuje. Čísla 42/18/30/2 jsou cílové hodnoty v JSON, ne vytvořené herní entity. Volání nativního generátoru vzhledu je pouze popsáno v datech.

**Obchody:** neexistuje připojení inventáře, pokladen ani online objednávek. Popis sdíleného katalogu je návrh.

**Realtor a vlastnictví:** veřejné `PropertyManager`/`BusinessManager` v prohlédnutém S1API zdroji enumerují existující záznamy. Veřejný hook pro registraci nové nemovitosti/podniku není prokázán. Žádný nový nákupní záznam proto nevzniká.

**Mapové ikony:** není připojena mapa, world-to-map transform, nativní klasifikace budov ani vlastnické ikony.

**Policie:** nejsou napojené hlídky, dispatch ani checkpointy.

**Dveře/interakce:** prefaby se pouze vyhledávají; žádný nativní door/storage/shop prefab se ve hře nevytváří.

**Ukládání a multiplayer:** geometrie je pouze session-local. Mod ji po načtení znovu vytváří; neukládá se do save. Co-op je záměrně zablokovaný.

## Co je potřeba pro skutečné potvrzení

1. Spustit tento build v cílové verzi Schedule I a získat `summary.txt`, `native-properties.csv`, `native-buildings.csv`, `native-businesses.csv`, `native-game-api-surface.txt` a `native-prefabs.csv`.
2. Ověřit, že ve hře existuje přesný záznam Docks Warehouse, že generátor vybere skutečně volnou plochu v Docks a že modely nekolidují s původní mapou ani vodou.
3. Teprve po runtime průzkumu implementovat nativní NPC, budovy, dveře, mapu, obchodní flow a policii.
4. Otestovat save/load, přechod do menu, pozdní připojení hráče a host/client režim.

## Auditní data

Strojově čitelná matice je v `world/harborline/implementation-status.json`. Každá položka má jeden ze stavů `implemented_code_unverified_runtime`, `design_only`, `not_implemented`, `blocked_unverified_api` nebo `not_verified_in_game`.

## Finální CI kontrola po opravách

Auditovaný runtime kód: `ac46eb774a48bbfd044969c566e0e931b65ba110`.

- Statická validace a automatické testy: úspěch, GitHub Actions run `41431858850`.
- .NET 6 build a vytvoření Thunderstore ZIP: úspěch, run `41431858837`.
- Artefakt `ScheduleIMegaExpansion-0.2.0`: ID `13133824318`.

Tyto kontroly nepotvrzují běh hry. Přesné souřadnice, prostorové hranice Docks, kolize na skutečném terénu, vykreslení v IL2CPP, mapové ikony ani interakce nebyly ověřeny uvnitř Schedule I.
