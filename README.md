# Schedule I Mega Expansion

> **Stav: rozpracovaný základ Harborline.** Tento repozitář je zdrojem herního rozšíření, nikoli webového pomocníka Schedule-I.

Cílem je přirozeně rozšířit Hyland Point o nové obyvatelné a funkční oblasti. Vývoj staví na existujících systémech Schedule I: skutečných NPC, zákaznické ekonomice, rozvrzích, budovách, interakcích, ukládání a multiplayerové synchronizaci. Vlastní AI obyvatel ani druhý systém ekonomiky vytvářet nebudeme.

## Principy

1. **Nejdřív znovu použít hru.** Preferujeme existující prefaby, budovy, nábytek, světla, silniční prvky a herní interakce. Vlastní geometrii použijeme jen tam, kde ve hře vhodný prvek neexistuje.
2. **NPC jako data.** Cílem je tvořit NPC jako S1API.Entities.NPC přes původní herní systém. Jejich vzhled, zákaznické/dealerské vlastnosti a rozvrhy jsou zatím návrhem; runtime třídy NPC v projektu dosud nejsou.
3. **Žádné vymyšlené souřadnice.** Prototyp používá pozici existující Docks Warehouse a fyzikální kontroly Unity. Přesné hranice Docks a výsledné umístění zatím nejsou ověřené v cílové hře.
4. **Host je autorita.** Síťové změny a stav světa musí respektovat síťové/ukládací mechanismy hry a S1API.
5. **Po malých ověřitelných krocích.** Nejdřív funkční Harborline, až potom další provincie a rozšiřování mobilní sítě.

## První oblast: Harborline

Harborline je pojmenovaná podčtvrť původního herního regionu **Docks**; nevytváříme nepodporovanou novou hodnotu region enum. Plánované dominanty: Portside Supply, Harbor Freight Warehouse, Harbor Market, Harbor Auto Works, Harbor Offices, Harbor Power Plant, Pier Promenade a policejní kontrolní bod. Seznam je v `world/harborline/anchors.json`; dosud neobsahuje herní souřadnice a není určen k přímému spawnu.

NPC role a cílové počty jsou v `packs/Harborline/roster.json`: nově 42 fyzických NPC, z toho zhruba 18 současně aktivních, 30 zákaznických profilů a 2 dealeři. Harborline je rozdělený do pěti propojených podčástí. Detail rozšíření je v `world/harborline/expansion-plan.json`.

## Návrh NPC a multiplayeru – zatím neimplementováno

- Plán počítá s vlastními zákaznickými preferencemi a jejich uložením. V současném runtime však nejsou žádné Harborline NPC třídy ani ukládání těchto profilů.
- Nativní vzhled má být vytvořen přes Appearance.GenerateRandomAppearance() a Appearance.Build(); tyto metody zatím nejsou volány žádnou Harborline NPC třídou.
- Policie se nebude nahrazovat vlastním AI; napojení na nativní spawn/dispatch zatím není implementováno.
- Host má být autoritou pro spawn a stav NPC. Multiplayerová implementace zatím není přítomná.
- Detailní kontrakt a testovací brány: `docs/npc-generation-and-multiplayer.md`.

## Technologická vrstva

- **MelonLoader + S1API Forked** – základní mod runtime a herní API.
- **S1MAPI** – budovy a interiéry pouze pro prvky, které nelze vhodně znovu použít z původní hry.

Diagnostický IL2CPP projekt cílí na .NET 6 a MelonLoader 0.7.3; S1API se objevuje dynamicky až v běžící hře, takže sestavení nevyžaduje kopii herních DLL. Výstup musí být ověřen proti skutečně nainstalované verzi S1API v cílové hře. Viz [S1API Custom NPCs](https://ifbars.github.io/S1API/docs/custom-npcs.html), [NPCAppearance](https://ifbars.github.io/S1API/api/S1API.Entities.NPCAppearance.html) a [S1MAPI](https://ifbars.github.io/S1MAPI/).

## Instalace a závislosti

Pro mod manager je připraven manifest `manifest.json`. Balíček obsahuje samotný DLL a automaticky vyžaduje MelonLoader 0.7.3, S1API Forked 3.2.2 a S1MAPI 2.0.1. Všechny tři jsou samostatné knihovny; DLL modu je neobsahuje.

## Aktuální stav a limity

- Runtime obsahuje automatické generování pouze první geometrické prototypové části z Unity primitiv po načtení save. Přesné umístění, kolize a běh v cílové hře nejsou ověřeny. Nejde o nativní budovu, podnik ani obchod a žádné Harborline NPC se zatím nevytváří.
- Umístění se odvozuje z nativní pozice Docks Warehouse a runtime fyzikálních kontrol. To ale samo o sobě nezaručuje, že kandidát leží uvnitř hranic Docks nebo že není nad vodou s neoznačeným colliderem.
- Nativní Realtor/property registrace, mapové ikony, funkční interiéry, dveře, obchody, online objednávky, NPC, policie, ukládání a multiplayer nejsou v runtime implementované. Veřejné S1API PropertyManager a BusinessManager zatím pouze enumerují existující záznamy.
- GitHub Actions sestavuje IL2CPP mod a balí Thunderstore ZIP. Statické testy a build neprokazují chování ve hře. Diagnostické exportéry pouze čtou registry, ale celý mod navíc vytváří session-local Unity geometrii.

## Struktura

- `docs/architecture.md` – technická architektura a pořadí implementace.
- `docs/prefab-reuse-rules.md` – pravidla pro opakované použití vanilla prefabů a multiplayer.
- `docs/harborline-survey-checklist.md` – přesný postup zaměření spawnu, tras, budov a checkpointů ve hře.
- `docs/npc-generation-and-multiplayer.md` – náhodné preference, nativní generování vzhledu, policie a multiplayer.
- `docs/native-map-buildings-interiors.md` – herní mapa, nativní ikony/vlastnictví, dveře a funkční interiéry.
- `world/harborline/native-integration.json` – nativní integrační pravidla a kontrolní brány.
- `src/` – IL2CPP runtime prototyp, automatická geometrie a read-only průzkum nativních registrů a prefabů.
- `world/harborline/anchors.json` – seznam lokalit a míst k zaměření.
- `world/harborline/expansion-plan.json` – plán větší oblasti a pěti podčástí.
- `world/harborline/prefabs.json` – katalog kandidátních prefabů k ověření v cílové hře.
- `world/harborline/implementation-status.json` a `docs/implementation-audit.md` – matice skutečného stavu každého požadavku.
- `packs/Harborline/roster.json` – role a cílové počty NPC.
- `packs/Harborline/customer-designs.json` – 30 návrhů zákaznických preferencí, které se před vydáním mapují na ověřená herní data.
- `tools/validate_personnel_pack.py` zůstává pouze pomocným validátorem cizích Personnel packů; není součástí plánovaného runtime.
- `tests/` – testy validátoru.

## Lokální kontrola

```bash
python -m unittest discover -s tests -v
```

Runtime diagnostický mod se sestavuje jako samostatný .NET 6 projekt bez lokálních herních DLL. Příkaz `dotnet build src/ScheduleIMegaExpansion.csproj -c Release` vytvoří DLL; GitHub Actions navíc vytváří ZIP artefakt. Do hry se instaluje až po ověření s MelonLoader a S1API.

Současný fyzický prototyp není finální Harborline. Kompletní audit: docs/implementation-audit.md. Strojově čitelný stav: world/harborline/implementation-status.json.
