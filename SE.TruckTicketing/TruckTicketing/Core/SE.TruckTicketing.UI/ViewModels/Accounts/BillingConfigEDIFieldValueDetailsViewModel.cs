using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.UI.ViewModels.Accounts;

public class BillingConfigEDIFieldValueDetailsViewModel
{
    public BillingConfigEDIFieldValueDetailsViewModel(EDIFieldDefinition model)
    {
        EdiFieldDefinition = model;
        IsNew = EdiFieldDefinition.Id == default;
    }

    public string EDIFieldValue { get; set; }

    public EDIFieldDefinition EdiFieldDefinition { get; }

    public Response<EDIFieldDefinition> Response { get; set; }

    private bool IsNew { get; }

    public string SubmitButtonText => IsNew ? "Create" : "Update";

    public string SubmitButtonBusyText => IsNew ? "Creating" : "Updating";

    public string Title => IsNew ? "Add EDI Field Value" : $"Editing {EdiFieldDefinition?.EDIFieldName}";

    public string ValidationSummary { get; set; }
}
