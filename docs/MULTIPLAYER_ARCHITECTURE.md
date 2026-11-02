# Multiplayer architecture — two players

## Host-authoritative rule
Host is the source of truth for shared state. Clients send requests; host validates and commits. A client never locally commits money, inventory, reputation, ownership or order completion.

## Shared authoritative state
Province unlocks, business ownership, Realtor listings, shop stock/prices/orders/deliveries, shared NPC schedules, customer orders, jobs, dealer stock, reputation events, police checkpoints/patrols, contracts and expansion save state.

## Client-local state
UI layout, notification presentation, map filters, audio and accessibility settings.

## Message envelope
Each message contains protocolVersion, messageId, sessionId, senderSteamId, sequence, requestId, messageType, payload and optional worldRevision. Host validates sender, permission, payload bounds, sequence/replay, revision and idempotency key.

## Example purchase transaction
1. Client requests ShopPurchaseRequested with item ID, quantity and unique request ID.
2. Host validates shop, price, stock, budget and limits.
3. Host atomically deducts money, decreases stock and creates the order.
4. Host persists the result.
5. Host broadcasts ShopPurchaseCommitted and world revision.
6. Both clients render the same confirmed state.

## Late join
Host sends schema version and world revision, then a snapshot of provinces, ownership, stock, orders, reputation, jobs, police events and relevant NPC state. After acknowledgement, client receives incremental events. On mismatch, force a controlled resync.

## Disconnect/reconnect
Host continues. Pending operations are idempotent. Client rejoins from last confirmed revision. Disconnect during purchase/delivery cannot produce a double charge or reward.

## SteamNetworkLib
Candidate transport for Steam lobby/P2P/data sync. Exact API must be proven in a small test mod. The library itself is not the game's authority, transaction engine, persistence layer or late-join snapshot solution.

## Acceptance tests
- Client joins host.
- Compatible protocol and mod versions required.
- Client purchase processes exactly once.
- Two requests for last item: only one succeeds.
- NPC order generated once.
- Reputation matches on both clients.
- Business purchased exactly once.
- Police event is synchronized.
- Reconnect and late join load correct snapshot.
