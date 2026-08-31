namespace DesfudenciFy.Application.Common;

public static class VehicleEconomics
{
    public static decimal CalculateTotalExpenses(IEnumerable<decimal> expenseAmounts)
    {
        return Math.Round(expenseAmounts.Sum(), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Variação FIPE = valor pago + custos − FIPE.
    /// Positivo: custo acima da tabela; negativo: abaixo.
    /// </summary>
    public static decimal CalculateFipeVariance(decimal paidValue, decimal totalExpenses, decimal fipeValue)
    {
        return Math.Round(paidValue + totalExpenses - fipeValue, 2, MidpointRounding.AwayFromZero);
    }
}
