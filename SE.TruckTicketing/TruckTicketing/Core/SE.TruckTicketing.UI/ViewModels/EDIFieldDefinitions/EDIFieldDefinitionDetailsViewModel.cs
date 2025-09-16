using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.UI.ViewModels.EDIFieldDefinitions;

public class EDIFieldDefinitionDetailsViewModel
{
    public EDIFieldDefinitionDetailsViewModel(EDIFieldDefinition model)
    {
        EdiFieldDefinition = model;
        IsNew = model.Id == default;
    }

    public EDIFieldDefinition EdiFieldDefinition { get; }

    public Response<EDIFieldDefinition> Response { get; set; }

    public bool IsNew { get; }

    public string SubmitButtonText => IsNew ? "Create" : "Update";

    public string SubmitButtonBusyText => IsNew ? "Creating" : "Updating";

    public string Title => IsNew ? "Add EDI Field Definition" : $"Editing {EdiFieldDefinition?.EDIFieldName}";

    public string SubmitSuccessNotificationMessage => IsNew ? "EDI Field Definition created." : "EDI Field Definition updated.";

    public string ValidationSummary { get; set; }
}
