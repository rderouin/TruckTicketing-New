using SE.TruckTicketing.Contracts.Models;

namespace SE.BillingService.Contracts.Api.Models;

public class ValueFormatDto : GuidApiModelBase
{
    public string Name { get; set; }

    public string ValueExpression { get; set; }
}
