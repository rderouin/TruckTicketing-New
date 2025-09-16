using System;

using Trident.Search;

namespace SE.Shared.Domain.Entities.LoadConfirmation;

public static class LoadConfirmationRepositoryExtensions
{
    public static int GetIntFromFilter(this AxiomFilter axiomFilter, int i)
    {
        return axiomFilter.Axioms[i].GetIntFromFilter();
    }

    public static int GetIntFromFilter(this Axiom axiom)
    {
        return Convert.ToInt32(axiom.Value);
    }

    public static DateTimeOffset GetFirstDayOfMonth(this int month, int year)
    {
        return new(year, month, 1, 0, 0, 0, TimeSpan.Zero);
    }

    public static DateTimeOffset GetLastDayOfMonth(this int month, int year)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);

        return new(year, month, daysInMonth, 23, 59, 59, TimeSpan.Zero);
    }
}
