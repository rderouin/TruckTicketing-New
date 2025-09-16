using System;

using Humanizer;

using SE.Shared.Common.Extensions;
using SE.TruckTicketing.Contracts.Lookups;

namespace SE.TruckTicketing.UI.ViewModels;

public class LoadConfirmationReasonViewModel
{
    public bool ShowReason { get; set; }

    public LoadConfirmationReason Reason { get; set; } = LoadConfirmationReason.Unknown;

    public string Comment { get; set; }

    public bool IsOkToProceed { get; set; } = false;

    public bool CanProceed
    {
        get
        {
            switch (ShowReason)
            {
                case true when Reason == LoadConfirmationReason.Unknown:
                case false when !Comment.HasText():
                    return false;

                default:
                    return true;
            }
        }
    }

    public string GetFormattedComment()
    {
        var reason = ShowReason ? $"Reason for action: {Reason.Humanize()}{Environment.NewLine}" : string.Empty;
        return reason + $"User's comment: {Comment}";
    }
}
