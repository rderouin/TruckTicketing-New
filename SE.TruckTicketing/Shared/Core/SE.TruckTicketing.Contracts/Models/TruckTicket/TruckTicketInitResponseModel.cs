using System.Collections.Generic;

using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Contracts.Models.TruckTicket;

public class TruckTicketInitResponseModel : GuidApiModelBase
{
    public List<BillingConfiguration> BillingConfigurations { get; set; } = new();

    public List<SalesLine> SalesLines { get; set; } = new();

    public bool IsUpdateBillingConfiguration { get; set; }
}

public class TruckTicketInitRequestModel : GuidApiModelBase
{
    public Operations.TruckTicket TruckTicket { get; set; }

    public bool ShouldRunBillingConfiguration { get; set; }

    public bool ShouldRunSalesLine { get; set; }
}
