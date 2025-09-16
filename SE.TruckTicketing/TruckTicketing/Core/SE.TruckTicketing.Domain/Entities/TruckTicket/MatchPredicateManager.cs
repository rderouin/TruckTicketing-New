using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Lookups;
using SE.Shared.Domain;
using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.InvoiceConfiguration;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.TruckTicketing.Contracts.Models;
using SE.TruckTicketing.Domain.Entities.TruckTicket.Tasks;

using Trident.Data.Contracts;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

public class MatchPredicateManager : IMatchPredicateManager
{
    private readonly IProvider<Guid, BillingConfigurationEntity> _billingConfigurationProvider;

    private readonly Dictionary<string, int> _billingConfigurationWeights = new()
    {
        { MatchPredicateProperties.SourceLocation, 21 },
        { MatchPredicateProperties.Stream, 13 },
        { MatchPredicateProperties.WellClassification, 8 },
        { MatchPredicateProperties.ServiceType, 5 },
        { MatchPredicateProperties.Substance, 3 },
    };

    private readonly IProvider<Guid, InvoiceConfigurationEntity> _invoiceConfigurationProvider;

    private readonly Dictionary<string, int> _invoiceConfigurationWeights = new()
    {
        { MatchPredicateProperties.SourceLocation, 1 },
        { MatchPredicateProperties.WellClassification, 1 },
        { MatchPredicateProperties.Substance, 1 },
        { MatchPredicateProperties.ServiceType, 1 },
        { MatchPredicateProperties.Facility, 1 },
    };

    private readonly IMatchPredicateRankManager _matchPredicateRankManager;

    private readonly ITruckTicketEffectiveDateService _truckTicketEffectiveDateService;

    public MatchPredicateManager(IProvider<Guid, BillingConfigurationEntity> billingConfigurationProvider,
                                 IProvider<Guid, InvoiceConfigurationEntity> invoiceConfigurationProvider,
                                 IMatchPredicateRankManager matchPredicateRankManager,
                                 ITruckTicketEffectiveDateService truckTicketEffectiveDateService)
    {
        _billingConfigurationProvider = billingConfigurationProvider;
        _invoiceConfigurationProvider = invoiceConfigurationProvider;
        _matchPredicateRankManager = matchPredicateRankManager;
        _truckTicketEffectiveDateService = truckTicketEffectiveDateService;
    }

    public async Task<List<BillingConfigurationEntity>> GetOverlappingBillingConfigurations(BillingConfigurationEntity billingConfigurationEntity)
    {
        var endDate = billingConfigurationEntity.EndDate ?? DateTimeOffset.MaxValue;
        var matches = await _billingConfigurationProvider.Get(x => x.CustomerGeneratorId == billingConfigurationEntity.CustomerGeneratorId &&
                                                                   x.IncludeForAutomation && x.Id != billingConfigurationEntity.Id);

        var billingConfigurationEntities = matches?.OrderBy(x => x.StartDate).ToList();
        billingConfigurationEntities = billingConfigurationEntities?.Where(x => x.StartDate < endDate && (x.EndDate == null || x.EndDate >= billingConfigurationEntity.StartDate)).ToList();

        if (billingConfigurationEntities != null && billingConfigurationEntities.Any())
        {
            billingConfigurationEntities = billingConfigurationEntities.Where(x => x.Facilities == null || !x.Facilities.List.Any() || billingConfigurationEntity.Facilities == null
                                                                                || !billingConfigurationEntity.Facilities.List.Any() ||
                                                                                   x.Facilities.List.Intersect(billingConfigurationEntity.Facilities.List).Any()).ToList();
        }

        return billingConfigurationEntities?.ToList();
    }

