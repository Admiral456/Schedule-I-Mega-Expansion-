# Mod-owned save store and schema migration

## Scope
JsonSaveStore stores mod-owned extension data at a caller-supplied path. It must never be pointed at the vanilla Schedule I save file.

## Atomic-write approach
1. Serialize to a temporary file in the same directory.
2. Flush the temporary file to disk.
3. Copy the current mod file to a .bak backup.
4. Replace the target with the temporary file.
5. Clean up any leftover temporary file.

After a successful subsequent save, the previous valid file remains available as .bak. Production integration must still validate mod ID, schema version and data invariants after reading.

## Migration pipeline
- Migrations are explicit delegates from version N to N+1.
- Every intermediate step must exist.
- A save from a newer schema is rejected instead of downgraded.
- Wrong mod namespace, missing world ID, negative world revision and missing data fail closed.
- Migrations produce a new document; the source data should not be mutated.

## Existing-save safety
The store is not wired to S1API's actual save lifecycle yet. Integration must determine a separate mod-owned path from verified S1API documentation/API before release. First testing must use a copy of the player's save. Never pass the vanilla save path to this class.
