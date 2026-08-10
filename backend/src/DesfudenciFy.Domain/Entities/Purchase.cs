using DesfudenciFy.Domain.Common;

namespace DesfudenciFy.Domain.Entities;

public class Purchase : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ProductUrl { get; set; }

    public ICollection<Installment> Installments { get; set; } = new List<Installment>();
}
