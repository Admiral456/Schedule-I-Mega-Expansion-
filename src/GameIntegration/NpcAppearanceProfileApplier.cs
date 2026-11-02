using System;
using MelonLoader;
using ScheduleIMegaExpansion.Core;
using S1API.Entities;
using UnityEngine;

namespace ScheduleIMegaExpansion.GameIntegration;

/// <summary>
/// Applies a saved, deterministic Harborline appearance profile through S1API's
/// native NPCAppearance builder. No independent Unity avatar or mesh is created.
/// The same saved profile must be supplied on every peer.
/// </summary>
public static class NpcAppearanceProfileApplier
{
    public static bool TryApply(NPCAppearance appearance, NpcAppearanceProfile profile)
    {
        if (appearance is null)
            throw new ArgumentNullException(nameof(appearance));
        if (profile is null)
            throw new ArgumentNullException(nameof(profile));

        try
        {
            NPCAppearance builder = appearance
                .Set<CustomizationFields.Gender>(profile.Gender)
                .Set<CustomizationFields.Height>(profile.Height)
                .Set<CustomizationFields.Weight>(profile.Weight)
                .Set<CustomizationFields.SkinColor>(
                    new Color32(profile.SkinColor.R, profile.SkinColor.G, profile.SkinColor.B, 255))
                .Set<CustomizationFields.EyeBallTint>(ToColor(profile.EyeColor))
                .Set<CustomizationFields.HairColor>(ToColor(profile.HairColor))
                .Set<CustomizationFields.HairStyle>(profile.HairStylePath)
                .WithFaceLayer<FaceLayerFields.Face>(profile.FaceExpressionPath, Color.white)
                .WithBodyLayer<BodyLayerFields.Shirts>(profile.ShirtLayerPath, ToColor(profile.ShirtColor))
                .WithBodyLayer<BodyLayerFields.Pants>(profile.PantsLayerPath, ToColor(profile.PantsColor))
                .WithAccessoryLayer<AccessoryFields.Feet>(profile.ShoesLayerPath, ToColor(profile.ShoesColor));

            if (!string.IsNullOrWhiteSpace(profile.FacialHairPath))
            {
                builder = builder.WithFaceLayer<FaceLayerFields.FacialHair>(
                    profile.FacialHairPath,
                    ToColor(profile.HairColor));
            }

            if (!string.IsNullOrWhiteSpace(profile.HeadAccessoryPath))
            {
                builder = builder.WithAccessoryLayer<AccessoryFields.Head>(
                    profile.HeadAccessoryPath,
                    ToColor(profile.HeadAccessoryColor));
            }

            builder.Build();
            return true;
        }
        catch (Exception exception)
        {
            MelonLogger.Error(
                "[MegaExpansion] Could not apply saved Harborline appearance for " +
                profile.NpcId + ": " + exception.Message);

            // Do not roll a local random avatar here: in co-op that could create
            // different host/client appearances. Keep the native/default avatar.
            try
            {
                appearance.Build();
            }
            catch (Exception buildException)
            {
                MelonLogger.Error(
                    "[MegaExpansion] Native fallback avatar build failed for " +
                    profile.NpcId + ": " + buildException.Message);
            }

            return false;
        }
    }

    private static Color ToColor(NpcRgbColor color) =>
        new(color.R / 255f, color.G / 255f, color.B / 255f, 1f);
}
