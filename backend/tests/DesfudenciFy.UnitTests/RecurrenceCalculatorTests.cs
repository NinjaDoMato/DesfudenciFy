using DesfudenciFy.Application.Common;
using DesfudenciFy.Domain.Enums;

namespace DesfudenciFy.UnitTests;

public class RecurrenceCalculatorTests
{
    [Theory]
    [InlineData(CostRecurrence.Day, 1)]
    [InlineData(CostRecurrence.Week, 7)]
    public void Should_advance_due_date_by_days_for_day_and_week(CostRecurrence recurrence, int days)
    {
        var due = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
        var next = RecurrenceCalculator.AdvanceDueDate(due, recurrence);
        Assert.Equal(due.AddDays(days), next);
    }

    [Fact]
    public void Should_advance_due_date_by_one_month_for_monthly_recurrence()
    {
        var due = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        var next = RecurrenceCalculator.AdvanceDueDate(due, CostRecurrence.Month);
        Assert.Equal(new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc), next);
    }

    [Fact]
    public void Should_advance_due_date_by_one_year_for_yearly_recurrence()
    {
        var due = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
        var next = RecurrenceCalculator.AdvanceDueDate(due, CostRecurrence.Year);
        Assert.Equal(new DateTime(2027, 8, 10, 0, 0, 0, DateTimeKind.Utc), next);
    }
}
