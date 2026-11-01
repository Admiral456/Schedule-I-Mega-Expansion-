using System.Text.Json;
using MelonLoader;
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
}

public sealed class ExpansionOrderRecord
{
    public string CustomerId { get; set; } = string.Empty;
    public string ProvinceId { get; set; } = string.Empty;
    public string Status { get; set; } = "offered";
    public List<ExpansionOrderLine> Items { get; set; } = new();
    public long? DeadlineWorldTime { get; set; }
    public long Reward { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class ExpansionOrderLine
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
