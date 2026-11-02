# Mod-owned save data and schema migration

## Primary in-game persistence: S1API Saveable

The game integration uses S1API's supported per-save-slot Saveable system:
- a concrete, public subclass directly inherits S1API.Internal.Abstraction.Saveable;
- a public parameterless constructor allows S1API auto-discovery;
- persisted fields are marked with S1API.Saveables.SaveableField;
- OnLoaded and OnSaved are used for lifecycle handling.

The adapter stores one raw JSON string field. It checks the mod namespace and schema version before deserializing into a typed object. If the save is from a newer schema, belongs to another namespace or is malformed, the adapter leaves the original raw string untouched and marks expansion state read-only. This avoids a version downgrade silently deleting fields the current build does not understand.

Shared world data may only be changed by the authenticated host. The network adapter must derive host authority from the active SteamNetworkLib session, never from a client-supplied boolean or payload. The serialized field is updated before requesting the normal game save.

## Generic file-store utility

JsonSaveStore<T> remains available in the game-independent Core library for isolated tests or future mod-owned sidecar data. It writes to a caller-supplied path, uses a temporary file and keeps a .bak copy of the prior file.

It is not wired to the game's S1API lifecycle and must never be pointed at the vanilla Schedule I save file. Do not use it as a second competing persistence source for the same expansion state.

## Migration rules
- Migrations are explicit delegates from version N to N+1.
- Every intermediate step must exist.
- A save from a newer schema is read-only; never downgrade it.
- Wrong mod namespace, missing world ID, negative world revision and missing data fail closed.
- Existing-save tests use a copied save.
- Keep vanilla world data untouched.

## References
- https://ifbars.github.io/S1API/docs/save-system.html
- https://ifbars.github.io/S1API/docs/introduction.html
