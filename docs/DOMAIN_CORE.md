# Domain core

The src/Core library contains game-independent rules tested without launching Schedule I or loading MelonLoader.

## Included
- StableId: stable namespaced identifiers.
- OrderState: allowed customer-order state transitions.
- ShopPurchaseService: in-memory reference transaction with stock/balance checks and request idempotency.
- NpcCustomerAcceptance: deterministic preference, budget, price-sensitivity and player-reputation evaluation for NPC customer orders.
- ReputationLedger: scoped reputation, bounds and duplicate-event protection.
- NetworkMessageValidator and ReplayWindow: authenticated envelope checks, payload limits and replay detection.
- SaveMigrationPipeline: explicit schema migration chain.
- JsonSaveStore: mod-owned JSON file replacement with previous-file backup.

## Important production boundaries
The shop service is in-memory and not connected to game cash, stock, save data or Steam networking. Its production transaction and idempotency ledger must persist atomically.

NpcCustomerAcceptance only evaluates a proposed order from supplied profile data. It does not spawn NPCs or orders, connect to Schedule I item definitions or access the game AI. The host must own generation and persistence.

The replay window does not replace transport authentication, reliable delivery, encryption, host authorization or the SteamNetworkLib adapter.

The generic save store is not wired to the game's S1API save lifecycle and must never target vanilla save files.

Tests prove isolated domain behavior only. They do not prove game integration, live co-op packet delivery, map streaming, Realtor or NPC AI.
