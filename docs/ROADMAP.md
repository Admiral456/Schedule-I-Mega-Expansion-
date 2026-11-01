# Roadmap

## Phase 0 — Foundation
- [x] Repository created and inspected.
- [x] Three-province scope captured.
- [x] Hyland Network, multiplayer and save principles documented.
- [x] JSON schemas and cross-file validation.
- [x] Domain-core unit tests green on GitHub Actions.
- [x] Current S1API / S1MAPI / SteamNetworkLib IL2CPP baselines reviewed.
- [x] First S1API Saveable adapter scaffold written.
- [x] First Hyland Network phone navigation shell written.
- [x] SteamNetworkLib initialization and protocol-hello scaffold written.
- [ ] Compile GameIntegration against the user's local game assemblies and pinned DLLs.

## Phase 1 — Technical proof-of-concepts
- [ ] S1API IL2CPP mod boots and logs the exact local game build.
- [ ] Hyland Network appears on the in-game phone on host and client.
- [ ] S1API Saveable round-trip on a copied existing save.
- [ ] SteamNetworkLib handshake succeeds with two real players.
- [ ] Host-authoritative request/response and late-join snapshot.
- [ ] S1MAPI test mesh/building at a surveyed safe location.
- [ ] One scheduled NPC and a generated customer order.

## Phase 2 — Harborline vertical slice
- [ ] Survey the Docks/southern connection.
- [ ] Greybox terrain, road, bridge and one building.
- [ ] Portside physical shop and phone catalog.
- [ ] Shared stock, atomic checkout, pickup and delivery.
- [ ] Realtor adapter and one purchasable business.
- [ ] Contact, preference, order and reputation end-to-end.
- [ ] Police checkpoint.
- [ ] Two-player save/reload and late join.

## Phase 3 — Ridgeview
- [ ] Mountain road and terrain.
- [ ] Quarry/industrial yard.
- [ ] Production and freight contracts.
- [ ] Worker NPCs and customer profiles.
- [ ] Police freight inspection.
- [ ] Realtor purchase lifecycle.

## Phase 4 — Lakeside
- [ ] Lake, shoreline and marina.
- [ ] Resort, housing and shops.
- [ ] Tourist schedules and VIP customers.
- [ ] Seasonal events/hospitality contracts.
- [ ] Realtor lifecycle and police patrols.

## Phase 5 — Integration
- [ ] Hyland Network contacts, messages, reputation, orders, jobs and online shops.
- [ ] NPC orders driven by preferences and supply/demand.
- [ ] Economy across vanilla + three provinces.
- [ ] Dealers, police and contracts integrated.
- [ ] Performance and full two-player regression.

## Phase 6 — Release
- [ ] Clean install and pinned dependencies.
- [ ] Save backup/export/rollback.
- [ ] Czech localization.
- [ ] r2modman profile and release notes.
