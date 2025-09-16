using System;
using System.Collections.Generic;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class Product : GuidApiModelBase
{
    public string Name { get; set; }

    public string Number { get; set; }

    public List<ProductSubstance> Substances { get; set; } = new();

    public List<string> AllowedSites { get; set; } = new();

    public List<string> Categories { get; set; } = new();

    public bool IsActive { get; set; }

    public Guid LegalEntityId { get; set; }

    public string LegalEntityCode { get; set; }

    public string UnitOfMeasure { get; set; }

    public string DisposalUnit { get; set; }

    public string DisplayProduct => $"{Number} - {Name}";
}

public class ProductSubstance : GuidApiModelBase
{
    public string SubstanceName { get; set; }

    public string WasteCode { get; set; }
}
