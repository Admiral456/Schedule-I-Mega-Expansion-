# Shop purchase state snapshots

ShopPurchaseService exports and restores a detached JSON-serializable snapshot containing buyer balances, stock and prices, processed purchase requests and the economy revision.

The request ledger stores successful and failed outcomes. This prevents a request that failed for insufficient funds from unexpectedly becoming a successful purchase after a later balance change and host restart. Reusing an existing request ID with a different payload remains a conflict.

TryRestoreSnapshot validates the entire snapshot before replacing state. Invalid snapshots leave existing service state unchanged. Snapshot data is returned in stable key order.

This is a domain snapshot only. The game adapter still needs to connect it to the S1API Saveable and the actual Schedule I cash and item inventory APIs.
