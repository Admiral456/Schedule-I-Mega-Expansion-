# Roadmap

## Repository foundation
- Requirements, three-province design and stable data IDs.
- JSON schemas and cross-file validation.
- Game-independent domain services and unit tests.
- Native S1API phone navigation shell.
- S1API per-slot Saveable adapter scaffold.
- SteamNetworkLib initialization and protocol compatibility handshake.

## First integrated vertical slice
1. Compile the game-facing project against the exact local IL2CPP dependency set and verify the host request/result handlers in a live co-op lobby.
2. Map the persistent order escrow to actual Schedule I player cash settlement and item inventory delivery APIs.
3. Seed Portside Supply from real Schedule I item definitions and verify physical/online stock consistency.
4. Connect one real NPC profile and generated order to a spawned NPC identity and in-game schedule.
5. Bind completed-order revenue settlement to real Schedule I cash, and bind refunds to actual item/restock APIs where required.
6. Integrate one physical shop and one Realtor listing.
7. Run a two-player request/replay/finalize/restart/reconnect test and implement late-join order snapshots.
## World construction
- Survey vanilla world bounds and candidate exits in-game.
- Build Harborline as a greybox first, including road/terrain/colliders.
- Validate vehicle and pedestrian navigation.
- Only after the first province works, build Ridgeview and Lakeside using the shared systems.
