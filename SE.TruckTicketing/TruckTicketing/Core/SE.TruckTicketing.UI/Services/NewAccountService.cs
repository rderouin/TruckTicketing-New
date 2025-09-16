using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Contracts.Api.Client;
using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Service.Resources.newaccounts)]
public class NewAccountService : ServiceBase<NewAccountService, NewAccountModel, Guid>, INewAccountService
{
    public NewAccountService(ILogger<NewAccountService> logger,
                             IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }

    public async Task<Response<NewAccountModel>> CreateNewAccount(NewAccountModel model)
    {
        var response = await SendRequest<NewAccountModel>(HttpMethod.Post.Method, Routes.NewAccounts_BaseRoute, model);
        return response;
    }

    public async Task<Response<Account>> AccountWorkflowValidation(Account model)
    {
        var response = await SendRequest<Account>(CreateMethod, Routes.NewAccounts_Account_ValidationRoute, model);
        return response;
    }

    public async Task<Response<SourceLocation>> SourceLocationWorkflowValidation(SourceLocation model)
    {
        var response = await SendRequest<SourceLocation>(CreateMethod, Routes.NewAccounts_SourceLocation_ValidationRoute, model);
        return response;
    }
}
