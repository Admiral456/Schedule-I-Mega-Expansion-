# Harborline NPC generation, uniqueness and multiplayer

## NPCs use the game's own pipeline

Harborline NPCs are real S1API.Entities.NPC entities built on Schedule I's prefab, avatar, schedule, customer/dealer, save/load and FishNet networking systems. Personnel is not a required dependency and no standalone NPC pack framework is used for runtime spawning.

On the NPC instance, call `Appearance.GenerateRandomAppearance()` followed by `Appearance.Build()` to ask the native avatar pipeline for its appearance. `GenerateRandomAppearance()` is an instance method on the `NPCAppearance` object exposed by the NPC; it is not a static call on the type. Use the game's installed avatar settings and vanilla avatar layers; do not implement an independent face/body/clothing generator.

Generate the appearance once on the authoritative host. Preserve the resolved avatar state through native NPC save/load and replication so all peers see the same NPC and the appearance does not reroll after reload or late join. Detect exact duplicates from the resolved native avatar settings and request another native random appearance only for a collision. Verify this comparison against the target S1API build before claiming that duplicates are impossible.

## NPC population

The design target is 42 custom physical NPCs, with an initial runtime budget of about 18 simultaneously active. The roster contains 30 customer profiles, two dealers and optional phone-only contacts. Native police are additional and must be spawned by the game's existing police/dispatch system.

The population is staged: first validate a 3-5 NPC S1API test roster; then increase in batches while measuring spawn time, pathing, save size, frame time and co-op behavior. Do not activate the whole target roster until profiling shows it is safe.

## Random customer preferences

Configure customer/dealer behavior using the native S1API NPC customer and dealer builders, which wrap the game's economy. Choose each customer's affinities from a verified allow-list of products/effects present in the target game version. Randomize budget, standards, order frequency and preferred order window within valid native ranges.

Randomness must be deterministic and persistent:
1. Derive a stable seed from the save/world identity plus the stable NPC ID (not from the current client or wall-clock time).
2. Generate the resolved profile once on the authoritative host.
3. Validate non-empty in-game affinities and native numeric limits.
4. Detect duplicate preference profiles and reroll only the colliding values.
5. Persist the resolved profile in the native NPC/save state and replicate it to clients.
6. Never reroll on reload, save load, reconnect or late join.

## Appearance

- Randomize on the NPC instance with Appearance.GenerateRandomAppearance(), then call Appearance.Build(); do not create an independent avatar generator.
- Keep only vanilla avatar layers unless a later project scope explicitly needs custom art.
- Generate once and let the native save/network lifecycle preserve it.
- Compare resolved native avatar settings to identify exact full appearance duplicates. Do not claim this is implemented until the target API exposes and the runtime test confirms the required settings snapshot.
- All clients use the same mod/S1API build and receive the host-created NPC/avatar state.

## Police and law enforcement

Do not create a second police AI. Register Harborline as a supported region for the native police spawn/dispatch/response system. The district must expose valid road/nav connections and candidate patrol/checkpoint anchors. Native police should be able to appear throughout the district as they do in existing areas, with checkpoint and patrol behavior routed through the game's systems.

The three planned inspection zones are the district entry, freight road and market exit. Their coordinates and navigation links must be captured in a live game build before enabling them.

## Multiplayer contract

- Host/server is authoritative for NPC creation, randomized values, save writes and world-state transitions.
- Stable, namespaced NPC IDs prevent duplicate registrations.
- Clients receive replicated NPC state; they do not independently roll preferences or spawn authoritative NPCs.
- Late join must hydrate the same NPCs and values, not duplicate them.
- All players need matching mod versions and S1API versions. No Personnel dependency is required by this project.
- Detect optional integrations at runtime. If an optional mod is absent, disable only that integration and log a clear warning.
- Avoid Harmony patches to global methods unless necessary; patch narrowly, check target methods and provide graceful fallback.
- Never replace vanilla NPC, police, inventory, property or networking singletons.

## Validation gates

1. Verify target game build, MelonLoader and S1API versions.
2. Verify the instance-level Appearance.GenerateRandomAppearance() pipeline and vanilla avatar options against that build.
3. Validate stable NPC IDs, distinct resolved appearances and customer preference profiles.
4. Test 3-5 NPCs in single-player: spawn, schedule, economy, save/load.
5. Test host + client: initial join, late join, disconnect/reconnect, save/load, no duplicate NPCs and identical appearance/preferences.
6. Confirm native police spawn and navigate in Harborline.
7. Profile a staged increase to 18 active NPCs and then the full roster.
8. Test alongside a representative mod set; publish a compatibility matrix rather than claiming universal compatibility.

## Important limitation

The design files are not a runnable mod by themselves. Exact positions, nav links, native region registration, building/property registrations, product/effect identifiers and the resolved AvatarSettings duplicate check must be verified in the installed game. Do not fabricate them from a 2D map.