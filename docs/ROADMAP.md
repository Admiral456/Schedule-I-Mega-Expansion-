# Roadmap

## Repository foundation
- Requirements, three-province design and stable data IDs.
- JSON schemas and cross-file validation.
- Game-independent domain services and unit tests.
- Native S1API phone navigation shell.
- S1API per-slot Saveable adapter scaffold.
- SteamNetworkLib initialization and protocol compatibility handshake.
- Native-system reuse audit: `NATIVE_SYSTEM_REUSE_AUDIT.md`.

## Harborline population and world expansion
- Seven subdistricts and a 42-NPC target are defined in data/harborline_population.json.
- NpcDrugAffinityRandomizer creates deterministic, non-identical native drug-affinity vectors per save and stable NPC ID; runtime DrugType mapping and save-seed persistence remain integration tasks.
- Native avatar randomization uses S1API; appearance-signature duplicate rejection and appearance save/load persistence remain release gates.
- Native police patrols/checkpoints use LawController, LawManager and CheckpointManager, not custom police NPCs.
- Start with 3–5 NPCs, then measure 5/10/16/20 simultaneously active NPCs before expanding toward 42 physical residents.

## First native gameplay vertical slice
1. Compile the game-facing project against the exact local Schedule I IL2CPP assemblies, S1API 3.2.2 and SteamNetworkLib binaries. Documentation review is not a build result.
2. Spawn or bind one expansion resident to the native NPC lifecycle and native `NPCCustomer` component. Use `WithCustomerDefaults`, native product affinities/preferences, native budget/standards and native schedule/relationship behavior where available.
3. Verify the native customer offer/contract path, product request and delivery events, relationship progression, save/load and two-player late join. Do not call `NpcOrderOfferGenerator` to create a parallel in-world customer contract.
4. Add one competing dealer through native `NPCDealer` using the correct `DealerType` (for example `RivalDealer`) and verify its native contracts, recommendations, cash and unlock lifecycle.
5. Evaluate one native `NPCSupplier` for wholesale replenishment. Reuse its shop, stash, meeting and delivery flow where it fits; do not confuse supplier logistics with the Portside public retail shop.
6. Verify the live item definition, cash, inventory debit/delivery, storage quantity and save/reload compensation path before enabling any Portside checkout or expansion online purchase.
7. Integrate a physical expansion business through the original property/Business/Realtor lifecycle only after registration and purchase are proven on a copied save.
8. Enumerate loaded vanilla police routes and validate `LawManager`/ `LawController` behavior before adding new-province patrol routes or checkpoint triggers. Law intensity is global; do not use it as a local province toggle.
9. Run two-player spawn/request/contract/complete/restart/reconnect tests and implement expansion-specific late-join snapshots only for the state that is not already synchronized by the game's native systems.

## Expansion-only systems
- Keep Hyland Network for province directory, expansion-specific online services, business overview and cross-province information not already represented by the native phone.
- Keep the custom order domain/escrow only for expansion-only remote/online order rules. Native customer contracts, supplier deliveries, player cash and physical inventory remain owned by the game adapters.
- Keep separate reputation only for faction/crew/product-specific standing that native NPC relationships do not represent.
- Add new item definitions only for genuinely new expansion products; reuse vanilla item IDs and definitions elsewhere.

## World construction
- Survey vanilla world bounds, elevations, existing POIs, police stations, road graph, navigation and candidate exits in-game.
- Build Harborline as a greybox first, including road/terrain/colliders; validate native pedestrian and vehicle AI before population.
- Add Ridgeview and Lakeside only after shared geometry, save, navigation and native NPC integration are proven.
- Use native police patrol route objects where possible; create new route content only when the map/navigation spike proves it safe.
