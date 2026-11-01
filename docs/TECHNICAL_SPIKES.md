# Technical spikes — blockers to resolve before production content

## S-001 Map survey
Measure world bounds, elevations, collider bounds, water, barriers and road graph. Output: coordinate map and surveyed build regions.

## S-002 Runtime geometry
S1MAPI describes procedural meshes, building construction and GLTF loading. Measure load time, colliders, memory and FPS for staged object counts.

## S-003 Road graph, NavMesh and vehicle AI
Determine if the game uses baked NavMesh, runtime NavMesh, custom pedestrian graph or separate vehicle routes. Test walking NPCs and traffic separately. Fallback: waypoint-driven stationary/limited NPCs; do not claim full AI traffic.

## S-004 Realtor integration
Find safe hook/API for existing property listings. Test a dummy property purchase on a copy save. Fallback: provider screen opened through Realtor interaction.

## S-005 Phone
Compare S1API Phone Apps and Sideload lifecycle, inputs, persistence, focus and update behavior. Build Hello World app plus one host-confirmed action.

## S-006 Networking
Prove SteamNetworkLib request/response, reliability, ordering, reconnect and late join with a two-player test.

## S-007 NPCs
Compare direct S1API NPC creation and ScheduleOne-Personnel. Test one resident, one customer/dealer and one contact-only profile through save/load and late join.

## S-008 Save
Test round-trip, migration fixtures, backup and rollback.

## S-009 Business lifecycle
Test purchase, ownership, employees, inventory, income, sale and Realtor visibility.

## S-010 Police
Test vanilla Heat integration and one synchronized checkpoint event without corrupting vanilla police state.
