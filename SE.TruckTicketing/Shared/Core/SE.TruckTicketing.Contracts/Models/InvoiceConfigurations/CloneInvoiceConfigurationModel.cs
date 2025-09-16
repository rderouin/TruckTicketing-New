using System.Collections.Generic;

using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;

public class CloneInvoiceConfigurationModel : GuidApiModelBase
{
    public InvoiceConfiguration InvoiceConfiguration { get; set; }

    public List<BillingConfiguration> BillingConfigurations { get; set; }
}
