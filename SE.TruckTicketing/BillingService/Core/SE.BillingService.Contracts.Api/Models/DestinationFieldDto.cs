using SE.TruckTicketing.Contracts.Models;

namespace SE.BillingService.Contracts.Api.Models;

public class DestinationFieldDto : GuidApiModelBase
{
    public string JsonPath { get; set; }

    public string DataType { get; set; }

    public string EntityName { get; set; }

    public string FieldName { get; set; }

    public string Namespace { get; set; }
}
