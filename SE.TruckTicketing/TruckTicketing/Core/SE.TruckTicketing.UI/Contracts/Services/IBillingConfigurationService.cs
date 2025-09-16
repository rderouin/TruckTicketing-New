using System;

using SE.TruckTicketing.Contracts.Models.Operations;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface IBillingConfigurationService : IServiceBase<BillingConfiguration, Guid>
{
}
