using DesfudenciFy.Domain.Common;

namespace DesfudenciFy.Domain.Entities;

public class PropertyAmortization : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;
    public decimal Amount { get; set; }
    public int InstallmentsAmortized { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public string? Observation { get; set; }
    public Guid? EntryId { get; set; }
    public Entry? Entry { get; set; }
}
