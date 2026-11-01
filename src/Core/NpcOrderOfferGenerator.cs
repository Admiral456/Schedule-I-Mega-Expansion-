using System.Security.Cryptography;
using System.Text;

namespace ScheduleIMegaExpansion.Core;

public sealed record NpcOrderProductStock(string ProductId, long UnitPrice, int AvailableStock);

public enum NpcOrderGenerationFailure
{
    None,
    InvalidProfile,
    InvalidArguments,
    NoEligibleProduct,
    NoAcceptedOffer
}

public sealed record NpcOrderGenerationResult(
    bool Success,
    NpcOrderGenerationFailure Failure,
    CustomerOrderOffer? Offer,
    CustomerAcceptanceResult? Evaluation)
{
    public long TotalPrice => Evaluation?.TotalPrice ?? 0;
    public int Score => Evaluation?.Score ?? 0;
}

/// <summary>
/// Creates a deterministic order offer from the products a resident actually likes or tolerates.
/// It has no Unity/Steam dependencies; the multiplayer host must persist accepted offers.
/// </summary>
public static class NpcOrderOfferGenerator
{
    public const int DefaultMaximumLines = 3;
    public const int DefaultMaximumUnitsPerProduct = 3;

    public static NpcOrderGenerationResult Generate(
        NpcCustomerProfile? profile,
        IReadOnlyCollection<NpcOrderProductStock>? products,
        long worldInterval,
        int playerReputation,
        int maximumLines = DefaultMaximumLines,
        int maximumUnitsPerProduct = DefaultMaximumUnitsPerProduct)
    {
        if (!IsValidProfile(profile))
            return Failure(NpcOrderGenerationFailure.InvalidProfile);

        if (products is null || worldInterval < 0 || maximumLines < 1 || maximumLines > 5 ||
            maximumUnitsPerProduct < 1 || maximumUnitsPerProduct > 10)
        {
            return Failure(NpcOrderGenerationFailure.InvalidArguments);
        }

        var preferred = new HashSet<string>(profile!.PreferredProductIds, StringComparer.Ordinal);
        var neutral = new HashSet<string>(profile.NeutralProductIds, StringComparer.Ordinal);
        var rejected = new HashSet<string>(profile.RejectedProductIds, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        var eligible = new List<(NpcOrderProductStock Product, int MaximumQuantity, int PreferenceTier, string TieBreak)>();
        foreach (NpcOrderProductStock? product in products)
        {
            // A malformed inventory snapshot must fail closed. Zero stock is valid and is skipped.
            if (product is null || string.IsNullOrWhiteSpace(product.ProductId) ||
                product.UnitPrice <= 0 || product.AvailableStock < 0 ||
                !seen.Add(product.ProductId))
            {
                return Failure(NpcOrderGenerationFailure.InvalidArguments);
            }

            if (product.AvailableStock == 0 || rejected.Contains(product.ProductId))
                continue;

            int preferenceTier = preferred.Contains(product.ProductId) ? 0 :
                neutral.Contains(product.ProductId) ? 1 : 2;
            if (preferenceTier == 2)
                continue;

            long affordableUnits = profile.BudgetLimit / product.UnitPrice;
            int maximumQuantity = (int)Math.Min(
                maximumUnitsPerProduct,
                Math.Min(product.AvailableStock, Math.Min(int.MaxValue, affordableUnits)));
            if (maximumQuantity <= 0)
                continue;

            eligible.Add((product, maximumQuantity, preferenceTier, CreateTieBreak(profile.NpcId, worldInterval, product.ProductId)));
        }

        if (eligible.Count == 0)
            return Failure(NpcOrderGenerationFailure.NoEligibleProduct);

        // Price and preference are evaluated together before selecting the first line.
        var acceptable = new List<(NpcOrderProductStock Product, int MaximumQuantity, int PreferenceTier, string TieBreak, CustomerAcceptanceResult Evaluation)>();
        foreach (var candidate in eligible)
        {
            var oneLineOffer = new CustomerOrderOffer(new[]
            {
                new CustomerOrderLine(candidate.Product.ProductId, 1, candidate.Product.UnitPrice)
            });
            CustomerAcceptanceResult evaluation = NpcCustomerAcceptance.Evaluate(profile, oneLineOffer, playerReputation);
            if (evaluation.Accepted)
            {
                acceptable.Add((candidate.Product, candidate.MaximumQuantity, candidate.PreferenceTier, candidate.TieBreak, evaluation));
            }
        }

        if (acceptable.Count == 0)
            return Failure(NpcOrderGenerationFailure.NoAcceptedOffer);

        var ranked = acceptable
            .OrderByDescending(candidate => candidate.Evaluation.Score)
            .ThenBy(candidate => candidate.PreferenceTier)
            .ThenBy(candidate => candidate.TieBreak, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Product.ProductId, StringComparer.Ordinal)
            .ToArray();

        var lines = new List<CustomerOrderLine>();
        CustomerAcceptanceResult? currentEvaluation = null;

        foreach (var candidate in ranked)
        {
            if (lines.Count >= maximumLines)
                break;

            int quantity = 1;
            var proposed = new List<CustomerOrderLine>(lines)
            {
                new(candidate.Product.ProductId, quantity, candidate.Product.UnitPrice)
            };

            CustomerAcceptanceResult evaluation = NpcCustomerAcceptance.Evaluate(
                profile, new CustomerOrderOffer(proposed), playerReputation);
            if (!evaluation.Accepted)
                continue;

            // Increase each selected product only while the resident still accepts the total.
            while (quantity < candidate.MaximumQuantity)
            {
                quantity++;
                proposed[^1] = proposed[^1] with { Quantity = quantity };
                CustomerAcceptanceResult larger = NpcCustomerAcceptance.Evaluate(
                    profile, new CustomerOrderOffer(proposed), playerReputation);
                if (!larger.Accepted)
                {
                    quantity--;
                    proposed[^1] = proposed[^1] with { Quantity = quantity };
                    break;
                }
                evaluation = larger;
            }

            lines = proposed;
            currentEvaluation = evaluation;
        }

        if (lines.Count == 0 || currentEvaluation is null)
            return Failure(NpcOrderGenerationFailure.NoAcceptedOffer);

        return new NpcOrderGenerationResult(
            true,
            NpcOrderGenerationFailure.None,
            new CustomerOrderOffer(Array.AsReadOnly(lines.ToArray())),
            currentEvaluation);
    }

    private static bool IsValidProfile(NpcCustomerProfile? profile)
    {
        if (profile is null ||
            !StableId.HasPrefix(profile.NpcId, StableId.NpcPrefix) ||
            profile.BudgetLimit <= 0 ||
            !double.IsFinite(profile.PriceSensitivity) ||
            profile.PriceSensitivity < 0 || profile.PriceSensitivity > 1 ||
            profile.PreferredProductIds is null || profile.NeutralProductIds is null || profile.RejectedProductIds is null)
        {
            return false;
        }

        var preferred = new HashSet<string>(profile.PreferredProductIds, StringComparer.Ordinal);
        var neutral = new HashSet<string>(profile.NeutralProductIds, StringComparer.Ordinal);
        var rejected = new HashSet<string>(profile.RejectedProductIds, StringComparer.Ordinal);
        return preferred.Count == profile.PreferredProductIds.Count &&
               neutral.Count == profile.NeutralProductIds.Count &&
               rejected.Count == profile.RejectedProductIds.Count &&
               !preferred.Overlaps(neutral) && !preferred.Overlaps(rejected) && !neutral.Overlaps(rejected) &&
               !preferred.Any(string.IsNullOrWhiteSpace) && !neutral.Any(string.IsNullOrWhiteSpace) && !rejected.Any(string.IsNullOrWhiteSpace);
    }

    private static string CreateTieBreak(string npcId, long worldInterval, string productId)
    {
        byte[] input = Encoding.UTF8.GetBytes(npcId + "|" + worldInterval.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + productId);
        return Convert.ToHexString(SHA256.HashData(input));
    }

    private static NpcOrderGenerationResult Failure(NpcOrderGenerationFailure failure) =>
        new(false, failure, null, null);
}