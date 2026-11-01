# Schedule I Mega Expansion

Fan-made world and gameplay expansion for Schedule I, targeting Steam Default / IL2CPP and two-player co-op.

> **Current state:** repository foundation, tested game-independent domain core and first native S1API phone/save/network scaffolds. This is not yet a playable three-province build.

## World scope
Preserve vanilla Hyland Point and add three major provinces:
- **Harborline** — coastal port, logistics, warehouses, shops and industry.
- **Ridgeview** — mountain quarry, processing, factories, research and freight contracts.
- **Lakeside** — lake, marina, resorts, hotels, recreation and luxury businesses.

## Gameplay scope
- Hyland Network phone app with Home, Contacts, Orders, Online Shop, Businesses and Provinces.
- Portside Supply physical store and planned online catalogue with one shared stock and price source.
- Every purchasable expansion business must appear in the existing Realtor.
- Residents have distinct preferences, budgets, schedules and per-player reputation.
- New dealers, police patrols and checkpoints across the expansion provinces.
- Host-authoritative co-op for shared world state, orders, stock, reputation and ownership.

## Implemented domain foundation
- Nine resident profiles (three per province), preference evaluation and deterministic accepted-order generation.
- NPC order offers reject unknown/unliked products and respect stock, budget, reputation and deterministic world intervals.
- ShopPurchaseService performs atomic in-memory purchases, prevents request replay/double charges and can export/restore validated economy snapshots.
- Snapshot data includes balances, prices, stock, processed successful and failed requests, enabling reliable recovery after a host save/load.
- S1API Saveable now contains the optional shop snapshot and exposes host-only load/commit methods with optimistic world-revision checks to reject stale writes.
- NPC order requests now run through a host-authoritative Steam request/response path. The host resolves the NPC profile, player reputation, stock and pricing; the transaction reserves stock, debits a player-specific customer budget, funds order escrow and saves the order plus an idempotency receipt under one world revision.
- The Hyland Network Contacts screen exposes a request action and clients receive host-confirmed order results.

The host order pipeline currently uses an expansion-side customer budget and escrow ledger, not vanilla Schedule I player cash. Actual player-cash settlement, live item inventory delivery, Portside stock seeding from real game item definitions, spawned-NPC binding and late-join snapshots still require integration. The order deadline currently uses host UTC time until the game-clock adapter is connected.

## Technical baseline
- S1API 3.2.2 — native phone apps and per-save-slot Saveables.
- S1MAPI 2.0.1 — geometry/building/GLTF prototype candidate.
- SteamNetworkLib 1.6.0 — Steam lobby and typed P2P.
- .NET 6.0 game-facing IL2CPP target.

See docs/FRAMEWORK_INTEGRATION.md, docs/PHONE_APP_IMPLEMENTATION.md, docs/SHOP_PURCHASE_SNAPSHOTS.md and docs/COMPATIBILITY_AND_DEPENDENCIES.md.

## CI
GitHub Actions validates data registries and runs game-independent domain tests. Game-facing integration requires local Schedule I IL2CPP assemblies and installed dependencies.

## Safety and status
Use a copied save for first in-game tests. Unsupported/newer save schemas are preserved read-only. The phone app is still a read-only status shell; contacts, real cash/item checkout, Realtor, NPC AI, world streaming and live co-op gameplay are not yet integrated.
