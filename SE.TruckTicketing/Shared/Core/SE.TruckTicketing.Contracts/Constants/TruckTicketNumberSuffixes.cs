namespace SE.TruckTicketing.Contracts.Constants;

public static class TruckTicketNumberSuffixes
{
    public const string ScaleTicketSuffix = "LF";

    public static bool HasScaleTicketSuffix(this string ticketNumber)
    {
        return ticketNumber.EndsWith(ScaleTicketSuffix);
    }
}
