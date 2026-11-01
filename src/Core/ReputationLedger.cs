namespace ScheduleIMegaExpansion.Core;

/// <summary>
/// Idempotent reputation ledger. Use separate keys for player, crew, business and province scopes.
/// </summary>
public sealed class ReputationLedger
{
    public const int Minimum = -100;
    public const int Maximum = 100;

    private readonly object _gate = new();
    private readonly Dictionary<string, int> _values = new(StringComparer.Ordinal);
    private readonly HashSet<string> _appliedEventIds = new(StringComparer.Ordinal);

    public int Get(string scopeKey)
    {
        if (string.IsNullOrWhiteSpace(scopeKey))
            throw new ArgumentException("Reputation scope key is required.", nameof(scopeKey));

        lock (_gate) return _values.TryGetValue(scopeKey, out int value) ? value : 0;
    }

    /// <returns>The resulting reputation. A repeated event ID is ignored.</returns>
    public int Apply(string eventId, string scopeKey, int delta)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            throw new ArgumentException("Event ID is required.", nameof(eventId));
        if (string.IsNullOrWhiteSpace(scopeKey))
            throw new ArgumentException("Reputation scope key is required.", nameof(scopeKey));

        lock (_gate)
        {
            int current = _values.TryGetValue(scopeKey, out int value) ? value : 0;
            if (!_appliedEventIds.Add(eventId))
                return current;

            long candidate = (long)current + delta;
            int updated = (int)Math.Clamp(candidate, Minimum, Maximum);
            _values[scopeKey] = updated;
            return updated;
        }
    }
}
