namespace DesfudenciFy.Application.Common;

public static class VehicleEconomics
{
    public static decimal CalculateTotalExpenses(IEnumerable<decimal> expenseAmounts)
    {
        return Math.Round(expenseAmounts.Sum(), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Variação FIPE = FIPE − (valor pago + custos).
    /// Positivo: valor de mercado acima do custo; negativo: abaixo.
    /// </summary>
    public static decimal CalculateFipeVariance(decimal paidValue, decimal totalExpenses, decimal fipeValue)
    {
        return Math.Round(fipeValue - (paidValue + totalExpenses), 2, MidpointRounding.AwayFromZero);
    }
}
