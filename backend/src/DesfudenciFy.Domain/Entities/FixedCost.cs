using DesfudenciFy.Domain.Common;
using DesfudenciFy.Domain.Enums;

namespace DesfudenciFy.Domain.Entities;

public class FixedCost : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public CostRecurrence Recurrence { get; set; } = CostRecurrence.Month;
    public DateTime? DueDate { get; set; }
    public Guid? ReserveId { get; set; }
    public Reserve? Reserve { get; set; }

    public ICollection<CostPayment> Payments { get; set; } = new List<CostPayment>();
}
