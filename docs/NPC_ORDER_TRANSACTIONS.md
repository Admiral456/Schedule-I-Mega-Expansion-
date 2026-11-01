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

The customer budget and escrow currently live in the expansion's persistent money ledger. The escrow is a held customer payment, not yet a deposit into vanilla Schedule I player cash. Real player cash payout, actual item definitions/inventory delivery, and the physical shop's live stock adapter still require the exact local Schedule I assemblies and API calls. The project deliberately does not invent those game APIs or seed fake stock. Until Portside stock is registered from real item definitions, order requests fail safely with `NoEligibleProduct`.

## Multiplayer boundary

Only an initialized SteamNetworkLib host in an active lobby may run the placement service or commit the save. The co-op client sends only a request ID, resident ID and shop ID. The host derives the requester identity from the Steam sender, reads reputation from its own save, generates the offer, persists the transaction and returns the committed order. A late-join snapshot and settlement/payout workflow remain separate follow-up work.