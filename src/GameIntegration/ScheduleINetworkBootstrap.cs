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

    public override void OnDeinitializeMelon()
    {
        _networkReady = false;
        _helloSentTo.Clear();
        _compatiblePeers.Clear();

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
