using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.TruckTicketing.Contracts.Models.InvoiceConfigurations;

using Trident.Business;
using Trident.Contracts;
using Trident.Data.Contracts;
using Trident.Extensions;
using Trident.Logging;
using Trident.Mapper;
using Trident.Validation;
using Trident.Workflow;

namespace SE.Shared.Domain.Entities.InvoiceConfiguration;

public class InvoiceConfigurationManager : ManagerBase<Guid, InvoiceConfigurationEntity>, IInvoiceConfigurationManager
{
    private readonly IManager<Guid, BillingConfigurationEntity> _billingConfigurationManager;

    private readonly IMapperRegistry _mapper;

    private readonly PredicateParameters[] _parameters =
    {
        new(i => i.SourceLocations != null ? i.SourceLocations?.List : new(), p => p.SourceLocationId, p => p.SourceLocationValueState, i => i.AllSourceLocations,
            i => i.IsSplitBySourceLocation),
        new(i => i.WellClassifications != null ? i.WellClassifications?.List : new(), p => p.WellClassification, p => p.WellClassificationState,
            i => i.AllWellClassifications, i => i.IsSplitByWellClassification),
        new(i => i.ServiceTypes != null ? i.ServiceTypes?.List : new(), p => p.ServiceTypeId, p => p.ServiceTypeValueState, i => i.AllServiceTypes,
            i => i.IsSplitByServiceType),
        new(i => i.Substances != null ? i.Substances?.List : new(), p => p.SubstanceId, p => p.SubstanceValueState, i => i.AllSubstances, i => i.IsSplitBySubstance),
    };

    public InvoiceConfigurationManager(ILog logger,
                                       IProvider<Guid, InvoiceConfigurationEntity> provider,
                                       IManager<Guid, BillingConfigurationEntity> billingConfigurationManager,
                                       IMapperRegistry mapper,
                                       IValidationManager<InvoiceConfigurationEntity> validationManager = null,
                                       IWorkflowManager<InvoiceConfigurationEntity> workflowManager = null) : base(logger, provider, validationManager, workflowManager)
    {
        _billingConfigurationManager = billingConfigurationManager;
        _mapper = mapper;
    }

    public async Task<List<BillingConfigurationEntity>> ValidateBillingConfiguration(InvoiceConfigurationEntity invoiceConfigurationEntity)
    {
        List<(MatchPredicateEntity, BillingConfigurationEntity)> matchPredicates = new();
        List<BillingConfigurationEntity> invalidBillingConfigurations = new();
        var entity = invoiceConfigurationEntity.Clone();

        var billingConfigurations = await _billingConfigurationManager.Search(new()
        {
            Filters = new()
            {
                { nameof(BillingConfigurationEntity.InvoiceConfigurationId), entity.Id },
                { nameof(BillingConfigurationEntity.IsDefaultConfiguration), false },
            },
        });

        if (billingConfigurations != null && billingConfigurations.Results.Any())
        {
            matchPredicates = billingConfigurations.Results.SelectMany(x => x.MatchCriteria.Select(predicate => (predicate, billingConfig: x))).Where(x => x.predicate.IsEnabled
             && (x.predicate.StartDate == null || x.predicate.StartDate < DateTimeOffset.UtcNow) &&
                (x.predicate.EndDate == null || x.predicate.EndDate > DateTimeOffset.UtcNow)).ToList();

            if (matchPredicates.Any())
            {
                invalidBillingConfigurations = matchPredicates.Where(x => !Evaluate(entity, x.Item1)).Select(b => b.Item2).Distinct().ToList();
                invalidBillingConfigurations.ForEach(x => x.IsValid = false);
            }
        }

        return invalidBillingConfigurations;
    }

    public async Task CloneInvoiceConfiguration(CloneInvoiceConfigurationModel cloneInvoiceConfiguration)
    {
        var invoiceConfiguration = _mapper.Map<InvoiceConfigurationEntity>(cloneInvoiceConfiguration.InvoiceConfiguration);

        await CreateNewBillingConfigurations(_mapper.Map<List<BillingConfigurationEntity>>(cloneInvoiceConfiguration.BillingConfigurations), invoiceConfiguration);
        await Save(invoiceConfiguration);
    }

    private async Task CreateNewBillingConfigurations(List<BillingConfigurationEntity> billingConfigurationEntities, InvoiceConfigurationEntity invoiceConfigurationEntity)
    {
        foreach (var billingConfigurationEntity in billingConfigurationEntities)
        {
            billingConfigurationEntity.InvoiceConfigurationId = invoiceConfigurationEntity.Id;
            await _billingConfigurationManager.Save(billingConfigurationEntity, true);
        }
    }

    public bool Evaluate(InvoiceConfigurationEntity invoiceConfiguration, MatchPredicateEntity predicate)
    {
        var isValid = true;
        foreach (var parameter in _parameters)
        {
            if (isValid)
            {
                if (parameter.AllSelection(invoiceConfiguration))
                {
                    continue;
                }

                if (parameter.StateSelector(predicate) != MatchPredicateValueState.Value)
                {
                    continue;
                }

                isValid = ((IEnumerable)parameter.InvoiceConfigurationValueSelector(invoiceConfiguration)).Cast<object>()
                                                                                                          .Any(invoiceConfigValue =>
                                                                                                                   parameter.PredicateValueSelector(predicate).ToString()!
                                                                                                                            .Equals(invoiceConfigValue.ToString()));
            }
        }

        return isValid;
    }

    private class PredicateParameters
    {
        public PredicateParameters(Func<InvoiceConfigurationEntity, object> invoiceConfigurationValueSelector,
                                   Func<MatchPredicateEntity, object> predicateValueSelector,
                                   Func<MatchPredicateEntity, MatchPredicateValueState> stateSelector,
                                   Func<InvoiceConfigurationEntity, bool> allSelection,
                                   Func<InvoiceConfigurationEntity, bool> isSplit)
        {
            InvoiceConfigurationValueSelector = invoiceConfigurationValueSelector;
            PredicateValueSelector = predicateValueSelector;
            StateSelector = stateSelector;
            AllSelection = allSelection;
            IsSplit = isSplit;
        }

        public Func<InvoiceConfigurationEntity, object> InvoiceConfigurationValueSelector { get; }

        public Func<MatchPredicateEntity, object> PredicateValueSelector { get; }

        public Func<MatchPredicateEntity, MatchPredicateValueState> StateSelector { get; }

        public Func<InvoiceConfigurationEntity, bool> AllSelection { get; }

        public Func<InvoiceConfigurationEntity, bool> IsSplit { get; }
    }
}
