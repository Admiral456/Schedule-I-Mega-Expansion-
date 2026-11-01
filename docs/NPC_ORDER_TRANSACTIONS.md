# Host-authoritative NPC order transactions

`NpcOrderPlacementService` turns an NPC's deterministic offer into a persisted-ready order draft. It reads price and stock from the host's `ShopPurchaseService`; client messages do not contain prices, quantities, balances, player reputation or inventory values.

## Transaction

1. The host validates the active compatible peer and resolves the NPC profile from the local directory.
2. The host loads the last saved shop snapshot and its expected world revision.
3. A per-NPC, per-30-minute host interval budget account is initialized from that NPC's configured order budget, or the remaining account balance is reused.
4. The existing offer generator selects only preferred/neutral products and respects the authoritative shop stock and remaining customer budget.
5. Each line is purchased against the same local snapshot. If any line fails, the service restores the original snapshot; no partial order is committed.
6. The customer funds are moved into an order-specific escrow account. The resulting draft records the order, buyer account, escrow account, unit prices and quantities.
7. `ExpansionSaveable` commits the shop snapshot, order and request fingerprint together under one optimistic world revision, then requests the host game save.

Request IDs are namespaced with the sender's Steam ID and hashed into stable order IDs. Replaying an already saved request returns its existing order; using the same request key with a different customer/shop payload is rejected.

## Money and game-integration boundary

The customer budget and escrow currently live in the expansion's persistent money ledger. The escrow is a held customer payment, not yet a deposit into vanilla Schedule I player cash. Real player cash payout, actual item definitions/inventory delivery, and the physical shop's live stock adapter still require the exact local Schedule I assemblies and API calls. Domain escrow settlement and refunds are now implemented separately in NpcOrderSettlementService and the host save adapter. The project deliberately does not invent those game APIs or seed fake stock. Until Portside stock is registered from real item definitions, order requests fail safely with `NoEligibleProduct`.

## Multiplayer boundary

Only an initialized SteamNetworkLib host in an active lobby may run the placement service or commit the save. The co-op client sends only a request ID, resident ID and shop ID. The host derives the requester identity from the Steam sender, reads reputation from its own save, generates the offer, persists the transaction and returns the committed order. A late-join snapshot and vanilla Schedule I cash/item payout adapter remain separate follow-up work.

## Steam request path

The Contacts screen sends only a unique request ID, resident ID and shop ID. The host derives the requester from the Steam sender, reads reputation from its own save, chooses a host-time 30-minute interval and computes the deadline itself. The client accepts a response only from its sole compatible remote peer in the configured two-player lobby.

The host persists an idempotency receipt with a fingerprint of requester, customer and shop. A retry with the same ID and same payload returns the saved order; reusing the ID for a different payload is rejected. A player can have at most one non-terminal order from the same resident within the same 30-minute interval.

The customer budget account is isolated by NPC, Steam player and interval. This prevents one co-op partner from consuming another player's relationship-specific budget.

The current client order mirror is session-local. The host save remains the durable source of truth; late-join order snapshots are not implemented yet. The order clock currently uses host UTC time until the in-game Schedule I world-clock adapter is available.

## Escrow settlement and order closure

NpcOrderSettlementService handles the money and stock side of terminal order transitions.

- **Completed** orders transfer the exact escrow amount to a shop-revenue ledger account. The shop stock remains consumed.
- **Cancelled**, **Expired** and **Failed** orders return the escrow amount to the original customer budget and restore every reserved product quantity to the same shop.
- The service validates the complete refund plan before changing balances or inventory. If a product is missing or any balance/stock arithmetic overflows, no money or stock is changed.
- Each settlement/refund carries a deterministic operation ID. Replaying it returns the stored receipt and does not pay or refund twice.
- Escrow operation receipts are stored inside ShopPurchaseServiceSnapshot, including failed operations, so retry semantics survive a save reload.
- ExpansionSaveable.TryFinalizeNpcOrder applies the lifecycle transition, updated shop snapshot and order version in one host-only optimistic save revision.

The current revenue account and customer budget are expansion-side virtual ledger accounts. They are not vanilla Schedule I player cash. Real payout and item delivery still require a game adapter backed by the local game assemblies.