namespace DesfudenciFy.Application.Common;

public static class ProfitDistribution
{
    public sealed record Share(Guid? ReserveId, decimal Amount, decimal Proportion, decimal ProfitShare);

    /// <summary>
    /// Distributes investment profit proportionally by each source amount over the original invested total.
    /// </summary>
    public static IReadOnlyList<Share> Distribute(
        decimal startAmount,
        decimal currentAmount,
        IEnumerable<(Guid? ReserveId, decimal Amount)> sources)
    {
        var sourceList = sources.ToList();
        var profit = currentAmount - startAmount;
        if (profit <= 0 || startAmount <= 0 || sourceList.Count == 0)
        {
            return Array.Empty<Share>();
        }

        return sourceList
            .Select(source =>
            {
                var proportion = source.Amount / startAmount;
                var profitShare = Math.Round(proportion * profit, 2);
                return new Share(source.ReserveId, source.Amount, proportion, profitShare);
            })
            .Where(share => share.ProfitShare > 0)
            .ToList();
    }
}
