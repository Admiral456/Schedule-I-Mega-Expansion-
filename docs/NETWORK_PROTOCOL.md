# Network protocol and replay protection

## Envelope
Messages are validated against the authenticated transport peer and active Steam session before payload handling:
- protocolVersion
- sessionId
- senderSteamId
- sender sequence number
- messageType
- requestId for mutating requests
- JSON object payload with a byte limit

The payload byte limit defaults to 64 KiB. Mutating request types require requestId. A valid envelope is not authorization: host-side permission and state validation are still required.

## Sender identity
The transport layer's authenticated Steam ID is the source of truth. A sender ID copied into JSON is never trusted on its own.

## Replay window
Each sender has a 64-message sliding replay window. It permits limited out-of-order delivery but rejects duplicate messages and messages older than the window. Maintain separate windows per sender, session and protocol epoch. Reset only when a new authenticated session is established.

## Idempotency
Mutating operations have a requestId and stable semantic payload. A repeated ID with the exact same request replays the previously committed result. Reusing an ID with different parameters is a conflict and must not mutate state.

## Delivery assumptions
The replay window is defense-in-depth. It does not provide transport reliability, encryption, authentication or anti-cheat. These guarantees must be confirmed in the SteamNetworkLib adapter spike.
