using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.UI.ViewModels;

public class AdvancedEmailViewModel
{
    public string Bcc { get; set; }

    public string Cc { get; set; }

    public string To { get; set; }

    public List<DisplayEmailAddress> Contacts { get; set; } = new();

    public string AdHocNote { get; set; }

    public bool IsOkToProceed { get; set; } = false;

    public string ContactDropdownLabel { get; set; }

}

public class DisplayEmailAddress
{
    public string DisplayName { get; set; }

    public string Email { get; set; }

    public bool IsDefault { get; set; } = false;
}
