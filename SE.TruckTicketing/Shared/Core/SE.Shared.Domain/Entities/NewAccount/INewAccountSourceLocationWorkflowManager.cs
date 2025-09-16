using System.Threading.Tasks;
using SE.Shared.Domain.Entities.SourceLocation;
using Trident.Contracts;

namespace SE.Shared.Domain.Entities.NewAccount;

public interface INewAccountSourceLocationWorkflowManager : IManager
{
    Task RunSourceLocationWorkflowValidation(SourceLocationEntity sourceLocation);
}
