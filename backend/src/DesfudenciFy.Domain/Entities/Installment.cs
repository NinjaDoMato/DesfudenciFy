using DesfudenciFy.Domain.Common;

namespace DesfudenciFy.Domain.Entities;

public class Installment : BaseEntity
{
    public Guid PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;
    public decimal Amount { get; set; }
    public int InstallmentNumber { get; set; }
    public bool Paid { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? PaymentUrl { get; set; }
    public Guid? EntryId { get; set; }
    public Entry? Entry { get; set; }
}
