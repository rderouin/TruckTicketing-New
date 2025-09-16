using System;
using System.Collections.Generic;
using System.Linq;

using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.UI.ViewModels;

public class BillingConfigurationDetailsViewModel
{
    public bool IsLoadConfirmationDisabled;

    public BillingConfigurationDetailsViewModel(BillingConfiguration billingConfiguration, string operation)
    {
        BillingConfiguration = billingConfiguration;
        Breadcrumb = IsNew ? "New Billing Configuration" : "Billing Configuration ";
        IsNew = BillingConfiguration.Id == default || operation == "clone";
        LoadConfiguration = new()
        {
            LoadConfirmationsDisabled = billingConfiguration.LoadConfirmationsEnabled,
            LoadConfirmationsFrequency = billingConfiguration.LoadConfirmationFrequency,
            IncludeExternalAttachmentInLC = billingConfiguration.IncludeExternalAttachmentInLC,
            IncludeInternalAttachmentInLC = billingConfiguration.IncludeInternalAttachmentInLC,
        };
    }

    public string Breadcrumb { get; }

    public bool IsNew { get; }

    public BillingConfiguration BillingConfiguration { get; }

    public string SubmitButtonBusyText => IsNew ? "Creating" : "Saving";

    public bool SubmitButtonDisabled { get; set; } = true;

    public string SubmitButtonIcon => IsNew ? "add_circle_outline" : "save";

    public string SubmitButtonText => IsNew ? "Create" : "Save & Close";

    public string SubmitSuccessNotificationMessage => IsNew ? "Billing configuration created." : "Billing configuration updated.";

    public string SubmitFailNotificationMessage => IsNew ? "Billing configuration create failed." : "Billing configuration update failed.";

    public string Title => IsNew ? "Creating Billing Configuration" : $"Editing {BillingConfiguration?.Name} Billing Configuration";

    public LoadConfirmationViewModel LoadConfiguration { get; set; }

    private IEnumerable<Guid> _bindFacilities { get; set; }

    public List<Facility> facilityData { get; set; } = new();

    public List<(MatchPredicate, BillingConfiguration)> overlappingMatchPredicates { get; set; } = new();

    public string LastComment { get; set; }

    public List<BillingConfiguration> BillingConfigurationsForMatchPredicate { get; set; } = new();

    public IEnumerable<Guid> SelectedFacilities
    {
        get => BillingConfiguration.Facilities;
        set => BillingConfiguration.Facilities = new(value ?? Array.Empty<Guid>());
    }

    public void LoadMatchCriteria()
    {
        var endDate = BillingConfiguration.EndDate ?? DateTimeOffset.MaxValue;
        var results = BillingConfigurationsForMatchPredicate.OrderBy(x => x.StartDate).ToList();
        if (BillingConfiguration.Id != default && BillingConfigurationsForMatchPredicate.Any(x => x.Id == BillingConfiguration.Id))
        {
            results.Remove(results.First(x => x.Id == BillingConfiguration.Id));
        }

        results = results.Where(x => x.StartDate < endDate && (x.EndDate == null || x.EndDate >= BillingConfiguration.StartDate)).ToList();

        overlappingMatchPredicates = results.SelectMany(x => x.MatchCriteria.Select(predicate => (predicate, billingConfig: x))).Where(x => x.predicate.IsEnabled).OrderBy(x => x.predicate.StartDate)
                                            .ToList();

        var billingConfigurationStartDate = BillingConfiguration.StartDate ?? DateTime.MinValue;
        var billingConfigurationEndDate = BillingConfiguration.EndDate ?? DateTimeOffset.MaxValue;
        if (overlappingMatchPredicates != null && overlappingMatchPredicates.Any())
        {
            overlappingMatchPredicates = overlappingMatchPredicates.Where(predicate =>
                                                                          {
                                                                              var predicateStartDate = predicate.Item1.StartDate ?? predicate.Item2.StartDate ?? DateTime.MinValue;
                                                                              var predicateEndDate = predicate.Item1.EndDate ?? predicate.Item2.EndDate ?? DateTimeOffset.MaxValue;
                                                                              return predicateStartDate < billingConfigurationEndDate && predicateEndDate >= billingConfigurationStartDate;
                                                                          }).ToList();
        }
    }
}

public class LoadConfirmationViewModel
{
    public bool LoadConfirmationsDisabled { get; set; }

    public LoadConfirmationFrequency? LoadConfirmationsFrequency { get; set; }

    public bool IncludeExternalAttachmentInLC { get; set; }

    public bool IncludeInternalAttachmentInLC { get; set; }
}
