namespace SE.TruckTicketing.Contracts.Models.Operations;

public class EDIValidationPatternLookup : GuidApiModelBase
{
    public string Name { get; set; }

    public string Pattern { get; set; }

    public string ErrorMessage { get; set; }
}
