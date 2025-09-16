using System;

using Trident.Business;
using Trident.Search;

namespace SE.TruckTicketing.Domain.Entities.Configuration;

public class NavigationConfigurationProvider : ProviderBase<Guid, NavigationConfigurationEntity>
{
    public NavigationConfigurationProvider(ISearchRepository<NavigationConfigurationEntity> repository) : base(repository)
    {
    }
}
