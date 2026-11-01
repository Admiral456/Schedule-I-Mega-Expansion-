namespace ScheduleIMegaExpansion.Core;

/// <summary>
/// Helpers for stable, namespaced identifiers. Display names must never be used as save keys.
/// </summary>
public static class StableId
{
    public const string ProvincePrefix = "sime.province.";
    public const string PoiPrefix = "sime.poi.";
    public const string BusinessPrefix = "sime.business.";
    public const string NpcPrefix = "sime.npc.";
    public const string OrderPrefix = "sime.order.";
    public const string ContractPrefix = "sime.contract.";

    public static bool HasPrefix(string? value, string prefix) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.StartsWith(prefix, StringComparison.Ordinal) &&
        value.Length > prefix.Length &&
        value.Length <= 160;

    public static string NewOrderId() => $"{OrderPrefix}{Guid.NewGuid():N}";
    public static string NewContractId() => $"{ContractPrefix}{Guid.NewGuid():N}";
}
