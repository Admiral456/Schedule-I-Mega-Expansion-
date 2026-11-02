# Proč zatím nevytváříme nové Realtor nabídky přes interní seznamy

## Ověřený veřejný povrch S1API

Současný veřejný S1API zdroj poskytuje:
- `S1API.Property.PropertyManager.GetAllProperties()`
- `GetOwnedProperties()`
- `FindPropertyByName(string)`
- `S1API.Property.BusinessManager.GetAllBusinesses()`
- `GetOwnedBusinesses()`
- `FindBusinessByName(string)`
- `PropertyWrapper.SetOwned()` a cenu/vlastnické informace pro již existující objekt.

Toto jsou čtecí/úpravové operace pro již registrované nativní záznamy. Pro novou custom Property/Business nebyla ve veřejném S1API wrapperu nalezena podporovaná registrační metoda.

## Co už v modu používáme

- Existující Docks Warehouse pouze jako kotvu pro hledání volného místa.
- Unity geometrii pro první přístavní fyzický slice.
- Nativní NPC registry a prefab lifecycle pro dvě dealer a tři customer NPC.
- Nativní ShopManager/ItemManager a Shop.AddItem pro doplnění Hardware Store.

## Co je nutné ověřit pro Realtor

Z diagnostiky nainstalované verze Schedule I potřebujeme zejména:
- členy `Il2CppScheduleOne.Property.PropertyManager` (veřejné i neveřejné metody, properties a fields);
- typy, konstruktory a členy `Il2CppScheduleOne.Property.Property`, `Business`, `PropertyListing` a Realtor/real-estate UI;
- jak jsou native Property prefabs připojené k mapě, purchase flow, save data, ownership state a network replication;
- jaké callbacky hra volá při koupi existující nemovitosti.

Dokud tyto vazby nejsou ověřené, přímé vložení syntetického objektu do interního listu může vytvořit rozbitý Realtor záznam: bez validní dveřní cesty, mapového symbolu, kupní ceny, ukládání nebo multiplayeru. Takový záznam do modu nepřidáváme.

## Kritéria pro implementaci

1. Vytvořit skutečný native building/property z kompatibilního prefabu nebo podporovaného konstruktoru.
2. Zaregistrovat ho přes stejný native lifecycle, jaký používají vanilla properties.
3. Napojit Realtor listing, cenu, map icon, vstup, vlastnictví, save/load a host authority.
4. Otestovat koupit/prodat/načíst save a připojit klienta.
5. Teprve po splnění všech bodů jej uvést jako koupitelný podnik v Harborline.

Tento dokument je blokující technický požadavek, nikoli návrh falešné nabídky.