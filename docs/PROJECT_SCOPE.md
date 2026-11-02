# Project scope and acceptance requirements

## Product goal
Keep vanilla Hyland Point intact and add exactly three large provinces. They are regions of one connected world, not separate maps. They share NPC, contacts, business, economy, police, contracts and co-op progression.

## Map
- Preserve existing vanilla districts and content.
- Add Harborline, Ridgeview and Lakeside.
- Each province gets a distinct terrain theme, road connection, POIs, buildings, NPC population and local economy.
- Coordinates must be surveyed in-game. Concept art and map screenshots are not coordinate sources.
- New objects may not overlap vanilla roads, buildings, world bounds or active AI routes.

## Shops and businesses
- Every new shop has a physical location.
- Portside Supply also has an in-phone online catalog, cart, checkout, pickup and delivery.
- Physical and online sales share exactly one stock, price source and order ledger.
- Every purchasable new business is listed in the existing Realtor system; no unrelated parallel purchase menu.
- Realtor listing exposes price, costs, revenue model, capacity, unlock requirements, location and ownership.
- Ownership connects to shared party, employees, finances, stock and reputation.

## NPC and customers
- Every important NPC has a stable ID and profile: name, role, province, schedule, availability, relationships, product preferences, budget and reputation.
- Different NPCs have different preferences. No universal customer profile.
- NPC customers generate their own orders.
- Use physical NPCs when world presence matters; use contact-only profiles when physical spawning is not needed or not yet technically safe.
- New dealers are full contacts/NPCs connected to stock, clientele and reputation.

## Police
- Checkpoints, patrols and events exist in all three provinces.
- A checkpoint needs a trigger, state machine, Heat response and multiplayer synchronization, not just a decorative marker.
- Reuse vanilla police behavior where possible; do not invent unsupported AI capabilities.

## Hyland Network
- Contacts, profiles, messages, reputation, customer orders, contracts, online shop, business dashboard and Realtor access.
- This must be a real in-game phone app backed by persistent data, not a static mockup.
- Shared state is host-authoritative.
- External cloud accounts/backend are not required for MVP.

## Multiplayer
- Two-player target.
- Host is authoritative for shared world and economy state.
- Client never commits money, stock, reputation, ownership or completion locally.
- Late join and reconnect must resync correctly.
- Both players need compatible game build, protocol and mod versions.

## Existing save
- First tests only on a copy.
- Namespaced stable IDs, schema versions, backup, rollback and feature flags.
- Disabling a province must not corrupt or orphan save data.

## MVP non-goals
- Replacing the vanilla map.
- Replacing vanilla Realtor if a safe extension adapter is possible.
- Public social network with external user accounts.
- Hundreds of active NPCs before performance and pathfinding are measured.
