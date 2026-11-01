using ScheduleIMegaExpansion.Core;
using Xunit;

namespace ScheduleIMegaExpansion.Tests;

public sealed class NpcCustomerAcceptanceTests
{
    private const string Standard = "sime.product.batch.standard";
    private const string Industrial = "sime.product.batch.industrial";
    private const string Premium = "sime.product.batch.premium";

    private static readonly NpcCustomerProfile Morgan = new(
        "sime.npc.harborline.morgan_reed",
        BudgetLimit: 1_200,
        PriceSensitivity: 0.65,
        PreferredProductIds: new[] { Standard },
        NeutralProductIds: new[] { Industrial },
        RejectedProductIds: new[] { Premium });

    private static readonly NpcCustomerProfile Jordan = new(
        "sime.npc.ridgeview.jordan_pike",
        BudgetLimit: 4_000,
        PriceSensitivity: 0.30,
        PreferredProductIds: new[] { Industrial, Standard },
        NeutralProductIds: new[] { Premium },
        RejectedProductIds: Array.Empty<string>());

    private static readonly NpcCustomerProfile Taylor = new(
        "sime.npc.lakeside.taylor_brooks",
        BudgetLimit: 9_000,
        PriceSensitivity: 0.15,
        PreferredProductIds: new[] { Premium },
        NeutralProductIds: new[] { Industrial },
        RejectedProductIds: new[] { Standard });

    [Fact]
    public void DifferentResidentsReactDifferentlyToTheSameProduct()
    {
        var standardOffer = new CustomerOrderOffer(new[]
        {
            new CustomerOrderLine(Standard, Quantity: 1, UnitPrice: 600)
        });

        CustomerAcceptanceResult morgan = NpcCustomerAcceptance.Evaluate(Morgan, standardOffer, expansionStanding: 0);
        CustomerAcceptanceResult jordan = NpcCustomerAcceptance.Evaluate(Jordan, standardOffer, expansionStanding: 0);
        CustomerAcceptanceResult taylor = NpcCustomerAcceptance.Evaluate(Taylor, standardOffer, expansionStanding: 0);

        Assert.True(morgan.Accepted);
        Assert.True(jordan.Accepted);
        Assert.False(taylor.Accepted);
        Assert.Equal(CustomerAcceptanceReason.RejectedProduct, taylor.Reason);
        Assert.Equal(Standard, taylor.RejectedProductId);
    }

    [Fact]
    public void PremiumCustomerPrefersPremiumProductWhileBudgetAndPriceStillMatter()
    {
        var premiumOffer = new CustomerOrderOffer(new[]
        {
            new CustomerOrderLine(Premium, Quantity: 1, UnitPrice: 1_800)
        });

        CustomerAcceptanceResult morgan = NpcCustomerAcceptance.Evaluate(Morgan, premiumOffer, 0);
        CustomerAcceptanceResult taylor = NpcCustomerAcceptance.Evaluate(Taylor, premiumOffer, 0);

        Assert.False(morgan.Accepted);
        Assert.Equal(CustomerAcceptanceReason.RejectedProduct, morgan.Reason);
        Assert.True(taylor.Accepted);
        Assert.Equal(1, taylor.PreferredProductCount);
        Assert.True(taylor.Score > morgan.Score);
    }

    [Fact]
    public void OfferAboveIndividualBudgetIsRejected()
    {
        var offer = new CustomerOrderOffer(new[]
        {
            new CustomerOrderLine(Standard, Quantity: 2, UnitPrice: 700)
        });

        CustomerAcceptanceResult result = NpcCustomerAcceptance.Evaluate(Morgan, offer, 100);

        Assert.False(result.Accepted);
        Assert.Equal(CustomerAcceptanceReason.OverBudget, result.Reason);
        Assert.Equal(1_400, result.TotalPrice);
    }

    [Fact]
    public void PriceSensitivityChangesDecisionForSameNeutralOffer()
    {
        var offer = new CustomerOrderOffer(new[]
        {
            new CustomerOrderLine(Industrial, Quantity: 1, UnitPrice: 900)
        });

        var carefulCustomer = Morgan with { PriceSensitivity = 0.95 };
        var relaxedCustomer = Morgan with { PriceSensitivity = 0.10 };

        CustomerAcceptanceResult careful = NpcCustomerAcceptance.Evaluate(carefulCustomer, offer, 0);
        CustomerAcceptanceResult relaxed = NpcCustomerAcceptance.Evaluate(relaxedCustomer, offer, 0);

        Assert.False(careful.Accepted);
        Assert.Equal(CustomerAcceptanceReason.LowPreference, careful.Reason);
        Assert.True(relaxed.Accepted);
        Assert.True(relaxed.Score > careful.Score);
    }

    [Fact]
    public void ExpansionStandingModifiesOnlineOfferScoreWithoutChangingProductProfile()
    {
        var offer = new CustomerOrderOffer(new[]
        {
            new CustomerOrderLine(Industrial, Quantity: 1, UnitPrice: 1_000)
        });

        CustomerAcceptanceResult lowExpansionStanding = NpcCustomerAcceptance.Evaluate(Morgan, offer, -100);
        CustomerAcceptanceResult highExpansionStanding = NpcCustomerAcceptance.Evaluate(Morgan, offer, 100);

        Assert.True(highExpansionStanding.Score > lowExpansionStanding.Score);
        Assert.Equal(Industrial, Morgan.NeutralProductIds.Single());
    }

    [Fact]
    public void InvalidProfileAndMalformedOfferFailClosed()
    {
        var invalidProfile = Morgan with { PriceSensitivity = double.NaN };
        var emptyOffer = new CustomerOrderOffer(Array.Empty<CustomerOrderLine>());

        Assert.Equal(
            CustomerAcceptanceReason.InvalidProfile,
            NpcCustomerAcceptance.Evaluate(invalidProfile, emptyOffer, 0).Reason);
        Assert.Equal(
            CustomerAcceptanceReason.InvalidOffer,
            NpcCustomerAcceptance.Evaluate(Morgan, emptyOffer, 0).Reason);
    }

    [Fact]
    public void PriceOverflowDoesNotWrapIntoCheapOffer()
    {
        var offer = new CustomerOrderOffer(new[]
        {
            new CustomerOrderLine(Standard, Quantity: 2, UnitPrice: long.MaxValue)
        });

        CustomerAcceptanceResult result = NpcCustomerAcceptance.Evaluate(Morgan, offer, 0);

        Assert.False(result.Accepted);
        Assert.Equal(CustomerAcceptanceReason.PriceOverflow, result.Reason);
    }

    [Fact]
    public void ContradictoryProductPreferencesAreRejectedAsInvalidProfile()
    {
        var invalidProfile = Morgan with
        {
            PreferredProductIds = new[] { Standard },
            RejectedProductIds = new[] { Standard }
        };
        var offer = new CustomerOrderOffer(new[]
        {
            new CustomerOrderLine(Standard, Quantity: 1, UnitPrice: 200)
        });

        Assert.Equal(
            CustomerAcceptanceReason.InvalidProfile,
            NpcCustomerAcceptance.Evaluate(invalidProfile, offer, 0).Reason);
    }
}
