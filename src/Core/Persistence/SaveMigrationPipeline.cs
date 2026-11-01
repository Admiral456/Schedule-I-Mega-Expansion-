namespace ScheduleIMegaExpansion.Core.Persistence;

public sealed record ModSaveDocument<TData>(
    string ModId,
    int SchemaVersion,
    string WorldId,
    long WorldRevision,
    TData Data)
    where TData : class;

public sealed record SaveMigrationStep<TData>(
    int FromVersion,
    int ToVersion,
    Func<TData, TData> Migrate)
    where TData : class;

/// <summary>
/// Applies explicit one-version-at-a-time migrations to mod-owned save data.
/// It never reads or writes the vanilla game's save file.
/// </summary>
public sealed class SaveMigrationPipeline<TData> where TData : class
{
    private readonly string _expectedModId;
    private readonly int _currentVersion;
    private readonly Dictionary<int, SaveMigrationStep<TData>> _steps;

    public SaveMigrationPipeline(
        string expectedModId,
        int currentVersion,
        IEnumerable<SaveMigrationStep<TData>> steps)
    {
        if (string.IsNullOrWhiteSpace(expectedModId))
            throw new ArgumentException("Mod ID is required.", nameof(expectedModId));
        if (currentVersion < 1)
            throw new ArgumentOutOfRangeException(nameof(currentVersion));

        _expectedModId = expectedModId;
        _currentVersion = currentVersion;
        _steps = new Dictionary<int, SaveMigrationStep<TData>>();

        foreach (SaveMigrationStep<TData> step in steps ?? throw new ArgumentNullException(nameof(steps)))
        {
            if (step.FromVersion < 1 || step.ToVersion != step.FromVersion + 1)
                throw new ArgumentException("Migration steps must move exactly one version forward.", nameof(steps));
            if (step.Migrate is null)
                throw new ArgumentException("Migration delegate is required.", nameof(steps));
            if (!_steps.TryAdd(step.FromVersion, step))
                throw new ArgumentException("Duplicate migration from schema " + step.FromVersion + ".", nameof(steps));
        }
    }

    public ModSaveDocument<TData> Migrate(ModSaveDocument<TData> source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));
        if (!string.Equals(source.ModId, _expectedModId, StringComparison.Ordinal))
            throw new InvalidOperationException("Save belongs to a different mod namespace.");
        if (source.SchemaVersion < 1)
            throw new InvalidOperationException("Save schema version must be positive.");
        if (source.SchemaVersion > _currentVersion)
            throw new InvalidOperationException("Save was created by a newer mod version.");
        if (string.IsNullOrWhiteSpace(source.WorldId))
            throw new InvalidOperationException("World ID is missing.");
        if (source.WorldRevision < 0)
            throw new InvalidOperationException("World revision cannot be negative.");
        if (source.Data is null)
            throw new InvalidOperationException("Save data is missing.");

        TData data = source.Data;
        int version = source.SchemaVersion;

        while (version < _currentVersion)
        {
            if (!_steps.TryGetValue(version, out SaveMigrationStep<TData>? step))
                throw new InvalidOperationException("Missing migration from schema " + version + " to " + (version + 1) + ".");

            data = step.Migrate(data) ?? throw new InvalidOperationException("Migration " + version + "->" + step.ToVersion + " returned null.");
            version = step.ToVersion;
        }

        return source with { SchemaVersion = version, Data = data };
    }
}
