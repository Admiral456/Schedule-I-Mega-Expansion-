# Compatibility and dependencies

## Target
Schedule I Steam Default / IL2CPP. The current development baseline is .NET 6.0 for the game-facing mod. Capture the exact game build from the local test installation before every in-game integration test and release.

| Component | Baseline | Runtime / framework | Role | Status |
|---|---:|---|---|---|
| MelonLoader | Installed with current Schedule I profile | IL2CPP | Mod loader | Local version/build must be captured and tested |
| ifBars/S1API | 3.2.2 | IL2CPP / .NET 6.0 | NPC, phone apps, saveables, game API | Pinned development baseline |
| ifBars/S1MAPI | 2.0.1 | IL2CPP / .NET 6.0 | Geometry, buildings, GLTF/GLB | Pinned candidate; geometry spike pending |
| ifBars/SteamNetworkLib | 1.6.0 | IL2CPP release DLL / library target .NET Standard 2.1 | Steam lobby and typed P2P | Pinned development baseline |
| Sideload | Not pinned | Optional | Alternative phone web UI | Not needed unless S1API phone app path proves insufficient |
| ScheduleOne-Personnel | Not pinned | Optional | NPC pack integration | Not a hard dependency |

## Verified upstream references
- S1API: https://github.com/ifBars/S1API
- S1API phone app docs: https://ifbars.github.io/S1API/docs/phone-app.html
- S1API save system docs: https://ifbars.github.io/S1API/docs/save-system.html
- S1MAPI: https://github.com/ifBars/S1MAPI
- SteamNetworkLib: https://github.com/ifBars/SteamNetworkLib
- SteamNetworkLib P2P docs: https://ifbars.github.io/SteamNetworkLib/docs/p2p-messaging.html

## Target-framework alignment
The current ifBars S1API IL2CPP configuration targets net6.0. The current S1MAPI IL2CPP configuration also targets net6.0 and emits S1MAPI_Il2cpp.dll. SteamNetworkLib 1.6.0 targets netstandard2.1 and publishes separate Mono and IL2CPP binaries.

Do not switch the game-facing project to Mono or mix Mono runtime binaries into the default Steam IL2CPP build.

## Persistence
Use S1API Saveable for the actual per-slot Schedule I save lifecycle. The separate JsonSaveStore<T> class is an isolated sidecar utility and is not the in-game save adapter.

## Licensing
- S1API upstream reports MIT.
- SteamNetworkLib reports MIT.
- S1MAPI reports GPL-3.0-or-later. Review GPL distribution implications before bundling or redistributing its DLL.
- Every additional dependency/asset needs a recorded version, source, license and redistribution decision.

## Open technical questions
- World bounds, world streaming and terrain stitching.
- Runtime navmesh and pedestrian/vehicle route graphs.
- S1MAPI building colliders and scene persistence.
- Vanilla Realtor listing adapter.
- S1API NPC scheduling and runtime behavior in the current game build.
- SteamNetworkLib two-player request/response and reconnect in the real game.
- Saveable lifecycle behavior against the current public Schedule I build.
