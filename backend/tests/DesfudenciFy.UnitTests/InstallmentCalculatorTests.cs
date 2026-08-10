using DesfudenciFy.Application.Common;

namespace DesfudenciFy.UnitTests;

public class InstallmentCalculatorTests
{
    [Fact]
    public void Should_split_exact_total_into_equal_installments()
    {
        var amounts = InstallmentCalculator.SplitTotal(1000m, 4);

        Assert.Equal(4, amounts.Count);
        Assert.All(amounts, amount => Assert.Equal(250m, amount));
        Assert.Equal(1000m, amounts.Sum());
    }

    [Fact]
    public void Should_adjust_last_installment_for_rounding_difference()
    {
        var amounts = InstallmentCalculator.SplitTotal(100m, 3);

        Assert.Equal(3, amounts.Count);
        Assert.Equal(33.33m, amounts[0]);
        Assert.Equal(33.33m, amounts[1]);
        Assert.Equal(33.34m, amounts[2]);
        Assert.Equal(100m, amounts.Sum());
    }

    [Fact]
    public void Should_reject_non_positive_installment_count()
    {
        var exception = Assert.Throws<AppException>(() => InstallmentCalculator.SplitTotal(100m, 0));
        Assert.Equal("A quantidade de parcelas deve ser maior que zero.", exception.Message);
    }
}
