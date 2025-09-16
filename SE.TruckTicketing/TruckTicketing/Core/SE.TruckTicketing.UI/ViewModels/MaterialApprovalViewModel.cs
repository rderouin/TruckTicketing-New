using System;
using System.Collections.Generic;
using System.Linq;

using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.UI.ViewModels;

public class MaterialApprovalViewModel
{
    public List<AccountContactMap> AccountContacts = new();

    public bool IsLoadConfirmationDisabled;

    public MaterialApprovalViewModel(MaterialApproval materialApproval, List<AccountContactMap> loadSummaryRecipientContacts = null)
    {
        MaterialApproval = materialApproval;
        if (loadSummaryRecipientContacts != null && loadSummaryRecipientContacts.Any())
        {
            AccountContacts.AddRange(loadSummaryRecipientContacts);
        }

        Breadcrumb = IsNew ? "New Material Approval" : "Material Approval";
        IsNew = MaterialApproval.Id == default;
        Title = IsNew ? "Creating Material Approval" : $"Editing Material Approval {materialApproval?.MaterialApprovalNumber}";
    }

    public MaterialApproval MaterialApproval { get; set; }

    public string Breadcrumb { get; }

    public bool IsNew { get; }

    public string SubmitButtonBusyText => IsNew ? "Creating Approval" : "Saving Material Approval";

    public bool SubmitButtonDisabled { get; set; } = true;

    public string SubmitButtonIcon => IsNew ? "add_circle_outline" : "save";

    public string SubmitButtonText => IsNew ? "Create Material Approval" : "Save Material Approval";

    public string SubmitSuccessNotificationMessage => IsNew ? "Material Approval created." : "Material Approval updated.";

    public string Title { get; }

    public LoadConfirmationViewModel LoadConfiguration { get; set; }

    public string LastComment { get; set; }
}

public class AccountContactMap
{
    public Guid Id => Contact.Id;

    public Guid? AccountId { get; set; }

    public string AccountName { get; set; }

    public string DisplayName => Contact.DisplayName;

    public string ContactName => Contact.Name;

    public AccountContact Contact { get; set; }
}
