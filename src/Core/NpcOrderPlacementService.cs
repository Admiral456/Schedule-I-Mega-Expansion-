namespace ScheduleIMegaExpansion.Core;

public sealed record NpcOrderPlacementRequest(
    string RequestId,
    string RequestFingerprint,
    string RequesterId,
    string ShopId,
    long WorldInterval,
    long CreatedAtUnixSeconds,
    long DeadlineUnixSeconds,
    int ExpansionStanding);

public sealed record NpcOrderDraftLine(string ProductId, int Quantity, long UnitPrice);

public sealed record NpcOrderDraft(
    string OrderId,
    string RequestId,
    string RequestFingerprint,
    string RequesterId,
    string CustomerId,
    string ProvinceId,
    string ShopId,
    string BuyerAccountId,
    string EscrowAccountId,
    string Status,
    long WorldInterval,
    long CreatedAtUnixSeconds,
    long DeadlineUnixSeconds,
    long TotalPrice,
    int AcceptanceScore,
    IReadOnlyList<NpcOrderDraftLine> Items);

public enum NpcOrderPlacementFailure
{
    None,
    NotAuthoritativeHost,
    InvalidRequest,
    UnknownCustomer,
    SaveOrShopUnavailable,
    NoEligibleProduct,
    NoAcceptedOffer,
    PurchaseFailed,
    EscrowCreditFailed
}

public sealed record NpcOrderPlacementResult(
    bool Success,
    NpcOrderPlacementFailure Failure,
    NpcOrderDraft? Order,
    bool IsReplay = false)
{
    public static NpcOrderPlacementResult Failed(NpcOrderPlacementFailure failure) =>
        new(false, failure, null);
}

