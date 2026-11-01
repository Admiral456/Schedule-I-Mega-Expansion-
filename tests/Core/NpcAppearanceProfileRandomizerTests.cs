using ScheduleIMegaExpansion.Core;
using Xunit;

namespace ScheduleIMegaExpansion.Tests;

public sealed class NpcAppearanceProfileRandomizerTests
{
    private static readonly string[] NpcIds = Enumerable.Range(1, 42)
        .Select(index => $"sime.npc.harborline.resident_{index:00}")
        .ToArray();

    [Fact]
    public void GeneratesAUniqueAppearanceProfileForEveryHarborlineNpc()
    {
        var profiles = NpcAppearanceProfileRandomizer.CreateProfiles("harborline-save-1", NpcIds);

        Assert.Equal(42, profiles.Count);
        Assert.Equal(42, profiles.Values.Select(profile => profile.Signature).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void AppearanceProfilesAreStableRegardlessOfInputOrder()
    {
        var first = NpcAppearanceProfileRandomizer.CreateProfiles("harborline-save-2", NpcIds);
        var second = NpcAppearanceProfileRandomizer.CreateProfiles("harborline-save-2", NpcIds.Reverse());

        Assert.Equal(
            string.Join("|", first.OrderBy(item => item.Key).Select(item => item.Value.Signature)),
            string.Join("|", second.OrderBy(item => item.Key).Select(item => item.Value.Signature)));
    }

    [Fact]
    public void DifferentSaveSeedChangesAtLeastSomeAppearanceProfiles()
    {
        var first = NpcAppearanceProfileRandomizer.CreateProfiles("save-a", NpcIds);
        var second = NpcAppearanceProfileRandomizer.CreateProfiles("save-b", NpcIds);

        Assert.NotEqual(
            string.Join("|", first.OrderBy(item => item.Key).Select(item => item.Value.Signature)),
            string.Join("|", second.OrderBy(item => item.Key).Select(item => item.Value.Signature)));
    }

    [Fact]
    public void ProfilesUseOnlyKnownNativeAssetPaths()
    {
        var profiles = NpcAppearanceProfileRandomizer.CreateProfiles("harborline-save-3", NpcIds);

        Assert.All(profiles.Values, profile =>
        {
            Assert.StartsWith("Avatar/Hair/", profile.HairStylePath, StringComparison.Ordinal);
            Assert.StartsWith("Avatar/Layers/Face/", profile.FaceExpressionPath, StringComparison.Ordinal);
            Assert.StartsWith("Avatar/Layers/Top/", profile.ShirtLayerPath, StringComparison.Ordinal);
            Assert.StartsWith("Avatar/Layers/Bottom/", profile.PantsLayerPath, StringComparison.Ordinal);
            Assert.StartsWith("Avatar/Accessories/Feet/", profile.ShoesLayerPath, StringComparison.Ordinal);
            Assert.InRange(profile.Height, 0.86f, 1.14f);
            Assert.InRange(profile.Weight, 0.18f, 0.88f);
        });
    }

    [Fact]
    public void DuplicateAndBlankNpcIdsAreRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            NpcAppearanceProfileRandomizer.CreateProfiles(
                "save-seed",
                new[] { "sime.npc.harborline.same", "sime.npc.harborline.same" }));

        Assert.Throws<ArgumentException>(() =>
            NpcAppearanceProfileRandomizer.CreateProfiles(
                "save-seed",
                new[] { " " }));
    }
}
