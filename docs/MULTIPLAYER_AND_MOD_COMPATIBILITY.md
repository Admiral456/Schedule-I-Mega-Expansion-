# Multiplayer and third-party mod compatibility

## Runtime target

The current development baseline is Steam Default / IL2CPP with S1API 3.2.2, S1MAPI 2.0.1 and SteamNetworkLib 1.6.0. The exact game build and dependency versions must be captured from the local test installation before a game-facing build is accepted.

The core randomizer has no Unity or Schedule I assembly dependency. Runtime-specific mapping stays behind the S1API adapter.

## NPCs and world state

- Register expansion NPCs through S1API's native NPC prefab/lifecycle API.
- Do not create a second NPC save, schedule, relationship or customer contract system.
- Native Schedule I customer contracts remain owned by `NPCCustomer`.
- The host owns first-time affinity generation and any persisted appearance seed/settings.
- A client must not independently create a second physical instance or roll different customer preferences.
- Use stable IDs under `sime.npc.*`, unique across all expansion content.
- Shared shop, business, order, law-event and checkpoint state remains host-authoritative.

## Co-op compatibility gate

Both players must use a compatible Schedule I build, S1API version, Mega Expansion version and protocol. If the handshake rejects a peer, do not accept expansion-specific state-changing requests from that peer.

Test host and client for:
1. first NPC spawn and arrival after scene load;
2. same visual identity and drug affinities on both clients;
3. schedule movement and interactions;
4. customer offer generation/contract progression;
5. save, reload, reconnect and late join;
6. police patrol/checkpoint synchronization;
7. no duplicate NPC after scene reload or reconnect.

## Other mods

Compatibility policy:
- depend on public S1API surfaces rather than private game classes wherever possible;
- namespace IDs, save keys, phone app identifiers and message types;
- do not replace vanilla prefabs or globally patch random, schedule, law, inventory or relationship systems;
- avoid hard dependencies on optional mods such as Personnel; use a soft integration only if the user has it installed;
- scope any Harmony patch to the smallest target and check for other active patches before changing behavior;
- tolerate unknown JSON fields in expansion-owned data where the schema allows it; preserve unsupported save data read-only;
- do not assume a third-party mod's data or internal API is safe to call.

Universal compatibility cannot be guaranteed. Maintain a compatibility matrix of tested mod combinations and explicitly report known conflicts. Initial tests should include no other mods, S1API-only, and a small set of commonly used NPC/world/economy mods once identified.

## Failure handling

- Missing or newer dependencies: log one actionable warning and disable only the affected expansion feature.
- Missing optional mod: keep core content available; skip the optional integration.
- Protocol mismatch: reject expansion mutation requests without touching the save.
- Invalid or incompatible save schema: preserve source data read-only rather than resetting it.
- Unknown prefab/route/DrugType: skip the dependent feature and write a clear diagnostic.


## Native police activity in Harborline

Harborline does not create replacement police NPCs or override law intensity. After the live world contains verified patrol routes, the game adapter discovers them through `LawManager.GetAllFootPatrolRoutes()` and `GetAllVehiclePatrolRoutes()` (or resolves exact names with `FindFootPatrolRoute` / `FindVehiclePatrolRoute`). The native `StartFootPatrol` and `StartVehiclePatrol` APIs draw officers from the nearest police station and fail if the required staff or vehicle is not available.

Checkpoint activation stays inside `CheckpointManager` and the game's automatic evaluation rules. Its evaluation considers intensity, time, curfew, player distance and officer availability. As law intensity is global, Harborline must not set a district-specific intensity by changing the world-wide value. See the three planned Harbor Gate, Freight Depot and Market Square anchors in `data/harborline_population.json`.

A patrol or checkpoint is not marked complete until its route/point exists in the live scene and a host/client test confirms the officers move, assign and release correctly.
