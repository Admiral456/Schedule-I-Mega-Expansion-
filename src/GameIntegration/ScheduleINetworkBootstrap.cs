using MelonLoader;
using SteamNetworkLib;
using SteamNetworkLib.Core;
using SteamNetworkLib.Models;
using UnityEngine;
using SteamMemberInfo = SteamNetworkLib.Models.MemberInfo;

[assembly: MelonInfo(typeof(ScheduleIMegaExpansion.GameIntegration.ScheduleINetworkBootstrap), "Schedule I Mega Expansion", "0.0.2", "Admiral456")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ScheduleIMegaExpansion.GameIntegration;

public sealed class ExpansionHelloPayload
{
    public int ProtocolVersion { get; set; } = 1;
    public string ModVersion { get; set; } = "0.0.2";
    public int RequiredPlayerCount { get; set; } = 2;
}

public sealed class ExpansionNpcOrderRequestPayload
{
    public string RequestId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
}

public sealed class ExpansionNpcOrderRequestMessage : TypedP2PMessage<ExpansionNpcOrderRequestPayload>
{
    public override string MessageType => "SIME_NPC_ORDER_REQUEST_V1";
    public ExpansionNpcOrderRequestMessage() { Payload = new ExpansionNpcOrderRequestPayload(); }
    public ExpansionNpcOrderRequestMessage(ExpansionNpcOrderRequestPayload payload) : base(payload) { }
}

public sealed class ExpansionNpcOrderResultLinePayload
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public long UnitPrice { get; set; }
}

public sealed class ExpansionNpcOrderResultPayload
{
    public bool Success { get; set; }
    public string Code { get; set; } = string.Empty;
    public string ClientRequestId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string ProvinceId { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long TotalPrice { get; set; }
    public long Reward { get; set; }
    public long DeadlineUnixSeconds { get; set; }
    public long WorldRevision { get; set; }
    public bool IsReplay { get; set; }
    public List<ExpansionNpcOrderResultLinePayload> Items { get; set; } = new();
}

public sealed class ExpansionNpcOrderResultMessage : TypedP2PMessage<ExpansionNpcOrderResultPayload>
{
    public override string MessageType => "SIME_NPC_ORDER_RESULT_V1";
    public ExpansionNpcOrderResultMessage() { Payload = new ExpansionNpcOrderResultPayload(); }
    public ExpansionNpcOrderResultMessage(ExpansionNpcOrderResultPayload payload) : base(payload) { }
}

public sealed class ExpansionHelloMessage : TypedP2PMessage<ExpansionHelloPayload>
{
    public override string MessageType => "SIME_HELLO_V1";

    public ExpansionHelloMessage()
    {
        Payload = new ExpansionHelloPayload();
    }

    public ExpansionHelloMessage(ExpansionHelloPayload payload) : base(payload)
    {
    }
}

/// <summary>
/// SteamNetworkLib lifecycle bootstrap for the default IL2CPP branch.
/// This performs only protocol/version compatibility negotiation, not gameplay synchronization.
/// </summary>
public sealed class ScheduleINetworkBootstrap : MelonMod
{
    private const int SupportedProtocolVersion = 1;
    private const string SupportedModVersion = "0.0.2";
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(2);

    private SteamNetworkClient? _client;
    private bool _networkReady;
    private float _nextRetryAt;
    private string? _lastInitializationWarning;
    private readonly HashSet<ulong> _helloSentTo = new();
    private readonly HashSet<ulong> _compatiblePeers = new();
    private readonly Dictionary<ulong, float> _lastOrderRequestAt = new();

    public static ScheduleINetworkBootstrap? Instance { get; private set; }
    public SteamNetworkClient? Client => _client;

    public override void OnInitializeMelon()
    {
        Instance = this;
        _client = new SteamNetworkClient(new NetworkRules
        {
            EnableRelay = true,
            AcceptOnlyFriends = true
        });

        LoggerInstance.Msg("Mega Expansion loaded. Waiting for Steamworks to become available.");
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        if (string.Equals(sceneName, "Menu", StringComparison.Ordinal))
            TryInitializeNetworking();
    }

    public override void OnUpdate()
    {
        if (_client is null)
            return;

        if (!_networkReady && Time.realtimeSinceStartup >= _nextRetryAt)
            TryInitializeNetworking();

        if (!_networkReady || !_client.IsInitialized)
            return;

        try
        {
            _client.ProcessIncomingMessages();
            SendHelloToNewPeers();
        }
        catch (Exception exception)
        {
            _networkReady = false;
            _nextRetryAt = Time.realtimeSinceStartup + (float)RetryInterval.TotalSeconds;
            LoggerInstance.Warning("Steam networking processing paused: " + exception.Message);
        }
    }

