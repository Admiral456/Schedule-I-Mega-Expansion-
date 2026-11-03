# Audit addendum – obnovení zdrojů a integrace

Datum: 3. 11. 2026

Tento soubor je doplňkem ke kanonickému auditu `docs/implementation-audit.md`, který byl obnoven z přesného zdrojového commitu 0.3.1.

## Oprava předchozího závěru
Před obnovou se nepodařilo najít odpovídající zdrojový projekt ve výchozí větvi GitHubu. Následná kontrola assembly metadata v gameplay DLL 0.3.1 odhalila build commit `202b0b9080d2edadd09724cfd157d4fc0fee5a5a`. Z tohoto commitu byly obnoveny všechny původní runtime C# soubory, projekt, workflow, dokumentace, testy a datové kontrakty. Zdrojový projekt tedy nyní chybějící není.

## Co je potvrzené v kódu 0.3.1
- Nativní S1API NPC vlna: 2 dealeři, 8 zákazníků a 1 supplier.
- Supplier používá nativní S1API Supplier / dead-drop / meeting-shop flow.
- Katalog pracuje se 14 existujícími vanilla item ID.
- Hardware Store se rozšiřuje přes nativní `Shop.AddItem`; ceny se nastavují na sdíleném `StorableItemDefinition.BasePurchasePrice`.
- Fyzická část se generuje po načtení save z kotvy Docks Warehouse; umístění a fyzika zůstávají bez herního ověření.
- Nativní exportéry jsou read-only; slouží k průzkumu registrů, prefabů, policejních checkpointů, tras a API povrchů.

## Co není hotové
Samostatná kamenná prodejna Harborline, vlastní telefonní aplikace Hyland Network, společný sklad mezi fyzickým a online obchodem, nové Realtor záznamy, nativní mapové ikony/ownership, dokončená silniční a pěší síť, policejní kontrolní body, persistentní ukládání geometrie a multiplayerové generování/synchronizace.

## Daffy's Hills
Daffy's Hills 1.2 je volitelný sousední mod; nesmí být bundlován, upravován ani používán jako zdroj assetů/kódu. Viz `docs/DAFFYSHILLS-INTEGRATION.md` a původní `docs/daffys-hills-reference-study.md`.
