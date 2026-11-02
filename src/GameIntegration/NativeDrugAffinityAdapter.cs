using System;
using System.Collections.Generic;
using System.Linq;
using ScheduleIMegaExpansion.Core;
using S1API.Products;

namespace ScheduleIMegaExpansion.GameIntegration;

/// <summary>
/// Converts game-independent affinity assignments into the exact API-safe
/// S1API.Products.DrugType enum used by native customer configuration.
/// The runtime enum is discovered dynamically; no copy of the game's enum is
/// maintained in the expansion core.
/// </summary>
public static class NativeDrugAffinityAdapter
{
    public static IReadOnlyList<string> GetRuntimeDrugTypeNames() =>
        Enum.GetNames<DrugType>()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    public static (DrugType DrugType, float Affinity)[] ToNativeAffinities(
        IEnumerable<NpcDrugAffinity> affinities)
    {
        if (affinities is null)
            throw new ArgumentNullException(nameof(affinities));

        var result = new List<(DrugType DrugType, float Affinity)>();
        var seen = new HashSet<DrugType>();

        foreach (NpcDrugAffinity affinity in affinities)
        {
            if (affinity is null || string.IsNullOrWhiteSpace(affinity.DrugTypeName))
                throw new ArgumentException("Affinity contains an empty DrugType name.", nameof(affinities));

            if (!Enum.TryParse(affinity.DrugTypeName, ignoreCase: false, out DrugType nativeType) ||
                !Enum.IsDefined(typeof(DrugType), nativeType))
            {
                throw new ArgumentException(
                    "The running S1API build does not expose DrugType '" + affinity.DrugTypeName + "'.",
                    nameof(affinities));
            }

            if (!float.IsFinite(affinity.Affinity) || affinity.Affinity < -1f || affinity.Affinity > 1f)
                throw new ArgumentOutOfRangeException(nameof(affinities), "Native drug affinities must be between -1 and +1.");

            if (!seen.Add(nativeType))
                throw new ArgumentException("The affinity list contains the same native DrugType more than once.", nameof(affinities));

            result.Add((nativeType, affinity.Affinity));
        }

        if (result.Count == 0)
            throw new ArgumentException("At least one native drug affinity is required.", nameof(affinities));

        return result.ToArray();
    }
}
