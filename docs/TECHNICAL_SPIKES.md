# Technical spikes — blockers to resolve before production content

## S-001 Map survey
Measure world bounds, elevations, collider bounds, water, barriers and road graph. Output: coordinate map and surveyed build regions.

## S-002 Runtime geometry
S1MAPI describes procedural meshes, building construction and GLTF loading. Measure load time, colliders, memory and FPS for staged object counts.

## S-003 Road graph, NavMesh and vehicle AI
Determine if the game uses baked NavMesh, runtime NavMesh, custom pedestrian graph or separate vehicle routes. Test walking NPCs and traffic separately. Fallback: waypoint-driven stationary/limited NPCs; do not claim full AI traffic.

## S-004 Realtor/property integration
S1API exposes property/business wrappers, but the reviewed public surface does not prove the complete mod-added Realtor registration and purchase path. Inspect the exact game property registration lifecycle and test one dummy listing/purchase on a copied save. The vanilla Realtor remains the source of truth; do not create a parallel purchase menu.

## S-005 Phone
Compare S1API Phone Apps and Sideload lifecycle, inputs, persistence, focus and update behavior. Build Hello World app plus one host-confirmed action.

## S-006 Networking
Prove SteamNetworkLib request/response, reliability, ordering, reconnect and late join with a two-player test.

## S-007 NPCs, customers, dealers and suppliers
Start with S1API's native NPC lifecycle and prefab configuration. Bind one physical resident to native NPCCustomer and verify native offers/contracts, affinities, budget, standards, relationship and schedule through save/load and two-player late join. Test one DealerType.RivalDealer through NPCDealer. Test NPCSupplier separately for wholesale listings, stash, meetings and delivery. Use ScheduleOne-Personnel only if a measured gap exists; it is not a default dependency.

## S-008 Save
Test round-trip, migration fixtures, backup and rollback.

## S-009 Shop and business lifecycle
Resolve vanilla item definitions with ItemManager and existing shops with ShopManager. Distinguish catalogue entries from physical stock quantity. Test one physical shop's real inventory/storage mutations, cash checkout, ownership, employees, income, sale and original Realtor visibility. Inspect native supplier-owned Shop/StorageInstance and DeliveryRegistry as read/observe surfaces; do not treat read-only delivery wrappers or Shop.AddItem as physical stock checkout.

## S-010 Police and checkpoints
Reuse LawController/LawManager and native wanted-level/dispatch/patrol behavior. Enumerate existing loaded-scene foot and vehicle routes first, then test one native patrol on an actual route and one checkpoint evaluation on a copied save. Law intensity is global and can trigger automatic world activity; it is not a per-province toggle. New province patrols require proven geometry, routes, navigation and available police officers.
