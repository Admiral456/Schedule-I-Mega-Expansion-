# Schedule I game-economy adapter: verified API boundary

Last checked against the public S1API documentation: 2026-11-01.

This document records what the current public S1API surface can safely support and what still needs inspection of the exact local Schedule I IL2CPP assemblies. It is deliberately not an implementation claim.

## Verified public S1API surface

| Area | Public API | What it provides | What it does not prove |
|---|---|---|---|
| Player money | `S1API.Money.Money.GetCashBalance()`, `ChangeCashBalance(float amount, ...)` | Read and modify the local player's cash balance. | A transaction ID, idempotency, rollback, or cross-player cash mutation. |
| Existing shop catalogues | `S1API.Shops.ShopManager.GetShopByName(...)`, `GetAllShops()`; `Shop.AddItem(ItemDefinition, float? customPrice)`, `RemoveItem(...)` | Resolve an existing shop and add/remove its sellable catalogue entries. | Creating a new shop, per-item physical stock quantities, or a purchase checkout API. |
| Item definitions | `S1API.Items.ItemManager.GetDefinition(...)`, `GetAllItemDefinitions()` | Resolve known item definitions and validate item IDs. | Removing or inserting a precise quantity from a player's live inventory. |
| Player item grant | `S1API.ConsoleHelper.AddItemToInventory(itemCode, quantity)` | Give an item to the local player. | A matching safe debit/consume method or atomic inventory transfer. |
| Runtime storage | `StorageEntity` / `StorageInstance` APIs | Access to the game's storage system; runtime instance-level item operations need inspection against the installed assembly. | A version-independent public quantity reservation contract for the proposed Portside store. |

Official documentation:
- Money: https://ifbars.github.io/S1API/docs/api/S1API.Money.Money.html
- Shops: https://ifbars.github.io/S1API/docs/api/S1API.Shops.Shop.html
- Shop manager: https://ifbars.github.io/S1API/docs/api/S1API.Shops.ShopManager.html
- Items: https://ifbars.github.io/S1API/docs/api/S1API.Items.ItemManager.html
- Console helper: https://ifbars.github.io/S1API/docs/api/S1API.ConsoleHelper.html
- Storage entity: https://ifbars.github.io/S1API/docs/api/S1API.Storage.StorageEntity.html

## Safe design decision

Do not build a real checkout by combining only `ChangeCashBalance` and `AddItemToInventory`. The public surface documented above does not provide the opposite inventory operation or a single atomic transfer. That partial implementation could charge money without delivery, deliver twice after a retry, or make a refund duplicate items.

Likewise, `Shop.AddItem` changes the catalogue of an existing shop. It is not evidence that a new physical Portside Supply shop exists or that its stock quantity is tracked by that API. Do not register Portside products in an unrelated vanilla shop as a temporary shortcut.

## Required adapter contract before enabling checkout

The game-specific bridge must prove all of the following against the user's installed Schedule I build:

1. Resolve a stable player ID to the correct live player inventory and cash account on the host.
2. Resolve a stable product ID to the game's actual item definition.
3. Read and reserve an exact quantity from Portside's physical storage without a race with another purchase.
4. Debit the buyer's cash and deliver the exact item quantity, with a compensating rollback if any later step fails.
5. Persist a transaction ID or otherwise make retries idempotent across reconnects and save reloads.
6. Reconcile cash, item count and stock after save/load, disconnect, player death and failed delivery.
7. Keep the physical shop and Hyland Network online catalogue on the same authoritative price and stock record.

Recommended transaction order:

- Validate host authority, player, product, price and exact available stock.
- Reserve physical stock.
- Debit the live player cash.
- Deliver the exact item quantity.
- Verify the resulting cash and inventory values.
- Persist the host receipt and updated stock.
- If any step fails, compensate in reverse order and persist the failure receipt.

If the installed APIs cannot safely perform one of these steps, leave checkout disabled rather than substituting an add-only or catalogue-only API.

## Current project boundary

The current implementation intentionally stops at the expansion-side virtual ledger:

- NPC customer budget and order escrow are stored in `ShopPurchaseServiceSnapshot`.
- Completed orders settle to an expansion-side shop revenue account.
- Cancelled, expired and failed orders refund that virtual customer budget and restore the expansion-side stock count.
- The five-second host sweep expires due orders using host UTC time.
- None of those virtual accounts are vanilla Schedule I cash or live player inventory.
- The current repository has no local game DLL set, so the game-facing project has not been compiled or tested against the installed game.

## Next integration spike

Once the exact game install is available, inspect the actual method signatures and call paths for:
- player inventory add/remove and item instance quantities;
- storage container runtime item add/remove and container save behaviour;
- cash mutation and whether it can be read back synchronously;
- shop catalogue registration and whether physical stock is owned by a shop or an associated storage container.

Only after those checks should a concrete adapter be implemented and enabled for Portside Supply.
