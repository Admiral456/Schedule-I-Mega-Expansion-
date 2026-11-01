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
    public void ReusingRequestIdWithDifferentPayloadIsRejected()
    {
        var service = new ShopPurchaseService();
        service.SetBalance("player-a", 1_000);
        service.SetProduct("portside", "crate", 100, 5);
        service.SetProduct("portside", "box", 50, 5);

        Assert.True(service.Purchase(new ShopPurchaseRequest("same-id", "player-a", "portside", "crate", 1)).Succeeded);
        ShopPurchaseResult conflict = service.Purchase(new ShopPurchaseRequest("same-id", "player-a", "portside", "box", 1));

        Assert.Equal(ShopPurchaseOutcome.RequestIdConflict, conflict.Outcome);
        Assert.Equal(900, service.GetBalance("player-a"));
        Assert.Equal(5, service.GetStock("portside", "box"));
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

        ShopPurchaseResult result = service.Purchase(new ShopPurchaseRequest("req-poor", "player-a", "portside", "crate", 1));

        Assert.Equal(ShopPurchaseOutcome.InsufficientFunds, result.Outcome);
        Assert.Equal(50, service.GetBalance("player-a"));
        Assert.Equal(1, service.GetStock("portside", "crate"));
        Assert.Equal(0, service.WorldRevision);
    }

    [Fact]
    public void FailedPurchaseReplayRemainsFailedAfterBalanceChangesAndReload()
    {
        var source = new ShopPurchaseService();
        source.SetBalance("player-a", 10);
        source.SetProduct("portside", "crate", 100, 2);
        var request = new ShopPurchaseRequest("failed-request-persisted", "player-a", "portside", "crate", 1);

        ShopPurchaseResult first = source.Purchase(request);
        Assert.Equal(ShopPurchaseOutcome.InsufficientFunds, first.Outcome);

        string json = System.Text.Json.JsonSerializer.Serialize(source.CreateSnapshot());
        ShopPurchaseServiceSnapshot? snapshot =
            System.Text.Json.JsonSerializer.Deserialize<ShopPurchaseServiceSnapshot>(json);
        Assert.NotNull(snapshot);

        var restored = new ShopPurchaseService();
        Assert.True(restored.TryRestoreSnapshot(snapshot));
        restored.SetBalance("player-a", 1_000);

        ShopPurchaseResult replay = restored.Purchase(request);

        Assert.Equal(ShopPurchaseOutcome.InsufficientFunds, replay.Outcome);
        Assert.True(replay.IsReplay);
        Assert.Equal(1_000, restored.GetBalance("player-a"));
        Assert.Equal(2, restored.GetStock("portside", "crate"));
        Assert.Equal(0, restored.WorldRevision);
    }

    [Fact]
    public void OrderStateMachineRejectsInvalidTransitions()
    {
        Assert.True(OrderState.CanTransition(OrderStatus.Offered, OrderStatus.Accepted));
        Assert.True(OrderState.CanTransition(OrderStatus.Ready, OrderStatus.Delivered));
        Assert.True(OrderState.CanTransition(OrderStatus.Accepted, OrderStatus.Expired));
        Assert.True(OrderState.CanTransition(OrderStatus.Preparing, OrderStatus.Expired));
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
