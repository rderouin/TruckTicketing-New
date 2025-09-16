namespace SE.TruckTicketing.Contracts.Models;

public class TicketType : GuidApiModelBase
{
    public string TicketTypeName { get; set; }

    public string TicketTypeCode { get; set; }
}
