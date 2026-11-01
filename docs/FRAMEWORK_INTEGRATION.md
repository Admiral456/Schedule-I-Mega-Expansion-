# Verified framework integration baseline

Checked against the current upstream repositories and documentation on 2026-11-01. These are pinned development baselines, not a claim that the complete game mod has been integration-tested.

## IL2CPP runtime matrix

| Component | Baseline | Target/runtime | Role |
|---|---:|---|---|
| ifBars/S1API | 3.2.2 | IL2CPP MelonLoader, .NET 6.0 | Native phone apps, per-save-slot Saveables, NPC and game API abstractions |
| ifBars/S1MAPI | 2.0.1 | IL2CPP build, .NET 6.0 | Procedural meshes, building construction and GLTF/GLB loading |
| ifBars/SteamNetworkLib | 1.6.0 | IL2CPP release binary; library target .NET Standard 2.1 | Steam lobby and typed P2P messaging |
| Game-facing mod | development | .NET 6.0 | Matches S1API/S1MAPI IL2CPP targets |
| Domain Core | development | .NET 6.0 | Game-independent order, purchase, reputation, save and protocol rules |

## S1API: phone apps and save slots

The current ifBars fork exposes a native PhoneApp lifecycle and an attribute-based Saveable system.

Phone apps:
- derive from S1API.PhoneApp.PhoneApp;
- define AppName, AppTitle, IconLabel and IconFileName;
- build their UI in OnCreatedUI(GameObject container);
- are auto-discovered when the phone HomeScreen starts, so do not manually register them.

Saveables:
- directly inherit S1API.Internal.Abstraction.Saveable;
- are concrete public classes with a public parameterless constructor;
- mark persisted fields with S1API.Saveables.SaveableField;
- use OnLoaded / OnSaved and optional SaveableLoadOrder.

S1API automatically discovers Saveable subclasses. ModSaveableRegistry is obsolete and must not be used.

Our first game adapter stores the versioned expansion document as one raw JSON string field. It validates namespace and schema before deserializing into typed state. Unknown/newer data is left read-only and the original raw string is preserved, preventing an older build from silently overwriting fields it does not understand.

References:
- https://github.com/ifBars/S1API
- https://ifbars.github.io/S1API/docs/phone-app.html
- https://ifbars.github.io/S1API/docs/save-system.html

## SteamNetworkLib lifecycle and messages

Current SteamNetworkLib guidance and source confirm:
1. Create SteamNetworkClient from OnInitializeMelon.
2. Steamworks may not be ready yet; attempt TryInitialize() after the Menu scene and retry.
3. Call ProcessIncomingMessages() only after IsInitialized is true.
4. Dispose the client from OnDeinitializeMelon.

For custom gameplay messages, use TypedP2PMessage<TPayload> with a parameterless message constructor and small serializable DTOs. The handler receives native CSteamID as its sender argument. SendMessageToPlayerAsync also takes native CSteamID, while MemberInfo.SteamId64 is useful for stable dictionary keys and JSON.

Use stable IDs and primitive values in payloads. Do not send CSteamID, Unity objects, live item instances or scene references inside your DTOs. The request/response helper is the recommended pattern for host-approved checkout and other mutating operations.

The current ScheduleINetworkBootstrap only performs initialization and a compatibility hello handshake. It does not yet synchronize orders, shops, businesses, NPCs or world snapshots.

References:
- https://github.com/ifBars/SteamNetworkLib
- https://ifbars.github.io/SteamNetworkLib/docs/getting-started.html
- https://ifbars.github.io/SteamNetworkLib/docs/p2p-messaging.html
- https://ifbars.github.io/SteamNetworkLib/docs/data-synchronization.html

## S1MAPI scope and limitation

The current S1MAPI release has separate Mono and IL2CPP builds. Its IL2CPP project target is .NET 6.0 and its generated assembly is S1MAPI_Il2cpp. The framework describes procedural meshes, building construction and GLTF/GLB loading; this is enough to begin a geometry greybox spike.

It does not prove that Schedule I can stream new world regions, stitch terrain, rebuild navigation, extend vehicle traffic routes or expose the new regions in the vanilla map UI. Those remain separate in-game technical spikes.

Reference:
- https://github.com/ifBars/S1MAPI
- https://ifbars.github.io/S1MAPI/docs/getting-started.html

## Local developer prerequisites

Install matching dependencies for the same Schedule I public build and Steam Default / IL2CPP branch. Do not commit game assemblies or third-party DLLs.

Default paths used by src/GameIntegration/GameIntegration.csproj:
- MelonLoader: GameDir/MelonLoader/net6/MelonLoader.dll
- IL2CPP assemblies: GameDir/MelonLoader/Il2CppAssemblies/
- S1API: GameDir/Mods/S1API.Il2Cpp.MelonLoader.dll
- SteamNetworkLib: libs/SteamNetworkLib/Il2cpp/SteamNetworkLib.dll (download the matching IL2CPP release; this folder is git-ignored)

Override individual MSBuild properties when your local installation differs:
GameDir, MelonLoaderDll, Il2CppAssembliesDir, S1ApiDll, SteamNetworkLibDll.

Build command:

    dotnet build src/GameIntegration/GameIntegration.csproj -c Release -p:GameDir="D:\SteamLibrary\steamapps\common\Schedule I"

The game-facing project is not built by GitHub CI because the repository does not contain the player's game assemblies or third-party release DLLs. CI runs the independent domain-core tests and validates the project data instead.
