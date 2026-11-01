using ScheduleIMegaExpansion.Core;
using Xunit;

namespace ScheduleIMegaExpansion.Tests;

public sealed class NpcOrderSettlementServiceTests
{
    private const string OrderId = "sime.order.order-001";
    private const string ShopId = "sime.business.harborline.portside_supply";
    private const string EscrowId = "sime.escrow.order-001";
    private const string BuyerId = "sime.customer_budget.sime.npc.harborline.morgan_reed.player-a.42";
    private const string ProductId = "morgan-approved-crate";
    private static readonly string OperationId =
        StableId.FromOrderRequest("settlement:" + OrderId + ":completed");

    private static ShopPurchaseService CreateEconomy(int stock = 0)
    {
        var service = new ShopPurchaseService();
        service.SetBalance(EscrowId, 500);
        service.SetProduct(ShopId, ProductId, unitPrice: 500, stock);
        return service;
    }

    private static NpcOrderEscrowTerms CreateOrder() => new(
        OrderId,
        EscrowId,
        BuyerId,
        ShopId,
        500,
        new[] { new ShopRefundLine(ProductId, 1) });

    [Fact]
    public void CompletedOrderSettlesEscrowToShopRevenueOnlyOnce()
    {
        ShopPurchaseService economy = CreateEconomy();
        NpcOrderSettlementResult first = NpcOrderSettlementService.Finalize(
            economy, CreateOrder(), OrderStatus.Delivered, OrderStatus.Completed, OperationId);

        Assert.True(first.Success);
        Assert.NotNull(first.LedgerResult);
        Assert.Equal(500, economy.GetBalance("sime.business_revenue." + ShopId));
        Assert.Equal(0, economy.GetBalance(EscrowId));
        Assert.Equal(0, economy.GetStock(ShopId, ProductId));

        NpcOrderSettlementResult replay = NpcOrderSettlementService.Finalize(
            economy, CreateOrder(), OrderStatus.Delivered, OrderStatus.Completed, OperationId);

        Assert.True(replay.Success);
        Assert.True(replay.LedgerResult!.IsReplay);
        Assert.Equal(500, economy.GetBalance("sime.business_revenue." + ShopId));
        Assert.Equal(0, economy.GetBalance(EscrowId));
        Assert.Equal(1, economy.WorldRevision);
    }

    [Fact]
    public void CancelledOrderRefundsCustomerAndRestoresStockOnlyOnce()
    {
        ShopPurchaseService economy = CreateEconomy();

        NpcOrderSettlementResult first = NpcOrderSettlementService.Finalize(
            economy, CreateOrder(), OrderStatus.Offered, OrderStatus.Cancelled,
            StableId.FromOrderRequest("settlement:" + OrderId + ":cancelled"));

        Assert.True(first.Success);
        Assert.Equal(500, economy.GetBalance(BuyerId));
        Assert.Equal(0, economy.GetBalance(EscrowId));
        Assert.Equal(1, economy.GetStock(ShopId, ProductId));

        NpcOrderSettlementResult replay = NpcOrderSettlementService.Finalize(
            economy, CreateOrder(), OrderStatus.Offered, OrderStatus.Cancelled,
            StableId.FromOrderRequest("settlement:" + OrderId + ":cancelled"));

        Assert.True(replay.Success);
        Assert.True(replay.LedgerResult!.IsReplay);
        Assert.Equal(500, economy.GetBalance(BuyerId));
        Assert.Equal(1, economy.GetStock(ShopId, ProductId));
        Assert.Equal(1, economy.WorldRevision);
    }

    [Fact]
    public void InvalidLifecycleTransitionDoesNotTouchEscrowOrStock()
    {
        ShopPurchaseService economy = CreateEconomy();
        NpcOrderSettlementResult result = NpcOrderSettlementService.Finalize(
            economy, CreateOrder(), OrderStatus.Offered, OrderStatus.Completed, OperationId);

        Assert.False(result.Success);
        Assert.Equal(NpcOrderSettlementFailure.InvalidTransition, result.Failure);
        Assert.Equal(500, economy.GetBalance(EscrowId));
        Assert.Equal(0, economy.GetBalance("sime.business_revenue." + ShopId));
        Assert.Equal(0, economy.WorldRevision);
    }

    [Fact]
    public void RefundCannotPartiallyMutateWhenStockCannotBeRestored()
    {
        ShopPurchaseService economy = new();
        economy.SetBalance(EscrowId, 500);
        economy.SetProduct(ShopId, "different-product", 500, 1);
        string before = System.Text.Json.JsonSerializer.Serialize(economy.CreateSnapshot());

        NpcOrderSettlementResult result = NpcOrderSettlementService.Finalize(
            economy, CreateOrder(), OrderStatus.Offered, OrderStatus.Expired,
            StableId.FromOrderRequest("settlement:" + OrderId + ":expired"));

        Assert.False(result.Success);
        Assert.Equal(NpcOrderSettlementFailure.LedgerRejected, result.Failure);
        Assert.Equal(ShopEscrowOperationOutcome.UnknownRestockProduct, result.LedgerResult!.Outcome);
        Assert.Equal(500, economy.GetBalance(EscrowId));
        Assert.Equal(0, economy.GetBalance(BuyerId));

        string after = System.Text.Json.JsonSerializer.Serialize(economy.CreateSnapshot());
        Assert.NotEqual(before, after); // the failed operation receipt is persisted for idempotent retry.
        var restored = new ShopPurchaseService();
        Assert.True(restored.TryRestoreSnapshot(economy.CreateSnapshot()));
    }

    [Fact]
    public void ReusingSettlementOperationIdForDifferentOrderIsRejected()
    {
        ShopPurchaseService economy = CreateEconomy();
        ShopEscrowOperation first = new(
            "settlement-operation", OrderId, EscrowId, "business-a", 500,
            ShopEscrowOperationKind.Settle);
        ShopEscrowOperation conflicting = new(
            "settlement-operation", "sime.order.order-002", EscrowId, "business-b", 500,
            ShopEscrowOperationKind.Settle);

        Assert.True(economy.ProcessEscrowOperation(first).Succeeded);
        Assert.Equal(
            ShopEscrowOperationOutcome.OperationIdConflict,
            economy.ProcessEscrowOperation(conflicting).Outcome);
        Assert.Equal(500, economy.GetBalance("business-a"));
        Assert.Equal(0, economy.GetBalance("business-b"));
    }
}