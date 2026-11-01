using ScheduleIMegaExpansion.Core;
using Xunit;

namespace ScheduleIMegaExpansion.Tests;

public sealed class NpcDrugAffinityRandomizerTests
{
    private static readonly string[] DrugTypes = { "DrugA", "DrugB", "DrugC", "DrugD" };
    private static readonly string[] NpcIds =
        Enumerable.Range(1, 26)
            .Select(index => $"sime.npc.harborline.customer_{index:00}")
            .ToArray();

    [Fact]
    public void AssignmentsAreStableForTheSameSaveAndNpcIds()
    {
        var first = NpcDrugAffinityRandomizer.CreateAssignments("save-seed-1", NpcIds, DrugTypes);
        var second = NpcDrugAffinityRandomizer.CreateAssignments(
            "save-seed-1",
            NpcIds.Reverse(),
            DrugTypes.Reverse());

        Assert.Equal(ToSignatures(first), ToSignatures(second));
    }

    [Fact]
    public void EveryNpcReceivesAFavouriteAndADislikedDrug()
    {
        var assignments = NpcDrugAffinityRandomizer.CreateAssignments("save-seed-2", NpcIds, DrugTypes);

        foreach (IReadOnlyList<NpcDrugAffinity> vector in assignments.Values)
        {
            Assert.Contains(vector, affinity => affinity.Affinity >= 0.70f);
            Assert.Contains(vector, affinity => affinity.Affinity <= -0.70f);
        }
    }

    [Fact]
    public void AffinityVectorsAreUniqueWithinTheGeneratedRoster()
    {
        var assignments = NpcDrugAffinityRandomizer.CreateAssignments("save-seed-3", NpcIds, DrugTypes);

        string[] signatures = ToSignatures(assignments).Values.ToArray();
        Assert.Equal(signatures.Length, signatures.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void DifferentSaveSeedsCanProduceDifferentPreferences()
    {
        var first = NpcDrugAffinityRandomizer.CreateAssignments("save-seed-A", NpcIds, DrugTypes);
        var second = NpcDrugAffinityRandomizer.CreateAssignments("save-seed-B", NpcIds, DrugTypes);

        Assert.NotEqual(
    string.Join("|", ToSignatures(first).OrderBy(item => item.Key).Select(item => item.Value)),
    string.Join("|", ToSignatures(second).OrderBy(item => item.Key).Select(item => item.Value)));
    }

    [Fact]
    public void OneDrugTypeIsRejectedBecauseFavouriteAndDislikeCannotDiffer()
    {
        Assert.Throws<ArgumentException>(() =>
            NpcDrugAffinityRandomizer.CreateAssignments(
                "save-seed",
                new[] { "sime.npc.harborline.customer_01" },
                new[] { "DrugA" }));
    }

    [Fact]
    public void DuplicateNpcIdsAndDrugTypesAreRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            NpcDrugAffinityRandomizer.CreateAssignments(
                "save-seed",
                new[] { "sime.npc.harborline.customer_01", "sime.npc.harborline.customer_01" },
                DrugTypes));

        Assert.Throws<ArgumentException>(() =>
            NpcDrugAffinityRandomizer.CreateAssignments(
                "save-seed",
                new[] { "sime.npc.harborline.customer_01" },
                new[] { "DrugA", "DrugA" }));
    }

    private static Dictionary<string, string> ToSignatures(
        IReadOnlyDictionary<string, IReadOnlyList<NpcDrugAffinity>> assignments) =>
        assignments.ToDictionary(
            item => item.Key,
            item => string.Join(
                ";",
                item.Value.Select(affinity =>
                    affinity.DrugTypeName + "=" + affinity.Affinity.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))),
            StringComparer.Ordinal);
}
