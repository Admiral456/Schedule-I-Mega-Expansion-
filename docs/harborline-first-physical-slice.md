# Harborline – první fyzický runtime slice

## Co se nyní skutečně vytváří

Po načtení uložené hry mod automaticky načte nativní umístění nemovitosti Docks Warehouse, prohledá okolní oblast a na nalezeném rovném volném terénu vytvoří fyzický přístavní areál jako sadu Unity GameObjects. Každý základní blok vzniká přes `GameObject.CreatePrimitive(PrimitiveType.Cube)`, takže má skutečnou geometrii, MeshRenderer a primitivní kolizní collider.

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
3. Po načtení save se Harborline vygeneruje automaticky. Není potřeba nic stisknout ani nastavovat.
4. Výsledek a zvolená pozice se zaznamenají do `UserData/ScheduleIMegaExpansion/Harborline/last-generation.json`.
5. Po načtení hry také vznikne `Diagnostics/<timestamp>/native-prefabs.csv`, který vypíše dostupné známé prefaby a jejich komponenty.

## Záměrná omezení této verze

Generování je automatické, ale toto je stále první fyzická prototypová etapa, nikoli hotová čtvrť. Umístění se odvozuje od Docks Warehouse a bezpečně ověřovaného terénu; při neúspěšné kontrole se nic nespawnuje. Objekty jsou zatím session-local a nejsou replikované přes multiplayer. Sklad není registrovaný jako nativní Schedule I Building/Property/Business, dveře jsou zatím průchozí otvor, ne nativní interaktivní dveře. Nejsou ještě vytvořeni NPC, zákaznické chování, obchody, Realtor záznamy, mobilní objednávky, policejní dispatch ani nativní mapové ikony.

Další implementace musí převést ověřené herní prefaby/komponenty a registry na skutečné herní systémy; současné dekorativní prvky nejsou vydávány za hotové funkční podniky.

## Katalog nativních prefabů

Diagnostika nyní používá pouze `S1MAPI.Core.PrefabRef` a dokumentovanou metodu `Find()` k ověření přítomnosti známých assetů jako ATM, dveře, storage a switch. Nevytváří jejich instance. `native-prefabs.csv` obsahuje název reference, zda byla nalezena, komponenty a informaci, zda má `NetworkObject`. To umožní další implementaci správně zvolit síťovou cestu místo rizikového lokálního spawnu.
