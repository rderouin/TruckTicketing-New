using SE.TruckTicketing.Contracts.Constants.SourceLocations;
using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.Contracts.Models.SourceLocations;

public class SourceLocationType : GuidApiModelBase
{
    public SourceLocationTypeCategory Category { get; set; }

    public CountryCode CountryCode { get; set; }

    public DeliveryMethod DefaultDeliveryMethod { get; set; }

    public DownHoleType DefaultDownHoleType { get; set; }

    public string FormatMask { get; set; }

    public string SourceLocationCodeMask { get; set; }

    public string Name { get; set; }

    public bool EnforceSourceLocationCodeMask { get; set; }

    public bool RequiresApiNumber { get; set; }

    public bool IsApiNumberVisible { get; set; }

    public bool RequiresCtbNumber { get; set; }

    public bool IsCtbNumberVisible { get; set; }

    public bool RequiresPlsNumber { get; set; }

    public bool IsPlsNumberVisible { get; set; }

    public bool RequiresWellFileNumber { get; set; }

    public bool IsWellFileNumberVisible { get; set; }

    public string ShortFormCode { get; set; }

    public string Display => $"{CountryCode} - {Name} - {Category}";

    public bool IsActive { get; set; }
}
