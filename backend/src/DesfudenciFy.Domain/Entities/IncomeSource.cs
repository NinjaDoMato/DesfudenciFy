using DesfudenciFy.Domain.Common;

namespace DesfudenciFy.Domain.Entities;

public class IncomeSource : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid IncomeTypeId { get; set; }
    public IncomeType IncomeType { get; set; } = null!;
    public Guid? PropertyId { get; set; }
    public Property? Property { get; set; }
}
