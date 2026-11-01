# NPC customer preferences and order acceptance

## Goals

Customers must not behave as interchangeable copies. Their profiles carry independent preferred, neutral and rejected product IDs, price sensitivity and a concrete maximum order budget. The player relationship is also supplied per player so co-op partners can receive different outcomes from the same NPC.

## Example profiles

- Morgan Reed (Harborline): prefers the standard tier, is neutral to industrial tier, rejects premium tier, budget 1,200, price sensitivity 0.65.
- Jordan Pike (Ridgeview): prefers industrial and standard tiers, accepts premium as neutral, budget 4,000, price sensitivity 0.30.
- Taylor Brooks (Lakeside): prefers premium, accepts industrial as neutral, rejects standard, budget 9,000, price sensitivity 0.15.

These are initial balance examples, not final in-game economy tuning. Product IDs in product_catalog.example.json are expansion sample IDs and are not yet connected to Schedule I runtime item definitions.

## Evaluation rules

The game-independent NpcCustomerAcceptance service evaluates one proposed order:

1. Validate profile, product IDs, quantities, prices and offer lines.
2. Reject immediately if any requested product is in that NPC's rejected list.
3. Reject if the total exceeds that NPC's explicit budget limit.
4. Count distinct preferred and neutral products; duplicate lines do not multiply preference bonuses.
5. Apply a price penalty based on both offer/budget ratio and that customer's price sensitivity.
6. Apply the current player-specific reputation modifier.
7. Clamp the final score to 0–100; score 50 or higher accepts the order.

The evaluator is deterministic and uses no random rolls. The host must call it when generating or validating an order, then persist the accepted result through the host-authoritative order pipeline. A client cannot authoritatively accept an order by running this evaluator locally.

## Current scoring constants

- Base score: 55.
- Preferred product: +15 each, capped at +30.
- Neutral product: +4 each, capped at +8.
- Price penalty: up to 35 points, scaled by price sensitivity and budget usage.
- Player-specific reputation: 0.10 points per reputation point, clamped to the ledger's -100 to +100 range.
- Acceptance threshold: 50.

These constants are testable tuning defaults, not final balancing values. They should move into configuration once the live order-generation adapter exists.

## Runtime status

The data profiles and pure domain evaluator are implemented and tested. They are not yet connected to live NPC entities, dealer/customer AI, the in-game phone contacts list or live order spawning. Runtime product IDs must be mapped to actual Schedule I item definitions before the first in-game order is enabled.
