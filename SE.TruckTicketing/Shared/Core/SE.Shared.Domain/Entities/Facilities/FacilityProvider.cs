using System;

using Trident.Business;
using Trident.Search;

namespace SE.Shared.Domain.Entities.Facilities;

public class FacilityProvider : ProviderBase<Guid, FacilityEntity>
{
    public FacilityProvider(ISearchRepository<FacilityEntity> repository) : base(repository)
    {
    }
}
