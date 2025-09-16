using System;
using System.Collections.Generic;
using System.Linq;

using SE.Shared.Common.Lookups;
using SE.Shared.Common.Utilities;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;

using Trident.Api.Search;
using Trident.Search;

using CompareOperators = Trident.Search.CompareOperators;

namespace SE.TruckTicketing.UI.ViewModels.BillingConfigurations;

public class MatchPredicateViewModel
{
    public MatchPredicateViewModel(MatchPredicate matchPredicate, BillingConfiguration billingConfiguration, InvoiceConfiguration invoiceConfigurations, List<SourceLocation> sourceLocations)
    {
        BillingConfiguration = billingConfiguration;
        MatchPredicate = matchPredicate;
        IsNew = matchPredicate.Id == default;
        InvoiceConfiguration = invoiceConfigurations;
        GeneratorSourceLocationData = new(sourceLocations);
        if (!InvoiceConfiguration.AllWellClassifications && InvoiceConfiguration.WellClassifications != null && InvoiceConfiguration.WellClassifications.Any())
        {
            WellClassificationListData = DataDictionary.ForSelectedValues<WellClassifications>(InvoiceConfiguration.WellClassifications);
        }
        else
        {
            WellClassificationListData = DataDictionary.For<WellClassifications>();
        }

        var localCount = 0;

        WellClassificationMatchPredicateValueStatesList = DataDictionary.For<MatchPredicateValueState>().Select(x => new MatchPredicateValueStates(ref localCount)
        {
            Value = x.Value,
            Key = x.Key,
            IsDisabled = (x.Key == MatchPredicateValueState.Any && InvoiceConfiguration.IsSplitByWellClassification) ||
                         (x.Key == MatchPredicateValueState.NotSet && InvoiceConfiguration.IsSplitByWellClassification),
        }).ToList();

        SubstanceMatchPredicateValueStatesList = DataDictionary.For<MatchPredicateValueState>().Select(x => new MatchPredicateValueStates(ref localCount)
        {
            Value = x.Value,
            Key = x.Key,
            IsDisabled = (x.Key == MatchPredicateValueState.Any && InvoiceConfiguration.IsSplitBySubstance) ||
                         (x.Key == MatchPredicateValueState.NotSet && InvoiceConfiguration.IsSplitBySubstance),
        }).ToList();

        ServiceTypeMatchPredicateValueStatesList = DataDictionary.For<MatchPredicateValueState>().Select(x => new MatchPredicateValueStates(ref localCount)
        {
            Value = x.Value,
            Key = x.Key,
            IsDisabled = (x.Key == MatchPredicateValueState.Any && InvoiceConfiguration.IsSplitByServiceType) ||
                         (x.Key == MatchPredicateValueState.NotSet && InvoiceConfiguration.IsSplitByServiceType),
        }).ToList();

        SourceLocationMatchPredicateValueStatesList = DataDictionary.For<MatchPredicateValueState>().Select(x => new MatchPredicateValueStates(ref localCount)
        {
            Value = x.Value,
            Key = x.Key,
            IsDisabled = (x.Key == MatchPredicateValueState.Any && InvoiceConfiguration.IsSplitBySourceLocation) ||
                         (x.Key == MatchPredicateValueState.NotSet && InvoiceConfiguration.IsSplitBySourceLocation),
        }).ToList();

        SourceLocationPreLoadData = InvoiceConfiguration.IsSplitBySourceLocation ? InvoiceConfiguration.SourceLocations.Cast<Guid?>().ToList() : null;
        SubstancePreLoadData = InvoiceConfiguration.IsSplitBySubstance ? InvoiceConfiguration.Substances.Cast<Guid?>().ToList() : null;
        ServiceTypePreLoadData = InvoiceConfiguration.IsSplitByServiceType ? InvoiceConfiguration.ServiceTypes.Cast<Guid?>().ToList() : null;
    }

    public IEnumerable<MatchPredicateValueStates> WellClassificationMatchPredicateValueStatesList { get; }

    public IEnumerable<MatchPredicateValueStates> SubstanceMatchPredicateValueStatesList { get; }

    public IEnumerable<MatchPredicateValueStates> ServiceTypeMatchPredicateValueStatesList { get; }

    public IEnumerable<MatchPredicateValueStates> SourceLocationMatchPredicateValueStatesList { get; }

    public IEnumerable<Guid?> SourceLocationPreLoadData { get; }

    public IEnumerable<Guid?> SubstancePreLoadData { get; }

    public IEnumerable<Guid?> ServiceTypePreLoadData { get; }

    public IReadOnlyDictionary<WellClassifications, string> WellClassificationListData { get; }

    public List<SourceLocation> GeneratorSourceLocationData { get; set; }

    public bool IsNew { get; }

    public BillingConfiguration BillingConfiguration { get; }

    public InvoiceConfiguration InvoiceConfiguration { get; }

    public MatchPredicate MatchPredicate { get; }

    public string SubmitButtonBusyText => IsNew ? "Adding" : "Saving";

    public bool SubmitButtonDisabled { get; set; } = true;

    public string SubmitButtonIcon => IsNew ? "add_circle_outline" : "save";

    public string SubmitButtonText => IsNew ? "Add" : "Save";

    public string SubmitSuccessNotificationMessage => IsNew ? "Match Predicate Added" : "Match Predicate Updated";

    public string Title => IsNew ? "Adding Match Predicate" : "Editing Match Predicate";

    public void ApplyFilter(SearchCriteriaModel criteria, List<Guid> data, string filterPath, CompareOperators comparerOperator)
    {
        var values = data?.Select(x => x.ToString());

        if (data == null || !data.Any())
        {
            return;
        }

        IJunction query = AxiomFilterBuilder.CreateFilter()
                                            .StartGroup();

        var index = 0;
        foreach (var value in values)
        {
            if (query is GroupStart groupStart)
            {
                query = groupStart.AddAxiom(new()
                {
                    Key = $"{filterPath}{++index}".Replace(".", string.Empty),
                    Field = filterPath,
                    Operator = comparerOperator,
                    Value = value,
                });
            }
            else if (query is AxiomTokenizer axiom)
            {
                query = axiom.Or().AddAxiom(new()
                {
                    Key = $"{filterPath}{++index}".Replace(".", string.Empty),
                    Field = filterPath,
                    Operator = comparerOperator,
                    Value = value,
                });
            }

            criteria.Filters[filterPath] = ((AxiomTokenizer)query).EndGroup().Build();
        }
    }

    public class MatchPredicateValueStates
    {
        public MatchPredicateValueStates(ref int id)
        {
            Id = id + 1;
        }

        public int Id { get; set; }

        public string Value { get; set; }

        public MatchPredicateValueState Key { get; set; }

        public bool? IsDisabled { get; set; }

        public bool? IsVisible { get; set; }
    }
}
