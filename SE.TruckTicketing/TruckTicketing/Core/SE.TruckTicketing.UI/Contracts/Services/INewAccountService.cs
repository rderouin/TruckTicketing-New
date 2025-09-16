using System;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface INewAccountService : IServiceBase<NewAccountModel, Guid>
{
    Task<Response<NewAccountModel>> CreateNewAccount(NewAccountModel model);

    Task<Response<Account>> AccountWorkflowValidation(Account model);

    Task<Response<SourceLocation>> SourceLocationWorkflowValidation(SourceLocation model);
}
