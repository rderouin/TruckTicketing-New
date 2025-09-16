using System;

using Trident.Business;
using Trident.Search;

namespace SE.TruckTicketing.Domain.Entities.SpartanProductParameters;

public class SpartanProductParameterProvider : ProviderBase<Guid, SpartanProductParameterEntity>
{
    public SpartanProductParameterProvider(ISearchRepository<SpartanProductParameterEntity> repository) : base(repository)
    {
    }
}
