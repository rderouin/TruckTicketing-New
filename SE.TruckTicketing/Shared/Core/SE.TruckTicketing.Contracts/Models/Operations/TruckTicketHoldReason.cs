namespace SE.TruckTicketing.Contracts.Models.Operations;

public class TruckTicketHoldReason : GuidApiModelBase
{
    public string HoldReason { get; set; }

    public bool IsDeleted { get; set; }
}
