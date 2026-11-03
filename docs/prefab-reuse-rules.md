# Safe reuse of native game assets and interactive prefabs

This document is mandatory for new Harborline buildings, doors, interiors and environment objects. A wrong network spawn mode can break FishNet multiplayer.

## 1. Reuse a real vanilla building first
- Prefer an existing enterable Schedule I building/prefab that already has its building identity, entrances, collision, interaction components and interior.
- Record exact names and source in world/harborline/prefabs.json; never guess prefab names.
- A visual GameObject is not automatically a registered native Building, Property, Shop or Business.
- S1API Building.GetAll() and Building.GetByName() expose registered enterable buildings. Verify every Harborline building is returned by the game's building lookup after registration.

## 2. BuildingBuilder is a geometry fallback, not a complete business
S1MAPI BuildingBuilder can create floors, walls, openings, lighting and structure. It does not by itself create a native Property, Shop, Business, map category or functional interior. Use it only when a suitable game building cannot be reused, then register the result through verified native building/property/business hooks.
Use NavigationBuilder/doorway data where supported and test the resulting navigation. Every NPC schedule must reference a real registered building, not only a GameObject name.

## 3. Interactive networked objects
If a prefab has NetworkObject, always instantiate it through the server-side network path:
- Use the verified PrefabRef.InstantiateNetworked() or appropriate S1MAPI builder path.
- Set its parent, local position and rotation before network spawn.
- Never call local Instantiate() on a networked prefab.
- Test host, remote client, late join and save/load.

Interactive doors, ATM, storage and switches are networked examples in current S1MAPI docs. The exact prefab names/components must still be verified in the target build.

## 4. Functional doors
Use the vanilla interactive door prefab/controller when available. A decorative opening or a static sliding-door mesh is not a working entrance.
S1MAPI BuildingBuilder.AddSlidingDoors() is network-spawned by the server. Its current documentation notes a limitation: automatic proximity open/close is server-gated; client manual handle interaction works via ServerRpc. Test each entrance for host and client manually, proximity behavior and NPC movement. Prefer a verified native prefab where it offers the full game behavior.

## 5. Interiors: decoration versus gameplay
- Use InteriorBuilder for static decoration only (desks, chairs, plants and similar meshes).
- Use original game prefabs/components for functional counters, stock shelves, storage, registers, workshop or vendor interactions, and any other gameplay object.
- Functional objects with NetworkObject must be network-spawned.
- A site's interior is not complete until its key objects work, state saves, and host/client interactions agree.

## 6. Map icons and property identity
- Register shops as native shop/building types and properties in the original property/business registry.
- The normal game map should supply the icon for the registered native category.
- The native property ownership state must drive the owned-property marker/house icon where the corresponding vanilla type uses it.
- Do not add custom sprites as a substitute for native shop/business/owned-property icons.
- S1API MapPOI is for public stand-alone points of interest, not a substitute for the building/property map system.

## Release checklist
1. The building is visible on the normal expanded game map at its correct world position.
2. It is registered as the correct native building/property/business type.
3. It shows the appropriate native map icon.
4. If purchasable, it is listed by the game's native real-estate flow and the icon changes with native ownership.
5. Doors work for host and client, not just as static geometry.
6. NPCs can navigate through the entrance and stay in the building using native schedules.
7. Key interior gameplay objects function, replicate and save.
8. No duplicates remain after reload/late join.