    public async Task<List<BillingConfigurationEntity>> GetBillingConfigurations(TruckTicketEntity truckTicketEntity, bool includeForAutomation)
    {
        var billingConfigurations = new List<BillingConfigurationEntity>();
        var currentDate = truckTicketEntity.EffectiveDate ?? await _truckTicketEffectiveDateService.GetTruckTicketEffectiveDate(truckTicketEntity) ?? DateTime.Today;
        var matches = await _billingConfigurationProvider.Get(x => x.CustomerGeneratorId == truckTicketEntity.GeneratorId &&
                                                                   x.IncludeForAutomation == includeForAutomation && (x.StartDate == null || x.StartDate <= currentDate) &&
                                                                   (x.EndDate == null || x.EndDate > currentDate));

        var billingConfigurationEntities = matches?.ToList();
        if (billingConfigurationEntities != null && billingConfigurationEntities.Any())
        {
            billingConfigurationEntities = billingConfigurationEntities.Where(x => x.Facilities == null || !x.Facilities.List.Any() || x.Facilities.List.Contains(truckTicketEntity.FacilityId))
                                                                       .ToList();
        }

        if (billingConfigurationEntities == null || !billingConfigurationEntities.Any())
        {
            return billingConfigurations;
        }

        var selectedInvoiceConfiguration = await FilterInvoiceConfigurationFromBillingConfiguration(billingConfigurationEntities, truckTicketEntity);
        if (!selectedInvoiceConfiguration.Any())
        {
            return billingConfigurations;
        }

        var selectedInvoiceConfigurationIds = selectedInvoiceConfiguration.Select(x => x.Id).ToList();
        billingConfigurations = billingConfigurationEntities.Where(x => selectedInvoiceConfigurationIds.Contains(x.InvoiceConfigurationId)).ToList();

        return billingConfigurations;
    }

    public async Task<BillingConfigurationEntity> GetMatchingBillingConfiguration(List<BillingConfigurationEntity> billingConfigurations, TruckTicketEntity truckTicket)
    {
        var billingConfigurationEntity = new BillingConfigurationEntity();
        var currentDate = truckTicket.EffectiveDate ?? await _truckTicketEffectiveDateService.GetTruckTicketEffectiveDate(truckTicket) ?? DateTime.Today;
        var overlappingMatchPredicates = billingConfigurations.SelectMany(x => x.MatchCriteria.Select(predicate => (predicate, billingConfig: x))).Where(x => x.predicate.IsEnabled
         && (x.predicate.StartDate == null || x.predicate.StartDate <= currentDate) &&
            (x.predicate.EndDate == null || x.predicate.EndDate > currentDate)).ToList();

        if (!overlappingMatchPredicates.Any())
        {
            return billingConfigurationEntity;
        }

        var billingConfigurationRankMatchingInput = GeneratePredicateRankMatchingInputForBillingConfiguration(overlappingMatchPredicates);
        var truckTicketProperties = TruckTicketProperties(truckTicket);
        var selectedBillingConfigs = _matchPredicateRankManager.EvaluatePredicateRank(billingConfigurationRankMatchingInput,
                                                                                      truckTicketProperties, _billingConfigurationWeights, "*", false, true)?.ToList();

        if (selectedBillingConfigs == null || !selectedBillingConfigs.Any())
        {
            return billingConfigurationEntity;
        }

        var selectedBillingConfiguration = selectedBillingConfigs.First();
        return billingConfigurations.FirstOrDefault(x => x.Id == selectedBillingConfiguration.EntityId, new());
    }

    public BillingConfigurationEntity SelectAutomatedBillingConfiguration(List<BillingConfigurationEntity> billingConfigurations, TruckTicketEntity truckTicket)
    {
        var billingConfigurationEntity = new BillingConfigurationEntity();
        var currentDate = truckTicket.EffectiveDate ?? DateTime.Today;
        var overlappingMatchPredicates = billingConfigurations
                                        .Where(billingConfig => billingConfig.IncludeForAutomation)
                                        .SelectMany(x => x.MatchCriteria.Select(predicate => (predicate, billingConfig: x))).Where(x => x.predicate.IsEnabled
                                                                                                                                    && (x.predicate.StartDate == null ||
                                                                                                                                                   x.predicate.StartDate <= currentDate) &&
                                                                                                                                       (x.predicate.EndDate == null ||
                                                                                                                                               x.predicate.EndDate > currentDate)).ToList();

        if (!overlappingMatchPredicates.Any())
        {
            return billingConfigurationEntity;
        }

        var billingConfigurationRankMatchingInput = GeneratePredicateRankMatchingInputForBillingConfiguration(overlappingMatchPredicates);
        var truckTicketProperties = TruckTicketProperties(truckTicket);
        var selectedBillingConfigs = _matchPredicateRankManager.EvaluatePredicateRank(billingConfigurationRankMatchingInput,
                                                                                      truckTicketProperties, _billingConfigurationWeights, "*", false, true)?.ToList();

        if (selectedBillingConfigs == null || !selectedBillingConfigs.Any())
        {
            return billingConfigurationEntity;
        }

        var selectedBillingConfiguration = selectedBillingConfigs.First();
        return billingConfigurations.FirstOrDefault(x => x.Id == selectedBillingConfiguration.EntityId, new());
    }

