using System.Threading.Tasks;

using Trident.Contracts.Changes;

namespace SE.Shared.Domain.Entities.Changes;

public interface IChangeOrchestrator
{
    Task<bool> ProcessChange(ChangeModel changeModel);
}
