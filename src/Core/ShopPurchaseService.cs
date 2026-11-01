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

public sealed record ShopBalanceSnapshot(string BuyerId, long Balance);
public sealed record ShopProductSnapshot(string ShopId, string ProductId, long UnitPrice, int Stock);
public sealed record ShopProcessedRequestSnapshot(ShopPurchaseRequest Request, ShopPurchaseResult Result);

/// <summary>
/// A detached, JSON-serializable representation of shop economy state. It includes
/// processed requests so retries after save/load cannot charge or decrement stock again.
/// </summary>
public sealed record ShopPurchaseServiceSnapshot(
    long WorldRevision,
    IReadOnlyList<ShopBalanceSnapshot> Balances,
    IReadOnlyList<ShopProductSnapshot> Products,
    IReadOnlyList<ShopProcessedRequestSnapshot> ProcessedRequests);

/// <summary>
/// In-memory domain reference for a host-authoritative shop purchase.
/// Snapshot export/import is implemented; the game adapter must still persist and restore it.
/// </summary>
public sealed class ShopPurchaseService
{
    private const char KeySeparator = '\u001f';

    private readonly object _gate = new();
    private readonly Dictionary<string, long> _balances = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _stock = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _prices = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string ShopId, string ProductId)> _productIdentity = new(StringComparer.Ordinal);
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
        if (!IsValidKeyPart(shopId))
            throw new ArgumentException("Shop ID is required and cannot contain the internal key separator.", nameof(shopId));
        if (!IsValidKeyPart(productId))
            throw new ArgumentException("Product ID is required and cannot contain the internal key separator.", nameof(productId));
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));
        if (stock < 0)
            throw new ArgumentOutOfRangeException(nameof(stock));

        string key = CreateStockKey(shopId, productId);
        lock (_gate)
        {
            _prices[key] = unitPrice;
            _stock[key] = stock;
            _productIdentity[key] = (shopId, productId);
        }
    }

    public long GetBalance(string buyerId)
    {
        lock (_gate) return _balances.TryGetValue(buyerId, out long balance) ? balance : 0;
    }

    public int GetStock(string shopId, string productId)
    {
        if (!IsValidKeyPart(shopId) || !IsValidKeyPart(productId))
            return 0;

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
                !IsValidKeyPart(request.ShopId) ||
                !IsValidKeyPart(request.ProductId) ||
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

    /// <summary>
    /// Creates a detached snapshot in stable key order. The snapshot contains replay
    /// results for both successful and remembered failed requests.
    /// </summary>
    public ShopPurchaseServiceSnapshot CreateSnapshot()
    {
        lock (_gate)
        {
            ShopBalanceSnapshot[] balances = _balances
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new ShopBalanceSnapshot(pair.Key, pair.Value))
                .ToArray();

            ShopProductSnapshot[] products = _productIdentity
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new ShopProductSnapshot(
                    pair.Value.ShopId,
                    pair.Value.ProductId,
                    _prices[pair.Key],
                    _stock[pair.Key]))
                .ToArray();

            ShopProcessedRequestSnapshot[] processed = _processedRequests
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new ShopProcessedRequestSnapshot(pair.Value.Request, pair.Value.Result))
                .ToArray();

            return new ShopPurchaseServiceSnapshot(
                _worldRevision,
                Array.AsReadOnly(balances),
                Array.AsReadOnly(products),
                Array.AsReadOnly(processed));
        }
    }

    /// <summary>
    /// Validates all data before replacing state. On failure the current service is unchanged.
    /// Call this only with a snapshot loaded from trusted host save data.
    /// </summary>
    public bool TryRestoreSnapshot(ShopPurchaseServiceSnapshot? snapshot)
    {
        if (snapshot is null ||
            snapshot.WorldRevision < 0 ||
            snapshot.Balances is null ||
            snapshot.Products is null ||
            snapshot.ProcessedRequests is null)
        {
            return false;
        }

        var balances = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (ShopBalanceSnapshot? entry in snapshot.Balances)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.BuyerId) ||
                entry.Balance < 0 || !balances.TryAdd(entry.BuyerId, entry.Balance))
            {
                return false;
            }
        }

        var stock = new Dictionary<string, int>(StringComparer.Ordinal);
        var prices = new Dictionary<string, long>(StringComparer.Ordinal);
        var identity = new Dictionary<string, (string ShopId, string ProductId)>(StringComparer.Ordinal);
        foreach (ShopProductSnapshot? entry in snapshot.Products)
        {
            if (entry is null || !IsValidKeyPart(entry.ShopId) || !IsValidKeyPart(entry.ProductId) ||
                entry.UnitPrice < 0 || entry.Stock < 0)
            {
                return false;
            }

            string key = CreateStockKey(entry.ShopId, entry.ProductId);
            if (!stock.TryAdd(key, entry.Stock))
                return false;

            prices.Add(key, entry.UnitPrice);
            identity.Add(key, (entry.ShopId, entry.ProductId));
        }

        var processed = new Dictionary<string, (ShopPurchaseRequest Request, ShopPurchaseResult Result)>(StringComparer.Ordinal);
        foreach (ShopProcessedRequestSnapshot? entry in snapshot.ProcessedRequests)
        {
            if (entry?.Request is null || entry.Result is null)
                return false;

            ShopPurchaseRequest request = entry.Request;
            ShopPurchaseResult result = entry.Result;
            if (string.IsNullOrWhiteSpace(request.RequestId) ||
                string.IsNullOrWhiteSpace(request.BuyerId) ||
                !IsValidKeyPart(request.ShopId) ||
                !IsValidKeyPart(request.ProductId) ||
                request.Quantity <= 0 ||
                !Enum.IsDefined(typeof(ShopPurchaseOutcome), result.Outcome) ||
                !string.Equals(request.RequestId, result.RequestId, StringComparison.Ordinal) ||
                result.TotalPrice < 0 ||
                result.RemainingBalance < 0 ||
                result.RemainingStock < 0 ||
                result.WorldRevision < 0 ||
                result.WorldRevision > snapshot.WorldRevision ||
                result.IsReplay ||
                !processed.TryAdd(request.RequestId, (request, result)))
            {
                return false;
            }
        }

        lock (_gate)
        {
            Replace(_balances, balances);
            Replace(_stock, stock);
            Replace(_prices, prices);
            Replace(_productIdentity, identity);
            Replace(_processedRequests, processed);
            _worldRevision = snapshot.WorldRevision;
        }

        return true;
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

    private static bool IsValidKeyPart(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.IndexOf(KeySeparator) < 0;

    private static string CreateStockKey(string shopId, string productId) => $"{shopId}{KeySeparator}{productId}";

    private static void Replace<TKey, TValue>(Dictionary<TKey, TValue> target, Dictionary<TKey, TValue> source)
        where TKey : notnull
    {
        target.Clear();
        foreach (KeyValuePair<TKey, TValue> pair in source)
            target.Add(pair.Key, pair.Value);
    }
}