    public async Task<List<BillingConfigurationEntity>> GetBillingConfigurations(TruckTicketEntity truckticket)
    {
        truckticket.EffectiveDate ??= await _truckTicketEffectiveDateService.GetTruckTicketEffectiveDate(truckticket) ?? DateTime.Today;
        var billingConfigs = (await _billingConfigurationProvider.Get(x => x.CustomerGeneratorId == truckticket.GeneratorId &&
                                                                           (x.StartDate == null || x.StartDate <= truckticket.EffectiveDate) &&
                                                                           (x.EndDate == null || x.EndDate > truckticket.EffectiveDate)))?
                            .Where(x => x.Facilities == null || !x.Facilities.List.Any() ||
                                        x.Facilities.List.Contains(truckticket.FacilityId))
                            .ToList();

        if (billingConfigs?.Any() is not true)
        {
            return billingConfigs;
        }

        var invoiceConfigIds = billingConfigs.Select(entity => entity.InvoiceConfigurationId).Distinct().ToList();
        var invoiceConfigs = await _invoiceConfigurationProvider.Get(entity => invoiceConfigIds.Contains(entity.Id) &&
                                                                               entity.DocumentType == Databases.Discriminators.InvoiceConfiguration);

        var filteredInvoiceConfigIds = FilterInvoiceConfigsForTicket(invoiceConfigs, truckticket).Select(invoiceConfig => invoiceConfig.Id).ToHashSet();

        return billingConfigs.Where(billingConfig => filteredInvoiceConfigIds.Contains(billingConfig.InvoiceConfigurationId)).ToList();
    }

    private List<InvoiceConfigurationEntity> FilterInvoiceConfigsForTicket(IEnumerable<InvoiceConfigurationEntity> invoiceConfigs, TruckTicketEntity truckTicketEntity)
    {
        return invoiceConfigs.Where(invoice =>
                                        ((invoice.Facilities != null &&
                                          invoice.Facilities.List.Contains(truckTicketEntity.FacilityId)) ||
                                         (invoice.Facilities == null && invoice.AllFacilities))
                                     && ((invoice.SourceLocations != null &&
                                          invoice.SourceLocations.List.Contains(truckTicketEntity.SourceLocationId)) ||
                                         (invoice.SourceLocations == null && invoice.AllSourceLocations))
                                     && ((invoice.Substances != null &&
                                          invoice.Substances.List.Contains(truckTicketEntity.SubstanceId)) ||
                                         (invoice.Substances == null && invoice.AllSubstances))
                                     && ((invoice.ServiceTypes != null &&
                                          invoice.ServiceTypes.List.Contains(truckTicketEntity.ServiceTypeId.GetValueOrDefault())) ||
                                         (invoice.ServiceTypes == null && invoice.AllServiceTypes))
                                     && ((invoice.WellClassifications != null &&
                                          invoice.WellClassifications.List.Contains(truckTicketEntity.WellClassification.ToString())) ||
                                         (invoice.WellClassifications == null && invoice.AllWellClassifications)))
                             .ToList();
    }

    private async Task<List<InvoiceConfigurationEntity>> FilterInvoiceConfigurationFromBillingConfiguration(List<BillingConfigurationEntity> billingConfigurationEntities,
                                                                                                            TruckTicketEntity truckTicketEntity)
    {
        var invoiceConfigIds = billingConfigurationEntities.Select(billingConfig => billingConfig.InvoiceConfigurationId).Distinct().ToList();

        var invoiceConfigurationEntities = await _invoiceConfigurationProvider.Get(config => invoiceConfigIds.Contains(config.Id)
                                                                                          && config.DocumentType == Databases.Discriminators.InvoiceConfiguration);

        if (!invoiceConfigurationEntities.Any())
        {
            return new();
        }

        var matchingInvoiceConfigurationForTruckTicket = invoiceConfigurationEntities.Where(invoice =>
                                                                                                ((invoice.Facilities != null &&
                                                                                                  invoice.Facilities.List.Contains(truckTicketEntity.FacilityId)) ||
                                                                                                 (invoice.Facilities == null && invoice.AllFacilities))
                                                                                             && ((invoice.SourceLocations != null &&
                                                                                                  invoice.SourceLocations.List.Contains(truckTicketEntity.SourceLocationId)) ||
                                                                                                 (invoice.SourceLocations == null && invoice.AllSourceLocations))
                                                                                             && ((invoice.Substances != null &&
                                                                                                  invoice.Substances.List.Contains(truckTicketEntity.SubstanceId)) ||
                                                                                                 (invoice.Substances == null && invoice.AllSubstances))
                                                                                             && ((invoice.ServiceTypes != null &&
                                                                                                  invoice.ServiceTypes.List.Contains(truckTicketEntity.ServiceTypeId.GetValueOrDefault())) ||
                                                                                                 (invoice.ServiceTypes == null && invoice.AllServiceTypes))
                                                                                             && ((invoice.WellClassifications != null &&
                                                                                                  invoice.WellClassifications.List.Contains(truckTicketEntity.WellClassification.ToString())) ||
                                                                                                 (invoice.WellClassifications == null && invoice.AllWellClassifications)))
                                                                                     .ToList();

        return matchingInvoiceConfigurationForTruckTicket;
    }

