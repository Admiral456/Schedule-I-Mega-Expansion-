using System.Text.Json;
using MelonLoader;
using ScheduleIMegaExpansion.Core;
using S1API.Internal.Abstraction;
using S1API.Saveables;
using SteamNetworkLib;

namespace ScheduleIMegaExpansion.GameIntegration;

/// <summary>
/// S1API discovers concrete Saveable subclasses automatically.
/// Raw JSON is preserved when a newer or malformed schema is encountered.
/// </summary>
public sealed class ExpansionSaveable : Saveable
{
    public const string ModNamespace = "schedulei.megaexpansion";
    public const int CurrentSchemaVersion = 1;
    public const string WorldId = "schedulei.hyland_point";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    [SaveableField("sime_mega_expansion_blob")]
    private string _rawDocument = string.Empty;

    private ExpansionSaveDocument? _document;

    public static ExpansionSaveable? Instance { get; private set; }
    public bool IsReadOnly { get; private set; }
    public bool IsLoaded { get; private set; }

    public ExpansionSaveable()
    {
        Instance = this;
    }

    protected override void OnLoaded()
    {
        IsLoaded = true;
        IsReadOnly = false;

        if (string.IsNullOrWhiteSpace(_rawDocument))
        {
            _document = ExpansionSaveDocument.CreateDefault();
            RefreshSerializedField();
            return;
        }

        try
        {
            using JsonDocument parsed = JsonDocument.Parse(_rawDocument);
            JsonElement root = parsed.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                MarkReadOnly("Expansion save root is not a JSON object.");
                return;
            }

            string? modId = ReadString(root, "modId");
            if (!string.Equals(modId, ModNamespace, StringComparison.Ordinal))
            {
                MarkReadOnly("Expansion save namespace does not match this mod. Original data was preserved.");
                return;
            }

            if (!root.TryGetProperty("schemaVersion", out JsonElement versionElement) ||
                !versionElement.TryGetInt32(out int schemaVersion) || schemaVersion < 1)
            {
                MarkReadOnly("Expansion save schemaVersion is missing or invalid. Original data was preserved.");
                return;
            }

            if (schemaVersion > CurrentSchemaVersion)
            {
                MarkReadOnly("Expansion save was created by a newer mod version. It is being kept read-only.");
                return;
            }

            if (schemaVersion < CurrentSchemaVersion)
            {
                // Add explicit N -> N+1 migrations before increasing CurrentSchemaVersion.
                MarkReadOnly("No migration is registered for this older expansion save. Original data was preserved.");
                return;
            }

            ExpansionSaveDocument? loaded = JsonSerializer.Deserialize<ExpansionSaveDocument>(_rawDocument, JsonOptions);
            if (loaded is null || loaded.Data is null ||
                string.IsNullOrWhiteSpace(loaded.WorldId) || loaded.WorldRevision < 0)
            {
                MarkReadOnly("Expansion save failed its integrity checks. Original data was preserved.");
                return;
            }

            _document = loaded;
        }
        catch (Exception exception)
        {
            MarkReadOnly("Expansion save could not be parsed. Original data was preserved. " + exception.Message);
        }
    }

    protected override void OnSaved()
    {
        // S1API serializes SaveableField values before calling OnSaved.
        // Keep this field current at mutation time, not here.
    }

    /// <summary>Returns a detached snapshot, so callers cannot mutate persisted state.</summary>
    public ExpansionSaveDocument? GetDocumentSnapshot()
    {
        if (_document is null)
            return null;

        return JsonSerializer.Deserialize<ExpansionSaveDocument>(
            JsonSerializer.Serialize(_document, JsonOptions), JsonOptions);
    }

    /// <summary>
    /// Replaces shared state only when the initialized SteamNetworkLib client reports
    /// that this local peer is the host of an active lobby. Client payloads cannot assert authority.
    /// </summary>
    public bool TryReplaceWorldState(ExpansionWorldState newState, SteamNetworkClient? network)
    {
        if (!IsAuthoritativeHost(network) || IsReadOnly || !IsLoaded || _document is null || newState is null)
            return false;

        if (_document.WorldRevision == long.MaxValue)
            return false;

        try
        {
            ExpansionWorldState? detached = JsonSerializer.Deserialize<ExpansionWorldState>(
                JsonSerializer.Serialize(newState, JsonOptions), JsonOptions);
            if (detached is null)
                return false;

            _document.Data = detached;
            _document.WorldRevision++;
            RefreshSerializedField();
            return true;
        }
        catch (JsonException exception)
        {
            MelonLogger.Error("[MegaExpansion] State update rejected because serialization failed: " + exception.Message);
            return false;
        }
    }

    /// <summary>
    /// Loads the persisted shop economy as a detached domain service and returns the
    /// save revision that the caller must present when committing. Corrupt snapshots
    /// make the expansion save read-only rather than silently resetting the economy.
    /// </summary>
    public bool TryLoadShopPurchaseService(
        out ShopPurchaseService? service,
        out long expectedWorldRevision)
    {
        service = null;
        expectedWorldRevision = 0;

        if (!IsLoaded || IsReadOnly || _document is null)
            return false;

        ExpansionSaveDocument? snapshot = GetDocumentSnapshot();
        if (snapshot is null)
            return false;

        var restored = new ShopPurchaseService();
        if (snapshot.Data.ShopPurchaseState is not null &&
            !restored.TryRestoreSnapshot(snapshot.Data.ShopPurchaseState))
        {
            MarkReadOnly("Saved shop purchase state failed validation. Original expansion data was preserved.");
            return false;
        }

        service = restored;
        expectedWorldRevision = snapshot.WorldRevision;
        return true;
    }

    /// <summary>
    /// Persists a shop service snapshot only if the save revision has not changed
    /// since TryLoadShopPurchaseService. This prevents a stale service instance from
    /// overwriting a newer host update. It stores domain economy data only; callers
    /// must still integrate real Schedule I cash and item inventory transactions.
    /// </summary>
    public bool TryCommitShopPurchaseService(
        ShopPurchaseService service,
        long expectedWorldRevision,
        SteamNetworkClient? network)
    {
        if (service is null ||
            !IsAuthoritativeHost(network) ||
            IsReadOnly ||
            !IsLoaded ||
            _document is null ||
            expectedWorldRevision != _document.WorldRevision)
        {
            return false;
        }

        ExpansionSaveDocument? updated = GetDocumentSnapshot();
        if (updated is null || updated.WorldRevision != expectedWorldRevision)
            return false;

        var validator = new ShopPurchaseService();
        if (!validator.TryRestoreSnapshot(service.CreateSnapshot()))
            return false;

        updated.Data.ShopPurchaseState = validator.CreateSnapshot();
        return TryReplaceWorldState(updated.Data, network);
    }

    public bool TryGetProcessedOrderRequest(string requestId, string requestFingerprint, out ExpansionOrderRecord? order, out bool fingerprintConflict)
    {
        order = null;
        fingerprintConflict = false;
        if (string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(requestFingerprint) ||
            _document?.Data is null || _document.Data.ProcessedOrderRequests is null || _document.Data.Orders is null)
            return false;

        if (!_document.Data.ProcessedOrderRequests.TryGetValue(requestId, out ExpansionProcessedOrderRequest? processed))
            return false;
        if (!string.Equals(processed.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
        {
            fingerprintConflict = true;
            return true;
        }
        if (!_document.Data.Orders.TryGetValue(processed.OrderId, out ExpansionOrderRecord? savedOrder))
        {
            fingerprintConflict = true;
            return true;
        }

        order = JsonSerializer.Deserialize<ExpansionOrderRecord>(
            JsonSerializer.Serialize(savedOrder, JsonOptions), JsonOptions);
        return order is not null;
    }

    /// <summary>
    /// Commits the shop ledger, reserved stock, escrow balance, order and idempotency
    /// receipt under one host save revision. A rejected game-save request restores the
    /// previous in-memory document.
    /// </summary>
    public bool TryCommitNpcOrderTransaction(
        ShopPurchaseService service,
        NpcOrderDraft draft,
        long expectedWorldRevision,
        SteamNetworkClient? network)
    {
        if (service is null || draft is null || !IsAuthoritativeHost(network) ||
            IsReadOnly || !IsLoaded || _document is null ||
            expectedWorldRevision != _document.WorldRevision ||
            !StableId.HasPrefix(draft.OrderId, StableId.OrderPrefix) ||
            string.IsNullOrWhiteSpace(draft.RequestId) ||
            !string.Equals(draft.OrderId, StableId.FromOrderRequest(draft.RequestId), StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(draft.RequestFingerprint) ||
            string.IsNullOrWhiteSpace(draft.RequesterId) ||
            !StableId.HasPrefix(draft.CustomerId, StableId.NpcPrefix) ||
            !StableId.HasPrefix(draft.ProvinceId, StableId.ProvincePrefix) ||
            !StableId.HasPrefix(draft.ShopId, StableId.BusinessPrefix) ||
            !string.Equals(draft.RequestFingerprint,
                draft.RequesterId + "|" + draft.CustomerId + "|" + draft.ShopId,
                StringComparison.Ordinal) ||
            !string.Equals(draft.BuyerAccountId,
                NpcOrderPlacementService.CreateBudgetAccountId(draft.CustomerId, draft.RequesterId, draft.WorldInterval),
                StringComparison.Ordinal) ||
            !string.Equals(draft.EscrowAccountId, "sime.escrow." + draft.OrderId, StringComparison.Ordinal) ||
            draft.WorldInterval < 0 ||
            draft.CreatedAtUnixSeconds <= 0 ||
            draft.DeadlineUnixSeconds <= draft.CreatedAtUnixSeconds ||
            draft.AcceptanceScore < NpcCustomerAcceptance.DefaultAcceptanceThreshold ||
            draft.AcceptanceScore > 100 ||
            draft.TotalPrice <= 0 || draft.Items is null || draft.Items.Count == 0 ||
            !string.Equals(draft.Status, "offered", StringComparison.Ordinal))
            return false;

        long verifiedTotal = 0;
        var uniqueProducts = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (NpcOrderDraftLine line in draft.Items)
            {
                if (line is null || string.IsNullOrWhiteSpace(line.ProductId) ||
                    !uniqueProducts.Add(line.ProductId) ||
                    line.Quantity <= 0 || line.UnitPrice <= 0)
                    return false;
                verifiedTotal = checked(verifiedTotal + checked(line.UnitPrice * line.Quantity));
            }
        }
        catch (OverflowException)
        {
            return false;
        }
        if (verifiedTotal != draft.TotalPrice)
            return false;

        ExpansionSaveDocument? updated = GetDocumentSnapshot();
        if (updated is null || updated.WorldRevision != expectedWorldRevision || updated.Data is null)
            return false;

        updated.Data.Orders ??= new Dictionary<string, ExpansionOrderRecord>(StringComparer.Ordinal);
        updated.Data.ProcessedOrderRequests ??= new Dictionary<string, ExpansionProcessedOrderRequest>(StringComparer.Ordinal);
        updated.Data.ProcessedRequestIds ??= new HashSet<string>(StringComparer.Ordinal);
        if (updated.Data.Orders.ContainsKey(draft.OrderId) ||
            updated.Data.ProcessedOrderRequests.ContainsKey(draft.RequestId))
            return false;

        var validator = new ShopPurchaseService();
        if (!validator.TryRestoreSnapshot(service.CreateSnapshot()))
            return false;

        var record = new ExpansionOrderRecord
        {
            RequestId = draft.RequestId,
            RequesterId = draft.RequesterId,
            CustomerId = draft.CustomerId,
            ProvinceId = draft.ProvinceId,
            ShopId = draft.ShopId,
            BuyerAccountId = draft.BuyerAccountId,
            EscrowAccountId = draft.EscrowAccountId,
            Status = draft.Status,
            Items = draft.Items.Select(item => new ExpansionOrderLine
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList(),
            DeadlineWorldTime = draft.DeadlineUnixSeconds,
            CreatedAtUnixSeconds = draft.CreatedAtUnixSeconds,
            WorldInterval = draft.WorldInterval,
            TotalPrice = draft.TotalPrice,
            Reward = draft.TotalPrice,
            AcceptanceScore = draft.AcceptanceScore,
            Version = 1
        };

        updated.Data.ShopPurchaseState = validator.CreateSnapshot();
        updated.Data.Orders.Add(draft.OrderId, record);
        updated.Data.ProcessedOrderRequests.Add(draft.RequestId, new ExpansionProcessedOrderRequest
        {
            RequestFingerprint = draft.RequestFingerprint,
            OrderId = draft.OrderId
        });
        updated.Data.ProcessedRequestIds.Add(draft.RequestId);

        ExpansionSaveDocument previousDocument = _document;
        string previousRawDocument = _rawDocument;
        if (!TryReplaceWorldState(updated.Data, network))
            return false;
        if (RequestHostSave(network))
            return true;

        _document = previousDocument;
        _rawDocument = previousRawDocument;
        return false;
    }

    /// <summary>
    /// Finalizes an NPC order and its escrow in the same persisted world revision.
    /// Completed orders settle to shop revenue; cancelled, expired and failed orders
    /// refund the customer budget and restore the reserved shop stock.
    /// </summary>
    public bool TryFinalizeNpcOrder(
        string orderId,
        OrderStatus targetStatus,
        long expectedWorldRevision,
        SteamNetworkClient? network)
    {
        if (string.IsNullOrWhiteSpace(orderId) ||
            !StableId.HasPrefix(orderId, StableId.OrderPrefix) ||
            !IsAuthoritativeHost(network) ||
            IsReadOnly ||
            !IsLoaded ||
            _document is null ||
            expectedWorldRevision != _document.WorldRevision)
        {
            return false;
        }

        ExpansionSaveDocument? updated = GetDocumentSnapshot();
        if (updated?.Data is null ||
            updated.WorldRevision != expectedWorldRevision ||
            !updated.Data.Orders.TryGetValue(orderId, out ExpansionOrderRecord? order) ||
            order is null)
        {
            return false;
        }

        if (!Enum.TryParse(order.Status, ignoreCase: true, out OrderStatus currentStatus) ||
            !Enum.IsDefined(typeof(OrderStatus), currentStatus))
        {
            return false;
        }

        // A persisted terminal state means the escrow operation was committed with it.
        if (currentStatus == targetStatus &&
            targetStatus is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Expired or OrderStatus.Failed)
        {
            return true;
        }

        if (order.Version < 1 || order.Version == long.MaxValue ||
            order.Items is null || order.Items.Count == 0 ||
            order.TotalPrice <= 0 ||
            string.IsNullOrWhiteSpace(order.RequestId) ||
            string.IsNullOrWhiteSpace(order.RequesterId) ||
            !StableId.HasPrefix(order.CustomerId, StableId.NpcPrefix) ||
            !StableId.HasPrefix(order.ProvinceId, StableId.ProvincePrefix) ||
            !StableId.HasPrefix(order.ShopId, StableId.BusinessPrefix) ||
            !string.Equals(order.EscrowAccountId, "sime.escrow." + orderId, StringComparison.Ordinal) ||
            !string.Equals(order.BuyerAccountId,
                NpcOrderPlacementService.CreateBudgetAccountId(order.CustomerId, order.RequesterId, order.WorldInterval),
                StringComparison.Ordinal))
        {
            return false;
        }

        long verifiedTotal = 0;
        var refundLines = new List<ShopRefundLine>();
        var seenProducts = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (ExpansionOrderLine line in order.Items)
            {
                if (line is null ||
                    string.IsNullOrWhiteSpace(line.ProductId) ||
                    line.Quantity <= 0 ||
                    line.UnitPrice <= 0 ||
                    !seenProducts.Add(line.ProductId))
                {
                    return false;
                }

                verifiedTotal = checked(verifiedTotal + checked(line.UnitPrice * line.Quantity));
                refundLines.Add(new ShopRefundLine(line.ProductId, line.Quantity));
            }
        }
        catch (OverflowException)
        {
            return false;
        }

        if (verifiedTotal != order.TotalPrice)
            return false;

        if (updated.Data.ShopPurchaseState is null)
            return false;

        var economy = new ShopPurchaseService();
        if (!economy.TryRestoreSnapshot(updated.Data.ShopPurchaseState))
            return false;

        string operationId = StableId.FromOrderRequest(
            "npc-order-finalization:" + orderId + ":" + targetStatus.ToString());

        var escrowTerms = new NpcOrderEscrowTerms(
            orderId,
            order.EscrowAccountId,
            order.BuyerAccountId,
            order.ShopId,
            order.TotalPrice,
            Array.AsReadOnly(refundLines.ToArray()));

        NpcOrderSettlementResult settlement = NpcOrderSettlementService.Finalize(
            economy,
            escrowTerms,
            currentStatus,
            targetStatus,
            operationId);

        if (!settlement.Success || settlement.LedgerResult is null)
            return false;

        order.Status = targetStatus.ToString().ToLowerInvariant();
        order.Version++;
        updated.Data.ShopPurchaseState = economy.CreateSnapshot();

        ExpansionSaveDocument previousDocument = _document;
        string previousRawDocument = _rawDocument;
        if (!TryReplaceWorldState(updated.Data, network))
            return false;

        if (RequestHostSave(network))
            return true;

        _document = previousDocument;
        _rawDocument = previousRawDocument;
        return false;
    }

    public bool RequestHostSave(SteamNetworkClient? network)
    {
        if (!IsAuthoritativeHost(network) || IsReadOnly || _document is null)
            return false;
        return Saveable.RequestGameSave();
    }

    private static bool IsAuthoritativeHost(SteamNetworkClient? network) =>
        network is not null &&
        network.IsInitialized &&
        network.IsInLobby &&
        network.IsHost;

    private void RefreshSerializedField()
    {
        if (_document is not null)
            _rawDocument = JsonSerializer.Serialize(_document, JsonOptions);
    }

    private void MarkReadOnly(string reason)
    {
        IsReadOnly = true;
        _document = null;
        // Do not clear _rawDocument: S1API will round-trip this field unchanged.
        MelonLogger.Warning("[MegaExpansion] " + reason);
    }

    private static string? ReadString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}

public sealed class ExpansionSaveDocument
{
    public string ModId { get; set; } = ExpansionSaveable.ModNamespace;
    public int SchemaVersion { get; set; } = ExpansionSaveable.CurrentSchemaVersion;
    public string WorldId { get; set; } = ExpansionSaveable.WorldId;
    public long WorldRevision { get; set; }
    public ExpansionWorldState Data { get; set; } = new();

    public static ExpansionSaveDocument CreateDefault() => new();
}

public sealed class ExpansionWorldState
{
    public List<string> EnabledProvinceIds { get; set; } = new();
    public Dictionary<string, string> BusinessOwners { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> ReputationByScope { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, ExpansionOrderRecord> Orders { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, Dictionary<string, int>> ShopStockByInventoryId { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> ProcessedRequestIds { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, ExpansionProcessedOrderRequest> ProcessedOrderRequests { get; set; } = new(StringComparer.Ordinal);
    public ShopPurchaseServiceSnapshot? ShopPurchaseState { get; set; }
}

public sealed class ExpansionOrderRecord
{
    public string RequestId { get; set; } = string.Empty;
    public string RequesterId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string ProvinceId { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
    public string BuyerAccountId { get; set; } = string.Empty;
    public string EscrowAccountId { get; set; } = string.Empty;
    public string Status { get; set; } = "offered";
    public List<ExpansionOrderLine> Items { get; set; } = new();
    public long? DeadlineWorldTime { get; set; }
    public long CreatedAtUnixSeconds { get; set; }
    public long WorldInterval { get; set; }
    public long TotalPrice { get; set; }
    public long Reward { get; set; }
    public int AcceptanceScore { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class ExpansionProcessedOrderRequest
{
    public string RequestFingerprint { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
}

public sealed class ExpansionOrderLine
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public long UnitPrice { get; set; }
}
