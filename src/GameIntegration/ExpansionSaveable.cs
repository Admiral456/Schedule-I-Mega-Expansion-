using System.Text.Json;
using MelonLoader;
using S1API.Internal.Abstraction;
using S1API.Saveables;

namespace ScheduleIMegaExpansion.GameIntegration;

/// <summary>
/// S1API discovers concrete Saveable subclasses automatically.
/// The persisted field is a raw JSON string so newer schemas can be preserved
/// without deserializing them into an older DTO and discarding unknown fields.
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
    public ExpansionSaveDocument? Document => _document;

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
                !versionElement.TryGetInt32(out int schemaVersion) ||
                schemaVersion < 1)
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
                string.IsNullOrWhiteSpace(loaded.WorldId) ||
                loaded.WorldRevision < 0)
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
        // Keep the serialized field current at mutation time, not here.
    }

    /// <summary>
    /// Caller authority must be derived from the authenticated SteamNetworkLib session,
    /// never from a client-supplied boolean or payload.
    /// </summary>
    public bool TryReplaceWorldState(ExpansionWorldState newState, bool callerIsAuthoritativeHost)
    {
        if (!callerIsAuthoritativeHost || IsReadOnly || !IsLoaded || _document is null || newState is null)
            return false;

        _document.Data = newState;
        _document.WorldRevision = checked(_document.WorldRevision + 1);
        RefreshSerializedField();
        return true;
    }

    public bool RequestHostSave(bool callerIsAuthoritativeHost)
    {
        if (!callerIsAuthoritativeHost || IsReadOnly || _document is null)
            return false;

        return Saveable.RequestGameSave();
    }

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
