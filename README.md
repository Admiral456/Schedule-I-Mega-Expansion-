# Schedule I Mega Expansion

Fan-made world and gameplay expansion for Schedule I, targeting Steam Default / IL2CPP and two-player co-op.

> **Current state:** project foundation plus testable game-independent domain core. This is not yet a finished, compiled or tested in-game expansion DLL.

## World scope
Preserve vanilla Hyland Point and add three major provinces:
- **Harborline** — southern coastal port, logistics, warehouses, shops and industry.
- **Ridgeview** — mountain quarry, processing, factories, research and freight contracts.
- **Lakeside** — lake, marina, resorts, hotels, recreation and luxury businesses.

## Gameplay scope
- **Hyland Network:** phone app with contacts, profiles, messages, reputation, NPC orders, contracts and online shops.
- **Portside Supply:** physical shop and online phone ordering with one shared stock and price source.
- **Realtor:** every purchasable expansion business is listed through the existing Realtor flow.
- **NPCs:** individual preferences, budgets, schedules, relationships and generated customer orders.
- **Dealers and police:** new dealers, patrol routes, checkpoints and controls in all three provinces.
- **Co-op:** host-authoritative ownership, shop transactions, orders, reputation, police and save state.

## Technical candidates
MelonLoader, S1API, S1MAPI and SteamNetworkLib IL2CPP. Sideload and ScheduleOne-Personnel remain optional candidates pending integration/license checks.

These candidates do not prove map streaming, NavMesh, vehicle road graph extension, Realtor hooks or multiplayer persistence. Those require separate in-game proof-of-concepts.

## Start here
- [Scope](docs/PROJECT_SCOPE.md)
- [World map](docs/WORLD_MAP_AND_PROVINCES.md)
- [Hyland Network](docs/HYLAND_NETWORK.md)
- [Multiplayer](docs/MULTIPLAYER_ARCHITECTURE.md)
- [Network protocol](docs/NETWORK_PROTOCOL.md)
- [Save store](docs/SAVE_STORE.md)
- [Domain core](docs/DOMAIN_CORE.md)
- [Compatibility](docs/COMPATIBILITY_AND_DEPENDENCIES.md)
- [Technical spikes](docs/TECHNICAL_SPIKES.md)
- [Implementation plan](docs/IMPLEMENTATION_PLAN.md)
- [Roadmap](docs/ROADMAP.md)
- [Test plan](docs/TEST_PLAN.md)

## CI
GitHub Actions validates project JSON schemas/cross-file references and runs .NET domain-core unit tests.

## Existing-save safety
The core save store writes only to a caller-provided mod-owned path and maintains a previous-file backup. It is not wired to the game's S1API save lifecycle. Never point it at the vanilla save file. First tests must use a copy of the existing save.

## Status
The domain core is tested independently, but no physical map, phone app, Realtor integration, NPC AI or two-player game adapter is claimed complete.