    private List<RankConfiguration> GeneratePredicateRankMatchingInputForInvoiceConfiguration(List<InvoiceConfigurationEntity> invoiceConfigurationEntities,
                                                                                              string wildCard = "*")
    {
        var rankConfigurationInput = new List<RankConfiguration>();
        List<string> predicates = new();

        if (invoiceConfigurationEntities == null || !invoiceConfigurationEntities.Any())
        {
            return rankConfigurationInput;
        }
        //If All -> Wildcard *
        //If Values defined -> Split them into single records

        foreach (var invoiceConfigurationEntity in invoiceConfigurationEntities)
        {
            var predicate = String.Empty;

            if (invoiceConfigurationEntity.AllFacilities)
            {
                predicate = $"{MatchPredicateProperties.Facility}:{wildCard}";
                predicates.Add(predicate);
            }
            else
            {
                if (invoiceConfigurationEntity.Facilities is { List: { } } &&
                    invoiceConfigurationEntity.Facilities.List.Any())
                {
                    invoiceConfigurationEntity.Facilities.List.ForEach(facility =>
                                                                       {
                                                                           predicate = $"{MatchPredicateProperties.Facility}:{facility}";
                                                                           predicates.Add(predicate);
                                                                       });
                }
            }

            if (invoiceConfigurationEntity.AllSourceLocations)
            {
                predicate = $"{MatchPredicateProperties.SourceLocation}:{wildCard}";
                predicates.Add(predicate);
            }
            else
            {
                if (invoiceConfigurationEntity.SourceLocations is { List: { } } &&
                    invoiceConfigurationEntity.SourceLocations.List.Any())
                {
                    invoiceConfigurationEntity.SourceLocations.List.ForEach(sl =>
                                                                            {
                                                                                predicate = $"{MatchPredicateProperties.SourceLocation}:{sl}";
                                                                                predicates.Add(predicate);
                                                                            });
                }
            }

            if (invoiceConfigurationEntity.AllSubstances)
            {
                predicate = $"{MatchPredicateProperties.Substance}:{wildCard}";
                predicates.Add(predicate);
            }
            else
            {
                if (invoiceConfigurationEntity.Substances is { List: { } } &&
                    invoiceConfigurationEntity.Substances.List.Any())
                {
                    invoiceConfigurationEntity.Substances.List.ForEach(substance =>
                                                                       {
                                                                           predicate = $"{MatchPredicateProperties.Substance}:{substance}";
                                                                           predicates.Add(predicate);
                                                                       });
                }
            }

            if (invoiceConfigurationEntity.AllWellClassifications)
            {
                predicate = $"{MatchPredicateProperties.WellClassification}:{wildCard}";
                predicates.Add(predicate);
            }
            else
            {
                if (invoiceConfigurationEntity.WellClassifications is { List: { } } &&
                    invoiceConfigurationEntity.WellClassifications.List.Any())
                {
                    invoiceConfigurationEntity.WellClassifications.List.ForEach(wc =>
                                                                                {
                                                                                    predicate = $"{MatchPredicateProperties.WellClassification}:{wc}";
                                                                                    predicates.Add(predicate);
                                                                                });
                }
            }

            if (invoiceConfigurationEntity.AllServiceTypes)
            {
                predicate = $"{MatchPredicateProperties.ServiceType}:{wildCard}";
                predicates.Add(predicate);
            }
            else
            {
                if (invoiceConfigurationEntity.ServiceTypes is { List: { } } &&
                    invoiceConfigurationEntity.ServiceTypes.List.Any())
                {
                    invoiceConfigurationEntity.ServiceTypes.List.ForEach(st =>
                                                                         {
                                                                             predicate = $"{MatchPredicateProperties.ServiceType}:{st}";
                                                                             predicates.Add(predicate);
                                                                         });
                }
            }

            var rankConfiguration = new RankConfiguration
            {
                EntityId = invoiceConfigurationEntity.Id,
                Name = invoiceConfigurationEntity.Name,
                Predicates = predicates.ToArray(),
            };

            rankConfigurationInput.Add(rankConfiguration);
            predicates = new();
        }

        return rankConfigurationInput;
    }

