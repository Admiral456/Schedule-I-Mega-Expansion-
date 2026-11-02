# Network protocol and replay protection

## Transport and payload
SteamNetworkLib 1.6.0 is the planned transport. Use typed P2P messages for compact gameplay DTOs and P2PRequestResponseClient for host-approved request/response operations such as checkout.

Typed message payloads should contain simple serializable values and stable IDs. Do not place CSteamID values, Unity objects, live item instances or scene object references inside payload DTOs.

The authenticated sender Steam ID comes from the typed handler sender metadata (CSteamID) and is converted to a 64-bit key for our domain envelope. A sender ID copied inside the payload is never trusted.

## Envelope checks
The game-independent validator checks:
- protocolVersion;
- active session ID;
- authenticated sender identity;
- non-negative sequence;
- registered message type;
- required requestId for mutating requests;
- JSON object payload and a default 64 KiB byte limit.

A valid envelope is not authorization. Host-side permission and state validation remain mandatory.

## Replay window
Each sender has a 64-message sliding replay window. It permits limited out-of-order delivery but rejects duplicate messages and messages older than the window. Maintain one window per sender, session and protocol epoch. Reset it only after a new authenticated session is established.

## Idempotency
A repeated requestId with the exact same semantic request replays the previous result. Reusing an ID with different parameters is a conflict and must not mutate state.

## Initialization lifecycle
Steamworks can be unavailable during OnInitializeMelon. The adapter creates the client, attempts initialization after the Menu scene, retries while Steamworks is not ready, processes incoming messages only after successful initialization, and disposes the client during mod shutdown.

The game-facing bootstrap currently performs only a protocol handshake. The host-authoritative order/shop/business messages and late-join snapshot are not wired yet.

## Limits
The replay window and request idempotency are our own defense-in-depth. They do not independently provide encryption, Steam authentication, guaranteed delivery, authority or anti-cheat. Those responsibilities are split between Steamworks transport metadata and our host-authoritative services.