    public bool IsPeerProtocolCompatible(ulong steamId64) => _compatiblePeers.Contains(steamId64);

    public async Task<bool> RequestNpcOrderAsync(string customerId, string shopId)
    {
        if (_client is null || !_networkReady || !_client.IsInitialized || !_client.IsInLobby)
            return false;

        var payload = new ExpansionNpcOrderRequestPayload
        {
            RequestId = Guid.NewGuid().ToString("N"),
            CustomerId = customerId,
            ShopId = shopId
        };

        if (_client.IsHost)
        {
            ExpansionNpcOrderResultPayload localResult =
                new NpcOrderTransactionCoordinator().HandleRequest(payload, 0, _client);
            if (!localResult.Success)
                LoggerInstance.Warning("NPC order request was not committed: " + localResult.Code);
            return localResult.Success;
        }

        List<SteamMemberInfo> remoteMembers = _client.GetRemoteMembers();
        if (remoteMembers.Count != 1 || !_compatiblePeers.Contains(remoteMembers[0].SteamId64))
            return false;

        try
        {
            return await _client.SendMessageToPlayerAsync(
                remoteMembers[0].SteamId,
                new ExpansionNpcOrderRequestMessage(payload));
        }
        catch (Exception exception)
        {
            LoggerInstance.Warning("Could not send NPC order request to the host: " + exception.Message);
            return false;
        }
    }

    public override void OnDeinitializeMelon()
    {
        _networkReady = false;
        _helloSentTo.Clear();
        _compatiblePeers.Clear();
        _lastOrderRequestAt.Clear();
        ExpansionOrderSyncCache.Clear();

        try
        {
            _client?.Dispose();
        }
        catch (Exception exception)
        {
            LoggerInstance.Warning("Steam networking cleanup failed: " + exception.Message);
        }

        _client = null;
        Instance = null;
    }

    private void TryInitializeNetworking()
    {
        if (_client is null)
            return;

        if (_client.IsInitialized)
        {
            MarkNetworkingReady();
            return;
        }

        try
        {
            if (_client.TryInitialize(out var error))
            {
                MarkNetworkingReady();
                return;
            }

            _networkReady = false;
            _nextRetryAt = Time.realtimeSinceStartup + (float)RetryInterval.TotalSeconds;

            string warning = error?.Message ?? "Steamworks is not ready yet.";
            if (!string.Equals(warning, _lastInitializationWarning, StringComparison.Ordinal))
            {
                LoggerInstance.Warning("Steam networking is unavailable; local expansion systems remain enabled. " + warning);
                _lastInitializationWarning = warning;
            }
        }
        catch (Exception exception)
        {
            _networkReady = false;
            _nextRetryAt = Time.realtimeSinceStartup + (float)RetryInterval.TotalSeconds;
            LoggerInstance.Warning("Steam networking initialization failed; retrying later. " + exception.Message);
        }
    }

