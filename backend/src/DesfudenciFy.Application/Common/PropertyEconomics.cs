namespace DesfudenciFy.Application.Common;

public static class PropertyEconomics
{
    public static decimal CalculateCost(decimal initialFinancingAmount, IEnumerable<decimal> expenseAmounts)
    {
        var expenses = expenseAmounts.Sum();
        return Math.Round(initialFinancingAmount + expenses, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateReturn(decimal appraisedValue, decimal propertyCost, IEnumerable<decimal> rentPaymentAmounts)
    {
        var rents = rentPaymentAmounts.Sum();
        return Math.Round(appraisedValue - propertyCost + rents, 2, MidpointRounding.AwayFromZero);
    }
}
