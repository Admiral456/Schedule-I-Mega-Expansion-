using System.Collections.Concurrent;

namespace ScheduleIMegaExpansion.GameIntegration;

/// <summary>
/// Session-local client mirror of orders confirmed by the host.
/// The host save remains the durable source of truth.
/// </summary>
public static class ExpansionOrderSyncCache
{
    private static readonly ConcurrentDictionary<string, ExpansionNpcOrderResultPayload> Orders =
        new(StringComparer.Ordinal);

    public static void Upsert(ExpansionNpcOrderResultPayload? payload)
    {
        if (payload is null || !payload.Success || string.IsNullOrWhiteSpace(payload.OrderId))
            return;

        Orders[payload.OrderId] = payload;
    }

    public static IReadOnlyList<ExpansionNpcOrderResultPayload> GetAll() =>
        Orders.Values.OrderBy(order => order.OrderId, StringComparer.Ordinal).ToArray();

    public static void Clear() => Orders.Clear();
}