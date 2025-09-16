using SE.Shared.Common;

using Trident.Contracts.Api;

namespace SE.TruckTicketing.Contracts.Models.Operations;

public class FeatureTogglesModel : ApiModelBase<string>
{
    public FeatureToggles FeatureToggles { get; set; }
}
