using DesfudenciFy.Application.Common;

namespace DesfudenciFy.UnitTests;

public class ProfitDistributionTests
{
    [Fact]
    public void Should_distribute_profit_proportionally_across_source_reserves()
    {
        var reserveA = Guid.NewGuid();
        var reserveB = Guid.NewGuid();

        var shares = ProfitDistribution.Distribute(
            startAmount: 1000m,
            currentAmount: 1200m,
            sources:
            [
                (reserveA, 700m),
                (reserveB, 300m),
            ]);

        Assert.Equal(2, shares.Count);
        Assert.Equal(140m, shares[0].ProfitShare);
        Assert.Equal(60m, shares[1].ProfitShare);
        Assert.Equal(200m, shares.Sum(s => s.ProfitShare));
        Assert.Equal(0.7m, shares[0].Proportion);
        Assert.Equal(0.3m, shares[1].Proportion);
    }

    [Fact]
    public void Should_include_free_balance_source_in_distribution()
    {
        var reserveId = Guid.NewGuid();

        var shares = ProfitDistribution.Distribute(
            startAmount: 1000m,
            currentAmount: 1100m,
            sources:
            [
                (null, 400m),
                (reserveId, 600m),
            ]);

        Assert.Equal(2, shares.Count);
        Assert.Null(shares[0].ReserveId);
        Assert.Equal(40m, shares[0].ProfitShare);
        Assert.Equal(reserveId, shares[1].ReserveId);
        Assert.Equal(60m, shares[1].ProfitShare);
    }

    [Fact]
    public void Should_return_empty_when_there_is_no_profit()
    {
        var shares = ProfitDistribution.Distribute(
            startAmount: 1000m,
            currentAmount: 1000m,
            sources: [(Guid.NewGuid(), 1000m)]);

        Assert.Empty(shares);
    }

    [Fact]
    public void Should_return_empty_when_current_is_below_start()
    {
        var shares = ProfitDistribution.Distribute(
            startAmount: 1000m,
            currentAmount: 900m,
            sources: [(Guid.NewGuid(), 1000m)]);

        Assert.Empty(shares);
    }

    [Fact]
    public void Should_round_shares_to_two_decimal_places()
    {
        var shares = ProfitDistribution.Distribute(
            startAmount: 300m,
            currentAmount: 310m,
            sources:
            [
                (Guid.NewGuid(), 100m),
                (Guid.NewGuid(), 100m),
                (Guid.NewGuid(), 100m),
            ]);

        Assert.All(shares, share => Assert.Equal(3.33m, share.ProfitShare));
        Assert.Equal(9.99m, shares.Sum(s => s.ProfitShare));
    }
}
