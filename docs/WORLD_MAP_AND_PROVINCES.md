# World map and the three new provinces

## Design rule
Vanilla Hyland Point remains the center. Add three large provinces at map edges. Previous generated posters are concept art only; the user's vanilla map screenshot is a visual reference, not a coordinate map. Do not guess world coordinates from pixels.

## Existing districts to preserve
Westville, Northtown, Downtown, Uptown, Docks and Suburbia. Their exact boundaries and road/nav nodes must be confirmed in-game.

## Province 1 — Harborline
**Theme:** southern coast, cargo port, logistics, retail and industry.

Planned POIs:
- Portside Supply — physical store plus in-phone online shop.
- Harbor Freight Warehouse — purchasable warehouse through Realtor.
- Harbor Market — purchasable market.
- Harbor Auto Works — purchasable repair/tuning business.
- Harbor Offices — office/business plots.
- Pier Promenade — public waterfront.
- Shipyard and freight yard — logistics contracts.
- Police checkpoint and new dealer contact.

Economy: freight, storage, delivery, port-worker demand, operating costs and supply contracts.

## Province 2 — Ridgeview
**Theme:** mountains, quarry, industry and research.

Planned POIs:
- Ridgeview Quarry and processing yard.
- Ridgeview Industrial Park.
- Research/lab buildings.
- Mountain Depot.
- Workers' Housing.
- Mountain police post and local dealer/contact.

Economy: extraction-themed contracts, freight, manufacturing inputs, industrial customers and higher transport costs.

## Province 3 — Lakeside
**Theme:** lake, marina, resort, tourism and luxury housing.

Planned POIs:
- Lakeside Marina.
- Lakeside Resort — purchasable via Realtor.
- Lakeside Villas.
- Lakeside Market.
- Campsite and recreation.
- Restaurants and event grounds.
- Lakeside Police Station and VIP contact.

Economy: tourism, seasonal demand, hotel supplies, reservations, event contracts and higher-budget customers.

## Connections
- Harborline: candidate connection from Docks / southern coast via road or bridge.
- Ridgeview: candidate mountain road, possibly tunnel depending on terrain.
- Lakeside: candidate east-side connection with bridge/ring road as terrain permits.
All are hypotheses until world-coordinate survey.

## Implementation gates
1. **Survey:** bounds, elevation, water, colliders, hidden barriers and road graph.
2. **Greybox:** ground, road, bridge and test building; validate collisions, FPS and host/client parity.
3. **Navigation:** determine baked/runtime NavMesh and vehicle road graph. Test pedestrian and vehicle AI separately.
4. **Production:** only then build final terrain, roads, buildings, interiors, lighting and props.
