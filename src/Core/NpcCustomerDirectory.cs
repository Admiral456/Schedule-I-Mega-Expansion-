namespace ScheduleIMegaExpansion.Core;

/// <summary>
/// Static identity and preference catalogue for expansion residents.
/// These are data profiles, not spawned Unity NPCs; runtime binding is performed by the game adapter.
/// </summary>
public sealed record NpcCustomerDirectoryEntry(
    string ProvinceId,
    string DisplayName,
    string Role,
    string AvailabilityWindow,
    NpcCustomerProfile Profile);

public static class NpcCustomerDirectory
{
    public const string HarborlineProvinceId = "sime.province.harborline";
    public const string RidgeviewProvinceId = "sime.province.ridgeview";
    public const string LakesideProvinceId = "sime.province.lakeside";

    public const string StandardProductId = "sime.product.batch.standard";
    public const string IndustrialProductId = "sime.product.batch.industrial";
    public const string PremiumProductId = "sime.product.batch.premium";

    private static readonly IReadOnlyList<NpcCustomerDirectoryEntry> Entries =
        Array.AsReadOnly(new[]
        {
            Create(HarborlineProvinceId, "Morgan Reed", "Portside shopkeeper", "Weekdays / morning",
                "sime.npc.harborline.morgan_reed", 1_200, 0.65,
                new[] { StandardProductId }, new[] { IndustrialProductId },
                new[] { PremiumProductId }),

            Create(HarborlineProvinceId, "Casey Ward", "Dock worker", "Daily / early morning",
                "sime.npc.harborline.casey_ward", 850, 0.82,
                new[] { StandardProductId }, new[] { IndustrialProductId },
                new[] { PremiumProductId }),

            Create(HarborlineProvinceId, "Ellis Shaw", "Warehouse supervisor", "Weekdays / afternoon",
                "sime.npc.harborline.ellis_shaw", 3_500, 0.28,
                new[] { IndustrialProductId }, new[] { StandardProductId },
                Array.Empty<string>()),

            Create(RidgeviewProvinceId, "Jordan Pike", "Quarry contractor", "Weekdays / daytime",
                "sime.npc.ridgeview.jordan_pike", 4_000, 0.30,
                new[] { IndustrialProductId, StandardProductId }, new[] { PremiumProductId },
                Array.Empty<string>()),

            Create(RidgeviewProvinceId, "Avery Cole", "Workshop mechanic", "Daily / evening",
                "sime.npc.ridgeview.avery_cole", 2_100, 0.48,
                new[] { IndustrialProductId }, new[] { StandardProductId },
                new[] { PremiumProductId }),

            Create(RidgeviewProvinceId, "Quinn Hayes", "Research technician", "Weekdays / late afternoon",
                "sime.npc.ridgeview.quinn_hayes", 5_200, 0.22,
                new[] { PremiumProductId }, new[] { IndustrialProductId },
                new[] { StandardProductId }),

            Create(LakesideProvinceId, "Taylor Brooks", "Resort investor", "Daily / afternoon",
                "sime.npc.lakeside.taylor_brooks", 9_000, 0.15,
                new[] { PremiumProductId }, new[] { IndustrialProductId },
                new[] { StandardProductId }),

            Create(LakesideProvinceId, "Riley Morgan", "Marina operator", "Daily / morning",
                "sime.npc.lakeside.riley_morgan", 2_800, 0.42,
                new[] { StandardProductId }, new[] { PremiumProductId },
                new[] { IndustrialProductId }),

            Create(LakesideProvinceId, "Skylar James", "Restaurant owner", "Weekdays / evening",
                "sime.npc.lakeside.skylar_james", 1_650, 0.72,
                new[] { PremiumProductId }, new[] { StandardProductId },
                new[] { IndustrialProductId })
        });

    private static readonly IReadOnlyDictionary<string, NpcCustomerDirectoryEntry> ById =
        Entries.ToDictionary(entry => entry.Profile.NpcId, StringComparer.Ordinal);

    public static IReadOnlyList<NpcCustomerDirectoryEntry> All => Entries;

    public static NpcCustomerDirectoryEntry? FindById(string? npcId) =>
        !string.IsNullOrWhiteSpace(npcId) && ById.TryGetValue(npcId, out NpcCustomerDirectoryEntry? entry)
            ? entry
            : null;

    public static IReadOnlyList<NpcCustomerDirectoryEntry> ForProvince(string? provinceId)
    {
        if (string.IsNullOrWhiteSpace(provinceId))
            return Array.Empty<NpcCustomerDirectoryEntry>();

        NpcCustomerDirectoryEntry[] matches = Entries
            .Where(entry => string.Equals(entry.ProvinceId, provinceId, StringComparison.Ordinal))
            .ToArray();

        return Array.AsReadOnly(matches);
    }

    private static NpcCustomerDirectoryEntry Create(
        string provinceId,
        string displayName,
        string role,
        string availabilityWindow,
        string npcId,
        long budget,
        double priceSensitivity,
        string[] preferred,
        string[] neutral,
        string[] rejected)
    {
        if (!StableId.HasPrefix(provinceId, StableId.ProvincePrefix))
            throw new InvalidOperationException("Customer catalogue contains an invalid province ID.");

        var profile = new NpcCustomerProfile(
            npcId,
            budget,
            priceSensitivity,
            Array.AsReadOnly((string[])preferred.Clone()),
            Array.AsReadOnly((string[])neutral.Clone()),
            Array.AsReadOnly((string[])rejected.Clone()));

        return new NpcCustomerDirectoryEntry(
            provinceId,
            displayName,
            role,
            availabilityWindow,
            profile);
    }
}