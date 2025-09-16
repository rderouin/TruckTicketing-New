using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;

using SE.Shared.Common;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.NewAccount;
using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Azure.Security;
using Trident.Contracts;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;
using Trident.Validation;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById,
                 Route = Routes.Accounts_IdRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Search,
                 Route = Routes.Accounts_SearchRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Create,
                 Route = Routes.Accounts_BaseRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Update,
                 Route = Routes.Accounts_IdRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Patch, Route = Routes.Accounts_IdRoute,
                 ClaimsAuthorizeResource = Permissions.Resources.Account,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class AccountFunctions : HttpFunctionApiBase<Account, AccountEntity, Guid>
{
    private readonly ILog _appLogger;

    private readonly IMapperRegistry _mapper;

    private readonly INewAccountManager _newAccountManager;

    public AccountFunctions(ILog log, IMapperRegistry mapper, IManager<Guid, AccountEntity> manager, INewAccountManager newAccountManager) : base(log, mapper, manager)
    {
        _mapper = mapper;
        _appLogger = log;
        _newAccountManager = newAccountManager;
    }

    [Function(nameof(InitiateAccountCreditReviewal))]
    [OpenApiOperation(nameof(InitiateAccountCreditReviewal), nameof(AccountFunctions), Summary = nameof(Routes.Accounts_InitiateCreditReviewal))]
    [OpenApiRequestBody("application/json", typeof(Account))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(Account))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.Account, Permissions.Operations.Write)]
    public async Task<HttpResponseData> InitiateAccountCreditReviewal(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.Accounts_InitiateCreditReviewal)] HttpRequestData req)
    {
        var response = req.CreateResponse();
        response.StatusCode = HttpStatusCode.OK;
        try
        {
            var request = await req.ReadFromJsonAsync<InitiateAccountCreditReviewalRequest>();

            _appLogger.Information<HttpFunctionApiBase<Account, AccountEntity, Guid>>(messageTemplate: $"{nameof(InitiateAccountCreditReviewal)} - Received Request",
                                                                                      propertyValues: new Dictionary<string, object> { { "Data", request } });

            await _newAccountManager.InitiateNewAccountCreditReviewal(request);

            response = req.CreateResponse(HttpStatusCode.OK);
            _appLogger.Information<HttpFunctionApiBase<Account, AccountEntity, Guid>>(messageTemplate: $"{nameof(InitiateAccountCreditReviewal)} - Completed");
        }
        catch (ValidationRollupException validationRollupException)
        {
            _appLogger.Error<HttpFunctionApiBase<Account, AccountEntity, Guid>>(messageTemplate: $"{nameof(Account)} - Validation Exception - {validationRollupException.Message}");
            response = req.CreateResponse();
            await response.WriteAsJsonAsync(validationRollupException.ValidationResults);
            response.StatusCode = HttpStatusCode.BadRequest;
        }
        catch (ArgumentException argEx)
        {
            _appLogger.Error<HttpFunctionApiBase<Account, AccountEntity, Guid>>(argEx, argEx.Message);
            response = req.CreateResponse(HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _appLogger.Error<HttpFunctionApiBase<Account, AccountEntity, Guid>>(ex, ex.Message);
            response = req.CreateResponse(HttpStatusCode.InternalServerError);
        }

        return response;
    }
}
