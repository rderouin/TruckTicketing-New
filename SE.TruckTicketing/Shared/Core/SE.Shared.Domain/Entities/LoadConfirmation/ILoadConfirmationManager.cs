using System;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.LoadConfirmations;

using Trident.Contracts;

namespace SE.Shared.Domain.Entities.LoadConfirmation;

public interface ILoadConfirmationManager : IManager<Guid, LoadConfirmationEntity>
{
    Task<LoadConfirmationBulkResponse> QueueLoadConfirmationAction(LoadConfirmationBulkRequest request);
}
