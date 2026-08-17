using DesfudenciFy.Domain.Common;

namespace DesfudenciFy.Domain.Entities;

public class PropertyExpense : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;
    public Guid ExpenseTypeId { get; set; }
    public PropertyExpenseType ExpenseType { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Observation { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid? EntryId { get; set; }
    public Entry? Entry { get; set; }
}
