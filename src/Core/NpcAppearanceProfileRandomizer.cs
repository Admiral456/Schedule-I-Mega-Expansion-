using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ScheduleIMegaExpansion.Core;

public sealed record NpcRgbColor(byte R, byte G, byte B);

/// <summary>
/// A complete, deterministic appearance variation profile made only from native
/// Schedule I avatar fields and documented native clothing/accessory layer paths.
/// The game-facing adapter applies it through S1API NPCAppearance.
/// </summary>
public sealed record NpcAppearanceProfile(
    string NpcId,
    float Gender,
    float Height,
    float Weight,
    NpcRgbColor SkinColor,
    NpcRgbColor EyeColor,
    NpcRgbColor HairColor,
    string HairStylePath,
    string FaceExpressionPath,
    string? FacialHairPath,
    string ShirtLayerPath,
    NpcRgbColor ShirtColor,
    string PantsLayerPath,
    NpcRgbColor PantsColor,
    string ShoesLayerPath,
    NpcRgbColor ShoesColor,
    string? HeadAccessoryPath,
    NpcRgbColor HeadAccessoryColor)
{
    public string Signature => string.Join("|", new[]
    {
        Gender.ToString("0.00", CultureInfo.InvariantCulture),
        Height.ToString("0.00", CultureInfo.InvariantCulture),
        Weight.ToString("0.00", CultureInfo.InvariantCulture),
        ColorSignature(SkinColor),
        ColorSignature(EyeColor),
        ColorSignature(HairColor),
        HairStylePath,
        FaceExpressionPath,
        FacialHairPath ?? "-",
        ShirtLayerPath,
        ColorSignature(ShirtColor),
        PantsLayerPath,
        ColorSignature(PantsColor),
        ShoesLayerPath,
        ColorSignature(ShoesColor),
        HeadAccessoryPath is null ? "-" : HeadAccessoryPath + ":" + ColorSignature(HeadAccessoryColor)
    });

    private static string ColorSignature(NpcRgbColor color) =>
        $"{color.R:X2}{color.G:X2}{color.B:X2}";
}

/// <summary>
/// Produces unique, save-stable visual profiles for expansion NPCs. The adapter
/// first calls the native random appearance routine, then applies these values
/// through NPCAppearance so that the generated residents cannot share an identical
/// expansion-owned appearance descriptor.
/// </summary>
public static class NpcAppearanceProfileRandomizer
{
    private const string AlgorithmId = "sime.npc-appearance.v1";

    private static readonly float[] Heights = { 0.82f, 0.88f, 0.94f, 1.00f, 1.04f, 1.08f, 1.10f };
    private static readonly float[] Weights = { 0.18f, 0.28f, 0.38f, 0.48f, 0.58f, 0.68f, 0.78f, 0.88f };
    private static readonly string[] HairStyles =
    {
        "Avatar/Hair/Spiky/Spiky",
        "Avatar/Hair/Long/Long"
    };
    private static readonly string[] FaceExpressions =
    {
        "Avatar/Layers/Face/Face_Neutral",
        "Avatar/Layers/Face/Face_Happy",
        "Avatar/Layers/Face/Face_Sad",
        "Avatar/Layers/Face/Face_Agitated"
    };
    private static readonly string[] FacialHair =
    {
        "Avatar/Layers/Face/Beard",
        "Avatar/Layers/Face/Mustache",
        "Avatar/Layers/Face/Goatee"
    };
    private static readonly string[] Shirts =
    {
        "Avatar/Layers/Top/T-Shirt",
        "Avatar/Layers/Top/Shirt",
        "Avatar/Layers/Top/RolledButtonUp",
        "Avatar/Layers/Top/Hoodie"
    };
    private static readonly string[] Pants =
    {
        "Avatar/Layers/Bottom/Jeans",
        "Avatar/Layers/Bottom/Jorts",
        "Avatar/Layers/Bottom/Shorts",
        "Avatar/Layers/Bottom/Sweatpants"
    };
    private static readonly string[] Shoes =
    {
        "Avatar/Accessories/Feet/Sneakers/Sneakers",
        "Avatar/Accessories/Feet/Boots",
        "Avatar/Accessories/Feet/Sandals"
    };
    private static readonly string?[] HeadAccessories =
    {
        null,
        "Avatar/Accessories/Head/BaseballCap",
        "Avatar/Accessories/Head/Beanie",
        "Avatar/Accessories/Head/Hat"
    };
    private static readonly NpcRgbColor[] SkinColors =
    {
        new(245, 210, 180), new(224, 181, 143), new(195, 145, 105),
        new(160, 112, 79), new(125, 83, 58), new(94, 61, 45)
    };
    private static readonly NpcRgbColor[] HairColors =
    {
        new(28, 22, 18), new(65, 40, 25), new(110, 72, 42), new(185, 135, 75),
        new(150, 52, 35), new(90, 90, 90), new(205, 195, 175), new(235, 225, 210)
    };
    private static readonly NpcRgbColor[] EyeColors =
    {
        new(45, 30, 20), new(95, 65, 35), new(55, 105, 145), new(55, 115, 75), new(115, 125, 135)
    };
    private static readonly NpcRgbColor[] ClothingColors =
    {
        new(30, 42, 60), new(45, 75, 105), new(125, 45, 40), new(170, 125, 50),
        new(45, 90, 65), new(100, 100, 100), new(205, 205, 195), new(70, 50, 85),
        new(145, 85, 65), new(25, 25, 25)
    };

