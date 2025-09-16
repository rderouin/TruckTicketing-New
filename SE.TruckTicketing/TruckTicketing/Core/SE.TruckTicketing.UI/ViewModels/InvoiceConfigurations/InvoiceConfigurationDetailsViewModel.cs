using System;
using System.Collections.Generic;
using System.Linq;

using SE.Shared.Common.Lookups;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.UI.ViewModels.InvoiceConfigurations;

public class InvoiceConfigurationDetailsViewModel
{
    public InvoiceConfigurationDetailsViewModel(InvoiceConfiguration invoiceConfiguration, string operation, Account customer)
    {
        InvoiceConfiguration = invoiceConfiguration;
        Breadcrumb = IsNew ? "New Invoice Configuration" : "Invoice Configuration ";
        IsNew = InvoiceConfiguration.Id == default || operation == "clone";
        Customer = customer;
        if (!IsNew)
        {
        }
    }

    public string Breadcrumb { get; }

    public Account Customer { get; }

    public IEnumerable<AccountContact> BillingContacts => Customer.Contacts.Where(c => c?.ContactFunctions?.Contains(AccountContactFunctions.BillingContact.ToString()) == true);

    private string CustomerName => Customer == null || Customer.Id == Guid.Empty ? string.Empty : Customer.Name;

    public bool IsNew { get; }

    public InvoiceConfiguration InvoiceConfiguration { get; }

    public string SubmitButtonBusyText => IsNew ? "Creating" : "Saving";

    public bool SubmitButtonDisabled { get; set; } = true;

    public string SubmitButtonIcon => IsNew ? "add_circle_outline" : "save";

    public string SubmitButtonText => IsNew ? "Create" : "Save & Close";

    public string SubmitSuccessNotificationMessage => IsNew ? "Invoice configuration created." : "Invoice configuration updated.";

    public string Title => IsNew ? $"Creating Invoice Configuration for {CustomerName}" : $"Editing Invoice Configuration for {CustomerName}";

    public void CleanupPrimitiveCollections()
    {
        if (InvoiceConfiguration.AllFacilities)
        {
            InvoiceConfiguration.Facilities = null;
            InvoiceConfiguration.FacilityCode = null;
        }

        if (InvoiceConfiguration.AllWellClassifications)
        {
            InvoiceConfiguration.WellClassifications = null;
        }

        if (InvoiceConfiguration.AllServiceTypes)
        {
            InvoiceConfiguration.ServiceTypes = null;
            InvoiceConfiguration.ServiceTypesName = null;
        }

        if (InvoiceConfiguration.AllSubstances)
        {
            InvoiceConfiguration.Substances = null;
            InvoiceConfiguration.SubstancesName = null;
        }

        if (InvoiceConfiguration.SourceLocations != null && InvoiceConfiguration.AllSourceLocations && !InvoiceConfiguration.SourceLocations.Any())
        {
            InvoiceConfiguration.SourceLocationIdentifier = null;
            InvoiceConfiguration.SourceLocations = null;
        }

        if (InvoiceConfiguration.SplitEdiFieldDefinitions != null && !InvoiceConfiguration.SplitEdiFieldDefinitions.Any())
        {
            InvoiceConfiguration.SplitEdiFieldDefinitions = null;
        }

        if (InvoiceConfiguration.SplittingCategories != null && !InvoiceConfiguration.SplittingCategories.Any())
        {
            InvoiceConfiguration.SplittingCategories = null;
        }
    }
}
