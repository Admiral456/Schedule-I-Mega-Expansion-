# Hyland Network phone app implementation

## Current implementation
The native S1API PhoneApp has six interactive navigation sections:
- Home reads the current S1API Saveable snapshot and displays world revision, enabled province count, open orders and owned-business count.
- Contacts displays three registered resident profiles and marks them as not yet bound to spawned NPC instances.
- Orders reads persisted expansion order records from the current save slot.
- Online Shop documents the shared Portside Supply inventory contract and deliberately disables checkout until the real game cash/item transaction adapter exists.
- Businesses reads saved ownership records and states that the vanilla Realtor hook is pending.
- Provinces shows the three planned provinces and keeps them disabled until world streaming, collisions and navigation are proven.

The app uses S1API PhoneApp auto-discovery, UIFactory, ButtonUtils and a scrollable card list.

## Deliberately not claimed
- Contacts are not live spawned NPCs.
- The shop does not place orders or charge money.
- Businesses are not yet purchasable through vanilla Realtor.
- Provinces are not loaded into the game world.
- Order cards show only records already written into ExpansionSaveable.

## Next integration steps
1. Connect NPC profiles to S1API NPC identity and schedule APIs.
2. Build a host-only shop checkout request/response using SteamNetworkLib.
3. Bind checkout to actual Schedule I cash and item inventory transactions.
4. Load and commit the domain shop snapshot through ExpansionSaveable's host-only, optimistic-revision methods.
5. Bind the committed purchase to actual Schedule I cash and item inventory transactions.
6. Hook expansion Realtor listings into the existing vanilla listing UI and purchase flow.
7. Implement a world greybox and validate collision/navigation before enabling a province.
