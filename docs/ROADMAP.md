# Roadmap

## Repository foundation
- Requirements, three-province design and stable data IDs.
- JSON schemas and cross-file validation.
- Game-independent domain services and unit tests.
- Native S1API phone navigation shell.
- S1API per-slot Saveable adapter scaffold.
- SteamNetworkLib initialization and protocol compatibility handshake.

## First integrated vertical slice
1. Compile the game-facing project against the exact local IL2CPP dependency set.
2. Connect the persisted shop snapshot to actual Schedule I cash and item inventory transactions.
3. Seed Portside Supply from a shared product/price registry and verify online/physical stock consistency.
4. Connect one real NPC profile and generated order to spawned NPC identity and schedule.
5. Integrate one physical shop and one Realtor listing.
6. Run a two-player host/client purchase test, reconnect and late-join snapshot.

## World construction
- Survey vanilla world bounds and candidate exits in-game.
- Build Harborline as a greybox first, including road/terrain/colliders.
- Validate vehicle and pedestrian navigation.
- Only after the first province works, build Ridgeview and Lakeside using the shared systems.
