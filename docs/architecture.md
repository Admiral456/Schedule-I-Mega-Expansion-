# Bezpečné opakované použití herních prefabů

Tento dokument je závazný pro všechny nové budovy, obchody a environmentální objekty v Harborline. Chybná volba síťového spawn režimu může rozbít FishNet multiplayer.

## 1. Nejprve ověřit existující objekt

- Prefab lze dohledat přes `S1MAPI.Core.PrefabRef` s přesným názvem v registru FishNet.
- `PrefabRef.Find()` vrací odpovídající herní GameObject nebo `null`.
- Do `world/harborline/prefabs.json` se zapíše přesný název, kategorie, zdroj, přítomnost `NetworkObject` a stav ověření.
- Název prefabů se nesmí domýšlet podle toho, jak objekt vypadá ve hře.

## 2. Síťové objekty

Pokud prefab obsahuje `NetworkObject`, vždy se musí vytvořit přes síťovou cestu:

- použít `PrefabRef.InstantiateNetworked()` nebo overload s parentem, lokální pozicí a rotací;
- volat pouze na hostu/serveru;
- pozici a rotaci nastavit před network spawnem, aby ji klienti obdrželi správně;
- nepoužívat lokální `Instantiate()` na síťovém prefabu.

Dokumentace S1MAPI uvádí mezi síťovými příklady ATM, interaktivní dveře, storage kontejnery, ModularSwitch a SlidingDoors. Konkrétní prefaby je ale nutné ověřit v aktuální instalaci hry.

## 3. Statické dekorace

Pro objekty bez `NetworkObject` lze použít lokální instanci, pokud jsou skutečně pouze vizuální a nepotřebují stav sdílený mezi hráči.

- Pro interiéry preferovat `InteriorBuilder` a jeho nativní herní nábytek (například stoly, židle, rostliny).
- Pro dostupné herní mesh assety použít S1MAPI `Meshes` / `MeshRef`.
- Nedávat statické dekorace do síťového spawn systému bez důvodu.

## 4. Pozor na BuildingBuilder

- `BuildingBuilder` vytváří geometrii budovy (podlahu, stěny, strop, světla, dekorace).
- `BuildingBuilder.AddPrefab()` přijímá `PrefabRef` a podle dokumentace vytváří síťovou instanci.
- Statické S1 nábytkové meshe patří do `InteriorBuilder`, ne do `AddPrefab()`.
- Jakékoli interaktivní dveře, ATM či skladovací kontejnery je nutné testovat na hostu i klientovi.

## 5. Povinný záznam do katalogu

Před použitím prefab v produkčním prostředí musí mít záznam:

| Pole | Význam |
|---|---|
| `id` | interní stabilní klíč |
| `exactPrefabName` | přesný název v herním registru |
| `category` | building, prop, furniture, door, storage, vehicle, lighting… |
| `networked` | skutečný výsledek kontroly komponent |
| `source` | místo/scene nebo API, odkud byl získán |
| `verified` | true až po testu v cílové verzi |
| `notes` | orientace, transform, závislosti nebo omezení |

Neověřený záznam se nesmí použít pro automatické generování. Prázdný katalog je správný stav, dokud neproběhne herní průzkum.

## Testovací checklist

1. Prefab se najde přes registr.
2. Je ověřeno, zda obsahuje `NetworkObject`.
3. Je umístěn na zaměřené pozici.
4. Host vidí správný objekt a interakce fungují.
5. Klient po připojení vidí stejný objekt a stejnou pozici.
6. Interakce (otevření dveří, použití ATM, storage) se synchronizuje.
7. Uložení a opětovné načtení nezanechá duplikáty.

## Reference

- S1MAPI PrefabRef: https://ifbars.github.io/S1MAPI/api/S1MAPI.Core.PrefabRef.html
- S1MAPI API Overview: https://ifbars.github.io/S1MAPI/docs/api-overview.html
- S1MAPI Building Guide: https://ifbars.github.io/S1MAPI/docs/building.html
