using DesfudenciFy.Domain.Common;

namespace DesfudenciFy.Domain.Entities;

public class VehicleExpenseType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<VehicleExpense> Expenses { get; set; } = new List<VehicleExpense>();
}
