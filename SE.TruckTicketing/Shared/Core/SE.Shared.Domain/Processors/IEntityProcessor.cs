using System.Threading.Tasks;

using SE.Enterprise.Contracts.Models;

namespace SE.Shared.Domain.Processors;

public interface IEntityProcessor
{
    Task Process(string message);
}

public interface IEntityProcessor<T> : IEntityProcessor
{
    Task Process(EntityEnvelopeModel<T> entityModel);
}
