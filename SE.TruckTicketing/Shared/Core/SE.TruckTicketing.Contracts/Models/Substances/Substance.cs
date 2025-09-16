namespace SE.TruckTicketing.Contracts.Models.Substances;

public class Substance : GuidApiModelBase
{
    public string SubstanceName { get; set; }

    public string WasteCode { get; set; }

    public string SearchableId { get; set; }
}
