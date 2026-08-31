using DesfudenciFy.Domain.Common;

namespace DesfudenciFy.Domain.Entities;

public class VehicleExpense : BaseEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public Guid ExpenseTypeId { get; set; }
    public VehicleExpenseType ExpenseType { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Observation { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid? EntryId { get; set; }
    public Entry? Entry { get; set; }
}
