using DesfudenciFy.Domain.Common;

namespace DesfudenciFy.Domain.Entities;

public class IncomeType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<IncomeSource> IncomeSources { get; set; } = new List<IncomeSource>();
}