/// <summary>
/// Creates a persisted-ready customer order using the host copy of the shop economy.
/// All line purchases and escrow funding are rolled back together if any step fails.
/// </summary>
public static class NpcOrderPlacementService
{
    public static NpcOrderPlacementResult Create(
        ShopPurchaseService? shop,
        NpcCustomerDirectoryEntry? customer,
        NpcOrderPlacementRequest? request,
        bool isAuthoritativeHost)
    {
        if (!isAuthoritativeHost)
            return NpcOrderPlacementResult.Failed(NpcOrderPlacementFailure.NotAuthoritativeHost);

        if (shop is null)
            return NpcOrderPlacementResult.Failed(NpcOrderPlacementFailure.SaveOrShopUnavailable);

        if (customer is null)
            return NpcOrderPlacementResult.Failed(NpcOrderPlacementFailure.UnknownCustomer);

        if (request is null ||
            !StableId.HasPrefix(customer.ProvinceId, StableId.ProvincePrefix) ||
            !StableId.HasPrefix(customer.Profile.NpcId, StableId.NpcPrefix) ||
            !StableId.HasPrefix(request.ShopId, StableId.BusinessPrefix) ||
            !IsValidRequestId(request.RequestId) ||
            string.IsNullOrWhiteSpace(request.RequestFingerprint) ||
            string.IsNullOrWhiteSpace(request.RequesterId) ||
            request.WorldInterval < 0 ||
            request.CreatedAtUnixSeconds <= 0 ||
            request.DeadlineUnixSeconds <= request.CreatedAtUnixSeconds)
        {
            return NpcOrderPlacementResult.Failed(NpcOrderPlacementFailure.InvalidRequest);
        }

        NpcCustomerProfile profile = customer.Profile;
        string buyerAccount = CreateBudgetAccountId(profile.NpcId, request.RequesterId, request.WorldInterval);
        string orderId = StableId.FromOrderRequest(request.RequestId);
        string escrowAccount = "sime.escrow." + orderId;
        ShopPurchaseServiceSnapshot originalSnapshot = shop.CreateSnapshot();

        if (!shop.TryGetBalance(buyerAccount, out long availableBudget))
        {
            availableBudget = profile.BudgetLimit;
            shop.SetBalance(buyerAccount, availableBudget);
        }

        long orderBudget = Math.Min(profile.BudgetLimit, availableBudget);
        if (orderBudget <= 0)
        {
            shop.TryRestoreSnapshot(originalSnapshot);
            return NpcOrderPlacementResult.Failed(NpcOrderPlacementFailure.NoAcceptedOffer);
        }

        IReadOnlyList<ShopProductSnapshot> shopProducts = shop.GetProductsForShop(request.ShopId);
        NpcOrderProductStock[] stock = shopProducts
            .Select(product => new NpcOrderProductStock(product.ProductId, product.UnitPrice, product.Stock))
            .ToArray();

        NpcOrderGenerationResult generated = NpcOrderOfferGenerator.Generate(
            profile with { BudgetLimit = orderBudget },
            stock,
            request.WorldInterval,
            request.ExpansionStanding);

        if (!generated.Success || generated.Offer is null || generated.Evaluation is null)
        {
            shop.TryRestoreSnapshot(originalSnapshot);
            NpcOrderPlacementFailure failure = generated.Failure switch
            {
                NpcOrderGenerationFailure.NoEligibleProduct => NpcOrderPlacementFailure.NoEligibleProduct,
                NpcOrderGenerationFailure.NoAcceptedOffer => NpcOrderPlacementFailure.NoAcceptedOffer,
                _ => NpcOrderPlacementFailure.InvalidRequest
            };
            return NpcOrderPlacementResult.Failed(failure);
        }

        long verifiedTotal = 0;
        for (int index = 0; index < generated.Offer.Lines.Count; index++)
        {
            CustomerOrderLine line = generated.Offer.Lines.ElementAt(index);
            string lineRequestId = request.RequestId + ":line:" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ShopPurchaseResult purchase = shop.Purchase(new ShopPurchaseRequest(
                lineRequestId, buyerAccount, request.ShopId, line.ProductId, line.Quantity));

            if (!purchase.Succeeded || purchase.TotalPrice != checked(line.UnitPrice * line.Quantity))
            {
                shop.TryRestoreSnapshot(originalSnapshot);
                return NpcOrderPlacementResult.Failed(NpcOrderPlacementFailure.PurchaseFailed);
            }

            try
            {
                verifiedTotal = checked(verifiedTotal + purchase.TotalPrice);
            }
            catch (OverflowException)
            {
                shop.TryRestoreSnapshot(originalSnapshot);
                return NpcOrderPlacementResult.Failed(NpcOrderPlacementFailure.PurchaseFailed);
            }
        }

        if (verifiedTotal != generated.TotalPrice ||
            !shop.TryCreditBalance(escrowAccount, verifiedTotal, out _))
        {
            shop.TryRestoreSnapshot(originalSnapshot);
            return NpcOrderPlacementResult.Failed(NpcOrderPlacementFailure.EscrowCreditFailed);
        }

        var draft = new NpcOrderDraft(
            orderId,
            request.RequestId,
            request.RequestFingerprint,
            request.RequesterId,
            profile.NpcId,
            customer.ProvinceId,
            request.ShopId,
            buyerAccount,
            escrowAccount,
            "offered",
            request.WorldInterval,
            request.CreatedAtUnixSeconds,
            request.DeadlineUnixSeconds,
            verifiedTotal,
            generated.Score,
            Array.AsReadOnly(generated.Offer.Lines
                .Select(line => new NpcOrderDraftLine(line.ProductId, line.Quantity, line.UnitPrice))
                .ToArray()));

        return new NpcOrderPlacementResult(true, NpcOrderPlacementFailure.None, draft);
    }

    public static string CreateBudgetAccountId(string npcId, string requesterId, long worldInterval)
    {
        if (!StableId.HasPrefix(npcId, StableId.NpcPrefix))
            throw new ArgumentException("NPC ID is invalid.", nameof(npcId));
        if (string.IsNullOrWhiteSpace(requesterId) || requesterId.Contains('\u001f'))
            throw new ArgumentException("Requester ID is invalid.", nameof(requesterId));
        if (worldInterval < 0)
            throw new ArgumentOutOfRangeException(nameof(worldInterval));

        return "sime.customer_budget." + npcId + "." + requesterId + "." +
            worldInterval.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static bool IsValidRequestId(string? requestId) =>
        !string.IsNullOrWhiteSpace(requestId) &&
        requestId.Length <= 160 &&
        requestId.All(character => (character <= 127 && char.IsLetterOrDigit(character)) || character is '-' or '_' or '.' or ':');
}