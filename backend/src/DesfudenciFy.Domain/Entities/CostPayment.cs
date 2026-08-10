using DesfudenciFy.Domain.Common;

namespace DesfudenciFy.Domain.Entities;

public class CostPayment : BaseEntity
{
    public decimal PaidAmount { get; set; }
    public DateTime DatePaid { get; set; } = DateTime.UtcNow;
    public Guid FixedCostId { get; set; }
    public FixedCost FixedCost { get; set; } = null!;
    public Guid? EntryId { get; set; }
    public Entry? Entry { get; set; }
}
