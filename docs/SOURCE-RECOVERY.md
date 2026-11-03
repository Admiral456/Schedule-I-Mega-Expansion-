# Zdrojový kód a obnova

## Dostupné artefakty
| Soubor | Obsah |
|---|---|
| `ScheduleIMegaExpansion-0.3.1-thunderstore.zip` | Gameplay DLL 114 688 B + README/manifest/licence |
| `ScheduleIMegaExpansion-current.dll` | Diagnostická read-only DLL 43 520 B; jiný SHA-256 |
| `ScheduleIMegaExpansion-runtime-diagnostic-current.zip` | Read-only diagnostický build |
| `Harborline_Expansion_Dev_v0_2.zip` | Starší C# greybox: ModEntry, Config, World, csproj a docs |

## Co chybí
Přesný C# projekt, ze kterého vznikla gameplay DLL 0.3.1. Při kontrole měl GitHub pouze licenci a placeholder README; cesty `src/`, `docs/` a `world/` vracely 404.

Názvy tříd/metod lze z DLL částečně vyčíst, ale bez dekompilátoru a ověření nelze metadata vydávat za původní zdroj. Tento repozitář proto nebude vydávat odhadnutý kód za originální zdroj 0.3.1.

## Starší zdroj v0.2
Dostupný prototyp v0.2 je pouze vizuální greybox, defaultně vypnutý (`Enabled=false`) a používá `S1MAPI_Mono.dll`. Není zdrojem současného IL2CPP buildu. Má být zachován pouze v archivu.

## Bezpečná obnova
1. Zachovat 0.3.1 DLL jako referenční baseline.
2. Obnovit originální pracovní adresář nebo CI artefakt.
3. Přidat přesný .csproj, všechny .cs soubory, verze NuGet a build nastavení.
4. Znovu sestavit proti správným IL2CPP/game assemblies a porovnat manifest i výsledný build.
5. Teprve poté rozvíjet původní herní třídy.

Viditelné názvy z DLL zahrnují např. `HarborlineGameplayBootstrap`, `HarborlineNpcRoster`, `HarborlineNpcRegistrar`, `HarborlineNpcLifecycle`, `HarborlinePhysicalBuilder`, `HarborlineProceduralBuildingFactory`, `HarborlineShopCatalog`, `NativeApiSurfaceScanner`, `NativePrefabCatalogExporter` a `NativeLawSnapshotExporter`. Jde o metadata, nikoli obnovený C# zdroj.