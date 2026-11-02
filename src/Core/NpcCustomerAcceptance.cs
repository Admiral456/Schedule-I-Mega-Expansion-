namespace ScheduleIMegaExpansion.Core;

public enum CustomerAcceptanceReason
{
    Accepted,
    RejectedProduct,
    OverBudget,
    LowPreference,
    InvalidProfile,
    InvalidOffer,
    PriceOverflow
}

public sealed record NpcCustomerProfile(
    string NpcId,
    long BudgetLimit,
    double PriceSensitivity,
    IReadOnlyCollection<string> PreferredProductIds,
    IReadOnlyCollection<string> NeutralProductIds,
    IReadOnlyCollection<string> RejectedProductIds);

public sealed record CustomerOrderLine(
    string ProductId,
    int Quantity,
    long UnitPrice);

public sealed record CustomerOrderOffer(
    IReadOnlyCollection<CustomerOrderLine> Lines);

public sealed record CustomerAcceptanceResult(
    bool Accepted,
    CustomerAcceptanceReason Reason,
    int Score,
    long TotalPrice,
    int PreferredProductCount,
    int NeutralProductCount,
    string? RejectedProductId = null);

/// <summary>
/// Deterministic NPC order evaluation. Each profile supplies its own budget and
/// product preferences; an optional expansion-specific standing modifier is provided by the caller. It is not the native NPC relationship.
/// This class contains no game or Steam dependencies.
/// </summary>
public static class NpcCustomerAcceptance
{
    public const int DefaultAcceptanceThreshold = 50;

    private const int BaseScore = 55;
    private const int PreferredProductBonus = 15;
    private const int MaximumPreferredBonus = 30;
    private const int NeutralProductBonus = 4;
    private const int MaximumNeutralBonus = 8;
    private const double MaximumPricePenalty = 35.0;
    private const double ReputationScale = 0.10;

    public static CustomerAcceptanceResult Evaluate(
        NpcCustomerProfile? profile,
        CustomerOrderOffer? offer,
        int expansionStanding)
    {
        if (!IsValidProfile(profile))
            return Invalid(CustomerAcceptanceReason.InvalidProfile);

        if (offer?.Lines is null || offer.Lines.Count == 0)
            return Invalid(CustomerAcceptanceReason.InvalidOffer);

        var lineProductIds = new HashSet<string>(StringComparer.Ordinal);
        long totalPrice = 0;

        foreach (CustomerOrderLine? line in offer.Lines)
        {
            if (line is null ||
                string.IsNullOrWhiteSpace(line.ProductId) ||
                line.Quantity <= 0 ||
                line.UnitPrice < 0)
            {
                return Invalid(CustomerAcceptanceReason.InvalidOffer);
            }

            lineProductIds.Add(line.ProductId);

            try
            {
                long lineTotal = checked(line.UnitPrice * line.Quantity);
                totalPrice = checked(totalPrice + lineTotal);
            }
            catch (OverflowException)
            {
                return Invalid(CustomerAcceptanceReason.PriceOverflow);
            }
        }

        var preferred = new HashSet<string>(profile!.PreferredProductIds, StringComparer.Ordinal);
        var neutral = new HashSet<string>(profile.NeutralProductIds, StringComparer.Ordinal);
        var rejected = new HashSet<string>(profile.RejectedProductIds, StringComparer.Ordinal);

        string? rejectedProduct = lineProductIds.FirstOrDefault(rejected.Contains);
        if (rejectedProduct is not null)
        {
            return new CustomerAcceptanceResult(
                false,
                CustomerAcceptanceReason.RejectedProduct,
                0,
                totalPrice,
                0,
                0,
                rejectedProduct);
        }

        if (totalPrice > profile.BudgetLimit)
        {
            return new CustomerAcceptanceResult(
                false,
                CustomerAcceptanceReason.OverBudget,
                0,
                totalPrice,
                0,
                0);
        }

        int preferredCount = lineProductIds.Count(preferred.Contains);
        int neutralCount = lineProductIds.Count(neutral.Contains);

        int preferredBonus = Math.Min(MaximumPreferredBonus, preferredCount * PreferredProductBonus);
        int neutralBonus = Math.Min(MaximumNeutralBonus, neutralCount * NeutralProductBonus);

        double budgetUse = (double)totalPrice / profile.BudgetLimit;
        int pricePenalty = (int)Math.Round(
            profile.PriceSensitivity * budgetUse * MaximumPricePenalty,
            MidpointRounding.AwayFromZero);

        int clampedStanding = Math.Clamp(expansionStanding, ReputationLedger.Minimum, ReputationLedger.Maximum);
        int standingModifier = (int)Math.Round(
            clampedStanding * ReputationScale,
            MidpointRounding.AwayFromZero);

        int score = Math.Clamp(
            BaseScore + preferredBonus + neutralBonus - pricePenalty + standingModifier,
            0,
            100);

        bool accepted = score >= DefaultAcceptanceThreshold;

        return new CustomerAcceptanceResult(
            accepted,
            accepted ? CustomerAcceptanceReason.Accepted : CustomerAcceptanceReason.LowPreference,
            score,
            totalPrice,
            preferredCount,
            neutralCount);
    }

    private static bool IsValidProfile(NpcCustomerProfile? profile)
    {
        if (profile is null ||
            !StableId.HasPrefix(profile.NpcId, StableId.NpcPrefix) ||
            profile.BudgetLimit <= 0 ||
            !double.IsFinite(profile.PriceSensitivity) ||
            profile.PriceSensitivity < 0 ||
            profile.PriceSensitivity > 1 ||
            profile.PreferredProductIds is null ||
            profile.NeutralProductIds is null ||
            profile.RejectedProductIds is null)
        {
            return false;
        }

        var preferred = new HashSet<string>(profile.PreferredProductIds, StringComparer.Ordinal);
        var neutral = new HashSet<string>(profile.NeutralProductIds, StringComparer.Ordinal);
        var rejected = new HashSet<string>(profile.RejectedProductIds, StringComparer.Ordinal);

        if (preferred.Any(string.IsNullOrWhiteSpace) ||
            neutral.Any(string.IsNullOrWhiteSpace) ||
            rejected.Any(string.IsNullOrWhiteSpace))
        {
            return false;
        }

        return !preferred.Overlaps(neutral) &&
               !preferred.Overlaps(rejected) &&
               !neutral.Overlaps(rejected);
    }

    private static CustomerAcceptanceResult Invalid(CustomerAcceptanceReason reason) =>
        new(false, reason, 0, 0, 0, 0);
}
