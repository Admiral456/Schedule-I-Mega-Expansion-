using ScheduleIMegaExpansion.Core;
using Xunit;

namespace ScheduleIMegaExpansion.Tests;

public sealed class NpcOrderPlacementServiceTests
{
    private const string ShopId = "sime.business.harborline.portside_supply";
    private static readonly NpcCustomerDirectoryEntry Morgan =
        NpcCustomerDirectory.FindById("sime.npc.harborline.morgan_reed")!;

    private static NpcOrderPlacementRequest Request(string id = "request-001") => new(
        id, "steam-player-1|morgan|portside", "76561198000000001", ShopId,
        WorldInterval: 42, CreatedAtUnixSeconds: 1_800_000_000, DeadlineUnixSeconds: 1_800_001_800, ExpansionStanding: 0);

    private static ShopPurchaseService CreateShop()
    {
        var shop = new ShopPurchaseService();
        shop.SetProduct(ShopId, NpcCustomerDirectory.StandardProductId, 600, 4);
        shop.SetProduct(ShopId, NpcCustomerDirectory.IndustrialProductId, 200, 5);
        shop.SetProduct(ShopId, NpcCustomerDirectory.PremiumProductId, 100, 20);
        return shop;
    }

    [Fact]
    public void HostCreatesOrderAndReservesCustomerFundsAndShopStock()
    {
        ShopPurchaseService shop = CreateShop();
        int originalStandard = shop.GetStock(ShopId, NpcCustomerDirectory.StandardProductId);
        int originalIndustrial = shop.GetStock(ShopId, NpcCustomerDirectory.IndustrialProductId);

        NpcOrderPlacementResult result = NpcOrderPlacementService.Create(shop, Morgan, Request(), isAuthoritativeHost: true);

        Assert.True(result.Success);
        Assert.NotNull(result.Order);
        Assert.Equal("offered", result.Order!.Status);
        Assert.True(StableId.HasPrefix(result.Order.OrderId, StableId.OrderPrefix));
        Assert.True(result.Order.TotalPrice > 0);
        Assert.True(result.Order.TotalPrice <= Morgan.Profile.BudgetLimit);
        Assert.Equal(result.Order.TotalPrice, shop.GetBalance(result.Order.EscrowAccountId));
        Assert.True(shop.TryGetBalance(result.Order.BuyerAccountId, out long remainingBuyerBudget));
        Assert.Equal(Morgan.Profile.BudgetLimit - result.Order.TotalPrice, remainingBuyerBudget);
        Assert.True(shop.GetStock(ShopId, NpcCustomerDirectory.StandardProductId) <= originalStandard);
        Assert.True(shop.GetStock(ShopId, NpcCustomerDirectory.IndustrialProductId) <= originalIndustrial);
        Assert.All(result.Order.Items, line => Assert.True(line.Quantity > 0 && line.UnitPrice > 0));
    }

    [Fact]
    public void ClientCannotCreateOrderOrMutateEconomy()
    {
        ShopPurchaseService shop = CreateShop();
        string before = System.Text.Json.JsonSerializer.Serialize(shop.CreateSnapshot());

        NpcOrderPlacementResult result = NpcOrderPlacementService.Create(shop, Morgan, Request(), isAuthoritativeHost: false);

        Assert.False(result.Success);
        Assert.Equal(NpcOrderPlacementFailure.NotAuthoritativeHost, result.Failure);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(shop.CreateSnapshot()));
    }

    [Fact]
    public void FailedGenerationRollsBackTemporaryBudgetAndAnyPartialMutation()
    {
        var shop = new ShopPurchaseService();
        shop.SetProduct(ShopId, NpcCustomerDirectory.PremiumProductId, 100, 50); // Morgan rejects it.
        string before = System.Text.Json.JsonSerializer.Serialize(shop.CreateSnapshot());

        NpcOrderPlacementResult result = NpcOrderPlacementService.Create(shop, Morgan, Request(), isAuthoritativeHost: true);

        Assert.False(result.Success);
        Assert.Equal(NpcOrderPlacementFailure.NoEligibleProduct, result.Failure);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(shop.CreateSnapshot()));
    }

    [Fact]
    public void MissingCustomerOrInvalidRequestFailsClosed()
    {
        ShopPurchaseService shop = CreateShop();
        Assert.Equal(NpcOrderPlacementFailure.UnknownCustomer,
            NpcOrderPlacementService.Create(shop, null, Request(), true).Failure);

        NpcOrderPlacementRequest badDeadline = Request() with { DeadlineUnixSeconds = 1 };
        Assert.Equal(NpcOrderPlacementFailure.InvalidRequest,
            NpcOrderPlacementService.Create(shop, Morgan, badDeadline, true).Failure);
    }

    [Fact]
    public void OrderIdIsStableForAHostRequestKey()
    {
        string first = StableId.FromOrderRequest("76561198000000001:request-001");
        string second = StableId.FromOrderRequest("76561198000000001:request-001");
        string another = StableId.FromOrderRequest("76561198000000002:request-001");
        Assert.Equal(first, second);
        Assert.NotEqual(first, another);
    }
    [Fact]
    public void CustomerBudgetAccountsAreIsolatedByPlayerAndWorldInterval()
    {
        string morganPlayerA = NpcOrderPlacementService.CreateBudgetAccountId(
            Morgan.Profile.NpcId, "76561198000000001", 42);
        string morganPlayerB = NpcOrderPlacementService.CreateBudgetAccountId(
            Morgan.Profile.NpcId, "76561198000000002", 42);
        string morganPlayerANextInterval = NpcOrderPlacementService.CreateBudgetAccountId(
            Morgan.Profile.NpcId, "76561198000000001", 43);

        Assert.NotEqual(morganPlayerA, morganPlayerB);
        Assert.NotEqual(morganPlayerA, morganPlayerANextInterval);
    }

}