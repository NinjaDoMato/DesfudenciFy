using DesfudenciFy.Domain.Common;

namespace DesfudenciFy.Domain.Entities;

public class Property : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
    public bool IsRented { get; set; }
    public decimal AppraisedValue { get; set; }
    public decimal RentalAmount { get; set; }
    public decimal InitialFinancingAmount { get; set; }
    public decimal InstallmentAmount { get; set; }
    public int RemainingInstallments { get; set; }
    public decimal RemainingBalance { get; set; }

    public ICollection<PropertyAmortization> Amortizations { get; set; } = new List<PropertyAmortization>();
    public ICollection<PropertyExpense> Expenses { get; set; } = new List<PropertyExpense>();
    public ICollection<PropertyRentPayment> RentPayments { get; set; } = new List<PropertyRentPayment>();
}
