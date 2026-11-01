# NPC order offer generation

`NpcOrderOfferGenerator` builds a game-independent draft order from current shop stock and one NPC's profile.

## Selection rules

- Only products in that resident's preferred or neutral sets are eligible. Rejected and unknown product IDs are excluded.
- Items must have a positive price and non-negative stock. Zero stock is ignored; malformed rows, blank IDs, negative stock and duplicate IDs fail closed. The proposed quantity cannot exceed stock, the per-product cap or the resident's total budget.
- The generator evaluates each one-item option with the same customer acceptance rules used by the offer evaluator.
- A stable SHA-256 tie-breaker based on NPC ID, world interval and product ID makes selection reproducible across host/client environments.
- Extra lines and units are added only while the resident still accepts the total offer. The final offer is therefore within budget and has an accepted evaluation.

The method does not create a persistent order or change inventory or money. The host must still validate the request, assign an order ID/deadline, reserve or decrement stock transactionally, persist it and replicate it to the co-op client.

Default caps: 3 unique product lines, up to 3 units per product. These are safe prototype defaults; balancing should move to configuration after the Schedule I economy/item adapter exists.