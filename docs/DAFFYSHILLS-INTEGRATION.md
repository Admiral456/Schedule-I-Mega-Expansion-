# Integrace s Daffy's Hills 1.2

Nahraný ZIP obsahuje pouze `DaffysHills.dll` (2 757 632 B). SHA-256 souboru:
`826e09f50485a2c82f1c0e2460ce1a2f42551515efdca57c305966e0968c506a`

Oficiální stránka modu: https://www.nexusmods.com/schedule1/mods/2618?tab=description

## Funkce podle stránky autora
- Nová přibližně 720m dlouhá dvouproudá silnice do východní vyvýšené oblasti.
- Rezidenční ulice a domy.
- 43 nových zákazníků s vlastními vzhledy, preferencemi, výdaji a vztahy.
- Nový dealer DrDaffy.
- Otevíratelné/klepáním aktivované přední dveře, rozvrhy obyvatel a návštěvy sousedů.
- Vozidla, zastávky, lavičky, monument a billboard.
- Verze 1.2 přidala novou koupitelnou budovu.

**Počty domů na stránce nejsou konzistentní:** nadpis uvádí 30 domů, zatímco popis funkce uvádí 32. Nadpis uvádí 44 NPC, text funkcí 43 nových zákazníků a samostatného dealera. Projekt proto používá přesné počty pouze jako referenční tvrzení autora, nikoli jako ověřenou geometrii.

## Kompatibilita
Autor uvádí Schedule I 0.4.6f13, MelonLoader 0.7.x a S1API Forked 3.2.0 nebo novější. Tato deklarace není potvrzením společného běhu s naší verzí hry/modu. S1API 3.2.2 je pin naší baseline 0.3.1; novější vydání S1API neměníme bez nového buildu a testu.

## Hranice integrace
Daffy's Hills zůstane volitelným sousedním modem pro východní vyvýšenou oblast. Harborline zůstane přístavní část původních Docks.

- Nepřekrývat jeho východní vyvýšený terén, 720m silnici, domovní parcely ani jeho existující spawn pointy.
- Nevytvářet duplicity zákazníků, domů, dealerů nebo vozidel v jeho oblasti.
- Nepoužívat jeho entity IDs, save keys ani názvy pro vlastní objekty.
- Nekopírovat ani neupravovat jeho DLL, kód, assety nebo modely.
- Nepřidávat Daffy's Hills do našeho Thunderstore balíčku ani mezi povinné závislosti.

## Testovací matice
| Test | Stav |
|---|---|
| Daffy's Hills samostatně | Čeká na spuštění ve hře |
| Mega Expansion samostatně | Čeká na spuštění ve hře |
| Oba mody – pořadí načtení A/B | Čeká |
| Nový save a opětovné načtení | Čeká |
| Překryv spawnů / kolize / navmesh | Čeká |
| Výkon při dlouhé relaci | Čeká |
| Host/client multiplayer | Čeká |

Autorova pravidla distribuce zakazují nahrávání souboru na jiný web, úpravy modu bez svolení a použití jeho assetů bez svolení. Náš repozitář proto obsahuje jen integrační poznámky, ne cizí soubor.
