using DesfudenciFy.Domain.Common;

namespace DesfudenciFy.Domain.Entities;

public class Vehicle : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? PhotoPath { get; set; }
    public decimal PaidValue { get; set; }
    public decimal FipeValue { get; set; }

    public ICollection<VehicleExpense> Expenses { get; set; } = new List<VehicleExpense>();
}
