using DesfudenciFy.Domain.Common;
using DesfudenciFy.Domain.Enums;

namespace DesfudenciFy.Domain.Entities;

public class Entry : BaseEntity
{
    public decimal Amount { get; set; }
    public string Observation { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public EntryDestination Destination { get; set; }
    public Guid? ReserveId { get; set; }
    public Reserve? Reserve { get; set; }
}
