# NPC order offer generation

`NpcOrderOfferGenerator` builds a game-independent draft order from current shop stock and one NPC's profile.

## Selection rules

- Only products in that resident's preferred or neutral sets are eligible. Rejected and unknown product IDs are excluded.
- Items must have a positive price and non-negative stock. Zero stock is ignored; malformed rows, blank IDs, negative stock and duplicate IDs fail closed. The proposed quantity cannot exceed stock, the per-product cap or the resident's total budget.
- The generator evaluates each one-item option with the same customer acceptance rules used by the offer evaluator.
- A stable SHA-256 tie-breaker based on NPC ID, world interval and product ID makes selection reproducible across host/client environments.
- Extra lines and units are added only while the resident still accepts the total offer. The final offer is therefore within budget and has an accepted evaluation.

The method does not create a persistent order or change inventory or money. The host must still validate the request, assign an order ID/deadline, reserve or decrement stock transactionally, persist it and replicate it to the co-op client.

## Runtime boundary: native customers own native contracts

Ordinary in-world customer offers and contracts must be created and progressed by Schedule I's native `NPCCustomer` component (for example its native offer path and contract events). This generator is not a replacement for `NPCCustomer`, does not create a vanilla customer contract and must not be used to simulate one in parallel.

Keep this deterministic domain generator for unit tests, simulations and expansion-only remote/online order rules that the native customer workflow does not provide. When a profile is bound to a real game NPC, the adapter should configure native customer defaults/affinities and let the game's own customer component decide and own the in-world offer, contract, delivery and relationship lifecycle. Any expansion-only online order must be visibly separated from native in-world customer contracts and use its own ledger/transaction IDs.

Default caps: 3 unique product lines, up to 3 units per product. These are safe prototype defaults; balancing should move to configuration after the Schedule I economy/item adapter exists.