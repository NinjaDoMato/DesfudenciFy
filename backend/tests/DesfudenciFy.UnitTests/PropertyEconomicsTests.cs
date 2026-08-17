using DesfudenciFy.Application.Common;

namespace DesfudenciFy.UnitTests;

public class PropertyEconomicsTests
{
    [Fact]
    public void Should_calculate_cost_as_financing_plus_expenses()
    {
        var cost = PropertyEconomics.CalculateCost(
            initialFinancingAmount: 200_000m,
            expenseAmounts: [1_500m, 800.50m]);

        Assert.Equal(202_300.50m, cost);
    }

    [Fact]
    public void Should_calculate_cost_with_no_expenses_as_financing_only()
    {
        var cost = PropertyEconomics.CalculateCost(150_000m, []);

        Assert.Equal(150_000m, cost);
    }

    [Fact]
    public void Should_calculate_return_as_appraised_minus_cost_plus_rents()
    {
        var cost = PropertyEconomics.CalculateCost(200_000m, [5_000m]);
        var result = PropertyEconomics.CalculateReturn(
            appraisedValue: 250_000m,
            propertyCost: cost,
            rentPaymentAmounts: [1_200m, 1_200m]);

        Assert.Equal(47_400m, result);
    }

    [Fact]
    public void Should_allow_negative_return_when_cost_exceeds_appraised_and_rents()
    {
        var result = PropertyEconomics.CalculateReturn(
            appraisedValue: 100_000m,
            propertyCost: 120_000m,
            rentPaymentAmounts: [500m]);

        Assert.Equal(-19_500m, result);
    }

    [Fact]
    public void Should_round_cost_and_return_away_from_zero()
    {
        var cost = PropertyEconomics.CalculateCost(100m, [0.004m]);
        Assert.Equal(100.00m, cost);

        var costRoundedUp = PropertyEconomics.CalculateCost(100m, [0.005m]);
        Assert.Equal(100.01m, costRoundedUp);

        var ret = PropertyEconomics.CalculateReturn(100.005m, 50m, []);
        Assert.Equal(50.01m, ret);
    }

    [Fact]
    public void Should_use_sale_amount_as_realization_price_for_return()
    {
        var cost = PropertyEconomics.CalculateCost(200_000m, [5_000m]);
        var result = PropertyEconomics.CalculateReturn(
            appraisedValue: 280_000m,
            propertyCost: cost,
            rentPaymentAmounts: [1_200m]);

        Assert.Equal(76_200m, result);
    }
}
