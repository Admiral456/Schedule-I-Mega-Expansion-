# Changelog

All notable changes to Schedule I Mega Expansion are documented here.

## [0.3.1] - 2026-11-03

### Fixed
- Corrected Hardware Store pricing to write the native `StorableItemDefinition.BasePurchasePrice`; S1API 3.2.2 ignores the `Shop.AddItem(item, customPrice)` argument.
- Applied native shop catalogue activation independently from physical Harborline geometry, so multiplayer sessions and failed terrain placement do not block the existing-shop stock integration.

### Clarified
- Item prices are set on shared vanilla item definitions and therefore affect every shop that sells the same item ID; they are not per-shop prices.
- The Realtor registration and separate Harborline storefront remain unimplemented until a supported native registration path is verified.

## [0.3.0] - 2026-11-02

### Added
- Two physical native S1API dealer NPCs and three customer NPCs with individual native economy data.
- Native Hardware Store catalogue integration for 14 existing Schedule I item IDs.
- S1API compile-time dependency pinned to 3.2.2 and a CI-built Thunderstore package.

## [0.2.0] - 2026-11-02

### Added
- Automatic Harborline physical dockyard geometry generation after native save-load completion.
- Placement search anchored to the native Docks Warehouse property position, with collider occupancy and multi-point ground checks.
- Read-only S1MAPI prefab catalog export to `native-prefabs.csv`.
- Thunderstore package manifest with pinned MelonLoader, S1API Forked and S1MAPI dependencies.
- CI packaging of a complete Thunderstore ZIP with manifest, DLL, README and license.

### Changed
- Prefab network status is tri-state; missing component inspection is reported as unknown rather than local/non-networked.
- Retain fail-closed behavior in multiplayer and when placement cannot be verified.
