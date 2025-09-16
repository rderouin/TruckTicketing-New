using System;

using SE.TruckTicketing.Contracts.Constants.SpartanProductParameters;
using SE.TruckTicketing.Contracts.Models;

namespace SE.TruckTicketing.Contracts.Api.Models.SpartanProductParameters;

public class SpartanProductParameter : GuidApiModelBase
{
    public string ProductName { get; set; }

    public FluidIdentity FluidIdentity { get; set; }

    public double MinFluidDensity { get; set; }

    public double MaxFluidDensity { get; set; }

    public double MinWaterPercentage { get; set; }

    public double MaxWaterPercentage { get; set; }

    public bool ShowDensity { get; set; }

    public LocationOperatingStatus LocationOperatingStatus { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsActive { get; set; }

    public Guid LegalEntityId { get; set; }

    public string LegalEntity { get; set; }

    public string Display =>
        $"{(FluidIdentity != FluidIdentity.Undefined ? FluidIdentity + "; " : "")}{ProductName}; Density {MinFluidDensity:N1} - {MaxFluidDensity:N1}; Water {MinWaterPercentage:N2} - {MaxWaterPercentage:N2}; {(LocationOperatingStatus != LocationOperatingStatus.Blank ? LocationOperatingStatus.ToString() : "")}";
}
