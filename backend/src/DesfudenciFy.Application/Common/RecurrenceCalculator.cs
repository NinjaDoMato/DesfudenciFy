using DesfudenciFy.Domain.Enums;

namespace DesfudenciFy.Application.Common;

public static class RecurrenceCalculator
{
    public static DateTime AdvanceDueDate(DateTime dueDate, CostRecurrence recurrence) =>
        recurrence switch
        {
            CostRecurrence.Day => dueDate.AddDays(1),
            CostRecurrence.Week => dueDate.AddDays(7),
            CostRecurrence.Year => dueDate.AddYears(1),
            _ => dueDate.AddMonths(1),
        };
}
