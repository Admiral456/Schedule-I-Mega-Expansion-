# Schedule I Mega Expansion

Fan-made world and gameplay expansion for Schedule I, targeting Steam Default / IL2CPP and two-player co-op.

> **Project state:** foundation/specification in progress. This repository does not yet contain a finished, compiled, or tested in-game expansion DLL.

## Scope
Preserve vanilla Hyland Point and add exactly three major provinces:
- **Harborline** — southern coastal port, logistics, warehouses, shops and industry.
- **Ridgeview** — mountain quarry, processing, factories, research and freight contracts.
- **Lakeside** — lake, marina, resorts, hotels, recreation and luxury businesses.

All provinces share one world, economy, NPC/contact network, police simulation, business ownership and co-op state.

## Required systems
- **Hyland Network:** phone app with contacts, NPC profiles, messages, per-NPC reputation, orders, contracts and online shops.
- **Portside Supply:** physical shop and online phone ordering backed by one shared inventory/pricing system.
- **Realtor integration:** every purchasable new business must be listed through the existing Realtor flow.
- **NPC:** named residents, individual product preferences, budgets, schedules and generated orders.
- **Dealer network:** new province contacts, inventory and customer relationships.
- **Police:** checkpoints, patrols, inspections and events across all three provinces.
- **Co-op:** host-authoritative economy, ownership, orders, police events, save/load and late join.

## Technical candidates
- MelonLoader
- ifBars/S1API
- ifBars/S1MAPI
- SteamNetworkLib IL2CPP
- Sideload (only if the S1API phone app path needs it)
- ScheduleOne-Personnel (optional NPC framework; API/licensing check required)

Frameworks are candidates, not proof that map streaming, NavMesh, vehicle road graphs, Realtor extension or multiplayer persistence already work. Those are tracked as technical spikes.

## Start here
- [Project scope](docs/PROJECT_SCOPE.md)
- [World map and provinces](docs/WORLD_MAP_AND_PROVINCES.md)
- [Hyland Network](docs/HYLAND_NETWORK.md)
- [Multiplayer architecture](docs/MULTIPLAYER_ARCHITECTURE.md)
- [Save migration and safety](docs/SAVE_AND_MIGRATIONS.md)
- [Compatibility and dependencies](docs/COMPATIBILITY_AND_DEPENDENCIES.md)
- [Technical spikes](docs/TECHNICAL_SPIKES.md)
- [Roadmap](docs/ROADMAP.md)
- [Test plan](docs/TEST_PLAN.md)
- [Deep research summary](docs/DEEP_RESEARCH_SUMMARY.md)

## Save safety
All first tests use a copy of an existing save. Mod data is namespaced and versioned. Migrations must be reversible and idempotent. The vanilla save must never be silently rewritten.

## Status
Foundation files are being established. No finished map, shop, NPC, phone app or co-op feature is claimed until it passes the documented tests.
