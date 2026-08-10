using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DesfudenciFy.Domain.Enums;

namespace LegacyMigrator;

internal static class LegacyMappings
{
    private static readonly IReadOnlyDictionary<int, string> AccountNames = new Dictionary<int, string>
    {
        [0] = "Modal",
        [1] = "XP",
        [2] = "NuInvest",
        [3] = "Bradesco",
        [4] = "Wise"
    };

    private static readonly IReadOnlyDictionary<int, string> InvestmentTypeNames = new Dictionary<int, string>
    {
        [0] = "CDB",
        [1] = "Tesouro SELIC",
        [2] = "FII",
        [3] = "LCI",
        [4] = "LCA",
        [5] = "Viagem"
    };

    public static string BankAccountName(int accountType) =>
        AccountNames.TryGetValue(accountType, out var name)
            ? name
            : $"Conta legado #{accountType}";

    public static string InvestmentTypeName(int investmentType) =>
        InvestmentTypeNames.TryGetValue(investmentType, out var name)
            ? name
            : $"Tipo legado #{investmentType}";

    public static CostRecurrence ToRecurrence(int costType) =>
        Enum.IsDefined(typeof(CostRecurrence), costType)
            ? (CostRecurrence)costType
            : CostRecurrence.Month;

    public static string FormatRentability(decimal rentability) =>
        rentability.ToString("0.####", CultureInfo.InvariantCulture);

    public static string OwnerLabel(int owner) =>
        owner switch
        {
            0 => "Daniel",
            1 => "Cassia",
            2 => "Comum",
            _ => $"Owner#{owner}"
        };

    /// <summary>
    /// Gera Guid estável (MD5) para entidades de lookup criadas a partir de enums legados.
    /// </summary>
    public static Guid StableGuid(string key)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes("DesfudenciFy.LegacyMigrator:" + key));
        return new Guid(bytes);
    }

    public static Guid EnsureLinkId(Guid id, Guid reserveId, Guid investmentId) =>
        id == Guid.Empty
            ? StableGuid($"ReserveInvestment:{reserveId:N}:{investmentId:N}")
            : id;

    public static string FullNameFromEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0)
        {
            return email;
        }

        return email[..at];
    }

    public static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
