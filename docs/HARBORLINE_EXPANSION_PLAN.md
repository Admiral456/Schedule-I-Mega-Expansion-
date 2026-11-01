# Harborline — expanded district plan

**Status:** design and data foundation only. No Unity world coordinates or prefab names are fabricated.

## Scale and structure

Harborline is planned as a large coastal district linked directly to the vanilla Docks. It should feel like a second urban-industrial district, not a small dock extension. The first pass is a greybox that follows the terrain, road network and pedestrian/vehicle navigation already present in Schedule I.

### Seven subdistricts

1. **Harbor Gate and Customs Approach** — the main road/pedestrian entrance from Docks, vehicle queue, port administration and access control.
2. **Container Port and Shipyard** — container stacks, cranes, loading lanes, quays, repair sheds, shipyard and fishing quay.
3. **Freight and Logistics Park** — large warehouses, cold storage, loading docks, truck staging, delivery depot and shipping offices.
4. **Harbor Market and High Street** — market hall, Portside Supply, restaurants, retail row and a walkable central square.
5. **Industrial and Vehicle Service Quarter** — Harbor Auto Works, power infrastructure, maintenance depot and fabrication yard.
6. **Workers' Residential Quarter** — apartment blocks, worker housing, laundromat, diner, convenience store and parking courts.
7. **Waterfront, Marina and Pier Promenade** — public waterfront, marina, fishing spots, boat dock, seafood café, bait shop and visitor areas.

The machine-readable list of zones and points of interest lives in `data/harborline_population.json`. Every coordinate and route remains unset until captured in the live game. The concept art is not a source of geometry or transforms.

## Population target

The expanded district targets **42 physical NPCs**:

- 8 dockworkers
- 5 cargo drivers
- 3 fishers
- 5 market vendors
- 4 mechanics
- 6 residents
- 4 tourists
- 4 office staff
- 2 port operations staff
- 1 native dealer

The design includes **26 native customers** with different preferences. This is a total roster target, not a promise to spawn 42 active agents simultaneously. The initial target is 16 active NPCs, with a ceiling of 20 only after performance tests. The first game-facing slice still starts with 3–5 NPCs.

Police are intentionally excluded from the custom NPC count. Police should appear through the native Schedule I law-enforcement system, not from a second custom police AI.

## Favourite drugs and individual behaviour

Ordinary in-world customers will use Schedule I's native `NPCCustomer` component. Each gets affinities to the installed build's real `DrugType` values. The selection is seeded by the save and stable NPC ID, so host and clients do not roll different preferences.

The new core helper `NpcDrugAffinityRandomizer` generates varied affinity vectors, requires at least one strong favourite and one strong dislike, and rejects duplicate vectors within the generated roster. The S1API adapter still needs to map those names to the installed `DrugType` enum and persist the host's save seed before runtime use.

The existing `sime.product.batch.*` values are only expansion online-order examples. They are not Schedule I drug types and must not be used as native customer affinities.

## Appearance

Each physical NPC will use the native S1API avatar pipeline:
- generate its appearance using `NPCAppearance.GenerateRandomAppearance()`;
- finalize it with `Appearance.Build()`;
- keep a stable NPC ID and avoid re-rolling on every tick or schedule update.

S1API documents the random generator and build step. It does not currently document a seed parameter or a public appearance-signature getter. The project therefore treats visual duplicate rejection and appearance save/load persistence as explicit release gates. A mod should not claim that every character is unique until the running game build has passed a signature comparison over the complete Harborline roster.

## Police activity

The project uses `LawController`, `LawManager` and `CheckpointManager`. Planned checkpoints are at Harbor Gate, the freight depot and the market square. Patrol routes follow the port loop and freight-market loop after route objects and navigation are verified in-game.

Law intensity is global. Harborline must not change global law intensity to fake a local patrol system, force-spawn officers, or override dispatch. Native automatic evaluation, available officers and existing game rules remain in control.

## Multiplayer and other mods

- S1API NPC definitions and native customer components are the source of truth for spawn, appearance, schedule, persistence and NPC behavior.
- The host owns first-time random rolls and persists their seed/assignments; clients consume the same registered identities instead of rolling locally.
- Shared orders, business ownership, checkpoints and any expansion-only stock mutations remain host-authoritative.
- All content identifiers use the `sime.` namespace. Do not patch or overwrite another mod's IDs, prefabs or save fields.
- Prefer S1API public wrappers and optional integrations; use Harmony only where no public extension point exists.
- Mod versions and dependency versions must be compatible between co-op players. No project can truthfully promise compatibility with every third-party mod; maintain a tested compatibility matrix.

## Build order

1. Survey Harborline's actual world connection, terrain, bounds, road nodes, pedestrian navigation, police routes and vanilla prefab catalog.
2. Greybox one continuous road/pedestrian network and test it without population.
3. Spawn 3–5 stable NPCs; prove customer affinity, distinct appearance, schedule, save/load and two-player late join.
4. Test native police patrol/checkpoint integration.
5. Test original Realtor listing/purchase flow and native shop/business state.
6. Scale toward 16 active NPCs, then 20, measuring performance and testing streaming/activation.
7. Expand the total roster toward 42 only after the preceding gates pass.

See `docs/NPC_RANDOMIZATION.md`, `docs/MULTIPLAYER_AND_MOD_COMPATIBILITY.md`, `docs/NATIVE_SYSTEM_REUSE_AUDIT.md` and `docs/TEST_PLAN.md`.
