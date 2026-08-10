using DesfudenciFy.Domain.Common;
using DesfudenciFy.Domain.Enums;

namespace DesfudenciFy.Domain.Entities;

public class Investment : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Rentability { get; set; } = string.Empty;
    public decimal StartAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;
    public Guid InvestmentTypeId { get; set; }
    public InvestmentType InvestmentType { get; set; } = null!;
    public InvestmentStatus Status { get; set; } = InvestmentStatus.Active;

    public ICollection<ReserveInvestment> SourceReserves { get; set; } = new List<ReserveInvestment>();
}
