using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Facilities;

using Trident.Business;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Search;
using Trident.Validation;
using Trident.Workflow;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

public class TruckTicketTareWeightManager : ManagerBase<Guid, TruckTicketTareWeightEntity>, ITruckTicketTareWeightManager
{
    private readonly IProvider<Guid, TruckTicketTareWeightEntity> _tareWeightProvider;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    public TruckTicketTareWeightManager(ILog logger,
                                        IProvider<Guid, TruckTicketTareWeightEntity> provider,
                                        IProvider<Guid, FacilityEntity> facilityProvider,
                                        IValidationManager<TruckTicketTareWeightEntity> validationManager = null,
                                        IWorkflowManager<TruckTicketTareWeightEntity> workflowManager = null)
        : base(logger, provider, validationManager, workflowManager)
    {
        _tareWeightProvider = provider;
        _facilityProvider = facilityProvider;
    }

    public Task<List<TruckTicketTareWeightCsvResponse>> TruckTicketTareWeightCsvProcessing(IEnumerable<TruckTicketTareWeightCsvResponse> request)
    {
        var processedList = new List<TruckTicketTareWeightCsvResponse>();
        foreach (var truckTicketTareWeightCsvResponse in request.ToList())
        {
            if (!double.TryParse(truckTicketTareWeightCsvResponse.TareWeight, out double tareWeightResult))
            {
                continue;
            }

            var searchCriteria = new SearchCriteria();

            searchCriteria.AddFilter(nameof(TruckTicketTareWeightEntity.TruckNumber), truckTicketTareWeightCsvResponse.TruckNumber);
            searchCriteria.Filters[nameof(TruckTicketTareWeightEntity.TruckingCompanyName)] = new Compare
            {
                Operator = CompareOperators.ne,
                Value = truckTicketTareWeightCsvResponse.TruckingCompanyName,
            };

            var duplicateTruckNumber = _tareWeightProvider.Search(searchCriteria);
            if (duplicateTruckNumber != default && duplicateTruckNumber.Result.Results.Any())
            {
                truckTicketTareWeightCsvResponse.Result = "Truck Number exist with another company";
                processedList.Add((truckTicketTareWeightCsvResponse));

                continue;
            }

            var facilitySearchCriteria = new SearchCriteria();
            facilitySearchCriteria.AddFilter(nameof(FacilityEntity.SiteId), truckTicketTareWeightCsvResponse.FacilitySiteId);
            var facilityResponse = _facilityProvider.Search(facilitySearchCriteria);
            Guid? facilityId = facilityResponse?.Result.Results?.FirstOrDefault()?.Id;

            if (facilityResponse is null || facilityId is null)
            {
                truckTicketTareWeightCsvResponse.Result = "Facility does not exist";
                processedList.Add(truckTicketTareWeightCsvResponse);

                continue;
            }

            var truckTicketTareWeightEntity = new TruckTicketTareWeightEntity()
            {
                IsActivated = true,
                FacilityId = facilityId.Value,
                FacilityName = facilityResponse.Result.Results.FirstOrDefault()?.Name,
                FacilitySiteId = facilityResponse.Result.Results.FirstOrDefault()?.SiteId,
                TrailerNumber = truckTicketTareWeightCsvResponse.TrailerNumber,
                TruckingCompanyName = truckTicketTareWeightCsvResponse.TruckingCompanyName,
                TruckNumber = truckTicketTareWeightCsvResponse.TruckNumber,
                Id = Guid.NewGuid(),
                LoadDate = DateTimeOffset.UtcNow,
                TareWeight = tareWeightResult
            };

            //duplicate Tare weight index check
            var topIndex = (_tareWeightProvider.Get(entity => entity.IsActivated == true
                                                           && entity.TruckNumber == truckTicketTareWeightEntity.TruckNumber
                                                           && entity.TruckingCompanyName ==
                                                              truckTicketTareWeightEntity.TruckingCompanyName
                                                           && entity.FacilityId == truckTicketTareWeightEntity.FacilityId
                                                           && entity.TareWeight.Equals(tareWeightResult)
                                                           && entity.TrailerNumber == truckTicketTareWeightEntity.TrailerNumber)).Result?.MaxBy(entity => entity.LoadDate);

            if (topIndex == default)
            {
                _tareWeightProvider.Insert(truckTicketTareWeightEntity);
            }
            else
            {
                truckTicketTareWeightCsvResponse.Result = "Tare weight with this combination already exist";
                processedList.Add(truckTicketTareWeightCsvResponse);
            }
        }

        return Task.FromResult(processedList);
    }
}
