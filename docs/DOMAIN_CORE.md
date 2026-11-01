# Domain core

The src/Core library contains game-independent rules that can be tested without launching Schedule I or loading MelonLoader.

## Included
- StableId: namespaced stable IDs; display names are never persistence keys.
- OrderState: allowed transitions for NPC and shop orders.
- ShopPurchaseService: in-memory reference transaction with request idempotency, stock checks and balance checks.
- ReputationLedger: scoped reputation with bounds and duplicate-event protection.

## Important production boundary
ShopPurchaseService is an in-memory domain reference, not yet connected to Schedule I inventory, cash, save data or Steam networking. The host must call it only after the network adapter validates a client request. A production implementation must persist state and the idempotency ledger atomically; otherwise a crash between memory mutation and save could still lose or repeat a transaction.

The tests prove domain behavior only. They do not prove that game integration, co-op packet delivery or save persistence work.
