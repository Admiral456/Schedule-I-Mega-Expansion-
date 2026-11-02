# Changelog

All notable changes to Schedule I Mega Expansion are documented here.

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
