using DesfudenciFy.Domain.Common;
using DesfudenciFy.Domain.Enums;

namespace DesfudenciFy.Domain.Entities;

public class Purchase : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ProductUrl { get; set; }
    public PurchaseDebitSource DebitSource { get; set; } = PurchaseDebitSource.None;
    public Guid? ReserveId { get; set; }
    public Reserve? Reserve { get; set; }

    public ICollection<Installment> Installments { get; set; } = new List<Installment>();
}
