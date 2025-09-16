using System;

namespace SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;

public class InvoiceConfigurationPermutationsIndex : GuidApiModelBase
{
    public Guid InvoiceConfigurationId { get; set; }

    public Guid CustomerId { get; set; }

    public string Name { get; set; }

    public string Number { get; set; }

    public string SourceLocation { get; set; }

    public string ServiceType { get; set; }

    public string WellClassification { get; set; }

    public string Substance { get; set; }

    public string Facility { get; set; }
}
