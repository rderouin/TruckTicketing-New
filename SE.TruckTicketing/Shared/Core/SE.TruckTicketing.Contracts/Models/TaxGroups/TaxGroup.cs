using System;
using System.Collections.Generic;

namespace SE.TruckTicketing.Contracts.Models.TaxGroups;

public class TaxGroup : GuidApiModelBase
{
    public string Name { get; set; }

    public string Group { get; set; }

    public string LegalEntityName { get; set; }

    public Guid LegalEntityId { get; set; }

    public List<TaxCode> TaxCodes { get; set; }
}

public class TaxCode
{
    public string Code { get; set; }

    public string TaxName { get; set; }

    public string CurrencyCode { get; set; }

    public bool ExemptTax { get; set; }

    public bool UseTax { get; set; }

    public double TaxValuePercentage { get; set; }
}
