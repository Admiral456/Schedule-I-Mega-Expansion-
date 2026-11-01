# Schedule I Mega Expansion

> **Stav: rozpracovaný základ Harborline.** Tento repozitář je zdrojem herního rozšíření, nikoli webového pomocníka Schedule-I.

Cílem je přirozeně rozšířit Hyland Point o nové obyvatelné a funkční oblasti. Vývoj staví na existujících systémech Schedule I: skutečných NPC, zákaznické ekonomice, rozvrzích, budovách, interakcích, ukládání a multiplayerové synchronizaci. Vlastní AI obyvatel ani druhý systém ekonomiky vytvářet nebudeme.

## Principy

1. **Nejdřív znovu použít hru.** Preferujeme existující prefaby, budovy, nábytek, světla, silniční prvky a herní interakce. Vlastní geometrii použijeme jen tam, kde ve hře vhodný prvek neexistuje.
2. **NPC jako data.** NPC budou tvořena jako Personnel pack a poběží jako skutečné S1API postavy. Jejich zákaznické/dealerské vlastnosti, rozvrhy a kontakty budou definované v balíčku.
3. **Žádné vymyšlené souřadnice.** Pozice se musí zachytit přímo ve hře. 2D mapové souřadnice z webového pomocníka nejsou Unity World X/Y/Z.
4. **Host je autorita.** Síťové změny a stav světa musí respektovat síťové/ukládací mechanismy hry a S1API.
5. **Po malých ověřitelných krocích.** Nejdřív funkční Harborline, až potom další provincie a rozšiřování mobilní sítě.

## První oblast: Harborline

Plánované dominanty: Portside Supply, Harbor Freight Warehouse, Harbor Market, Harbor Auto Works, Harbor Offices, Harbor Power Plant, Pier Promenade a policejní kontrolní bod. Seznam je v `world/harborline/anchors.json`; dosud neobsahuje herní souřadnice a není určen k přímému spawnu.

NPC role a cílové počty jsou v `packs/Harborline/roster.json`. Finální herní manifest bude vytvořen z měření ve hře a ověřen nástrojem `tools/validate_personnel_pack.py`.

## Technologická vrstva

- **MelonLoader + S1API Forked** – základní mod runtime a herní API.
- **Personnel** – definice, registrace, spawn, rozvrhy, zákaznické/dealerské role, save/load a síťování NPC.
- **S1MAPI** – budovy a interiéry pouze pro prvky, které nelze vhodně znovu použít z původní hry.

Verze závislostí se uzamknou až při prvním sestavitelném runtime projektu. Aktuální Personnel dokumentace uvádí MelonLoader 0.7.3+ a S1API Forked 3.2.1+. Viz [Personnel](https://docs.doodesch.de/mods/personnel/) a [S1MAPI](https://ifbars.github.io/S1MAPI/).

## Aktuální stav a limity

- Repozitář zatím neobsahoval herní kód; tato větev připravuje datovou a validační základnu.
- Pozice budov, silnic a NPC musí být zachyceny v herním světě.
- Nákup businessů, obchodní inventář, policie a Hyland Network se napojí na původní systémy až po ověření jejich API v cílové verzi hry.
- Tato větev sama o sobě zatím není instalovatelným modem.

## Struktura

- `docs/architecture.md` – technická architektura a pořadí implementace.
- `docs/prefab-reuse-rules.md` – pravidla pro opakované použití vanilla prefabů a multiplayer.
- `world/harborline/anchors.json` – seznam lokalit a míst k zaměření.
- `world/harborline/prefabs.json` – katalog původních prefabů ověřených přímo ve hře.
- `packs/Harborline/roster.json` – role a cílové počty NPC.
- `packs/Harborline/customer-designs.json` – 14 individuálních návrhů zákaznických preferencí.
- `tools/validate_personnel_pack.py` – validace skutečného Personnel manifestu.
- `tests/` – testy validátoru.

## Lokální kontrola

```bash
python -m unittest discover -s tests -v
python tools/validate_personnel_pack.py path/to/manifest.json
```

Manifest se nesmí vydat, dokud nemá stabilní NPC ID, skutečné spawn souřadnice a ověřené trasy.
