using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.Entities.TruckTicket;
using SE.Shared.Domain.Entities.UserProfile;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Lookups;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Domain.Entities.FacilityService;

using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.Search;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

public class TruckTicketXlsxManager : ITruckTicketXlsxManager
{
    private readonly IProvider<Guid, ServiceTypeEntity> _serviceTypeProvider;

    private readonly ITruckTicketXlsxRenderer _truckTicketXlsxRenderer;

    private readonly IProvider<Guid, TruckTicketEntity> _truckTicketProvider;

    public TruckTicketXlsxManager(ILog log,
                                 IMapperRegistry mapper,
                                 ITruckTicketXlsxRenderer truckTicketXlsxRenderer,
                                 ITruckTicketManager truckTicketManager,
                                 IProvider<Guid, MaterialApprovalEntity> materialApprovalProvider,
                                 IProvider<Guid, FacilityEntity> facilityProvider,
                                 IProvider<Guid, TruckTicketEntity> truckTicketProvider,
                                 IProvider<Guid, BillingConfigurationEntity> billingConfigProvider,
                                 IProvider<Guid, ServiceTypeEntity> serviceTypeProvider,
                                 IProvider<Guid, SourceLocationEntity> sourceLocationProvider,
                                 IProvider<Guid, FacilityServiceEntity> facilityServiceProvider,
                                 IUserContextAccessor userContextAccessor,
                                 IUserProfileManager userManager)
    {
        _truckTicketXlsxRenderer = truckTicketXlsxRenderer;
        _truckTicketProvider = truckTicketProvider;
        _serviceTypeProvider = serviceTypeProvider;
    }

    public async Task<byte[]> CreateLandfillDailyReport(LandfillDailyReportRequest landfillRequest)
    {
        var allowedStatus = new List<TruckTicketStatus>
        {
            TruckTicketStatus.Approved,
            TruckTicketStatus.Hold,
            TruckTicketStatus.Open,
            TruckTicketStatus.Invoiced,
        };

        var searchCriteria = new SearchCriteria
        {
            Filters =
            {
                [nameof(TruckTicketEntity.FacilityId)] = landfillRequest.FacilityIds.AsInclusionAxiomFilter(nameof(TruckTicketEntity.FacilityId), CompareOperators.eq),
                [nameof(TruckTicketEntity.Status)] = allowedStatus.AsInclusionAxiomFilter(nameof(TruckTicketEntity.Status), CompareOperators.eq),
                [nameof(TruckTicketEntity.EffectiveDate)] = (landfillRequest.FromDate.Date, landfillRequest.ToDate.Date).AsRangeAxiomFilter(nameof(TruckTicketEntity.EffectiveDate)),
            },
            PageSize = int.MaxValue,
        };

        if (landfillRequest.SelectedClass != null)
        {
            searchCriteria.AddFilter(nameof(TruckTicketEntity.ServiceTypeClass), landfillRequest.SelectedClass);
        }

        var truckTickets = (await _truckTicketProvider.Search(searchCriteria))?.Results?.ToList() ?? new(); // PK - XP for TT by Facility ID

        var renderedTicket = _truckTicketXlsxRenderer.RenderLandfillDailyTicket(truckTickets, landfillRequest);

        return renderedTicket;
    }

    public async Task<byte[]> CreateFstDailyReport(FSTWorkTicketRequest fstReportRequest)
    {
        List<TruckTicketStatus> allowedStatus = fstReportRequest.SelectedTicketStatuses;

        var startDate = fstReportRequest.FromDate?.Date ?? DateTime.Today;
        var endDate = fstReportRequest.ToDate?.Date ?? DateTime.Today;

        var searchCriteria = new SearchCriteria
        {
            Filters =
            {
                [nameof(TruckTicketEntity.FacilityId)] = fstReportRequest.FacilityId,
                [nameof(TruckTicketEntity.Status)] = allowedStatus.AsInclusionAxiomFilter(nameof(TruckTicketEntity.Status), CompareOperators.eq),
                [nameof(TruckTicketEntity.EffectiveDate)] = (startDate, endDate).AsRangeAxiomFilter(nameof(TruckTicketEntity.EffectiveDate))
            },
            PageSize = Int32.MaxValue,
        };

        if (!string.IsNullOrEmpty(fstReportRequest.Destination))
        {
            var destinationLikeFilter = AxiomFilterBuilder.CreateFilter().StartGroup().AddAxiom(new()
            {
                Field = nameof(TruckTicketEntity.Destination),
                Value = fstReportRequest.Destination,
                Operator = CompareOperators.contains,
                Key = nameof(TruckTicketEntity.Destination),
            }).EndGroup().Build();

            searchCriteria.Filters[nameof(TruckTicketEntity.Destination)] = destinationLikeFilter;
        }


        searchCriteria.AddFilter(nameof(TruckTicketEntity.TotalVolume), new Compare
        {
            Operator = CompareOperators.gt,
            Value = 0d
        });

        if (fstReportRequest.ServiceTypeIds?.Any() ?? false)
        {
            searchCriteria.Filters[nameof(TruckTicketEntity.ServiceTypeId)] = fstReportRequest.ServiceTypeIds.AsInclusionAxiomFilter(nameof(TruckTicketEntity.ServiceTypeId), CompareOperators.eq);
            var serviceTypeNames = await _serviceTypeProvider.GetByIds(fstReportRequest.ServiceTypeIds);
            fstReportRequest.ServiceTypeName = string.Join(", ", serviceTypeNames.Select(id => id.Name));
        }

        var truckTickets = (await _truckTicketProvider.Search(searchCriteria))?.Results?.ToList() ?? new(); // PK - XP for TT by Facility ID

        var renderedTicket = _truckTicketXlsxRenderer.RenderFstDailyReport(truckTickets, fstReportRequest);

        return renderedTicket;
    }
}
