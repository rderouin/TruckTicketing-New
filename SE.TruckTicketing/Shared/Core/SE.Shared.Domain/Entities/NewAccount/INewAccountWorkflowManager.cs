using System.Threading.Tasks;

using SE.Shared.Domain.Entities.Account;

using Trident.Contracts;

namespace SE.Shared.Domain.Entities.NewAccount;

public interface INewAccountWorkflowManager : IManager
{
    Task RunAccountWorkflowValidation(AccountEntity account);
}
