using ScheduleIMegaExpansion.Core;
using Xunit;

namespace ScheduleIMegaExpansion.Tests;

public sealed class DomainServicesTests
{
    [Fact]
    public void DuplicatePurchaseRequestDoesNotChargeOrDecrementStockTwice()
    {
        var service = new ShopPurchaseService();
        service.SetBalance("player-a", 1_000);
        service.SetProduct("portside", "crate", unitPrice: 100, stock: 2);

        var request = new ShopPurchaseRequest("req-001", "player-a", "portside", "crate", 1);

        ShopPurchaseResult first = service.Purchase(request);
        ShopPurchaseResult replay = service.Purchase(request);

        Assert.True(first.Succeeded);
        Assert.True(replay.Succeeded);
        Assert.True(replay.IsReplay);
        Assert.Equal(900, service.GetBalance("player-a"));
        Assert.Equal(1, service.GetStock("portside", "crate"));
        Assert.Equal(1, service.WorldRevision);
    }

    [Fact]
    public async Task CompetingBuyersCannotPurchaseTheLastItemTwice()
    {
        var service = new ShopPurchaseService();
        service.SetBalance("player-a", 1_000);
        service.SetBalance("player-b", 1_000);
        service.SetProduct("portside", "last-crate", unitPrice: 100, stock: 1);

        ShopPurchaseResult[] results = await Task.WhenAll(
            Task.Run(() => service.Purchase(new ShopPurchaseRequest("req-a", "player-a", "portside", "last-crate", 1))),
            Task.Run(() => service.Purchase(new ShopPurchaseRequest("req-b", "player-b", "portside", "last-crate", 1))));

        Assert.Single(results.Where(result => result.Succeeded));
        Assert.Equal(0, service.GetStock("portside", "last-crate"));
        Assert.Equal(1, service.WorldRevision);
        Assert.Equal(1_900, service.GetBalance("player-a") + service.GetBalance("player-b"));
    }

    [Fact]
    public void FailedPurchaseDoesNotMutateMoneyOrStock()
    {
        var service = new ShopPurchaseService();
        service.SetBalance("player-a", 50);
        service.SetProduct("portside", "crate", unitPrice: 100, stock: 1);

        ShopPurchaseResult result = service.Purchase(
            new ShopPurchaseRequest("req-poor", "player-a", "portside", "crate", 1));

        Assert.Equal(ShopPurchaseOutcome.InsufficientFunds, result.Outcome);
        Assert.Equal(50, service.GetBalance("player-a"));
        Assert.Equal(1, service.GetStock("portside", "crate"));
        Assert.Equal(0, service.WorldRevision);
    }

    [Fact]
    public void OrderStateMachineRejectsInvalidTransitions()
    {
        Assert.True(OrderState.CanTransition(OrderStatus.Offered, OrderStatus.Accepted));
        Assert.True(OrderState.CanTransition(OrderStatus.Ready, OrderStatus.Delivered));
        Assert.True(OrderState.CanTransition(OrderStatus.Delivered, OrderStatus.Completed));
        Assert.False(OrderState.CanTransition(OrderStatus.Offered, OrderStatus.Completed));
        Assert.False(OrderState.CanTransition(OrderStatus.Completed, OrderStatus.Accepted));
    }

    [Fact]
    public void ReputationEventsAreIdempotentAndClamped()
    {
        var ledger = new ReputationLedger();

        Assert.Equal(95, ledger.Apply("event-1", "npc:morgan:crew", 95));
        Assert.Equal(95, ledger.Apply("event-1", "npc:morgan:crew", 20));
        Assert.Equal(100, ledger.Apply("event-2", "npc:morgan:crew", 20));
        Assert.Equal(-100, ledger.Apply("event-3", "npc:morgan:player-a", -500));
        Assert.Equal(100, ledger.Get("npc:morgan:crew"));
    }

    [Fact]
    public void StableOrderIdsUseNamespacedPrefix()
    {
        string id = StableId.NewOrderId();

        Assert.True(StableId.HasPrefix(id, StableId.OrderPrefix));
        Assert.False(StableId.HasPrefix("some-order", StableId.OrderPrefix));
    }
}
