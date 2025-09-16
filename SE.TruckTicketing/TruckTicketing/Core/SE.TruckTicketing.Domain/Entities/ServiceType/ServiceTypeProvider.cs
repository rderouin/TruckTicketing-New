using System;

using SE.Shared.Domain.Entities.ServiceType;

using Trident.Business;
using Trident.Search;

namespace SE.TruckTicketing.Domain.Entities.ServiceType;

public class ServiceTypeProvider : ProviderBase<Guid, ServiceTypeEntity>
{
    public ServiceTypeProvider(ISearchRepository<ServiceTypeEntity> repository) : base(repository)
    {
    }
}
