# Native Schedule I system reuse audit

Status date: 2026-11-01. Framework baseline: ifBars S1API Forked 3.2.2 (the latest listed package release on 2026-10-30). This is a documentation-backed integration map; the game-facing project still needs a local IL2CPP build and in-game verification.

## Core rule

Schedule I remains the authority for mechanics it already owns: NPC lifecycle, customer contract generation, dealer behaviour, supplier logistics, dialogue, relationships, daily schedules, law enforcement, item definitions, existing shop catalogues, and delivery state. Mega Expansion should add province-specific content, profiles, data and small adapters around those systems—not a second version of the same game system.

Do not confuse a documented wrapper with a fully proven end-to-end feature. A wrapper may expose read-only state or catalogue operations while the underlying game still owns mutation and persistence.

## Reuse map

| Existing game system | Native surface to use | Expansion responsibility | Do not build again |
|---|---|---|---|
| NPC lifecycle and appearance | S1API NPC types, prefab configuration, save/network integration and NPCImpostorCatalog | Define only the additional residents that the world actually needs; reuse existing game impostors where suitable | A second NPC spawn/save registry or unnecessary replacement character models |
| Customer contracts | IsCustomer, WithCustomerDefaults, NPCCustomer and ForceDealOffer | Map resident-specific preferences to native product affinities/properties, budget, standards and relationship settings; observe native events | A parallel generator for vanilla customer contracts |
| Dealer behaviour | IsDealer, WithDealerDefaults, NPCDealer and DealerType.PlayerDealer / IndependentDealer / RivalDealer | Define the new expansion dealer's identity, unlock/recruitment and province-specific content | A second dealer recruitment, commission, distribution-contract or recommendation engine |
| Supplier and wholesale logistics | IsSupplier, NPCSupplier, supplier-owned Shop/StorageInstance, dead-drop/meeting flow and native delivery vehicle | Configure expansion supplier listings and link expansion stock records only where the native supplier model is not sufficient | A custom supplier stash, delivery van, meeting scheduler or duplicate supplier phone listing |
| Dialogue | NPCDialogue and Dialogue Injection | Add expansion-only choices; inject branches into vanilla dialogue when extending an existing character | Replacing native customer/supplier/dealer/police/cartel dialogue state machines |
| Relationships | NPCRelationship / WithRelationshipDefaults and relationship events | Keep only separate faction or product-specific reputation that the native relationship score cannot represent | A second generic NPC friendship/unlock system |
| Daily activity and movement | WithSchedule, PrefabScheduleBuilder and native schedule actions such as WalkTo, StayInBuilding, DriveToCarPark and EnsureDealSignal | Supply surveyed positions and daily plans for new world content | A second wall-clock scheduler or custom NPC movement loop |
| Police, checkpoints and patrols | LawController and LawManager; native wanted levels, checkpoint evaluation and existing foot/vehicle patrol routes | Add new route/zone content only after geometry and navigation exist; tune native intensity carefully | A parallel wanted-level, dispatch, patrol-AI or checkpoint state machine |
| Items and products | ItemManager and the game's product definitions/registries | Add genuinely new item definitions only when required; reference existing item IDs and product types | Duplicate definitions for vanilla products or a private copy of the game's item catalogue |
| Existing shop catalogues | ShopManager and Shop wrappers | Add/remove catalogue entries on the correct existing shop when appropriate | A fake catalogue pretending to be physical stock or a new checkout built from catalogue calls alone |
| Property and businesses | PropertyWrapper / BusinessWrapper and the existing property/business objects | Verify how a new property is registered in the game's Realtor and connect expansion metadata | A separate Realtor, ownership purchase screen or shadow business purchase state |
| Delivery tracking | NPCSupplier.ActiveDeliveries and DeliveryRegistry read-only wrappers | Display expansion-relevant delivery summaries in Hyland Network | A replacement delivery vehicle or a parallel native-delivery status tracker |
| Phone | Native Schedule I phone apps and S1API PhoneApp | Keep Hyland Network for expansion-only province, online-order and business information | Duplicating ProductManagerApp, supplier listings or native delivery UI without a clear missing feature |

## Specific integration decisions

### Customers and order generation

The native customer system can already generate a contract using products currently listed for sale in ProductManagerApp, NPC drug-type affinities and preferences, spending budget adjusted by relationship, quality standards and addiction. It exposes events for unlocks, completed deals, assigned contracts, product requests and delivery requests. The native NPC also exposes its current contract and awaiting-delivery state.

Therefore, the native customer component must be the source of truth for ordinary in-world customer contracts. Our NpcOrderOfferGenerator remains useful for deterministic unit tests and for expansion-only remote/online order rules that the base game does not provide. It must not create a second vanilla customer offer or masquerade as a native contract.

