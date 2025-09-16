using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SE.Shared.Common.Extensions;
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

using Trident.Contracts.Api;
using Trident.Data.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.Search;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket.LocalReporting;

public class TruckTicketPdfManager : ITruckTicketPdfManager
{
    private readonly IProvider<Guid, BillingConfigurationEntity> _billingConfigProvider;

    private readonly IProvider<Guid, FacilityEntity> _facilityProvider;

    private readonly IProvider<Guid, FacilityServiceEntity> _facilityServiceProvider;

    private readonly IMapperRegistry _mapper;

    private readonly IProvider<Guid, MaterialApprovalEntity> _materialApprovalProvider;

    private readonly IProvider<Guid, ServiceTypeEntity> _serviceTypeProvider;

    private readonly IProvider<Guid, SourceLocationEntity> _sourceLocationProvider;

    private readonly ITruckTicketManager _truckTicketManager;

    private readonly ITruckTicketPdfRenderer _truckTicketPdfRenderer;

    private readonly IProvider<Guid, TruckTicketEntity> _truckTicketProvider;

    private readonly IUserContextAccessor _userContextAccessor;

    private readonly IUserProfileManager _userManager;

    public TruckTicketPdfManager(ILog log,
                                 IMapperRegistry mapper,
                                 ITruckTicketPdfRenderer truckTicketPdfRenderer,
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
        _mapper = mapper;
        _truckTicketManager = truckTicketManager;
        _truckTicketPdfRenderer = truckTicketPdfRenderer;
        _materialApprovalProvider = materialApprovalProvider;
        _facilityProvider = facilityProvider;
        _truckTicketProvider = truckTicketProvider;
        _billingConfigProvider = billingConfigProvider;
        _userContextAccessor = userContextAccessor;
        _serviceTypeProvider = serviceTypeProvider;
        _sourceLocationProvider = sourceLocationProvider;
        _facilityServiceProvider = facilityServiceProvider;
        _userManager = userManager;
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

        var renderedTicket = _truckTicketPdfRenderer.RenderLandfillDailyTicket(truckTickets, landfillRequest);

        return renderedTicket;
    }

    public async Task<byte[]> CreateTicketPrint(CompositeKey<Guid> truckTicketKey)
    {
        var truckTicket = await _truckTicketManager.GetById(truckTicketKey);

        if (truckTicket.TruckTicketType == TruckTicketType.WT)
        {
            return CreateWorkTicket(truckTicket);
        }

        return await CreateScaleTicket(truckTicket);
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
        
        var renderedTicket = _truckTicketPdfRenderer.RenderFstDailyReport(truckTickets, fstReportRequest);

        return renderedTicket;
    }

    public async Task<byte[]> CreateLoadSummaryReport(LoadSummaryReportRequest loadSummaryRequest)
    {
        var facility = await _facilityProvider.GetById(loadSummaryRequest.FacilityId);

        var materialApprovalIds = loadSummaryRequest.MaterialApprovalIds
                                                    .Where(guid => guid != Guid.Empty)
                                                    .Distinct()
                                                    .ToArray();

        var materialApprovals = (await _materialApprovalProvider.GetByIds(materialApprovalIds))?
                               .ToDictionary(materialApproval => materialApproval.Id) ?? new();

        var startDate = loadSummaryRequest.FromDate?.Date ?? DateTime.Today;
        var endDate = loadSummaryRequest.ToDate?.Date ?? DateTime.Today;
        
        List<TruckTicketEntity> truckTickets;

        if (!loadSummaryRequest.TruckingCompanyIds.Any())
        {
            truckTickets = (await _truckTicketProvider.Get(ticket => loadSummaryRequest.MaterialApprovalIds.Contains(ticket.MaterialApprovalId) &&
                                                                     ticket.EffectiveDate >= startDate &&
                                                                     ticket.EffectiveDate <= endDate &&
                                                                     ticket.TimeOut != null &&
                                                                     ticket.NetWeight > 0 &&
                                                                     ticket.Status != TruckTicketStatus.Void)).ToList();
        }
        else
        {
            truckTickets = (await _truckTicketProvider.Get(ticket => loadSummaryRequest.MaterialApprovalIds.Contains(ticket.MaterialApprovalId) &&
                                                                     loadSummaryRequest.TruckingCompanyIds.Contains(ticket.TruckingCompanyId) && //Bug 11670
                                                                     ticket.EffectiveDate >= startDate &&
                                                                     ticket.EffectiveDate <= endDate &&
                                                                     ticket.TimeOut != null &&
                                                                     ticket.NetWeight > 0 &&
                                                                     ticket.Status != TruckTicketStatus.Void)).ToList();
        }


        var renderedTicket = _truckTicketPdfRenderer.RenderLoadSummaryTicket(truckTickets, materialApprovals, loadSummaryRequest, facility);

        return renderedTicket;
    }

