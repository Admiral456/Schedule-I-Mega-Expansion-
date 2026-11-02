# Test plan and acceptance criteria

## Environment
Steam Default / IL2CPP, exact public game build pinned for each run, pinned loader/API versions, host-only, host+client, fresh save, copy of existing save, reconnect/late join and stress sample.

## Map
- No intersection with vanilla roads/buildings/bounds/AI paths.
- Every new interior has walkable entrances and correct colliders.
- No falling through terrain or invisible barriers.
- No duplicate geometry on scene reload.
- Road links are traversable.
- Pedestrian and vehicle AI are tested separately.
- FPS and memory recorded at incremental NPC/object counts.

## Business / Realtor
- Listing appears once.
- Purchase validates money, availability, unlocks and ownership.
- Simultaneous purchase cannot buy twice.
- Ownership persists through reload.
- Business sale/uninstall has cleanup.
- Employees, stock, income and UI use same business ID.

## Online shop
- Physical purchase reduces shared stock.
- Online purchase reduces same stock.
- Two purchases of last item: one succeeds, one receives out-of-stock.
- Payment and inventory update are atomic.
- Pickup/delivery survives restart.
- Duplicate request ID cannot double-charge.
- Host/client agree after reconnect.

## NPC
- Harborline data checks enforce 42 planned physical residents, 26 customers and matching zone/group totals.
- Affinity vectors are stable for the same save seed, contain a favourite and a disliked type, and are unique within the Harborline roster.
- Random appearance does not reroll on update; no duplicate appearance signature remains within Harborline after save/load.
- Compare active Harborline appearances against visible vanilla NPCs and other S1API NPCs where supported by the public API.
- Stable ID survives save/load.
- Schedule and spawn are deterministic.
- Preferences affect product selection.
- Budget affects acceptance.
- Orders generate only when eligible.
- Reputation changes exactly once.
- Contact-only NPC does not spawn unnecessary world entity.

## Police
- Checkpoint is deterministic and host-owned.
- Patrol does not block roads.
- Heat changes apply once.
- State agrees on both players.
- Save/load does not duplicate police events.
- Vanilla arrest/Heat remain intact.

## Hyland Network
- Contacts and profiles open.
- Messages sync.
- Orders/contracts appear once.
- Per-player and crew reputation remain distinct.
- Online shop and physical shop share data.
- Realtor listings match business registry.
- App recovers after late join.

## Failure tests
- Host reload.
- Client disconnect during purchase.
- Duplicate/delayed network message.
- Invalid payload/protocol mismatch.
- Migration n -> n+1.
- Migration failure rolls back.
- Feature flag disable preserves save.

## Release gate
Feature is not complete until host, two-player, save/load, reconnect/late join and log evidence pass.
