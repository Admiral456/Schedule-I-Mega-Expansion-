namespace ScheduleIMegaExpansion.Core;

public sealed record NpcOrderEscrowTerms(
    string OrderId,
    string EscrowAccountId,
    string BuyerAccountId,
    string ShopId,
    long TotalPrice,
    IReadOnlyList<ShopRefundLine> Items);

public enum NpcOrderSettlementFailure
{
    None,
    InvalidOrder,
    InvalidOperationId,
    InvalidTargetStatus,
    InvalidTransition,
    LedgerRejected
}

public sealed record NpcOrderSettlementResult(
    bool Success,
    NpcOrderSettlementFailure Failure,
    OrderStatus CurrentStatus,
    OrderStatus TargetStatus,
    ShopEscrowOperationResult? LedgerResult)
{
    public static NpcOrderSettlementResult Failed(
        NpcOrderSettlementFailure failure,
        OrderStatus current,
        OrderStatus target,
        ShopEscrowOperationResult? ledger = null)
        => new(false, failure, current, target, ledger);
}

/// <summary>
/// Finalizes a persisted NPC order against the expansion ledger.
/// Completed orders transfer the held escrow to shop revenue. Cancelled, expired
/// and failed orders refund the customer budget and restore the exact reserved stock.
/// This does not pay vanilla Schedule I cash; that requires a game-specific adapter.
/// </summary>
public static class NpcOrderSettlementService
{
    public static NpcOrderSettlementResult Finalize(
        ShopPurchaseService? economy,
        NpcOrderEscrowTerms? order,
        OrderStatus currentStatus,
        OrderStatus targetStatus,
        string? operationId)
    {
        if (economy is null ||
            order is null ||
            !StableId.HasPrefix(order.OrderId, StableId.OrderPrefix) ||
            !IsValidAccount(order.EscrowAccountId) ||
            !IsValidAccount(order.BuyerAccountId) ||
            !StableId.HasPrefix(order.ShopId, StableId.BusinessPrefix) ||
            order.TotalPrice <= 0 ||
            order.Items is null ||
            order.Items.Count == 0)
        {
            return NpcOrderSettlementResult.Failed(
                NpcOrderSettlementFailure.InvalidOrder, currentStatus, targetStatus);
        }

        if (string.IsNullOrWhiteSpace(operationId) ||
            operationId.Length > 160 ||
            operationId.Any(character => character > 127 || char.IsControl(character)))
        {
            return NpcOrderSettlementResult.Failed(
                NpcOrderSettlementFailure.InvalidOperationId, currentStatus, targetStatus);
        }

        bool settles = targetStatus == OrderStatus.Completed;
        bool refunds = targetStatus is OrderStatus.Cancelled or OrderStatus.Expired or OrderStatus.Failed;
        if (!settles && !refunds)
        {
            return NpcOrderSettlementResult.Failed(
                NpcOrderSettlementFailure.InvalidTargetStatus, currentStatus, targetStatus);
        }

        if (!OrderState.CanTransition(currentStatus, targetStatus))
        {
            return NpcOrderSettlementResult.Failed(
                NpcOrderSettlementFailure.InvalidTransition, currentStatus, targetStatus);
        }

        long expectedTotal = 0;
        var uniqueProducts = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (ShopRefundLine line in order.Items)
            {
                if (line is null ||
                    string.IsNullOrWhiteSpace(line.ProductId) ||
                    line.Quantity <= 0 ||
                    !uniqueProducts.Add(line.ProductId))
                {
                    return NpcOrderSettlementResult.Failed(
                        NpcOrderSettlementFailure.InvalidOrder, currentStatus, targetStatus);
                }

                // The order ledger stores unit price on its order line. The refund service
                // uses quantities only; total price is verified by the save adapter.
            }
        }
        catch (OverflowException)
        {
            return NpcOrderSettlementResult.Failed(
                NpcOrderSettlementFailure.InvalidOrder, currentStatus, targetStatus);
        }

        if (settles)
        {
            string revenueAccount = "sime.business_revenue." + order.ShopId;
            var operation = new ShopEscrowOperation(
                operationId,
                order.OrderId,
                order.EscrowAccountId,
                revenueAccount,
                order.TotalPrice,
                ShopEscrowOperationKind.Settle);

            ShopEscrowOperationResult ledger = economy.ProcessEscrowOperation(operation);
            return ledger.Succeeded
                ? new NpcOrderSettlementResult(true, NpcOrderSettlementFailure.None, currentStatus, targetStatus, ledger)
                : NpcOrderSettlementResult.Failed(
                    NpcOrderSettlementFailure.LedgerRejected, currentStatus, targetStatus, ledger);
        }

        var refund = new ShopEscrowOperation(
            operationId,
            order.OrderId,
            order.EscrowAccountId,
            order.BuyerAccountId,
            order.TotalPrice,
            ShopEscrowOperationKind.Refund,
            order.ShopId,
            Array.AsReadOnly(order.Items.Select(line =>
                new ShopRefundLine(line.ProductId, line.Quantity)).ToArray()));

        ShopEscrowOperationResult refundLedger = economy.ProcessEscrowOperation(refund);
        return refundLedger.Succeeded
            ? new NpcOrderSettlementResult(true, NpcOrderSettlementFailure.None, currentStatus, targetStatus, refundLedger)
            : NpcOrderSettlementResult.Failed(
                NpcOrderSettlementFailure.LedgerRejected, currentStatus, targetStatus, refundLedger);
    }

    private static bool IsValidAccount(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 160 &&
        !value.Contains('\u001f');
}