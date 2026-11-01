# NPC customer directory

The core now contains a fixed, deterministic directory of nine fictional customer profiles: three each for Harborline, Ridgeview and Lakeside.

The canonical profile definitions live in `data/npc_profiles.json`; the typed C# directory mirrors those records for the game-independent domain layer. Product IDs intentionally use only entries already present in `data/product_catalog.example.json`.

Every resident has:
- a stable namespaced NPC ID and province ID;
- a role and availability window for future schedule binding;
- an individual order budget and price-sensitivity value;
- non-overlapping preferred, neutral and rejected product sets.

The directory is data only. It does not spawn or move Unity NPCs, create phone contacts in the game's contact database, or grant the expansion access to a real resident's inventory. Runtime NPC binding remains a separate game-integration task.

`NpcCustomerAcceptance.Evaluate` can evaluate an offer against these profiles and a caller-supplied player reputation. Rejected products and offers over the resident's budget fail immediately. Remaining offers receive a deterministic preference/price/reputation score.