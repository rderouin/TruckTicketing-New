using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SE.Shared.Domain;

using Trident.Contracts.Configuration;
using Trident.EFCore;
using Trident.EFCore.Changes;
using Trident.EFCore.Contracts;
using Trident.IoC;
using Trident.Logging;

namespace SE.Shared.Functions;

public class TruckTicketingFacilityFilterCosmosDataContext : EFCoreCosmosDataContext
{
    public TruckTicketingFacilityFilterCosmosDataContext(IEFCoreModelBuilderFactory modelBuilderFactory,
                                                         IEntityMapFactory mapFactory,
                                                         string dataSource,
                                                         DbContextOptions options,
                                                         ILog log,
                                                         ILoggerFactory loggerFactory,
                                                         IAppSettings appSettings,
                                                         IIoCServiceLocator ioCServiceLocator,
                                                         IChangeObserver changeObserver)
        : base(modelBuilderFactory, mapFactory, dataSource, options, log, loggerFactory, appSettings, ioCServiceLocator, changeObserver)
    {
        var facilityFilterContextAccessor = ioCServiceLocator.Get<IFacilityQueryFilterContextAccessor>();
        var facilityFilterContext = facilityFilterContextAccessor?.FacilityQueryFilterContext;
        FacilityFilterCacheKey = string.Join("", facilityFilterContext?.AllowedFacilityIds ?? Array.Empty<Guid>());
    }

    public string FacilityFilterCacheKey { get; }
}
