using ScheduleIMegaExpansion.Core;
using Xunit;

namespace ScheduleIMegaExpansion.Tests;

public sealed class NpcOrderOfferGeneratorTests
{
    private static readonly NpcCustomerProfile Morgan = NpcCustomerDirectory.FindById("sime.npc.harborline.morgan_reed")!.Profile;

    private static readonly NpcOrderProductStock[] Stock =
    {
        new(NpcCustomerDirectory.StandardProductId, 600, 4),
        new(NpcCustomerDirectory.IndustrialProductId, 200, 5),
        new(NpcCustomerDirectory.PremiumProductId, 100, 20)
    };

    [Fact]
    public void GeneratedOrderOnlyContainsPreferredOrNeutralProductsAndIsAccepted()
    {
        NpcOrderGenerationResult result = NpcOrderOfferGenerator.Generate(Morgan, Stock, worldInterval: 12, playerReputation: 0);

        Assert.True(result.Success);
        Assert.Equal(NpcOrderGenerationFailure.None, result.Failure);
        Assert.NotNull(result.Offer);
        Assert.NotNull(result.Evaluation);
        Assert.True(result.Evaluation!.Accepted);
        Assert.InRange(result.Offer!.Lines.Count, 1, NpcOrderOfferGenerator.DefaultMaximumLines);
        Assert.True(result.TotalPrice <= Morgan.BudgetLimit);
        Assert.DoesNotContain(result.Offer.Lines, line => Morgan.RejectedProductIds.Contains(line.ProductId));
        Assert.All(result.Offer.Lines, line => Assert.True(Morgan.PreferredProductIds.Contains(line.ProductId) || Morgan.NeutralProductIds.Contains(line.ProductId)));
        Assert.All(result.Offer.Lines, line => Assert.True(line.Quantity <= Stock.Single(item => item.ProductId == line.ProductId).AvailableStock));
    }

    [Fact]
    public void SameProfileStockIntervalAndReputationProduceSameOffer()
    {
        NpcOrderGenerationResult first = NpcOrderOfferGenerator.Generate(Morgan, Stock, 55, 10);
        NpcOrderGenerationResult second = NpcOrderOfferGenerator.Generate(Morgan, Stock, 55, 10);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(first.Offer!.Lines, second.Offer!.Lines);
        Assert.Equal(first.Evaluation!.Score, second.Evaluation!.Score);
    }

    [Fact]
    public void RejectedProductsAndUnknownProductsAreNeverOffered()
    {
        var candidates = new[]
        {
            new NpcOrderProductStock(NpcCustomerDirectory.PremiumProductId, 1, 50),
            new NpcOrderProductStock("sime.product.unknown.item", 1, 50)
        };

        NpcOrderGenerationResult result = NpcOrderOfferGenerator.Generate(Morgan, candidates, 0, 0);

        Assert.False(result.Success);
        Assert.Equal(NpcOrderGenerationFailure.NoEligibleProduct, result.Failure);
    }

    [Fact]
    public void EmptyStockAndUnaffordableItemsDoNotCreateOrders()
    {
        var candidates = new[]
        {
            new NpcOrderProductStock(NpcCustomerDirectory.StandardProductId, 600, 0),
            new NpcOrderProductStock(NpcCustomerDirectory.IndustrialProductId, 5_000, 1)
        };

        NpcOrderGenerationResult result = NpcOrderOfferGenerator.Generate(Morgan, candidates, 0, 0);

        Assert.False(result.Success);
        Assert.Equal(NpcOrderGenerationFailure.NoEligibleProduct, result.Failure);
    }

    [Fact]
    public void LowReputationAndExpensiveOffersCanPreventOrderGeneration()
    {
        var candidates = new[]
        {
            new NpcOrderProductStock(NpcCustomerDirectory.StandardProductId, 1_000, 1)
        };

        NpcOrderGenerationResult result = NpcOrderOfferGenerator.Generate(Morgan, candidates, 1, playerReputation: -100);

        Assert.False(result.Success);
        Assert.Equal(NpcOrderGenerationFailure.NoAcceptedOffer, result.Failure);
    }

    [Fact]
    public void InvalidArgumentsFailClosed()
    {
        Assert.Equal(NpcOrderGenerationFailure.InvalidArguments,
            NpcOrderOfferGenerator.Generate(Morgan, null, 0, 0).Failure);
        Assert.Equal(NpcOrderGenerationFailure.InvalidArguments,
            NpcOrderOfferGenerator.Generate(Morgan, Stock, -1, 0).Failure);
        Assert.Equal(NpcOrderGenerationFailure.InvalidArguments,
            NpcOrderOfferGenerator.Generate(Morgan, Stock, 0, 0, maximumLines: 6).Failure);
        Assert.Equal(NpcOrderGenerationFailure.InvalidProfile,
            NpcOrderOfferGenerator.Generate(null, Stock, 0, 0).Failure);
    }
}