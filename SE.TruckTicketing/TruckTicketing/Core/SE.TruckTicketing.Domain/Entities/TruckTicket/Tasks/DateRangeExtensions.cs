using System;

using DateTimeExtensions;

using Trident.Extensions;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

public static class DateRangeExtensions
{
    public static (DateTime start, DateTime end) DailyDateRange(this DateTime date)
    {
        var start = date;
        var end = date.Clone().AddDays(1).AddTicks(-1);
        return (start, end);
    }

    public static (DateTime start, DateTime end) WeeklyDateRange(this DateTime date, DayOfWeek firstDayOfTheWeek)
    {
        var start = date.DayOfWeek == firstDayOfTheWeek ? date.AddDays(0) : date.LastDayOfWeek(firstDayOfTheWeek);
        var end = start.AddDays(7).AddTicks(-1);
        return (start, end);
    }

    public static (DateTime start, DateTime end) MonthlyDateRange(this DateTime date, int firstDayOfTheMonth)
    {
        var start = new DateTime(date.Year, date.Month, firstDayOfTheMonth);
        if (date.Day < firstDayOfTheMonth)
        {
            start = start.AddMonths(-1);
        }

        var end = start.AddMonths(1).AddTicks(-1);
        return (start, end);
    }
}
