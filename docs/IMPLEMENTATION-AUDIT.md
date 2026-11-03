# Implementační audit – 3. 11. 2026

## Zdroj stavu
Nejnovější nalezený instalační balíček je `ScheduleIMegaExpansion-0.3.1-thunderstore.zip`. Obsahuje `Mods/ScheduleIMegaExpansion.dll` (114 688 B), manifest, README a licenci. Níže uvedený stav je kontrola balíčku a jeho dokumentace, nikoli nezávisle potvrzený herní runtime.

## Implementováno v popisu 0.3.1
| Oblast | Rozsah | Ověření |
|---|---|---|
| Dealeři | Mason Reed, Sierra Voss | Zdrojová dokumentace; hra netestována |
| Zákazníci | 8 nativních customer NPC | Zdrojová dokumentace; hra netestována |
| Supplier | Portside Supply jako native Supplier NPC | Zdrojová dokumentace; hra netestována |
| Supplier katalog | 14 existujících item ID a původní Delivery flow | API limity |
| Hardware Store | 14 položek v původním obchodě | Změny ceny mohou být globální pro stejné item ID |
| Umístění | Docks Warehouse anchor + fyzikální kontroly | Nutný herní survey |
| Fyzická oblast | Unity geometrický prototyp | Runtime test chybí |

## Nehotovo / neprokázáno
| Funkce | Stav | Další krok |
|---|---|---|
| Kompletní Harborline | Nehotovo | Survey, terén, silnice a kolize |
| NPC cesty a návštěvy | Nehotovo | Zaměřit navmesh a waypointy |
| Dalších 22 zákaznických profilů | Návrhová data | Napojit na CustomerDataBuilder a otestovat |
| Samostatný fyzický obchod | Nehotovo | Znovu použít vanilla shop prefaby |
| Vlastní telefonní aplikace / online obchod | Nehotovo | Sideload/S1API phone integration |
| Jeden sdílený sklad | Nehotovo | Navrhnout podporovanou save/network vrstvu; Delivery API má limity |
| Realtor a nové podniky | Nehotovo | PropertyManager/BusinessManager registrace hook nebyl prokázán |
| Policejní hlídky a dispatch | Nehotovo | Prozkoumat nativní law registry a bezpečné hooky |
| Mapové POI, dveře a interiéry | Nehotovo | Ověřit map API, vanilla prefab reuse a collidery |
| Persistentní geometrie | Nehotovo | Save lifecycle a idempotentní world state |
| Multiplayer | Neprokázáno | Host-authoritative spawn, sync a late join |

## Rizika
`HarborlineShopCatalog` podle README upravuje `StorableItemDefinition.BasePurchasePrice` existujících itemů. Cena je vlastností sdílené definice, takže dopad nemusí být omezen na Hardware Store. Před dalším rozšířením musí být změna idempotentní a musí existovat ověření původní ceny i chování při vypnutí modu.

## Povinné testovací brány
1. Čistý save vytvoří nejvýše jednu instanci každého entity ID.
2. Reload save neduplikuje NPC a zachová stabilní identity.
3. Spawn nezasahuje do vody, zdí, silnic ani cizích budov.
4. Katalog, nákup, ceny a doručení odpovídají očekávání.
5. Policejní systémy nemění globální checkpointy bez explicitního nastavení.
6. Daffy's Hills + Mega Expansion se nepřekrývají.
7. Host je autoritou pro spawn a síťový stav.
8. Vypnutí/odinstalace nepoškodí save.

Skutečný runtime test vyžaduje cílovou instalaci hry a její logy.