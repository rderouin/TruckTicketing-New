using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.UI.ViewModels;

public class AdditionalServicesConfigurationViewModel
{
    public AdditionalServicesConfigurationViewModel(AdditionalServicesConfiguration additionalServicesConfiguration)
    {
        AdditionalServicesConfiguration = additionalServicesConfiguration;
        Breadcrumb = IsNew ? "New Additional Services Configuration" : "Additional Services Configuration ";
        IsNew = AdditionalServicesConfiguration.Id == default;
    }

    public AdditionalServicesConfiguration AdditionalServicesConfiguration { get; }

    public string Breadcrumb { get; }

    public bool IsNew { get; set; }

    public bool SubmitButtonDisabled { get; set; } = true;

    public string SubmitSuccessNotificationMessage => IsNew ? "Additional Services Configuration created." : "Additional Services Configuration updated.";

    public string Title => IsNew ? "Creating Additional Services Configuration" : "Editing Additional Services Configuration";

    public string SubmitButtonText => IsNew ? "Create" : "Save & Close";

    public string SubmitButtonBusyText => IsNew ? "Creating" : "Saving";

    public string SubmitButtonIcon => IsNew ? "add_circle_outline" : "save";
}