    private void MarkNetworkingReady()
    {
        if (_networkReady || _client is null)
            return;

        // SteamNetworkLib's handler receives native CSteamID, not MemberInfo.
        _client.RegisterMessageHandler<ExpansionHelloMessage>((message, senderId) =>
        {
            ulong peerId = senderId.m_SteamID;

            if (!_client.IsInLobby ||
                !_client.GetRemoteMembers().Any(member => member.SteamId64 == peerId))
            {
                LoggerInstance.Warning("Ignored compatibility message from a peer outside the active lobby: " + peerId);
                return;
            }

            int peerProtocol = message.Payload?.ProtocolVersion ?? 0;
            string peerVersion = message.Payload?.ModVersion ?? string.Empty;

            if (peerProtocol != SupportedProtocolVersion || peerVersion != SupportedModVersion)
            {
                _compatiblePeers.Remove(peerId);
                LoggerInstance.Warning("Co-op peer " + peerId + " has incompatible Mega Expansion version/protocol (" +
                    peerVersion + " / " + peerProtocol + ").");
                return;
            }

            _compatiblePeers.Add(peerId);
            LoggerInstance.Msg("Compatible Mega Expansion co-op peer detected: " + peerId + ".");
        });

        _client.RegisterMessageHandler<ExpansionNpcOrderRequestMessage>((message, senderId) =>
        {
            ulong peerId = senderId.m_SteamID;
            if (!_client.IsHost ||
                !_client.IsInLobby ||
                !_compatiblePeers.Contains(peerId) ||
                !_client.GetRemoteMembers().Any(member => member.SteamId64 == peerId))
            {
                LoggerInstance.Warning("Ignored NPC order request from a non-authoritative or incompatible peer: " + peerId);
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (_lastOrderRequestAt.TryGetValue(peerId, out float lastRequestAt) && now - lastRequestAt < 3f)
            {
                _ = SendNpcOrderResultAsync(peerId, new ExpansionNpcOrderResultPayload
                {
                    Success = false,
                    ClientRequestId = message.Payload?.RequestId ?? string.Empty,
                    Code = "RateLimited"
                });
                return;
            }

            _lastOrderRequestAt[peerId] = now;
            ExpansionNpcOrderResultPayload result =
                new NpcOrderTransactionCoordinator().HandleRequest(message.Payload, peerId, _client);
            _ = SendNpcOrderResultAsync(peerId, result);
        });

        _client.RegisterMessageHandler<ExpansionNpcOrderResultMessage>((message, senderId) =>
        {
            ulong peerId = senderId.m_SteamID;
            List<SteamMemberInfo> remotes = _client.GetRemoteMembers();
            if (_client.IsHost ||
                !_client.IsInLobby ||
                remotes.Count != 1 ||
                remotes[0].SteamId64 != peerId ||
                !_compatiblePeers.Contains(peerId))
            {
                LoggerInstance.Warning("Ignored NPC order result not received from the sole compatible co-op host.");
                return;
            }

            ExpansionOrderSyncCache.Upsert(message.Payload);
        });

        _networkReady = true;
        _lastInitializationWarning = null;
        LoggerInstance.Msg("SteamNetworkLib initialized. Waiting for co-op peers.");
        SendHelloToNewPeers();
    }

    private void SendHelloToNewPeers()
    {
        if (_client is null || !_client.IsInitialized || !_client.IsInLobby)
            return;

        List<SteamMemberInfo> remoteMembers = _client.GetRemoteMembers();
        var connectedIds = remoteMembers.Select(member => member.SteamId64).ToHashSet();
        _helloSentTo.RemoveWhere(id => !connectedIds.Contains(id));
        _compatiblePeers.RemoveWhere(id => !connectedIds.Contains(id));

        if (remoteMembers.Count > 1)
            LoggerInstance.Warning("This project is configured for two-player co-op, but more than one remote player is in the lobby.");

        foreach (SteamMemberInfo member in remoteMembers)
        {
            if (!_helloSentTo.Add(member.SteamId64))
                continue;

            _ = SendHelloAsync(member);
        }
    }

    private async Task SendNpcOrderResultAsync(ulong peerId, ExpansionNpcOrderResultPayload payload)
    {
        if (_client is null || !_client.IsInitialized || !_client.IsInLobby)
            return;

        List<SteamMemberInfo> matchingMembers = _client.GetRemoteMembers()
            .Where(member => member.SteamId64 == peerId)
            .ToList();
        if (matchingMembers.Count != 1)
            return;

        try
        {
            bool sent = await _client.SendMessageToPlayerAsync(
                matchingMembers[0].SteamId,
                new ExpansionNpcOrderResultMessage(payload));
            if (!sent)
                LoggerInstance.Warning("Could not deliver NPC order result to co-op peer " + peerId + ".");
        }
        catch (Exception exception)
        {
            LoggerInstance.Warning("NPC order result delivery failed for " + peerId + ": " + exception.Message);
        }
    }

    private async Task SendHelloAsync(SteamMemberInfo member)
    {
        if (_client is null || !_client.IsInitialized)
            return;

        try
        {
            var payload = new ExpansionHelloPayload
            {
                ProtocolVersion = SupportedProtocolVersion,
                ModVersion = SupportedModVersion,
                RequiredPlayerCount = 2
            };

            bool sent = await _client.SendMessageToPlayerAsync(member.SteamId, new ExpansionHelloMessage(payload));
            if (!sent)
            {
                _helloSentTo.Remove(member.SteamId64);
                LoggerInstance.Warning("SteamNetworkLib did not accept the compatibility handshake for " + member.SteamId64 + ".");
            }
        }
        catch (Exception exception)
        {
            _helloSentTo.Remove(member.SteamId64);
            LoggerInstance.Warning("Could not send co-op compatibility handshake to " + member.SteamId64 + ": " + exception.Message);
        }
    }
}
