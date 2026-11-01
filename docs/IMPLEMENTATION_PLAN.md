# Implementation plan — vertical slices first

## Why vertical slices
Three large provinces and a full phone/business simulation are too broad to build safely in parallel before core game integration is proven. We implement one end-to-end slice, then reuse the same registry/service contracts for the other two provinces.

## Vertical slice A — Harborline
1. Survey and greybox a safe road connection from the vanilla southern/Docks edge.
2. Add one physical store shell with an interior and tested colliders.
3. Register Portside Supply in the business registry.
4. Add the phone catalog using the same shop service and inventory ID.
5. Implement host-authoritative checkout, stock reservation, payment and order state.
6. Add one contact NPC profile and make it visible in Hyland Network.
7. Add one customer-generated order using a data-driven preference profile.
8. Add one Realtor listing and prove the existing Realtor interaction can open it.
9. Add one police checkpoint with a shared host-owned event.
10. Test host/client, reconnect, save/load and rollback.

## Vertical slice B — Ridgeview
- Reuse province/POI registry, business listing adapter, NPC profile and contract system.
- Build mountain terrain only after heightmap/collision/navigation spike.
- Add quarry, processing yard, depot and industrial contracts.

## Vertical slice C — Lakeside
- Reuse the same services and registries.
- Build lake shoreline and marina only after water/bounds/physics spike.
- Add resort, villas, visitor schedules, seasonal events and hospitality orders.

## Shared implementation order
1. Stable IDs and data registries.
2. Save envelope and migration tests.
3. Host-authoritative message envelope and idempotency.
4. Province/POI loader.
5. Phone app shell and navigation.
6. NPC contacts/profiles/reputation.
7. Order and contract services.
8. Shop inventory and online checkout.
9. Realtor adapter and ownership lifecycle.
10. Police event service.
11. Physical world population and schedules.
12. Performance, accessibility, localization and packaging.

## Critical architectural rule
Do not implement each province with its own copy of phone, shop, reputation, NPC, police or Realtor logic. All provinces register data into shared systems.
