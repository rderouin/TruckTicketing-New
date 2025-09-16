using System;

using Trident.Business;
using Trident.Search;

namespace SE.Shared.Domain.Entities.TruckTicket;

public class TruckTicketProvider : ProviderBase<Guid, TruckTicketEntity>
{
    public TruckTicketProvider(ISearchRepository<TruckTicketEntity> repository) : base(repository)
    {
    }
}
