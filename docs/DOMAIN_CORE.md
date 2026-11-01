# Domain core

The src/Core library contains game-independent rules tested without launching Schedule I or loading MelonLoader.

## Included
- StableId: stable namespaced identifiers.
- OrderState: allowed customer-order state transitions.
- ShopPurchaseService: in-memory reference transaction with stock/balance checks and request idempotency.
- ReputationLedger: scoped reputation, bounds and duplicate-event protection.
- NetworkMessageValidator and ReplayWindow: authenticated envelope checks, payload limits and replay detection.
- SaveMigrationPipeline: explicit schema migration chain.
- JsonSaveStore: mod-owned JSON file replacement with previous-file backup.

## Important production boundaries
The shop service is in-memory and not connected to game cash, stock, save data or Steam networking. Its production transaction and idempotency ledger must persist atomically.

The replay window does not replace transport authentication, reliable delivery, encryption, host authorization or the SteamNetworkLib adapter.

The save store uses a caller-provided path and is not wired to the game's S1API save lifecycle. It must never target vanilla save files.

Tests prove isolated domain behavior only. They do not prove game integration, live co-op packet delivery, map streaming, Realtor or NPC AI.
