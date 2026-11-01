# Harborline – první fyzický runtime slice

## Co se nyní skutečně vytváří

Mod umí po načtení uložené hry zachytit aktuální pozici a natočení hlavní herní kamery. Na F7 následně vytvoří v herním světě fyzický přístavní areál jako sadu Unity GameObjects. Každý základní blok vzniká přes `GameObject.CreatePrimitive(PrimitiveType.Cube)`, takže má skutečnou geometrii, MeshRenderer a primitivní kolizní collider.

Slice obsahuje:
- velkou přístavní zpevněnou plochu, asfaltovou komunikaci a vodicí čáry;
- sklad s podlahou, čtyřmi stěnami, střechou, průchozím nakládacím vjezdem a základním interiérem;
- skladové regály, pult, palety a náklad;
- barevné přepravní kontejnery s žebrováním;
- jednoduchou portálovou konstrukci jeřábu s ramenem a hákem;
- lampy, patníky, bezpečnostní zábranu, návěstní tabuli, molo a přístavní hranu.

## Použití ve hře

1. Nainstaluj DLL do `Schedule I/Mods`.
2. Spusť single-player hru a načti uloženou pozici.
3. Jdi do oblasti Docks na místo, kde chceš Harborline připojit. Stůj na volném místě a otoč se směrem do volného prostoru.
4. Stiskni F6. Vznikne `UserData/ScheduleIMegaExpansion/Harborline/harborline-anchor.json`.
5. Stiskni F7. Geometrie se vytvoří přibližně 22 metrů před zachyceným bodem.

## Záměrná omezení této verze

Toto je první fyzická prototypová etapa, nikoli hotová čtvrť. Objekty jsou zatím session-local a nejsou replikované přes multiplayer. Sklad není registrovaný jako nativní Schedule I Building/Property/Business, dveře jsou zatím průchozí otvor, ne nativní interaktivní dveře. Nejsou ještě vytvořeni NPC, zákaznické chování, obchody, Realtor záznamy, mobilní objednávky, policejní dispatch ani nativní mapové ikony.

Další implementace musí převést ověřené herní prefaby/komponenty a registry na skutečné herní systémy; současné dekorativní prvky nejsou vydávány za hotové funkční podniky.