    public async Task<byte[]> CreateProducerReport(ProducerReportRequest producerReportRequest)
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
                [nameof(TruckTicketEntity.EffectiveDate)] = (producerReportRequest.FromDate.Date, producerReportRequest.ToDate.Date).AsRangeAxiomFilter(nameof(TruckTicketEntity.EffectiveDate)),
                [nameof(TruckTicketEntity.Status)] = allowedStatus.AsInclusionAxiomFilter(nameof(TruckTicketEntity.Status), CompareOperators.eq),
            },
            PageSize = Int32.MaxValue,
        };

        if (producerReportRequest.SourceLocationIds?.Any() ?? false)
        {
            searchCriteria.Filters[nameof(TruckTicketEntity.SourceLocationId)] =
                producerReportRequest.SourceLocationIds.AsInclusionAxiomFilter(nameof(TruckTicketEntity.SourceLocationId), CompareOperators.eq);
        }

        if (producerReportRequest.FacilityIds?.Any() ?? false)
        {
            searchCriteria.Filters[nameof(TruckTicketEntity.FacilityId)] =
                producerReportRequest.FacilityIds.AsInclusionAxiomFilter(nameof(TruckTicketEntity.FacilityId), CompareOperators.eq);
        }

        if (producerReportRequest.GeneratorIds?.Any() ?? false)
        {
            searchCriteria.Filters[nameof(TruckTicketEntity.GeneratorId)] =
                producerReportRequest.GeneratorIds.AsInclusionAxiomFilter(nameof(TruckTicketEntity.GeneratorId), CompareOperators.eq);
        }

        if (producerReportRequest.ServiceTypeIds?.Any() ?? false)
        {
            var criteria = new SearchCriteria
            {
                PageSize = Int32.MaxValue,
            };

            criteria.Filters[nameof(FacilityServiceEntity.ServiceTypeId)] =
                producerReportRequest.ServiceTypeIds.AsInclusionAxiomFilter(nameof(FacilityServiceEntity.ServiceTypeId), CompareOperators.eq);

            var facilityServiceIds = (await _facilityServiceProvider.Search(criteria))?.Results?.ToList() ?? new();
            if (facilityServiceIds?.Any() ?? false)
            {
                producerReportRequest.FacilityServiceNames = facilityServiceIds.Select(s => s.FacilityServiceNumber).ToList();
                var ids = facilityServiceIds.Select(fs => fs.Id).ToList();
                searchCriteria.Filters[nameof(TruckTicketEntity.FacilityServiceId)] =
                    ids.AsInclusionAxiomFilter(nameof(TruckTicketEntity.FacilityServiceId), CompareOperators.eq);
            }
        }

        if (producerReportRequest.TruckingCompanyIds?.Any() ?? false)
        {
            searchCriteria.Filters[nameof(TruckTicketEntity.TruckingCompanyId)] =
                producerReportRequest.TruckingCompanyIds.AsInclusionAxiomFilter(nameof(TruckTicketEntity.TruckingCompanyId), CompareOperators.eq);
        }

        var truckTickets = (await _truckTicketProvider.Search(searchCriteria))?.Results?.ToList() ?? new(); // PK - XP for TT by Date and Status
        var renderedTicket = _truckTicketPdfRenderer.RenderProducerReport(truckTickets, producerReportRequest);
        return renderedTicket;
    }

    private async Task<byte[]> CreateScaleTicket(TruckTicketEntity truckTicket)
    {
        var signature = string.Empty;
        var materialApproval = await _materialApprovalProvider.GetById(truckTicket.MaterialApprovalId);
        var facility = await _facilityProvider.GetById(truckTicket.FacilityId);

        if (!string.IsNullOrEmpty(materialApproval?.SecureRepresentativeId))
        {
            await using var stream = await _userManager.DownloadSignature(materialApproval.SecureRepresentativeId);
            if (stream != null)
            {
                var byteSignature = await stream.ReadAll();
                signature = Convert.ToBase64String(byteSignature);
            }
        }

        var renderedTicket = _truckTicketPdfRenderer.RenderScaleTicket(truckTicket, materialApproval, facility, signature);
        return renderedTicket;
    }

    private byte[] CreateWorkTicket(TruckTicketEntity truckTicket)
    {
        var renderedTicket = _truckTicketPdfRenderer.RenderWorkTicket(truckTicket);
        return renderedTicket;
    }
}

public static class AxiomFilterBuilderExtensions
{
    public static AxiomFilter AsRangeAxiomFilter<T>(this (T start, T end) range, string filterPath)
    {
        var (start, end) = range;

        IJunction query = AxiomFilterBuilder.CreateFilter().StartGroup();
        var index = 0;

        if (start is not null)
        {
            query = ((GroupStart)query).AddAxiom(new()
            {
                Field = filterPath,
                Key = $"{filterPath}{++index}",
                Operator = CompareOperators.gte,
                Value = start,
            });
        }

        if (start is not null && end is not null)
        {
            query = ((AxiomTokenizer)query).And();
        }

        if (end is not null)
        {
            if (query is ILogicalOperator and)
            {
                query = and.AddAxiom(new()
                {
                    Field = filterPath,
                    Key = $"{filterPath}{++index}",
                    Operator = CompareOperators.lte,
                    Value = end,
                });
            }
            else
            {
                query = ((GroupStart)query).AddAxiom(new()
                {
                    Field = filterPath,
                    Key = $"{filterPath}{++index}",
                    Operator = CompareOperators.lte,
                    Value = end,
                });
            }
        }

        return ((AxiomTokenizer)query)
              .EndGroup()
              .Build();
    }

    public static AxiomFilter AsInclusionAxiomFilter<T>(this ICollection<T> values, string filterPath, CompareOperators compareOperator = CompareOperators.contains)
    {
        if (!values.Any())
        {
            return null;
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
                    Operator = compareOperator,
                    Value = value,
                });
            }
            else if (query is AxiomTokenizer axiom)
            {
                query = axiom.Or().AddAxiom(new()
                {
                    Key = $"{filterPath}{++index}".Replace(".", string.Empty),
                    Field = filterPath,
                    Operator = compareOperator,
                    Value = value,
                });
            }
        }

        return ((AxiomTokenizer)query).EndGroup().Build();
    }
}
