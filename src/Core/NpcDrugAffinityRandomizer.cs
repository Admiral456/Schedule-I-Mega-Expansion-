using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ScheduleIMegaExpansion.Core;

/// <summary>
/// One affinity for a native Schedule I DrugType. DrugTypeName is mapped to the
/// game's enum by the game-facing adapter; this core class has no Unity dependency.
/// </summary>
public sealed record NpcDrugAffinity(string DrugTypeName, float Affinity);

/// <summary>
/// Creates deterministic, varied, non-identical drug-affinity vectors for a group
/// of expansion NPCs. The host supplies a save-scoped seed and the DrugType names
/// discovered in the running game.
/// </summary>
public static class NpcDrugAffinityRandomizer
{
    private const string AlgorithmId = "sime.npc-affinity.v1";

    private static readonly float[] FavouriteLevels = { 0.70f, 0.82f, 0.94f, 1.00f };
    private static readonly float[] DislikeLevels = { -0.70f, -0.82f, -0.94f, -1.00f };
    private static readonly float[] OtherLevels =
    {
        -0.45f, -0.30f, -0.15f, 0.00f, 0.15f, 0.30f, 0.45f, 0.55f
    };

    /// <summary>
    /// Returns one stable affinity vector per NPC. Every NPC has at least one
    /// strong favourite and one strong dislike. Vectors are unique within this
    /// generated group; if the runtime DrugType catalog cannot provide enough
    /// combinations, the method fails closed instead of silently duplicating them.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<NpcDrugAffinity>> CreateAssignments(
        string saveSeed,
        IEnumerable<string> npcIds,
        IEnumerable<string> availableDrugTypes)
    {
        if (string.IsNullOrWhiteSpace(saveSeed))
            throw new ArgumentException("A save-scoped generation seed is required.", nameof(saveSeed));
        if (npcIds is null)
            throw new ArgumentNullException(nameof(npcIds));
        if (availableDrugTypes is null)
            throw new ArgumentNullException(nameof(availableDrugTypes));

        string[] ids = npcIds
            .Select(value => value?.Trim() ?? string.Empty)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        if (ids.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("NPC IDs must be non-empty.", nameof(npcIds));
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
            throw new ArgumentException("NPC IDs must be unique.", nameof(npcIds));

        string[] drugTypes = availableDrugTypes
            .Select(value => value?.Trim() ?? string.Empty)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        if (drugTypes.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("DrugType names must be non-empty.", nameof(availableDrugTypes));
        if (drugTypes.Distinct(StringComparer.Ordinal).Count() != drugTypes.Length)
            throw new ArgumentException("DrugType names must be unique.", nameof(availableDrugTypes));
        if (drugTypes.Length < 2)
            throw new ArgumentException("At least two runtime DrugType values are required.", nameof(availableDrugTypes));

        var assignments = new Dictionary<string, IReadOnlyList<NpcDrugAffinity>>(StringComparer.Ordinal);
        var usedSignatures = new HashSet<string>(StringComparer.Ordinal);

        foreach (string npcId in ids)
        {
            bool assigned = false;

            // The salt changes only this NPC's candidate vector; it never touches
            // UnityEngine.Random or the game's global random state.
            for (int salt = 0; salt < 8192; salt++)
            {
                int favouriteIndex = HashIndex(saveSeed, npcId, salt, "favourite-index", drugTypes.Length);
                int dislikeOffset = HashIndex(saveSeed, npcId, salt, "dislike-index", drugTypes.Length - 1);
                int dislikeIndex = dislikeOffset >= favouriteIndex ? dislikeOffset + 1 : dislikeOffset;

                var vector = new NpcDrugAffinity[drugTypes.Length];
                for (int i = 0; i < drugTypes.Length; i++)
                {
                    float affinity;
                    if (i == favouriteIndex)
                    {
                        affinity = FavouriteLevels[HashIndex(saveSeed, npcId, salt, "favourite-level", FavouriteLevels.Length)];
                    }
                    else if (i == dislikeIndex)
                    {
                        affinity = DislikeLevels[HashIndex(saveSeed, npcId, salt, "dislike-level", DislikeLevels.Length)];
                    }
                    else
                    {
                        affinity = OtherLevels[HashIndex(saveSeed, npcId, salt, "other:" + drugTypes[i], OtherLevels.Length)];
                    }

                    vector[i] = new NpcDrugAffinity(drugTypes[i], affinity);
                }

                string signature = BuildSignature(vector);
                if (!usedSignatures.Add(signature))
                    continue;

                assignments.Add(npcId, Array.AsReadOnly(vector));
                assigned = true;
                break;
            }

            if (!assigned)
            {
                throw new InvalidOperationException(
                    $"Could not create a unique affinity vector for '{npcId}'. " +
                    "The running game exposes too few usable DrugType combinations for this NPC roster.");
            }
        }

        return assignments;
    }

    private static int HashIndex(string saveSeed, string npcId, int salt, string channel, int count)
    {
        string input = string.Join("|", AlgorithmId, saveSeed, npcId, salt.ToString(CultureInfo.InvariantCulture), channel);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(hash.AsSpan(0, sizeof(uint)));
        return (int)(value % (uint)count);
    }

    private static string BuildSignature(IEnumerable<NpcDrugAffinity> vector) =>
        string.Join(
            ";",
            vector.Select(item =>
                item.DrugTypeName + "=" + item.Affinity.ToString("0.00", CultureInfo.InvariantCulture)));
}
