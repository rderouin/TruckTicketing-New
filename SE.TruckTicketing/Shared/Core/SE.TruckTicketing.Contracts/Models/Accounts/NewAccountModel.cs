using System.Collections.Generic;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Contracts.Models.Accounts;

public class NewAccountModel : GuidApiModelBase
{
    public Account Account { get; set; }
    public Account BillingCustomer { get; set; } 
    public BillingConfiguration BillingConfiguration { get; set; }
    public List<SourceLocation> GeneratorSourceLocations { get; set; }
    public List<EDIFieldDefinition> EDIFieldDefinitions { get; set; }
}
