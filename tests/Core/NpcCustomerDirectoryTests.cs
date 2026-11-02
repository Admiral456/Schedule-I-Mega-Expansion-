using ScheduleIMegaExpansion.Core;
using Xunit;

namespace ScheduleIMegaExpansion.Tests;

public sealed class NpcCustomerDirectoryTests
{
    [Fact]
    public void CatalogueContainsThreeDistinctResidentsPerExpansionProvince()
    {
        Assert.Equal(9, NpcCustomerDirectory.All.Count);
        Assert.Equal(3, NpcCustomerDirectory.ForProvince(NpcCustomerDirectory.HarborlineProvinceId).Count);
        Assert.Equal(3, NpcCustomerDirectory.ForProvince(NpcCustomerDirectory.RidgeviewProvinceId).Count);
        Assert.Equal(3, NpcCustomerDirectory.ForProvince(NpcCustomerDirectory.LakesideProvinceId).Count);
    }

    [Fact]
    public void EveryResidentHasUniqueStableIdentityAndValidPreferences()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (NpcCustomerDirectoryEntry resident in NpcCustomerDirectory.All)
        {
            Assert.True(StableId.HasPrefix(resident.Profile.NpcId, StableId.NpcPrefix));
            Assert.True(StableId.HasPrefix(resident.ProvinceId, StableId.ProvincePrefix));
            Assert.True(ids.Add(resident.Profile.NpcId));
            Assert.False(string.IsNullOrWhiteSpace(resident.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(resident.Role));
            Assert.True(resident.Profile.BudgetLimit > 0);
            Assert.InRange(resident.Profile.PriceSensitivity, 0, 1);

            var preferred = new HashSet<string>(resident.Profile.PreferredProductIds, StringComparer.Ordinal);
            var neutral = new HashSet<string>(resident.Profile.NeutralProductIds, StringComparer.Ordinal);
            var rejected = new HashSet<string>(resident.Profile.RejectedProductIds, StringComparer.Ordinal);

            Assert.False(preferred.Overlaps(neutral));
            Assert.False(preferred.Overlaps(rejected));
            Assert.False(neutral.Overlaps(rejected));
        }
    }

    [Fact]
    public void ResidentLookupIsStableAndUnknownResidentsAreNotInvented()
    {
        NpcCustomerDirectoryEntry expected = NpcCustomerDirectory.All[0];

        Assert.Same(expected, NpcCustomerDirectory.FindById(expected.Profile.NpcId));
        Assert.Null(NpcCustomerDirectory.FindById("sime.npc.unknown.resident"));
        Assert.Empty(NpcCustomerDirectory.ForProvince("sime.province.not_real"));
        Assert.Empty(NpcCustomerDirectory.ForProvince(null));
    }

    [Fact]
    public void SameOfferCanProduceDifferentOutcomesForDifferentResidents()
    {
        var offer = new CustomerOrderOffer(new[]
        {
            new CustomerOrderLine(NpcCustomerDirectory.StandardProductId, 1, 600)
        });

        CustomerAcceptanceResult dockWorker = NpcCustomerAcceptance.Evaluate(
            NpcCustomerDirectory.FindById("sime.npc.harborline.casey_ward")!.Profile,
            offer,
            expansionStanding: 0);

        CustomerAcceptanceResult resortInvestor = NpcCustomerAcceptance.Evaluate(
            NpcCustomerDirectory.FindById("sime.npc.lakeside.taylor_brooks")!.Profile,
            offer,
            expansionStanding: 0);

        Assert.True(dockWorker.Accepted);
        Assert.False(resortInvestor.Accepted);
        Assert.Equal(CustomerAcceptanceReason.RejectedProduct, resortInvestor.Reason);
    }

    [Fact]
    public void CataloguePreferenceCollectionsCannotBeMutatedThroughArrayCasts()
    {
        NpcCustomerProfile profile = NpcCustomerDirectory.All[0].Profile;

        Assert.IsNotType<string[]>(profile.PreferredProductIds);
        Assert.IsNotType<string[]>(profile.NeutralProductIds);
        Assert.IsNotType<string[]>(profile.RejectedProductIds);
    }
}