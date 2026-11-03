# Zaměření Harborline přímo ve hře

Tento seznam je pro první návštěvu nové oblasti a vytvoření přesných vstupních dat. Všechny výsledky musí pocházet z cílové verze Schedule I, nikoli z 2D mapových pixelů.

## Příprava

1. Zálohuj uloženou hru.
2. Spusť Schedule I s kompatibilní verzí MelonLoader, S1API Forked a Personnel.
3. Ověř, že Personnel načetl bez chyby. V konzoli použij `personnel help`; příkazy musí být dostupné.
4. Otevři save jako hostitel, ne jako připojený klient.

## Záznam poloh NPC

Pro každý plánovaný spawn:

1. Postav se přesně na místo, kde má NPC začínat den.
2. Spusť `personnel spawn`.
3. Ulož celý výstup včetně `x`, `y`, `z`, `rotationY`, `region` a `physical`.
4. Výstup přiřaď ke konkrétnímu ID z `packs/Harborline/roster.json`.

Pro jednotlivý bod denní trasy:

- Postav se na cílové místo a spusť `personnel pos HH:MM` (například `personnel pos 07:30`).
- Pro trasu z více bodů spusť `personnel route HH:MM` při každém zastavení.
- Hotový blok získáš přes `personnel route show`; reset rozpracované trasy přes `personnel route clear`.

Názvy existujících budov a regionů ponech přesně tak, jak je vrací hra. U každé trasy ověř, že vede po skutečně průchozím povrchu a NPC nezůstává zaseknuté.

## Povinné body k zaměření

| ID | Co zaznamenat |
|---|---|
| `docks_connection` | napojení původní Docks na novou oblast |
| `harborline_entry` | hlavní vstup a orientační bod |
| `portside_supply` | vstup do obchodu, pult a obslužný prostor |
| `harbor_freight_warehouse` | sklad, vstup a nákladní dvůr |
| `harbor_market` | stánky, hlavní průchod a zákaznická zóna |
| `harbor_auto_works` | vrata, pracovní plocha a parkování |
| `harbor_offices` | hlavní vstup a interiérový bod |
| `harbor_power_plant` | vjezd, obvod areálu a pracovní bod |
| `pier_promenade` | pěší cesta, rybářská místa a večerní zastávky |
| `checkpoint_harbor_entry` | kontrola vozidel při vjezdu od Docks |
| `checkpoint_freight_road` | kontrola nákladních vozidel |
| `checkpoint_market_exit` | kontrola u výjezdu od marketu |

## Vytvoření NPC

1. V hlavním menu otevři Side Hustle → Personify.
2. Vytvoř nový projekt/pack Harborline.
3. Pro první ověřovací balíček vytvoř jen 3–5 postav: jednoho pracovníka přístavu, jednoho prodejce, jednoho rybáře, jednoho zákazníka a případně dealera.
4. Nastav vzhled, roli, denní rutinu a vztahy. Zákazníka nastav jako skutečného Personnel zákazníka, nikoli jako běžný kontakt.
5. Otestuj postavu v existujícím save a exportuj Personnel pack.
6. Až po úspěšném testu rozšiř roster na všech 18 fyzických NPC a 14 zákaznických profilů.

## Co se musí vrátit do repozitáře

- Výstup každého `personnel spawn` pro body v tabulce.
- JSON bloky denních tras z `personnel route show`.
- Exportovaný Personify pack (manifest a případné obrázkové vrstvy).
- Přesná verze hry, MelonLoaderu, S1API, Personnel a S1MAPI.
- `MelonLoader/Latest.log` z načtení prvního testovacího packu.

## Stop podmínky

- Neznámá poloha zůstává `null`; nikdy se nedoplňuje odhadem.
- Pokud NPC nedojde do budovy, nejdřív se upraví spawn/route a průchodnost, ne jeho AI.
- Pokud multiplayerový klient nevidí stejného NPC nebo objekt, oblast se nerozšiřuje dál.
- Nepřidávat dalších 15 NPC, dokud první 3–5 neprojde spawnem, trasami, uložením a synchronizací.
