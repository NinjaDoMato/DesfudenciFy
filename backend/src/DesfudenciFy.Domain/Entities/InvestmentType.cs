using DesfudenciFy.Domain.Common;

namespace DesfudenciFy.Domain.Entities;

public class InvestmentType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Investment> Investments { get; set; } = new List<Investment>();
}
