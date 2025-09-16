namespace SE.TruckTicketing.Contracts.Models.Operations;

public class TruckTicketVoidReason : GuidApiModelBase
{
    public string VoidReason { get; set; }

    public bool IsDeleted { get; set; }
}
