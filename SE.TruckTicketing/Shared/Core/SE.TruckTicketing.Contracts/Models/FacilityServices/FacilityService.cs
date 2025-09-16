using System;
using System.Collections.Generic;

namespace SE.TruckTicketing.Contracts.Models.FacilityServices;

public class FacilityService : GuidApiModelBase
{
    public Guid FacilityId { get; set; }

    public Guid ServiceTypeId { get; set; }

    public string SiteId { get; set; }

    public string ServiceTypeName { get; set; }

    public string OilItem { get; set; }

    public string SolidsItem { get; set; }

    public string WaterItem { get; set; }

    public string TotalItem { get; set; }

    public Guid? TotalItemProductId { get; set; }

    public List<FacilityServiceSpartanProductParameter> SpartanProductParameters { get; set; } = new();

    public string Description { get; set; }

    public int? ServiceNumber { get; set; }

    public string FacilityServiceNumber { get; set; }

    public bool IsActive { get; set; }

    public List<Guid> AuthorizedSubstances { get; set; }

    public string DisplayFacilityServiceNumberName => $"{FacilityServiceNumber} - {ServiceTypeName}";
}

public class FacilityServiceSpartanProductParameter
{
    public Guid SpartanProductParameterId { get; set; }

    public string SpartanProductParameterName { get; set; }

    public string SpartanProductParameterDisplay { get; set; }
}