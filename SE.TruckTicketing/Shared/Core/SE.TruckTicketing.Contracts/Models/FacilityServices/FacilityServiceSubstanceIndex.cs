using System;
using System.Collections.Generic;
using System.Linq;

using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.Contracts.Models.FacilityServices;

public class FacilityServiceSubstanceIndex : GuidApiModelBase
{
    public Guid FacilityId { get; set; }

    public Guid FacilityServiceId { get; set; }

    public string FacilityServiceNumber { get; set; }

    public Guid ServiceTypeId { get; set; }

    public string ServiceTypeName { get; set; }

    public Guid TotalProductId { get; set; }

    public string TotalProductName { get; set; }

    public List<string> TotalProductCategories { get; set; }

    public Guid SubstanceId { get; set; }

    public string Substance { get; set; }

    public string WasteCode { get; set; }

    public string Stream { get; set; }

    public string UnitOfMeasure { get; set; }

    public bool IsAuthorized { get; set; }

    public string Display => $"{FacilityServiceNumber} - {ServiceTypeName} - {Substance} {WasteCode}";

    public bool IsServiceOnlyProduct()
    {
        return TotalProductCategories?.Any(c => c.StartsWith(ProductCategories.AdditionalServices.Lf) ||
                                                c.StartsWith(ProductCategories.AdditionalServices.Fst)) == true;
    }
}
