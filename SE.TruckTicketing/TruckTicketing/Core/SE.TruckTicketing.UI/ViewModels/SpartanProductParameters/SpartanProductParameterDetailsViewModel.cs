using System;

using SE.TruckTicketing.Contracts.Api.Models.SpartanProductParameters;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.UI.ViewModels.SpartanProductParameters;

public class SpartanProductParameterDetailsViewModel
{
    public SpartanProductParameterDetailsViewModel(SpartanProductParameter spartanProductParameter)
    {
        SpartanProductParameter = spartanProductParameter;
        IsNew = spartanProductParameter?.Id == Guid.Empty;
        Breadcrumb = IsNew ? "New Spartan Product Parameter" : "Spartan Product Parameter " + SpartanProductParameter?.ProductName;
    }

    public Response<SpartanProductParameter> Response { get; set; }

    public string Breadcrumb { get; }

    public bool IsNew { get; }

    public SpartanProductParameter SpartanProductParameter { get; }

    public string SubmitButtonBusyText => IsNew ? "Creating" : "Saving";

    public bool SubmitButtonDisabled { get; set; } = true;

    public string Title => IsNew ? "Add Spartan Product Parameter" : $"Editing {SpartanProductParameter?.ProductName}";

    public string SubmitButtonText => IsNew ? "Create" : "Update";

    public string SubmitButtonIcon => IsNew ? "add_circle_outline" : "save";

    public string ValidationSummary { get; set; }
}
