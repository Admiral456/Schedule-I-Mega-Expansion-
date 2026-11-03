# Volitelné mapové módy – kompatibilita

Tento dokument eviduje tři oddělené případy. Všechny zůstávají volitelné a nejsou distribuovány s Mega Expansion. Cizí DLL, zdrojový kód ani assety se nekopírují a neupravují.

## Pier Expansion – DrDaffy

- Oficiální stránka: https://www.nexusmods.com/schedule1/mods/2594
- Veřejná stránka uvádí verzi 1.2.0, testování na Schedule I 0.4.6f13, MelonLoader 0.7.3 a S1API Forked 3.2.0+.
- Dodaný archiv má v názvu 1.2.1, ale obsahuje pouze `PierExpansion.dll` (218 624 B). SHA-256: `e4d8d17c4e00d5e9bd1da9eb1a818252d4a546b38299717828c06ce7be85dc34`.
- Archiv neobsahuje zdrojové soubory ani samostatné assety. Omezená kontrola binárních textových identifikátorů odhalila startup řetězec s 8 budovami, 8 obyvateli a 2 koupitelnými nemovitostmi a nastavení `EnableNavMesh`, `EnableProperties`, `EnableDecor`, `WarehousePrice`, `BoathousePrice`, `BlockOffsetX` a `BlockOffsetZ`. Nejde o dekompilaci ani runtime test.
- Autor popisuje most mezi Westville a Docks, molo, 8 budov a 8 rezidentů. Dvě nemovitosti se ukládají do save; po odebrání modu nebo vypnutí `EnableProperties` může save nadále odkazovat na neexistující obsah. Autor také upozorňuje na možné překrytí s jinými módy, které přidávají budovy/NPC do stejného úseku vody.
- Verzní rozdíl 1.2.1 v názvu dodaného archivu vs. 1.2.0 na stránce autora zůstává nevyřešen. Verzi DLL nelze potvrdit jen z názvu ZIPu.

## Unofficial Map Enhancer – Serena Flats

- Oficiální stránka: https://www.nexusmods.com/schedule1/mods/2258
- Archiv nebyl dodán; záznam vychází pouze z veřejného popisu autora. Uživatel jej může stáhnout pouze na PC.
- Veřejný popis uvádí UME 1.0.0 / Hyland Bridge 0.1.0 a cílovou hru 0.4.5f2. Vyžaduje oba DLL soubory: `UnofficialMapEnhancer.dll` a `UME.HylandBridge.dll`.
- Instalace podle autora nahrazuje pět souborů v `Schedule I_Data`: `level2`, `level2.resS` a `sharedassets0.assets` až `sharedassets2.assets`. Před instalací je nutné zazálohovat původní soubory. Uvedené assety jsou svázané s herní verzí 0.4.5f2.
- Přidává Serena Flats, propojení tunelem/mostem, dům, RV, obyvatele se scheduly a další lokální funkce. Autor uvádí, že mod neukládá změny do herního save; postranní data jsou samostatně v `UserData/UME`.
- Není zaměnitelný za novější samostatný mod „Return to Serena Flats“ (jiný autor a jiné ID modu). Ten zatím není součástí této lokální kontroly.

## Ochrana našeho generování

Současný prototyp hledá volnou plochu relativně k nativnímu Docks Warehouse a před zaměřením mapy nezná přesné polygonální hranice cizích oblastí. Proto je zavedena konzervativní ochrana: pokud ve složce `Mods` zjistí `PierExpansion.dll`, `UnofficialMapEnhancer.dll`, `UME.HylandBridge.dll` nebo `SerenaFlats.Il2cpp.dll`, pozastaví automatické generování geometrie Harborline i přiřazování pozic jeho NPC. Nezastaví nezávislou inicializaci katalogu obchodu.

Tato kontrola pouze zjišťuje přítomnost souboru; není důkazem, že je cizí mod kompatibilní nebo že se úspěšně načetl. Ochranu lze odstranit nebo nahradit mapově specifickou integrační logikou až po získání skutečných pozic/oblastí z běžící hry a po testu nového save, opětovného načtení, NPC navigace a multiplayeru.

Daffy's Hills zůstává odděleným volitelným doplňkem východní vyvýšené oblasti. Harborline zůstává plánovanou přístavní částí původních Docks; žádné z těchto rozšíření se nesmí překrývat bez výslovně zaměřené a otestované integrace.

## Licence a distribuce

Nexus permission text u obou stránek zakazuje reupload. Úpravy a použití cizích assetů vyžadují svolení autora. Mega Expansion proto smí distribuovat pouze vlastní kód a vlastní obsah. Kompatibilita se řeší odděleným načítáním a neinvazivní detekcí souborů; žádný cizí DLL nebo asset do našeho ZIPu nepatří.

## Testovací brány

| Test | Stav |
|---|---|
| ZIP Pier Expansion – obsah, velikost a SHA-256 | Dokončeno |
| Pier Expansion – skutečný běh v herní verzi uživatele | Čeká |
| UME archive inspection | Čeká na ZIP z PC |
| UME – potvrzení verze hry a původních assetů | Čeká |
| Každý mod samostatně na kopii save | Čeká |
| Společný běh s Mega Expansion a pořadí načtení | Čeká |
| Kontrola hranic, kolizí, silnic a NavMesh | Čeká |
| Save/reload a vlastnictví nemovitostí | Čeká |
| Host/client multiplayer | Čeká |

Strojově čitelné záznamy:
- `world/integrations/pier-expansion.json`
- `world/integrations/ume-serena-flats.json`
- `world/integrations/map-expansion-guard.json`
