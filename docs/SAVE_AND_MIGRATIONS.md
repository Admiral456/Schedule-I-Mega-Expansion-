# Save data, schema versioning and migrations

## Rules
- Do not overwrite vanilla save structures unless a verified safe API requires it.
- Store extension data under namespace schedulei.megaexpansion.
- Every record has stable ID and schemaVersion.
- Host owns shared state; client-local preferences remain separate.
- First build never migrates the original save without explicit backup.

## Envelope
modId, schemaVersion, worldId, worldRevision, createdAt, updatedAt, provinceState, businesses, npcs, orders, contracts, reputation, policeEvents and idempotencyLedger.

## Stable IDs
- sime.province.harborline
- sime.poi.harborline.portside_supply
- sime.business.harborline.portside_supply
- sime.npc.harborline.morgan_reed
- sime.order.<uuid>
- sime.contract.<uuid>

Never recycle IDs or change them for localization.

## Migration
1. Read vanilla save without modifying it.
2. Detect extension data.
3. Back up before first write.
4. Validate schema.
5. Run migrations one version at a time.
6. Migrations must be deterministic and idempotent.
7. Write new versioned blob.
8. Verify reload and integrity.
9. On error preserve prior blob and roll back.

## Feature flags
world.harborline.enabled, world.ridgeview.enabled, world.lakeside.enabled, phone.hyland_network.enabled, systems.npc_orders.enabled, systems.police_events.enabled, systems.realtor_listings.enabled.

Provinces are disabled by default during development until their greybox passes.

## Uninstall
Export extension state first. If new buildings/businesses exist, warn and provide safe cleanup/export. Never delete unknown IDs automatically.
