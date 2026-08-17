using DesfudenciFy.Domain.Common;

namespace DesfudenciFy.Domain.Entities;

public class Reserve : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Goal { get; set; }
    public string? DisplayColor { get; set; }
    public decimal? MonthlyGoal { get; set; }

    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
    public ICollection<ReserveInvestment> LinkedInvestments { get; set; } = new List<ReserveInvestment>();
    public ICollection<FixedCost> FixedCosts { get; set; } = new List<FixedCost>();
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
