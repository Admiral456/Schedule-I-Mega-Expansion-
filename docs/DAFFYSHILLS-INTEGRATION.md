# Integrace s Daffy's Hills 1.2

Nahraný ZIP obsahuje pouze `DaffysHills.dll` (2 757 632 B), nikoli C# projekt nebo samostatné assety.

Podle stránky autora přidává Daffy's Hills přibližně 720m dvouproudou silnici, 32 domů, 43 zákaznických NPC, knockable doors, denní rozvrhy obyvatel, sousedské návštěvy, vozidla, monument a billboard. Autor uvádí testování na Schedule I 0.4.6f13, MelonLoader 0.7.x a S1API 3.2.0+. Tato kompatibilita není automaticky potvrzena pro pozdější verzi hry ani pro společné spuštění s Mega Expansion.

## Použití v našem projektu
Daffy's Hills bude volitelný sousední mod a benchmark pro hustotu osídlení a živou rezidenční ulici. Harborline zůstane přístavní podčtvrtí původních Docks.

- Nevytvářet Harborline stavby, silnice nebo NPC na jeho východní vyvýšené oblasti, silnici ani domovních parcelách.
- Nepoužívat jeho entity IDs, save keys ani názvy pro naše entity.
- Nepřebírat jeho DLL, billboard, assety ani kód.
- Nepřidávat ho jako povinnou závislost našeho manifestu.
- Otestovat oba pořadí načítání, nový save, reload, kolize spawnů, navmesh, výkon a multiplayer.

## Licence a hranice
Autor na stránce modu zakazuje úpravy modu a použití jeho assetů bez svolení; rovněž zakazuje jeho soubory znovu nahrávat na jiné weby. Proto DLL ani assety nejsou součástí našeho repozitáře ani Thunderstore ZIPu. Nahraný soubor slouží pro lokální identifikaci a kompatibilitní test, nikoli jako zdrojový kód.

## Testy
| Test | Stav |
|---|---|
| Každý mod samostatně | Čeká na hru |
| Oba mody – čistý save | Čeká na hru |
| Oba mody – reload save | Čeká na hru |
| Spawn overlap / NPC / navmesh | Čeká na hru |
| Výkon při dlouhé relaci | Čeká na hru |
| Multiplayer host/client | Čeká na hru |