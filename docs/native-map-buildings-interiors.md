# Native world map, building icons, properties, doors and interiors

## The expansion must exist on the actual game map

Harborline must be physically added to the existing Hyland Point scene and must appear on the normal in-game map. This means world geometry, terrain/coastline, roads, collision, navigation links and the map's world-to-map extent/projection must all agree. A few phone-map markers or an extra custom map screen do not count as map expansion.

The current public S1API surface documents MapPOI as a standalone point of interest on the phone map. It is appropriate for public landmarks such as the pier or a checkpoint, but it is not a replacement for the native building/property system. The actual base-game map image/bounds and custom region registration hook still need to be located and verified against the target game build.

## Native map icons and ownership

Each site has a semantic role in world/harborline/native-integration.json. At runtime, those roles must be mapped to the actual native game building/property classes and map icon categories.

- Shops use the game's shop/building marker.
- A purchasable business must exist as a real game property/business, not just a POI.
- The native ownership state controls its map marker. When bought, the native map renderer should display the same owned-property/house icon the game uses for its other owned properties, where applicable.
- Public landmarks use native POI behavior.
- Do not draw custom icon sprites to imitate native icons.

S1API exposes building lookup (Building.GetByName/GetAll) and property wrappers (PropertyManager, BaseProperty), but the documented wrapper surface does not by itself prove that custom property registration can be added through a public method. The project must verify the game's property/realtor registry insertion point before publishing purchasable Harborline properties.

## Physical buildings and interiors

Prefer a native building prefab with its built-in interactions. Use S1MAPI BuildingBuilder only when no suitable existing structure can be reused. BuildingBuilder creates geometry; it does not automatically register the resulting shell as an enterable native Building, a Property, a Shop or a functional business.

For each site:
- Portside Supply: native shop/business registration, entrance, customer route, stocked shelves, checkout/register and shared in-person/mobile inventory.
- Harbor Freight Warehouse: native warehouse/property registration, loading access, real storage/cargo interactions, staff entry and navigable stock areas.
- Harbor Market: native market/shop logic, stalls/counters, stock, customer access and working vendor interactions.
- Harbor Auto Works: native service/business registration if supported by the target build; otherwise implement no fake service station. Include accessible workshop interior and only real native vehicle/service interactions.
- Harbor Offices: real building registration, working entrances, usable office/interaction points, NPC schedules and any purchasable office-property flow.
- Harbor Power Plant: native industrial building identity, accessible service/maintenance areas and only functional electrical/utility interactions supported by the game. Otherwise keep equipment explicitly decorative.
- Pier Promenade and police checkpoint: public places; use public POI markers rather than property ownership markers unless the game defines these as properties.

Every interior must contain the functional objects needed for its purpose. An object that merely looks like a register, storage locker or workshop machine does not count unless the original interaction, inventory, state and network behavior work.

## Functional doors

Interactive doors must use the game's actual door prefab/controller and network object. Instantiate them through the server-authoritative networked path; never locally clone a networked door.

S1MAPI's BuildingBuilder.AddSlidingDoors() creates and network-spawns the door on the server, but its current documentation warns that automatic proximity open/close is server-gated: manual handle interaction works on clients via ServerRpc, while proximity automation does not run client-side. Therefore each door is tested separately for:

1. Host manual open/close.
2. Client manual open/close.
3. Host proximity open/close.
4. Client proximity behavior, including a player who is not the host.
5. NPC pathing through the opening and schedule entry into the building.
6. Save/load and late join, with no duplicated doors.

Do not mark a site as complete until its entrances work for both players and NPC schedules. If the native door system has a multiplayer limitation, retain native manual interaction and only add a narrowly scoped compatibility hook after testing; do not silently replace the game's door system.

## Navigation and schedules

Building interiors need collision and a traversable path from the street to interior interaction points. Use the S1MAPI NavigationBuilder/doorway data where it is supported, then validate the generated navigation in the real game. NPC schedules should reference actual Building wrappers by verified in-game names and use native schedule actions such as StayInBuilding; a GameObject name by itself is not enough.

## Release gate

The following are mandatory before a building is described as functional:
- Listed on the normal in-game map at the correct location.
- Uses the correct native category icon.
- If purchasable, appears in the real estate/realtor flow and changes to the native owned icon after purchase.
- Door interaction works for host and client.
- The building can be entered, traversed and used by NPCs.
- Its key interior objects have working original gameplay interactions.
- State persists through save/load and multiplayer late join.