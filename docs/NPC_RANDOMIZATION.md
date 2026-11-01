# NPC randomization and individuality

## Native appearance

Use the S1API appearance pipeline rather than separate character models:

```csharp
protected override void OnCreated()
{
    base.OnCreated();

    Appearance.GenerateRandomAppearance();
    Appearance.Build();

    if (IsPhysical)
        Schedule.Enable();
}
```

This follows the documented S1API lifecycle. The random call belongs to the NPC creation path, not the update loop. The NPC identity passed to `WithIdentity(...)` must remain stable for save/network identity.

### Duplicate-appearance requirement

Random generation reduces repetition but, by itself, does not prove that no two residents have the same visible appearance. The public `NPCAppearance` reference documents `GenerateRandomAppearance()` and `Build()`, but not a seeded overload or a public AvatarSettings signature/readback method.

Therefore:
- do not claim a uniqueness guarantee from random generation alone;
- inspect the installed S1API assembly and live avatar settings before implementing duplicate rejection;
- if a supported appearance signature is available, keep a host-owned set of used signatures and reroll only on a collision;
- persist the chosen appearance settings or stable appearance roll with the NPC save identity;
- test uniqueness against the Harborline roster, base-game NPCs in the active scene, and other S1API NPC mods where their data can be read safely.

The NPC impostor catalog can enumerate game-owned impostor definitions and return a deterministic random entry by seed, but do not assume a catalog definition can be assigned to a custom NPC until the installed API exposes and verifies that binding.

## Native customer drug preferences

The game-facing adapter must discover the available values from the installed runtime `DrugType` enum. It must not hardcode a list copied from an old game version.

The core helper is independent of Unity and receives:
- a save-scoped seed generated once by the host and persisted;
- stable namespaced NPC IDs;
- the valid runtime DrugType names.

It returns one affinity per available drug type. Every customer gets at least one strong favourite (0.70–1.00) and one strong dislike (-1.00 to -0.70). Other values vary. A vector is unique within the generated Harborline customer roster. If the installed build exposes too few drug types/combinations, generation fails closed rather than silently giving several customers the same profile.

The game-facing adapter maps each returned `DrugTypeName` to the actual enum value and calls native `WithAffinities(...)` inside `WithCustomerDefaults(...)`. The regular Schedule I customer contract generator remains in charge of in-world offers, budgets, product availability and deliveries.

Do not confuse expansion-only online-order product tiers (`sime.product.batch.*`) with the native drug type enum.

## Seed ownership and multiplayer

The host creates one random seed per save slot before the Harborline roster is initialized. The seed and resulting affinity vectors must be stored in the expansion save. The host is the only authority allowed to create or migrate those values. A client joining later receives the same registered NPC prefabs and save state; it never calls a separate local roll for an already registered NPC.

The seed must not use or reset `UnityEngine.Random`, because doing so can perturb unrelated game systems and other mods. The core helper uses SHA-256 over the save seed, NPC ID, algorithm version and choice channel. Changing one NPC ID does not reroll every other resident.

## Tests required

- same save seed + same IDs + same DrugType catalog gives byte-for-byte equivalent assignments;
- changing the input order does not alter assignments;
- each customer has a favourite and a disliked type;
- no two Harborline customers share the same full affinity vector;
- too few DrugType values fails safely;
- co-op late join sees the host's values;
- save/load does not reroll appearance or affinity values;
- third-party mods that register additional products do not crash the generator.
