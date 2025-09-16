using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.UI.ViewModels;

public class SalesLineEmailViewModel
{
    public string ToRecipients { get; set; }

    public string CcRecipients { get; set; }

    public string BccRecipients { get; set; }

    public string Note { get; set; }

    public AttachmentIndicatorType AttachmentIndicatorType { get; set; }
}