Native NPC relationships should own the ordinary relationship score and unlock progression. Keep ReputationLedger only for clearly separate concepts such as per-player crew/faction standing or a product-specific expansion reputation.

### Dealers and suppliers are different native roles

Use DealerType.RivalDealer for a competing dealer and PlayerDealer for a recruitable dealer. Native dealer configuration already covers signing fee, commission cut, quality checks, home building, contracts and recommendations.

Supplier NPCs use a different native root role: they must be physical and cannot also be a dealer. S1API creates their supplier stash, shop listings, meeting action, delivery UI integration and delivery vehicle. This is the correct candidate for wholesale replenishment and supplier logistics; it is not automatically proof that it replaces a general public storefront such as Portside Supply.

### Police and new provinces

LawController exposes the game's global law intensity and automatic law-activity evaluation. LawManager exposes current officers, native wanted-level operations, route discovery and starting patrols from the nearest police station. Customer configuration can also use the native call-police chance.

Important limitation: law intensity is a world-level setting, not a per-province switch. New province patrols still require real route objects, valid roads/navigation and a police station with available officers. Do not start patrols on a route that has not been discovered in the loaded scene, and do not change global intensity merely to simulate a local checkpoint.

### Shop, items, property and delivery boundaries

ItemManager and ShopManager are the first lookup points for existing game definitions and shop catalogues. Shop.AddItem/RemoveItem changes a shop's sellable catalogue; it does not prove physical quantity reservation or an atomic checkout. Keep the current safe checkout boundary until cash, inventory debit/delivery, storage quantity mutation and save/reload compensation are verified against the installed assemblies.

PropertyWrapper and BusinessWrapper expose native property/business objects and useful business state. The reviewed public documentation does not yet prove the complete Realtor registration and purchase flow for a mod-added property. S-004 stays open; do not ship a parallel Realtor menu as a shortcut.

DeliveryRegistry is intentionally read-only. Observe native delivery status and receipts; do not attempt to mutate or fabricate native delivery lifecycle transitions through those wrappers.

## Current project changes required by this audit

1. The Contacts page is a resident directory, not a button that manufactures a separate customer contract. The misleading custom request button is removed until profiles are bound to live native NPC components.
2. The Orders page is explicitly expansion-ledger/online-order status. It does not replace the native customer contract interface.
3. Domain order generation and escrow remain available for tests and expansion-only online orders. They are not the source of ordinary in-world customer offers or vanilla cash.
4. The first game-facing vertical slice should prove one native customer end-to-end before scaling to nine profiles, then one RivalDealer, then supplier and law adapters.

## Verification checklist before claiming an integration is complete

- Confirm S1API 3.2.2 and the exact installed game IL2CPP assemblies are the ones used by the build.
- Compile GameIntegration against those local assemblies; documentation-only API knowledge is not a compile result.
- Test one native customer through spawn, save/load, schedule, native offer, contract completion, relationship update and two-player late join.
- Test one RivalDealer through its native recruitment/competition path and ensure it does not create a duplicate custom dealer state.
- Test supplier listing, stash, meeting and delivery with native save/load and co-op authority.
- Enumerate actual police routes in the loaded scene before starting any patrol; test global law-intensity side effects on a copied save.
- Verify property registration/purchase in the original Realtor UI before enabling business purchase in expansion UI.
- Verify cash, item quantity, storage, and shop state after success, failure, retry, disconnect and save reload before enabling online checkout.

## Official references

- S1API overview: https://ifbars.github.io/S1API/docs/introduction.html
- Custom NPCs: https://ifbars.github.io/S1API/docs/custom-npcs.html
- Prefab configuration: https://ifbars.github.io/S1API/docs/prefab-configuration.html
- Customer behaviour: https://ifbars.github.io/S1API/docs/customer-behavior.html
- Dealer system: https://ifbars.github.io/S1API/docs/dealer-system.html
- Supplier NPCs: https://ifbars.github.io/S1API/docs/supplier-system.html
- Dialogue system: https://ifbars.github.io/S1API/docs/dialogue-system.html
- Scheduling system: https://ifbars.github.io/S1API/docs/scheduling-system.html
- Relationship management: https://ifbars.github.io/S1API/docs/relationship-management.html
- LawManager: https://ifbars.github.io/S1API/api/S1API.Law.LawManager.html
- LawController: https://ifbars.github.io/S1API/api/S1API.Law.LawController.html
- Shop: https://ifbars.github.io/S1API/api/S1API.Shops.Shop.html
- BusinessWrapper: https://ifbars.github.io/S1API/api/S1API.Property.BusinessWrapper.html
- Deliveries: https://ifbars.github.io/S1API/docs/delivery-system.html
- S1API 3.2.2 release listing: https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/versions