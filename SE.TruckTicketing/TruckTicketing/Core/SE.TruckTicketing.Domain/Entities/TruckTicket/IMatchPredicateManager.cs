using System.Collections.Generic;
using System.Threading.Tasks;

using SE.Shared.Domain.Entities.BillingConfiguration;
using SE.Shared.Domain.Entities.TruckTicket;

using Trident.Contracts;

namespace SE.TruckTicketing.Domain.Entities.TruckTicket;

public interface IMatchPredicateManager : IManager
{
    Task<List<BillingConfigurationEntity>> GetOverlappingBillingConfigurations(BillingConfigurationEntity billingConfigurationEntityEntity);

    Task<List<BillingConfigurationEntity>> GetBillingConfigurations(TruckTicketEntity truckTicketEntity, bool includeForAutomation);

    Task<List<BillingConfigurationEntity>> GetBillingConfigurations(TruckTicketEntity truckTicketEntity);

    Task<BillingConfigurationEntity> GetMatchingBillingConfiguration(List<BillingConfigurationEntity> billingConfigurations, TruckTicketEntity truckTicketEntity);

    BillingConfigurationEntity SelectAutomatedBillingConfiguration(List<BillingConfigurationEntity> billingConfigurations, TruckTicketEntity truckTicketEntity);
}
