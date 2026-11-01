using ScheduleIMegaExpansion.Core;
using SteamNetworkLib;

namespace ScheduleIMegaExpansion.GameIntegration;

/// <summary>
/// Converts untrusted network intent into a host-validated and persisted NPC order.
/// Prices, stock, resident preferences, reputation and deadlines are resolved by the host.
/// </summary>
public sealed class NpcOrderTransactionCoordinator
{
    private static readonly object TransactionGate = new();
    private const long OrderLifetimeSeconds = 30 * 60;
    private const long WorldIntervalSeconds = 30 * 60;

    public ExpansionNpcOrderResultPayload HandleRequest(
        ExpansionNpcOrderRequestPayload? payload,
        ulong requesterSteamId,
        SteamNetworkClient? network)
    {
        lock (TransactionGate)
        {
            if (network is null || !network.IsInitialized || !network.IsInLobby || !network.IsHost)
                return Failure(payload?.RequestId, "NotAuthoritativeHost");

            if (payload is null ||
                !Guid.TryParseExact(payload.RequestId, "N", out _) ||
                !StableId.HasPrefix(payload.CustomerId, StableId.NpcPrefix) ||
                !StableId.HasPrefix(payload.ShopId, StableId.BusinessPrefix))
            {
                return Failure(payload?.RequestId, "InvalidRequest");
            }

            string requesterId = requesterSteamId == 0
                ? "local-host"
                : requesterSteamId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string requestKey = requesterId + ":" + payload.RequestId;
            string fingerprint = requesterId + "|" + payload.CustomerId + "|" + payload.ShopId;

            ExpansionSaveable? save = ExpansionSaveable.Instance;
            if (save is null || !save.IsLoaded || save.IsReadOnly)
                return Failure(payload.RequestId, "SaveUnavailable");

            if (save.TryGetProcessedOrderRequest(
                requestKey, fingerprint, out ExpansionOrderRecord? previous, out bool conflict))
            {
                if (conflict || previous is null)
                    return Failure(payload.RequestId, "RequestIdConflict");

                return FromSavedOrder(
                    payload.RequestId,
                    previous,
                    save.GetDocumentSnapshot()?.WorldRevision ?? 0,
                    isReplay: true);
            }

            NpcCustomerDirectoryEntry? customer = NpcCustomerDirectory.FindById(payload.CustomerId);
            if (customer is null)
                return Failure(payload.RequestId, "UnknownCustomer");

            if (!save.TryLoadShopPurchaseService(out ShopPurchaseService? shop, out long expectedRevision) ||
                shop is null)
            {
                return Failure(payload.RequestId, "ShopSnapshotUnavailable");
            }

            ExpansionSaveDocument? current = save.GetDocumentSnapshot();
            if (current?.Data is null)
                return Failure(payload.RequestId, "SaveUnavailable");

            // Until the Schedule I world-clock adapter is bound, use host UTC time.
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long interval = now / WorldIntervalSeconds;
            string reputationKey = "player:" + requesterId + ":npc:" + customer.Profile.NpcId;
            int reputation = current.Data.ReputationByScope.TryGetValue(reputationKey, out int savedReputation)
                ? savedReputation
                : 0;

            var request = new NpcOrderPlacementRequest(
                requestKey,
                fingerprint,
                requesterId,
                payload.ShopId,
                interval,
                now,
                checked(now + OrderLifetimeSeconds),
                reputation);

            NpcOrderPlacementResult placement = NpcOrderPlacementService.Create(
                shop,
                customer,
                request,
                isAuthoritativeHost: network.IsHost);

            if (!placement.Success || placement.Order is null)
                return Failure(payload.RequestId, placement.Failure.ToString());

            if (!save.TryCommitNpcOrderTransaction(shop, placement.Order, expectedRevision, network))
                return Failure(payload.RequestId, "CommitRejectedOrSaveFailed");

            long revision = save.GetDocumentSnapshot()?.WorldRevision ?? expectedRevision + 1;
            return FromDraft(payload.RequestId, placement.Order, revision);
        }
    }

    private static ExpansionNpcOrderResultPayload FromSavedOrder(
        string clientRequestId,
        ExpansionOrderRecord order,
        long revision,
        bool isReplay)
        => new()
        {
            Success = true,
            Code = isReplay ? "Replay" : "Created",
            ClientRequestId = clientRequestId,
            OrderId = StableId.FromOrderRequest(order.RequestId),
            CustomerId = order.CustomerId,
            ProvinceId = order.ProvinceId,
            ShopId = order.ShopId,
            Status = order.Status,
            TotalPrice = order.TotalPrice,
            Reward = order.Reward,
            DeadlineUnixSeconds = order.DeadlineWorldTime ?? 0,
            WorldRevision = revision,
            IsReplay = isReplay,
            Items = order.Items.Select(item => new ExpansionNpcOrderResultLinePayload
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };

    private static ExpansionNpcOrderResultPayload FromDraft(
        string clientRequestId,
        NpcOrderDraft order,
        long revision)
        => new()
        {
            Success = true,
            Code = "Created",
            ClientRequestId = clientRequestId,
            OrderId = order.OrderId,
            CustomerId = order.CustomerId,
            ProvinceId = order.ProvinceId,
            ShopId = order.ShopId,
            Status = order.Status,
            TotalPrice = order.TotalPrice,
            Reward = order.TotalPrice,
            DeadlineUnixSeconds = order.DeadlineUnixSeconds,
            WorldRevision = revision,
            Items = order.Items.Select(item => new ExpansionNpcOrderResultLinePayload
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };

    private static ExpansionNpcOrderResultPayload Failure(string? requestId, string code) => new()
    {
        Success = false,
        Code = code,
        ClientRequestId = requestId ?? string.Empty
    };
}