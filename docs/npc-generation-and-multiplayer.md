# Harborline NPC generation, uniqueness and multiplayer

## Design goal

Harborline is a large, connected district with five subdistricts: Port District, Freight & Industrial Quarter, Harbor Market & Commercial Street, Waterfront Promenade and Residential Backstreets. It must connect to the existing Docks road/nav graph and must not be implemented as an isolated scene or disconnected island.

## NPC population

The design target is 42 custom physical NPCs, with an initial runtime budget of about 18 simultaneously active. The roster contains 30 customer profiles, two dealers and optional phone-only contacts. Native police are additional and must be spawned by the game's existing police/dispatch system.

The population is staged: first validate a 3-5 NPC Personnel pack; then increase in batches while measuring spawn time, pathing, save size, frame time and co-op behavior. Do not activate the whole target roster until profiling shows it is safe.

## Random customer preferences

Use Personnel's native customer/economy fields. At pack-build or first-world-generation time, choose each customer's affinities from a verified allow-list of products/effects present in the target game version. Randomize budget, standards, order frequency and preferred order window within validated ranges.

Randomness must be deterministic and persistent:
1. Derive a stable seed from the save/world identity plus the stable NPC ID (not from the current client or wall-clock time).
2. Generate a profile once on the authoritative host.
3. Validate that the profile contains valid, non-empty in-game affinities.
4. Detect duplicate full profiles and reroll only the colliding profile.
5. Persist the resolved profile in the save/manifest data and replicate it to clients.
6. Never reroll on reload, save load, reconnect or late join.

This gives every NPC individual tastes while preventing preferences from changing unexpectedly.

## Unique appearance generation

Use Personnel's avatar settings and the vanilla avatar layer library. Randomize only supported fields and known-valid layer paths: body presentation, safe height/weight ranges, skin tone, face/eye/eyebrow layers, hair and color, clothing and accessories.

For every generated NPC, create a normalized appearance fingerprint from every resolved appearance field. Compare it against all previously assigned Harborline fingerprints. If the complete fingerprint already exists, reroll from the valid asset pools. This is collision rejection, not a promise based on random probability.

Keep the seed and resolved appearance stable. Do not add custom PNG layers unless they are needed; clients must have identical packs/assets to render custom layers. Prefer vanilla layers for broad mod compatibility.

## Police and law enforcement

Do not create a second police AI. Register Harborline as a supported region for the native police spawn/dispatch/response system. The district should expose valid road/nav connections and candidate patrol/checkpoint anchors. Native police should be able to appear throughout the district as they do in existing areas, with checkpoint and patrol behavior routed through the game's systems.

The three planned inspection zones are the district entry, freight road and market exit. Their coordinates and navigation links must be captured in a live game build before enabling them.

## Multiplayer contract

- Host/server is authoritative for NPC creation, randomized values, save writes and world-state transitions.
- Stable, namespaced NPC IDs prevent duplicate registrations.
- Clients receive replicated NPC state; they do not independently roll preferences or spawn authoritative NPCs.
- Late join must hydrate the same NPCs and values, not duplicate them.
- All players need matching mod versions and required Personnel/S1API dependencies. Custom appearance layers require the same pack on every client.
- Detect optional integrations at runtime. If an optional mod is absent, disable only that integration and continue safely.
- Avoid Harmony patches to global methods unless necessary; patch narrowly, check target methods and provide graceful fallback.
- Never replace vanilla NPC, police, inventory, property or networking singletons.

Personnel's current documentation states that it creates real S1API NPCs using the game's prefab/networking/save machinery and that all co-op players need the same Personnel packs. This is the preferred foundation, but Harborline multiplayer still needs an actual host/client test before being called compatible.

## Validation gates

1. Verify target game build, MelonLoader, S1API and Personnel versions.
2. Verify all avatar paths and customer affinity identifiers against that build.
3. Validate unique IDs, appearance fingerprints and customer profile fingerprints.
4. Test 3-5 NPCs in single-player: spawn, schedule, economy, save/load.
5. Test host + client: initial join, late join, disconnect/reconnect, save/load, no duplicate NPCs and identical preferences/appearance.
6. Confirm native police spawn and navigate in Harborline.
7. Profile a staged increase to 18 active NPCs and then the full roster.
8. Test alongside a representative mod set; publish a compatibility matrix rather than claiming universal compatibility.

## Important limitation

The design files are not a runnable mod by themselves. Exact positions, nav links, vanilla avatar asset paths, product/effect identifiers and police region registration points must be verified in the installed game. Do not fabricate them from a 2D map.
