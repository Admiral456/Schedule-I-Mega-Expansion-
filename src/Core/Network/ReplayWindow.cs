namespace ScheduleIMegaExpansion.Core.Network;

/// <summary>
/// Sliding 64-message replay window for one authenticated sender.
/// Allows limited packet reordering while rejecting duplicates and stale sequences.
/// Use one instance per sender, session and protocol epoch.
/// </summary>
public sealed class ReplayWindow
{
    public const int WindowSize = 64;

    private readonly object _gate = new();
    private bool _initialized;
    private long _highestSequence;
    private ulong _seenBitmap;

    public long HighestSequence
    {
        get { lock (_gate) return _initialized ? _highestSequence : -1; }
    }

    public bool TryAccept(long sequence)
    {
        if (sequence < 0)
            return false;

        lock (_gate)
        {
            if (!_initialized)
            {
                _initialized = true;
                _highestSequence = sequence;
                _seenBitmap = 1UL;
                return true;
            }

            if (sequence > _highestSequence)
            {
                long distance = sequence - _highestSequence;
                _seenBitmap = distance >= WindowSize
                    ? 1UL
                    : (_seenBitmap << (int)distance) | 1UL;
                _highestSequence = sequence;
                return true;
            }

            long behind = _highestSequence - sequence;
            if (behind >= WindowSize)
                return false;

            ulong mask = 1UL << (int)behind;
            if ((_seenBitmap & mask) != 0)
                return false;

            _seenBitmap |= mask;
            return true;
        }
    }
}
