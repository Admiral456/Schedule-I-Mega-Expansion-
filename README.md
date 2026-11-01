# Schedule I Mega Expansion

> **Stav: rozpracovaný základ Harborline.** Tento repozitář je zdrojem herního rozšíření, nikoli webového pomocníka Schedule-I.

Cílem je přirozeně rozšířit Hyland Point o nové obyvatelné a funkční oblasti. Vývoj staví na existujících systémech Schedule I: skutečných NPC, zákaznické ekonomice, rozvrzích, budovách, interakcích, ukládání a multiplayerové synchronizaci. Vlastní AI obyvatel ani druhý systém ekonomiky vytvářet nebudeme.

## Principy

1. **Nejdřív znovu použít hru.** Preferujeme existující prefaby, budovy, nábytek, světla, silniční prvky a herní interakce. Vlastní geometrii použijeme jen tam, kde ve hře vhodný prvek neexistuje.
2. **NPC jako data.** NPC budou tvořena jako S1API.Entities.NPC přes původní herní NPC systém; vzhled generuje NPCAppearance.GenerateRandomAppearance(). Jejich zákaznické/dealerské vlastnosti, rozvrhy a kontakty budou definované v balíčku.
3. **Žádné vymyšlené souřadnice.** Pozice se musí zachytit přímo ve hře. 2D mapové souřadnice z webového pomocníka nejsou Unity World X/Y/Z.
4. **Host je autorita.** Síťové změny a stav světa musí respektovat síťové/ukládací mechanismy hry a S1API.
5. **Po malých ověřitelných krocích.** Nejdřív funkční Harborline, až potom další provincie a rozšiřování mobilní sítě.

## První oblast: Harborline

Harborline je pojmenovaná podčtvrť původního herního regionu **Docks**; nevytváříme nepodporovanou novou hodnotu region enum. Plánované dominanty: Portside Supply, Harbor Freight Warehouse, Harbor Market, Harbor Auto Works, Harbor Offices, Harbor Power Plant, Pier Promenade a policejní kontrolní bod. Seznam je v `world/harborline/anchors.json`; dosud neobsahuje herní souřadnice a není určen k přímému spawnu.

NPC role a cílové počty jsou v `packs/Harborline/roster.json`: nově 42 fyzických NPC, z toho zhruba 18 současně aktivních, 30 zákaznických profilů a 2 dealeři. Harborline je rozdělený do pěti propojených podčástí. Detail rozšíření je v `world/harborline/expansion-plan.json`.

## Generování NPC a multiplayer

- Každý zákazník má vlastní náhodné preference, ale generování je deterministické podle stabilního ID a uložené do save; po načtení ani připojení hráče se znovu nelosuje.
- Vzhledy generuje nativní NPCAppearance.GenerateRandomAppearance(); přesné duplicity se mají kontrolovat na skutečných AvatarSettings.
- Policie se nebude nahrazovat vlastním AI; Harborline se napojí na nativní policejní spawn/dispatch a navigaci.
- Host je autorita pro spawn a stav NPC. Všichni hráči musí mít shodnou verzi modu a S1API.
- Detailní kontrakt a testovací brány: `docs/npc-generation-and-multiplayer.md`.

## Technologická vrstva

- **MelonLoader + S1API Forked** – základní mod runtime a herní API.
- **S1MAPI** – budovy a interiéry pouze pro prvky, které nelze vhodně znovu použít z původní hry.

Verze závislostí se uzamknou až při prvním sestavitelném runtime projektu. Viz [S1API Custom NPCs](https://ifbars.github.io/S1API/docs/custom-npcs.html), [NPCAppearance](https://ifbars.github.io/S1API/api/S1API.Entities.NPCAppearance.html) a [S1MAPI](https://ifbars.github.io/S1MAPI/).

## Aktuální stav a limity

- Repozitář zatím neobsahoval herní kód; tato větev připravuje datovou a validační základnu.
- Pozice budov, silnic a NPC musí být zachyceny v herním světě.
- Nákup businessů, obchodní inventář, policie a Hyland Network se napojí na původní systémy až po ověření jejich API v cílové verzi hry.
- Tato větev nyní obsahuje základní diagnostický IL2CPP mod, ale zatím není ověřeně sestavený ani otestovaný ve hře. Diagnostický runtime nic nemění; jeho úkolem je vypsat původní registry pro další implementaci.

## Struktura

- `docs/architecture.md` – technická architektura a pořadí implementace.
- `docs/prefab-reuse-rules.md` – pravidla pro opakované použití vanilla prefabů a multiplayer.
- `docs/harborline-survey-checklist.md` – přesný postup zaměření spawnu, tras, budov a checkpointů ve hře.
- `docs/npc-generation-and-multiplayer.md` – náhodné preference, nativní generování vzhledu, policie a multiplayer.
- `docs/native-map-buildings-interiors.md` – herní mapa, nativní ikony/vlastnictví, dveře a funkční interiéry.
- `world/harborline/native-integration.json` – nativní integrační pravidla a kontrolní brány.
- `src/` – první IL2CPP diagnostický runtime projekt v režimu pouze pro čtení; exportuje původní registry hry.
- `world/harborline/anchors.json` – seznam lokalit a míst k zaměření.
- `world/harborline/expansion-plan.json` – plán větší oblasti a pěti podčástí.
- `world/harborline/prefabs.json` – katalog původních prefabů ověřených přímo ve hře.
- `packs/Harborline/roster.json` – role a cílové počty NPC.
- `packs/Harborline/customer-designs.json` – 30 návrhů zákaznických preferencí, které se před vydáním mapují na ověřená herní data.
- `tools/validate_personnel_pack.py` zůstává pouze pomocným validátorem cizích Personnel packů; není součástí plánovaného runtime.
- `tests/` – testy validátoru.

## Lokální kontrola

```bash
python -m unittest discover -s tests -v

```

Runtime balíček se nesmí označit za hotový, dokud nemá stabilní NPC ID, skutečné spawn souřadnice, ověřené trasy a průchozí budovy.
