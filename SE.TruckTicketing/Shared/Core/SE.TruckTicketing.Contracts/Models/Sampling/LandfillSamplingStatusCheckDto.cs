using System;

using SE.Shared.Common.Lookups;

namespace SE.TruckTicketing.Contracts.Models.Sampling;

public static class LandfillSamplingStatusCheckAction
{
    public static string Allow => nameof(Allow);
    public static string Warn => nameof(Warn);
    public static string Block => nameof(Block);
}

public class LandfillSamplingStatusCheckDto
{
    public string Action { get; set; }
    
    public string Message { get; set; }
}

public class LandfillSamplingStatusCheckRequestDto
{
    public Guid FacilityId { get; set; }
    
    public double NetWeight { get; set; }

    public WellClassifications WellClassification { get; set; }

    public Guid? FacilityServiceSubstanceId { get; set; }
}
