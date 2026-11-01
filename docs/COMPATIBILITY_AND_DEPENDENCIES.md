# Compatibility and dependencies

## Target
Steam Default / IL2CPP. Build number must be rechecked before every test/release. 0.4.7f12 is only a historical target from earlier planning, not a permanent guarantee.

| Component | Purpose | Status |
|---|---|---|
| MelonLoader 0.7.3+ | Runtime loader | Baseline candidate; retest game compatibility |
| ifBars/S1API | NPC, quests, items, phone apps, saveables | Preferred gameplay abstraction |
| ifBars/S1MAPI | Geometry/buildings/GLTF | Geometry helper; not proof of world streaming |
| SteamNetworkLib IL2CPP | Steam lobby/P2P/data sync | Transport candidate; authority is our code |
| ScheduleOne-Personnel | NPC packs/schedules/customers/dealers | Optional; API and license review |
| Sideload | HTML phone UI | Optional; compare with S1API phone apps |

## Primary links
- S1API: https://ifbars.github.io/S1API/
- S1MAPI: https://github.com/ifBars/S1MAPI
- SteamNetworkLib: https://github.com/ifBars/SteamNetworkLib
- MelonLoader: https://github.com/LavaGang/MelonLoader
- ScheduleOne-Personnel: https://github.com/DooDesch-Mods/ScheduleOne-Personnel

## Licensing
S1API documentation/repository reports MIT; SteamNetworkLib reports MIT; S1MAPI repository reports GPL-3.0. Every dependency/asset needs recorded version, source, license and redistribution decision. Do not bundle third-party DLLs without permission.

## Unknowns
Vanilla Realtor hook, region registration, map streaming, NavMesh/road graph, vehicle AI, NPC network persistence, external phone UI API, save migration on current public build and late-join guarantees are not yet proven.
