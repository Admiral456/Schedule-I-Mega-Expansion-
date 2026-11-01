# Schedule I Mega Expansion

Fan-made world and gameplay expansion for Schedule I, targeting Steam Default / IL2CPP and two-player co-op.

> **Current state:** project foundation, independently tested domain core and first game-facing IL2CPP adapter scaffold. A playable three-province build does not exist yet.

## World scope
Preserve vanilla Hyland Point and add three major provinces:
- **Harborline** — southern coastal port, logistics, warehouses, shops and industry.
- **Ridgeview** — mountain quarry, processing, factories, research and freight contracts.
- **Lakeside** — lake, marina, resorts, hotels, recreation and luxury businesses.

## Gameplay scope
- **Hyland Network:** native in-game phone app shell, with live NPC contacts, orders, jobs and online shop services planned on top.
- **Portside Supply:** physical shop and online phone ordering with one shared stock and price source.
- **Realtor:** every purchasable expansion business is listed through the existing Realtor flow.
- **NPCs:** individual product preferences, budgets, price sensitivity, per-player reputation and order evaluation logic.
- **Dealers and police:** new dealers, patrol routes, checkpoints and controls in all three provinces.
- **Co-op:** host-authoritative ownership, shop transactions, orders, reputation, police and save state.

## Technical baseline
- S1API 3.2.2 for native phone apps and per-save-slot Saveables.
- S1MAPI 2.0.1 for geometry/building/GLTF prototypes.
- SteamNetworkLib 1.6.0 for Steam lobby and typed P2P.
- .NET 6.0 game-facing IL2CPP target.

See docs/FRAMEWORK_INTEGRATION.md and docs/COMPATIBILITY_AND_DEPENDENCIES.md for runtime setup and limitations.

## Start here
- [Scope](docs/PROJECT_SCOPE.md)
- [World map](docs/WORLD_MAP_AND_PROVINCES.md)
- [Hyland Network](docs/HYLAND_NETWORK.md)
- [NPC preferences](docs/NPC_CUSTOMER_PREFERENCES.md)
- [Multiplayer](docs/MULTIPLAYER_ARCHITECTURE.md)
- [Network protocol](docs/NETWORK_PROTOCOL.md)
- [Save store](docs/SAVE_STORE.md)
- [Framework integration](docs/FRAMEWORK_INTEGRATION.md)
- [Domain core](docs/DOMAIN_CORE.md)
- [Implementation plan](docs/IMPLEMENTATION_PLAN.md)
- [Roadmap](docs/ROADMAP.md)
- [Test plan](docs/TEST_PLAN.md)

## CI
GitHub Actions validates project JSON schemas/cross-file references and runs .NET domain-core unit tests. Game integration requires the player's local Schedule I IL2CPP assemblies and installed framework DLLs.

## Existing-save safety
The game adapter uses S1API's native per-save-slot Saveable mechanism. Future-schema or invalid data is held read-only in its raw serialized field; it is never silently reset. First in-game tests must use a copied save. Never point the sidecar JSON utility at a vanilla save file.

## Status
The NPC preference/budget evaluator and three differentiated example profiles are tested in the game-independent core. They are not yet wired to spawned NPCs or live Schedule I product definitions. The first phone-app navigation shell, S1API save adapter and Steam compatibility handshake are written as game-facing scaffolds but have not been compiled against the user's local game assemblies or exercised in-game. Map streaming, Realtor integration and live two-player gameplay are not yet verified.
