using System;
using System.Collections.Generic;

using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.Facilities;
using SE.Shared.Domain.Entities.MaterialApproval;
using SE.Shared.Domain.Entities.SalesLine;
using SE.Shared.Domain.Entities.ServiceType;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.Shared.Domain.PricingRules;
using SE.Shared.Domain.Product;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Domain.Entities.FacilityService;

namespace SE.TruckTicketing.Domain.Entities.SalesLine;

public class SalesLinePreviewRequestContext
{
    public SalesLinePreviewRequest Request { get; set; }

    public FacilityEntity Facility { get; set; }

    public SourceLocationEntity SourceLocation { get; set; }

    public AccountEntity Account { get; set; }

    public AdditionalServicesConfig AdditionalServicesConfig { get; set; }

    public FacilityServiceSubstanceIndexEntity SubstanceIndex { get; set; }

    public MaterialApprovalEntity MaterialApproval { get; set; }

    public ServiceTypeEntity ServiceType { get; set; }

    public Dictionary<Guid, ProductEntity> ProductMap { get; set; }
    public List<SalesLineEntity> PersistedSalesLines { get; set; }
    public Dictionary<string, ComputePricingResponse> PricingByProductNumberMap { get; set; }
}
