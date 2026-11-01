namespace ScheduleIMegaExpansion.Core;

public enum ShopPurchaseOutcome
{
    Succeeded,
    InvalidRequest,
    UnknownShopItem,
    OutOfStock,
    InsufficientFunds,
    InvalidPrice,
    PriceOverflow,
    RequestIdConflict
}

public sealed record ShopPurchaseRequest(
    string RequestId,
    string BuyerId,
    string ShopId,
    string ProductId,
    int Quantity);

public sealed record ShopPurchaseResult(
    ShopPurchaseOutcome Outcome,
    string RequestId,
    long TotalPrice,
    long RemainingBalance,
    int RemainingStock,
    long WorldRevision,
    bool IsReplay = false)
{
    public bool Succeeded => Outcome == ShopPurchaseOutcome.Succeeded;
}

/// <summary>
/// In-memory domain reference for a host-authoritative shop purchase.
/// Production integration must persist state transactionally through the save adapter.
/// </summary>
public sealed class ShopPurchaseService
{
    private readonly object _gate = new();
    private readonly Dictionary<string, long> _balances = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _stock = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _prices = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (ShopPurchaseRequest Request, ShopPurchaseResult Result)> _processedRequests = new(StringComparer.Ordinal);
    private long _worldRevision;

    public long WorldRevision
    {
        get { lock (_gate) return _worldRevision; }
    }

    public void SetBalance(string buyerId, long balance)
    {
        if (string.IsNullOrWhiteSpace(buyerId))
            throw new ArgumentException("Buyer ID is required.", nameof(buyerId));
        if (balance < 0)
            throw new ArgumentOutOfRangeException(nameof(balance));

        lock (_gate) _balances[buyerId] = balance;
    }

    public void SetProduct(string shopId, string productId, long unitPrice, int stock)
    {
        if (string.IsNullOrWhiteSpace(shopId))
            throw new ArgumentException("Shop ID is required.", nameof(shopId));
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product ID is required.", nameof(productId));
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));
        if (stock < 0)
            throw new ArgumentOutOfRangeException(nameof(stock));

        string key = CreateStockKey(shopId, productId);
        lock (_gate)
        {
            _prices[key] = unitPrice;
            _stock[key] = stock;
        }
    }

    public long GetBalance(string buyerId)
    {
        lock (_gate) return _balances.TryGetValue(buyerId, out long balance) ? balance : 0;
    }

    public int GetStock(string shopId, string productId)
    {
        string key = CreateStockKey(shopId, productId);
        lock (_gate) return _stock.TryGetValue(key, out int stock) ? stock : 0;
    }

    public ShopPurchaseResult Purchase(ShopPurchaseRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        lock (_gate)
        {
            if (string.IsNullOrWhiteSpace(request.RequestId) ||
                string.IsNullOrWhiteSpace(request.BuyerId) ||
                string.IsNullOrWhiteSpace(request.ShopId) ||
                string.IsNullOrWhiteSpace(request.ProductId) ||
                request.Quantity <= 0)
            {
                return Result(ShopPurchaseOutcome.InvalidRequest, request.RequestId ?? string.Empty, 0, 0, 0);
            }

            if (_processedRequests.TryGetValue(request.RequestId, out var previous))
            {
                if (previous.Request != request)
                {
                    return Result(
                        ShopPurchaseOutcome.RequestIdConflict,
                        request.RequestId,
                        0,
                        GetBuyerBalance(request.BuyerId),
                        GetStockUnsafe(request.ShopId, request.ProductId));
                }

                return previous.Result with { IsReplay = true };
            }

            string key = CreateStockKey(request.ShopId, request.ProductId);
            if (!_stock.TryGetValue(key, out int stock) || !_prices.TryGetValue(key, out long unitPrice))
                return Remember(request, Result(ShopPurchaseOutcome.UnknownShopItem, request.RequestId, 0, GetBuyerBalance(request.BuyerId), 0));

            if (unitPrice <= 0)
                return Remember(request, Result(ShopPurchaseOutcome.InvalidPrice, request.RequestId, 0, GetBuyerBalance(request.BuyerId), stock));

            if (stock < request.Quantity)
                return Remember(request, Result(ShopPurchaseOutcome.OutOfStock, request.RequestId, 0, GetBuyerBalance(request.BuyerId), stock));

            long totalPrice;
            try
            {
                totalPrice = checked(unitPrice * request.Quantity);
            }
            catch (OverflowException)
            {
                return Remember(request, Result(ShopPurchaseOutcome.PriceOverflow, request.RequestId, 0, GetBuyerBalance(request.BuyerId), stock));
            }

            long balance = GetBuyerBalance(request.BuyerId);
            if (balance < totalPrice)
                return Remember(request, Result(ShopPurchaseOutcome.InsufficientFunds, request.RequestId, totalPrice, balance, stock));

            long remainingBalance = balance - totalPrice;
            int remainingStock = stock - request.Quantity;
            _balances[request.BuyerId] = remainingBalance;
            _stock[key] = remainingStock;
            _worldRevision++;

            return Remember(request, Result(ShopPurchaseOutcome.Succeeded, request.RequestId, totalPrice, remainingBalance, remainingStock));
        }
    }

    private ShopPurchaseResult Remember(ShopPurchaseRequest request, ShopPurchaseResult result)
    {
        _processedRequests[request.RequestId] = (request, result);
        return result;
    }

    private ShopPurchaseResult Result(
        ShopPurchaseOutcome outcome,
        string requestId,
        long totalPrice,
        long remainingBalance,
        int stock)
        => new(outcome, requestId, totalPrice, remainingBalance, stock, _worldRevision);

    private long GetBuyerBalance(string buyerId) =>
        _balances.TryGetValue(buyerId, out long balance) ? balance : 0;

    private int GetStockUnsafe(string shopId, string productId)
    {
        string key = CreateStockKey(shopId, productId);
        return _stock.TryGetValue(key, out int stock) ? stock : 0;
    }

    private static string CreateStockKey(string shopId, string productId) => $"{shopId}\u001f{productId}";
}