    private List<RankConfiguration> GeneratePredicateRankMatchingInputForBillingConfiguration(
        List<(MatchPredicateEntity predicate, BillingConfigurationEntity billingConfiguration)> billingConfigurationPredicates,
        string wildCard = "*")
    {
        var rankConfigurationInput = new List<RankConfiguration>();
        if (billingConfigurationPredicates == null || !billingConfigurationPredicates.Any())
        {
            return rankConfigurationInput;
        }
        //If All -> Wildcard *
        //If Values defined -> Split them into single records

        foreach (var billingConfigurationPredicate in billingConfigurationPredicates)
        {
            var predicate = billingConfigurationPredicate.predicate;
            var predicateMap = new List<string>();
            switch (predicate.SourceLocationValueState)
            {
                case MatchPredicateValueState.Any:
                    predicateMap.Add($"{MatchPredicateProperties.SourceLocation}:{wildCard}");
                    break;
                case MatchPredicateValueState.Value:
                    predicateMap.Add($"{MatchPredicateProperties.SourceLocation}:{predicate.SourceLocationId.GetValueOrDefault()}");
                    break;
            }

            switch (predicate.WellClassificationState)
            {
                case MatchPredicateValueState.Any:
                    predicateMap.Add($"{MatchPredicateProperties.WellClassification}:{wildCard}");
                    break;
                case MatchPredicateValueState.Value:
                    predicateMap.Add($"{MatchPredicateProperties.WellClassification}:{predicate.WellClassification}");
                    break;
            }

            switch (predicate.ServiceTypeValueState)
            {
                case MatchPredicateValueState.Any:
                    predicateMap.Add($"{MatchPredicateProperties.ServiceType}:{wildCard}");
                    break;
                case MatchPredicateValueState.Value:
                    predicateMap.Add($"{MatchPredicateProperties.ServiceType}:{predicate.ServiceTypeId.GetValueOrDefault()}");
                    break;
            }

            switch (predicate.SubstanceValueState)
            {
                case MatchPredicateValueState.Any:
                    predicateMap.Add($"{MatchPredicateProperties.Substance}:{wildCard}");
                    break;
                case MatchPredicateValueState.Value:
                    predicateMap.Add($"{MatchPredicateProperties.Substance}:{predicate.SubstanceId.GetValueOrDefault()}");
                    break;
            }

            switch (predicate.StreamValueState)
            {
                case MatchPredicateValueState.Any:
                    predicateMap.Add($"{MatchPredicateProperties.Stream}:{wildCard}");
                    break;
                case MatchPredicateValueState.Value:
                    predicateMap.Add($"{MatchPredicateProperties.Stream}:{predicate.Stream}");
                    break;
            }

            rankConfigurationInput.Add(new()
            {
                EntityId = billingConfigurationPredicate.billingConfiguration.Id,
                Name = billingConfigurationPredicate.billingConfiguration.Name,
                Predicates = predicateMap.ToArray(),
            });
        }

        return rankConfigurationInput;
    }

    private string[] TruckTicketProperties(TruckTicketEntity truckTicketEntity)
    {
        return new List<string>
        {
            $"{MatchPredicateProperties.SourceLocation}:{truckTicketEntity.SourceLocationId}",
            $"{MatchPredicateProperties.Facility}:{truckTicketEntity.FacilityId}",
            $"{MatchPredicateProperties.ServiceType}:{truckTicketEntity.ServiceTypeId.GetValueOrDefault()}",
            $"{MatchPredicateProperties.Substance}:{truckTicketEntity.SubstanceId}",
            $"{MatchPredicateProperties.WellClassification}:{truckTicketEntity.WellClassification}",
            $"{MatchPredicateProperties.Stream}:{truckTicketEntity.Stream}",
        }.ToArray();
    }
}
