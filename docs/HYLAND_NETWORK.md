# HYLAND NETWORK — in-game phone network

## Goal
A real phone app backed by persistent game data and host-authoritative services, not a decorative UI mockup.

## Phone modules
1. Home — notifications, active orders, contracts and important events.
2. Contacts — residents, customers, dealers, shopkeepers and businesses.
3. Profile — role, district, schedule, relationship, reputation and history.
4. Messages — NPC and system messages.
5. Orders — customer-generated orders, deadline, status and reward.
6. Jobs & Contracts — one-off, recurring and special jobs.
7. Online Shop — Portside Supply and other participating stores.
8. Business — owned businesses, stock, employees and finances.
9. Realtor — all new purchasable businesses through existing Realtor flow.
10. Map — contacts, shops, contracts, police checks and POIs.
11. Reputation — per-NPC, crew, business and province reputation.

## NPC profiles
Every significant NPC has stable npcId, display name, portrait reference, province/position, role, schedule, availability, preferred/neutral/rejected product IDs, budget band, individual budget limit, price sensitivity, per-player relationship, crew relationship, order history and message thread IDs.

A physical NPC is required when the player must meet/interact with the person in-world. Contact-only NPCs are allowed when a physical entity is unnecessary or not yet safe to spawn.

Three distinct sample customer profiles are defined:
- Morgan Reed: standard-preferring, premium-rejecting and relatively price-sensitive.
- Jordan Pike: prefers industrial/standard products and has a larger budget.
- Taylor Brooks: premium-preferring, standard-rejecting and less price-sensitive.

Product IDs are example expansion IDs only until a runtime product adapter maps them to actual Schedule I item definitions.

## Reputation scopes
- playerRep: NPC relationship with player A or B.
- crewRep: shared organization reputation.
- businessRep: reputation of a specific owned business.
- provinceRep: broader regional standing.

Reputation changes only after host-confirmed events. A customer's current player-specific reputation is an input to the acceptance evaluator; the crew value remains a separate ledger scope.

## NPC-generated orders
Generation depends on preferences, budget, schedule, availability, reputation and local events. Status flow:
offered -> accepted -> preparing -> ready -> delivered/collected -> completed
Terminal alternatives: expired, cancelled, failed, disputed.

Each order has orderId, customerId, product IDs, quantities, deadline, reward, fulfilment method, status, version and audit log.

The core preference evaluator is deterministic: rejected products and over-budget offers fail immediately; preferred/neutral products, budget usage, price sensitivity and player-specific reputation contribute to an acceptance score. It is currently game-independent and does not yet spawn live NPC orders.

## Online shop
Portside Supply has a physical shop and phone app but one shared inventory, one price source and one order ledger. Purchases and delivery are host-authoritative. Never keep two independent inventories for online and physical sales.

## Realtor integration
New purchasable businesses register through one property-catalog adapter. Purchase goes through vanilla Realtor flow if a safe hook is confirmed. Host validates cash, availability, unlocks and ownership. Fallback is a listing provider opened from the Realtor interaction, not an unrelated phone-only purchase.

## Real online service vs in-game network
MVP means an actual in-game app synchronized through Steam lobby/P2P. It does not require external cloud accounts. A future web backend requires separate authentication, privacy, hosting and uptime design.
