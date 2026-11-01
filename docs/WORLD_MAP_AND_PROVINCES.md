# World map and the three new provinces

## Design rule
Vanilla Hyland Point remains the center. Add three large provinces at map edges. Previous generated posters are concept art only; the user's vanilla map screenshot is a visual reference, not a coordinate map. Do not guess world coordinates from pixels.

## Existing districts to preserve
Westville, Northtown, Downtown, Uptown, Docks and Suburbia. Their exact boundaries and road/nav nodes must be confirmed in-game.

## Province 1 — Harborline
**Theme:** southern coast, cargo port, logistics, retail and industry.

Harborline is now planned as seven connected subdistricts: Harbor Gate and Customs Approach (including a native Harbor Police Substation); Container Port and Shipyard; Freight and Logistics Park; Harbor Market and High Street; Industrial and Vehicle Service Quarter; Workers' Residential Quarter; Waterfront, Marina and Pier Promenade.

The points of interest include the existing Portside Supply, Harbor Freight Warehouse, Harbor Market, Harbor Auto Works, Harbor Offices and Pier Promenade plus a container terminal, crane yard, ship repair, cold storage, freight depot, truck staging, market square, Harbor Diner, maintenance depot, worker apartments, laundromat, marina, bait shop and visitor services.

Target population: 42 physical NPCs (26 native customers and one native dealer), with 16 active during the first population stage. Police remain native LawController/LawManager/CheckpointManager activity; there are no custom police NPCs. Exact zone and POI assignments are listed in data/harborline_population.json.

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
