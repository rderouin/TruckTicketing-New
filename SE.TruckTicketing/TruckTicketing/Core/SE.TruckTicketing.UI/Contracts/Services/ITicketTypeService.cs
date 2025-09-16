using System;

using SE.TruckTicketing.Contracts.Models;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface ITicketTypeService : IServiceBase<TicketType, Guid>
{
}
