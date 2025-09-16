using System;

using Newtonsoft.Json;

using Trident.Contracts.Api;

namespace SE.Enterprise.Contracts.Models;

public class FacilityModel : ApiModelBase<Guid>
{
    [JsonProperty("SiteId")]
    public string SiteId { get; set; }

    [JsonProperty("SiteName")]
    public string Name { get; set; }

    [JsonProperty("FacilityType")]
    public string Type { get; set; }

    [JsonProperty("DataAreaId")]
    public string LegalEntity { get; set; }

    [JsonProperty("AdminEmail")]
    public string AdminEmail { get; set; }

    [JsonProperty("UWI")]
    public string SourceLocation { get; set; }

    [JsonProperty("CountryCode")]
    public string CountryCode { get; set; }

    [JsonProperty("Province")]
    public string Province { get; set; }

    [JsonProperty("FacilityRegulatoryCodePipeline")]
    public string Pipeline { get; set; }

    [JsonProperty("FacilityRegulatoryCodeTerminalling")]
    public string Terminaling { get; set; }

    [JsonProperty("FacilityRegulatoryCodeTreating")]
    public string Treating { get; set; }

    [JsonProperty("FacilityRegulatoryCodeWaste")]
    public string Waste { get; set; }

    [JsonProperty("FacilityRegulatoryCodeWater")]
    public string Water { get; set; }

    public bool IsActive { get; set; }
}
