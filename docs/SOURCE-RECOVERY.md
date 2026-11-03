# Zdrojový kód – obnovení revize 0.3.1

## Výsledek
Přesný C# zdrojový projekt pro gameplay DLL 0.3.1 byl nalezen v historii tohoto repozitáře a obnoven do hlavní větve.

DLL `ScheduleIMegaExpansion.dll` z balíčku 0.3.1 obsahuje assembly informational version:
`0.3.1+202b0b9080d2edadd09724cfd157d4fc0fee5a5a`

Toto je přesná Git commit revize použitá při sestavení DLL. Commit zůstal dostupný v historii i přesto, že tehdejší aktuální větev obsahovala pouze placeholder README a licenci. Zdroj byl obnoven podle této revize, nikoli rekonstruován odhadem.

## Obnovené soubory
- `src/ScheduleIMegaExpansion.csproj`
- `src/Core.cs`
- `src/Runtime/HarborlineNpcRoster.cs`
- `src/Runtime/HarborlinePhysicalBuilder.cs`
- `src/Runtime/HarborlineProceduralBuildingFactory.cs`
- `src/Runtime/HarborlineShopCatalog.cs`
- `src/Diagnostics/HarborlineIntegrationApiScanner.cs`
- `src/Diagnostics/NativeApiSurfaceScanner.cs`
- `src/Diagnostics/NativeLawSnapshotExporter.cs`
- `src/Diagnostics/NativePrefabCatalogExporter.cs`
- `src/Diagnostics/WorldSnapshotExporter.cs`
- `.github/workflows/build-runtime.yml`
- `.github/workflows/validate.yml`
- původní testy, datové soubory, manifest, changelog a dokumentace 0.3.1.

## Build baseline
- .NET 6 / IL2CPP.
- Compile-time S1API Forked 3.2.2.
- Thunderstore dependencies: MelonLoader 0.7.3, S1API Forked 3.2.2 a S1MAPI 2.0.1.
- Původní CI zaznamenalo úspěšný runtime build a statickou validaci pro baseline 0.3.1. To neprokazuje běh v cílové hře.

## Další zdrojové artefakty
- `ScheduleIMegaExpansion-current.dll` je odlišný read-only diagnostický build (43 520 B), nikoli gameplay DLL.
- `Harborline_Expansion_Dev_v0_2.zip` je starší Mono/S1MAPI vizuální greybox. Je uložen v `archive/Harborline_Expansion_Dev_v0_2/` a nesmí být zaměněn za 0.3.1.
- Daffy's Hills 1.2 obsahuje jen cizí DLL. Její kód ani assety nejsou součástí projektu.

## Stav obnovy
Zdrojový kód 0.3.1: **obnoven**.
Reprodukovatelné nové sestavení aktuální hlavní větve: **čeká na CI po obnově**.
Runtime test v cílové hře: **čeká na spuštění v uživatelově instalaci**.

Po každé změně runtime kódu musí projít `build-runtime.yml` a `validate.yml`. Verzi závislostí ani gameplay manifestu nezvyšovat bez kompatibilitního buildu a testu.
