using System.Text;
using System.Text.Json;

namespace ScheduleIMegaExpansion.Core.Network;

public enum NetworkMessageType
{
    SnapshotRequest,
    SnapshotResponse,
    ShopPurchaseRequested,
    ShopPurchaseCommitted,
    OrderOffered,
    OrderAccepted,
    OrderCompleted,
    ReputationChanged,
    BusinessPurchaseRequested,
    BusinessPurchaseCommitted,
    ContractUpdated,
    PoliceEventUpdated,
    WorldRevisionChanged
}

public enum NetworkValidationError
{
    None,
    MissingIdentity,
    SenderMismatch,
    SessionMismatch,
    UnsupportedProtocol,
    InvalidSequence,
    UnknownMessageType,
    MissingRequestId,
    InvalidRequestId,
    PayloadTooLarge,
    InvalidPayloadJson,
    PayloadMustBeObject
}

public sealed record NetworkMessageEnvelope(
    int ProtocolVersion,
    string SessionId,
    string SenderSteamId,
    long Sequence,
    string MessageType,
    string? RequestId,
    string PayloadJson);

public sealed record NetworkValidationResult(bool IsValid, NetworkValidationError Error, string Detail)
{
    public static NetworkValidationResult Success() =>
        new(true, NetworkValidationError.None, string.Empty);

    public static NetworkValidationResult Reject(NetworkValidationError error, string detail) =>
        new(false, error, detail);
}

/// <summary>
/// Validates an envelope against the authenticated transport peer and active session.
/// This does not replace Steam authentication or host-side authorization.
/// </summary>
public static class NetworkMessageValidator
{
    public const int DefaultMaximumPayloadBytes = 64 * 1024;

    private static readonly HashSet<NetworkMessageType> RequestTypes =
        new HashSet<NetworkMessageType>
        {
            NetworkMessageType.ShopPurchaseRequested,
            NetworkMessageType.BusinessPurchaseRequested,
            NetworkMessageType.OrderAccepted
        };

    public static NetworkValidationResult Validate(
        NetworkMessageEnvelope? message,
        string authenticatedSenderSteamId,
        string expectedSessionId,
        int supportedProtocolVersion,
        int maximumPayloadBytes = DefaultMaximumPayloadBytes)
    {
        if (message is null)
            return NetworkValidationResult.Reject(NetworkValidationError.MissingIdentity, "Message is missing.");

        if (string.IsNullOrWhiteSpace(authenticatedSenderSteamId) ||
            string.IsNullOrWhiteSpace(expectedSessionId) ||
            string.IsNullOrWhiteSpace(message.SenderSteamId) ||
            string.IsNullOrWhiteSpace(message.SessionId))
        {
            return NetworkValidationResult.Reject(NetworkValidationError.MissingIdentity, "Sender and session identities are required.");
        }

        if (!string.Equals(message.SenderSteamId, authenticatedSenderSteamId, StringComparison.Ordinal))
            return NetworkValidationResult.Reject(NetworkValidationError.SenderMismatch, "Envelope sender does not match the authenticated transport peer.");

        if (!string.Equals(message.SessionId, expectedSessionId, StringComparison.Ordinal))
            return NetworkValidationResult.Reject(NetworkValidationError.SessionMismatch, "Message belongs to a different multiplayer session.");

        if (message.ProtocolVersion != supportedProtocolVersion)
            return NetworkValidationResult.Reject(NetworkValidationError.UnsupportedProtocol, "Protocol version is not supported by this host.");

        if (message.Sequence < 0)
            return NetworkValidationResult.Reject(NetworkValidationError.InvalidSequence, "Sequence must be zero or greater.");

        if (!Enum.TryParse(message.MessageType, ignoreCase: false, out NetworkMessageType messageType) ||
            !Enum.IsDefined(typeof(NetworkMessageType), messageType))
        {
            return NetworkValidationResult.Reject(NetworkValidationError.UnknownMessageType, "Message type is not part of this protocol.");
        }

        if (RequestTypes.Contains(messageType) && string.IsNullOrWhiteSpace(message.RequestId))
            return NetworkValidationResult.Reject(NetworkValidationError.MissingRequestId, "Mutating requests require an idempotency requestId.");

        if (message.RequestId is not null &&
            (message.RequestId.Length > 128 || message.RequestId.Any(char.IsControl)))
        {
            return NetworkValidationResult.Reject(NetworkValidationError.InvalidRequestId, "requestId must be at most 128 characters and contain no control characters.");
        }

        if (maximumPayloadBytes < 2)
            return NetworkValidationResult.Reject(NetworkValidationError.PayloadTooLarge, "Configured payload limit is invalid.");

        string payload = message.PayloadJson ?? string.Empty;
        if (Encoding.UTF8.GetByteCount(payload) > maximumPayloadBytes)
            return NetworkValidationResult.Reject(NetworkValidationError.PayloadTooLarge, "Payload exceeds the configured byte limit.");

        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return NetworkValidationResult.Reject(NetworkValidationError.PayloadMustBeObject, "Payload root must be a JSON object.");
        }
        catch (JsonException)
        {
            return NetworkValidationResult.Reject(NetworkValidationError.InvalidPayloadJson, "Payload is not valid JSON.");
        }

        return NetworkValidationResult.Success();
    }
}
