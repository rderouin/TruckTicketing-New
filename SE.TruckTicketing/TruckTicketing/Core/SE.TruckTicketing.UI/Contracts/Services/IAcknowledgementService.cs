using System;

using SE.TruckTicketing.Contracts.Models.Acknowledgement;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface IAcknowledgementService : IServiceBase<Acknowledgement, Guid>
{
}
