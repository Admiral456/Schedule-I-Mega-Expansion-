# Deep research summary

## Frameworks

### S1API
Documentation describes Mono/IL2CPP support, NPCs, quests, items/products, phone apps, saveables and lifecycle hooks.
- https://ifbars.github.io/S1API/
- https://ifbars.github.io/S1API/docs/introduction.html

### S1MAPI
Describes procedural meshes, building construction and GLTF/GLB loading.
- https://github.com/ifBars/S1MAPI
- https://ifbars.github.io/S1MAPI/docs/getting-started.html
- Repository license: GPL-3.0; distribution impact requires review.
It does not establish runtime world streaming, terrain stitching, NavMesh, road graph, pedestrian routing or vehicle traffic.

### SteamNetworkLib
Describes Steam lobby management, P2P messaging and data synchronization.
- https://ifbars.github.io/SteamNetworkLib/
- https://github.com/ifBars/SteamNetworkLib
It is a transport utility, not a complete authority, transaction, persistence, anti-duplication or late-join snapshot implementation.

### MelonLoader
- https://github.com/LavaGang/MelonLoader
Pin and retest actual versions with each game patch.

### ScheduleOne-Personnel
- https://github.com/DooDesch-Mods/ScheduleOne-Personnel
Optional NPC framework candidate; exact API, compatibility and license must be rechecked before hard dependency.

## IL2CPP correction
Normal Steam branch is IL2CPP; alternate beta branch is Mono. Do not switch to Mono as a workaround. S1MAPI identifies S1MAPI_Il2Cpp.dll for default Steam branch and S1MAPI_Mono.dll for alternate branch. Earlier notes suggesting a Mono reference for IL2CPP must not be copied into final build.

## Open research questions
1. Can world bounds be extended without scene streaming changes?
2. How do generated colliders, batching and persistence behave?
3. Is navigation baked, runtime or custom?
4. How does vehicle traffic select roads?
5. How can new properties join vanilla Realtor?
6. Which NPC types/schedules are safe to save and network?
7. Which phone app framework is safest?
8. What are exact SteamNetworkLib reliability/ordering/late-join guarantees?
9. How does save migration behave on the current game build?

## Map mods as references only
Pier Expansion and DaffyHills were previously identified as experiments but reported issues against newer builds. They are not runtime dependencies. Their code/assets require license and compatibility review.