    public static IReadOnlyDictionary<string, NpcAppearanceProfile> CreateProfiles(
        string saveSeed,
        IEnumerable<string> npcIds)
    {
        if (string.IsNullOrWhiteSpace(saveSeed))
            throw new ArgumentException("A save-scoped appearance seed is required.", nameof(saveSeed));
        if (npcIds is null)
            throw new ArgumentNullException(nameof(npcIds));

        string[] ids = npcIds
            .Select(value => value?.Trim() ?? string.Empty)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        if (ids.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("NPC IDs must be non-empty.", nameof(npcIds));
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
            throw new ArgumentException("NPC IDs must be unique.", nameof(npcIds));

        var profiles = new Dictionary<string, NpcAppearanceProfile>(StringComparer.Ordinal);
        var signatures = new HashSet<string>(StringComparer.Ordinal);

        foreach (string npcId in ids)
        {
            bool assigned = false;
            for (int salt = 0; salt < 8192; salt++)
            {
                NpcAppearanceProfile candidate = BuildCandidate(saveSeed, npcId, salt);
                if (!signatures.Add(candidate.Signature))
                    continue;

                profiles.Add(npcId, candidate);
                assigned = true;
                break;
            }

            if (!assigned)
            {
                throw new InvalidOperationException(
                    $"Could not generate a unique appearance descriptor for '{npcId}'.");
            }
        }

        return profiles;
    }

    private static NpcAppearanceProfile BuildCandidate(string seed, string npcId, int salt)
    {
        int genderIndex = Pick(seed, npcId, salt, "gender", 2);
        float gender = genderIndex == 0 ? 0.0f : 1.0f;

        string? facialHairPath = genderIndex == 0
            ? FacialHair[Pick(seed, npcId, salt, "facial-hair", FacialHair.Length)]
            : null;

        return new NpcAppearanceProfile(
            npcId,
            gender,
            Heights[Pick(seed, npcId, salt, "height", Heights.Length)],
            Weights[Pick(seed, npcId, salt, "weight", Weights.Length)],
            SkinColors[Pick(seed, npcId, salt, "skin", SkinColors.Length)],
            EyeColors[Pick(seed, npcId, salt, "eyes", EyeColors.Length)],
            HairColors[Pick(seed, npcId, salt, "hair-colour", HairColors.Length)],
            HairStyles[Pick(seed, npcId, salt, "hair-style", HairStyles.Length)],
            FaceExpressions[Pick(seed, npcId, salt, "expression", FaceExpressions.Length)],
            facialHairPath,
            Shirts[Pick(seed, npcId, salt, "shirt", Shirts.Length)],
            ClothingColors[Pick(seed, npcId, salt, "shirt-colour", ClothingColors.Length)],
            Pants[Pick(seed, npcId, salt, "pants", Pants.Length)],
            ClothingColors[Pick(seed, npcId, salt, "pants-colour", ClothingColors.Length)],
            Shoes[Pick(seed, npcId, salt, "shoes", Shoes.Length)],
            ClothingColors[Pick(seed, npcId, salt, "shoes-colour", ClothingColors.Length)],
            HeadAccessories[Pick(seed, npcId, salt, "head-accessory", HeadAccessories.Length)],
            ClothingColors[Pick(seed, npcId, salt, "accessory-colour", ClothingColors.Length)]);
    }

    private static int Pick(string seed, string npcId, int salt, string channel, int count)
    {
        string input = string.Join("|", AlgorithmId, seed, npcId, salt.ToString(CultureInfo.InvariantCulture), channel);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(hash.AsSpan(0, sizeof(uint)));
        return (int)(value % (uint)count);
    }
}
