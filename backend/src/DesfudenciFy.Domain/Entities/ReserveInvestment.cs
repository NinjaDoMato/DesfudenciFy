using DesfudenciFy.Domain.Common;

namespace DesfudenciFy.Domain.Entities;

public class ReserveInvestment : BaseEntity
{
    public Guid? ReserveId { get; set; }
    public Reserve? Reserve { get; set; }
    public Guid InvestmentId { get; set; }
    public Investment Investment { get; set; } = null!;
    public decimal Amount { get; set; }
}
