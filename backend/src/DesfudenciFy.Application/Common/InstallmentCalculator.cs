namespace DesfudenciFy.Application.Common;

public static class InstallmentCalculator
{
    /// <summary>
    /// Splits a total into N installments of equal amount (2 decimal places),
    /// adjusting the last installment so the sum matches the total exactly.
    /// </summary>
    public static IReadOnlyList<decimal> SplitTotal(decimal totalAmount, int installmentCount)
    {
        if (installmentCount <= 0)
        {
            throw new AppException("A quantidade de parcelas deve ser maior que zero.");
        }

        var installmentAmount = Math.Round(totalAmount / installmentCount, 2);
        var amounts = Enumerable.Repeat(installmentAmount, installmentCount).ToList();
        var allocated = installmentAmount * installmentCount;
        var diff = totalAmount - allocated;
        if (diff != 0 && amounts.Count > 0)
        {
            amounts[^1] += diff;
        }

        return amounts;
    }
}
