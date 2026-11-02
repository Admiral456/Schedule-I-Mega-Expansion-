# NPC customer preferences and order acceptance

## Goals

Customers must not behave as interchangeable copies. Expansion profiles carry independent preferred, neutral and rejected product IDs, price sensitivity and a concrete maximum order budget. Ordinary in-world customer relationships, unlocks and contract decisions belong to Schedule I's native NPCRelationship and NPCCustomer systems; this document does not define a replacement for them.

## Example profiles

- Morgan Reed (Harborline): prefers the standard tier, is neutral to industrial tier, rejects premium tier, budget 1,200, price sensitivity 0.65.
- Jordan Pike (Ridgeview): prefers industrial and standard tiers, accepts premium as neutral, budget 4,000, price sensitivity 0.30.
- Taylor Brooks (Lakeside): prefers premium, accepts industrial as neutral, rejects standard, budget 9,000, price sensitivity 0.15.

These are initial balance examples, not final in-game economy tuning. Product IDs in product_catalog.example.json are expansion sample IDs and are not yet connected to Schedule I runtime item definitions.

## Evaluation rules

The game-independent NpcCustomerAcceptance service evaluates one proposed expansion-only remote/online order or test offer. It is not used to create vanilla in-world customer contracts:

1. Validate profile, product IDs, quantities, prices and offer lines.
2. Reject immediately if any requested product is in that NPC's rejected list.
3. Reject if the total exceeds that NPC's explicit budget limit.
4. Count distinct preferred and neutral products; duplicate lines do not multiply preference bonuses.
5. Apply a price penalty based on both offer/budget ratio and that customer's price sensitivity.
6. Apply an optional expansion-specific standing modifier supplied by the online-order caller. This value is not a native NPC relationship.
7. Clamp the final score to 0–100; score 50 or higher accepts the order.

The evaluator is deterministic and uses no random rolls. The host may call it for the expansion's online-order/test path, then persist the accepted result through the host-authoritative expansion ledger. A client cannot authoritatively accept an order by running this evaluator locally. Native in-world customer offers must instead use Schedule I's NPCCustomer component.

## Current scoring constants

- Base score: 55.
- Preferred product: +15 each, capped at +30.
- Neutral product: +4 each, capped at +8.
- Price penalty: up to 35 points, scaled by price sensitivity and budget usage.
- Expansion standing modifier: 0.10 points per standing point, clamped to -100 to +100. The current host coordinator passes neutral 0 until a distinct online-standing system is justified.
- Acceptance threshold: 50.

These constants are testable tuning defaults, not final balancing values. They should move into configuration once the live order-generation adapter exists.

## Runtime status

The data profiles and pure domain evaluator are implemented and tested. They are not yet bound to live NPC entities or native NPCCustomer components. Native customer offer/contract lifecycle is not implemented by this domain service. Runtime product IDs must be mapped to actual Schedule I item definitions before expansion online checkout is enabled.
