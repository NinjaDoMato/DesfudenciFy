using DesfudenciFy.Application.Common;

namespace DesfudenciFy.UnitTests;

public class VehicleEconomicsTests
{
    [Fact]
    public void Should_calculate_total_expenses_as_sum_of_costs()
    {
        var total = VehicleEconomics.CalculateTotalExpenses([1_500m, 800.50m]);

        Assert.Equal(2_300.50m, total);
    }

    [Fact]
    public void Should_calculate_total_expenses_as_zero_when_empty()
    {
        var total = VehicleEconomics.CalculateTotalExpenses([]);

        Assert.Equal(0m, total);
    }

    [Fact]
    public void Should_calculate_fipe_variance_as_fipe_minus_paid_and_expenses()
    {
        var totalExpenses = VehicleEconomics.CalculateTotalExpenses([2_000m, 1_500m]);
        var variance = VehicleEconomics.CalculateFipeVariance(
            paidValue: 80_000m,
            totalExpenses: totalExpenses,
            fipeValue: 75_000m);

        Assert.Equal(-8_500m, variance);
    }

    [Fact]
    public void Should_allow_positive_fipe_variance_when_fipe_exceeds_paid_plus_costs()
    {
        var variance = VehicleEconomics.CalculateFipeVariance(
            paidValue: 50_000m,
            totalExpenses: 2_000m,
            fipeValue: 60_000m);

        Assert.Equal(8_000m, variance);
    }

    [Fact]
    public void Should_round_totals_away_from_zero()
    {
        var total = VehicleEconomics.CalculateTotalExpenses([0.004m]);
        Assert.Equal(0.00m, total);

        var totalRoundedUp = VehicleEconomics.CalculateTotalExpenses([0.005m]);
        Assert.Equal(0.01m, totalRoundedUp);

        var variance = VehicleEconomics.CalculateFipeVariance(100.005m, 0m, 50m);
        Assert.Equal(-50.01m, variance);
    }
}
