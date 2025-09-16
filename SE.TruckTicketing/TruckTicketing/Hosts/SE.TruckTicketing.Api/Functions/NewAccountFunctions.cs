using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;

using SE.Shared.Common;
using SE.Shared.Domain.Entities.Account;
using SE.Shared.Domain.Entities.NewAccount;
using SE.Shared.Domain.Entities.SourceLocation;
using SE.TruckTicketing.Contracts.Api.Models;
using SE.TruckTicketing.Contracts.Models.Accounts;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.SourceLocations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Azure.Security;
using Trident.Logging;
using Trident.Mapper;
using Trident.Validation;

namespace SE.TruckTicketing.Api.Functions;

public sealed class NewAccountFunctions : IFunctionController
{
    private readonly ILog _appLogger;

    private readonly IMapperRegistry _mapper;

    private readonly INewAccountManager _newAccountManager;

    private readonly INewAccountSourceLocationWorkflowManager _newAccountSourceLocationWorkflowManager;

    private readonly INewAccountWorkflowManager _newAccountWorkflowManager;

    public NewAccountFunctions(ILog log,
                               IMapperRegistry mapper,
                               INewAccountManager newAccountManager,
                               INewAccountWorkflowManager newAccountWorkflowManager,
                               INewAccountSourceLocationWorkflowManager newAccountSourceLocationWorkflowManager)
    {
        _appLogger = log;
        _mapper = mapper;
        _newAccountManager = newAccountManager;
        _newAccountWorkflowManager = newAccountWorkflowManager;
        _newAccountSourceLocationWorkflowManager = newAccountSourceLocationWorkflowManager;
    }

    [Function(nameof(CreateNewAccount))]
    [OpenApiOperation(nameof(CreateNewAccount), nameof(NewAccountFunctions), Summary = nameof(RouteTypes.Create))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(NewAccountModel))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(NewAccountModel))]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.Account, Permissions.Operations.Write)]
    public async Task<HttpResponseData> CreateNewAccount([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.NewAccounts_BaseRoute)] HttpRequestData req)
    {
        HttpResponseData response;

        _appLogger.Information(messageTemplate: "Inside New Account Function - Create New Account - Received Request");
        try
        {
            //save Account
            var request = await req.ReadFromJsonAsync<NewAccountModel>();

            if (request?.Account.Id != default)
            {
                await _newAccountManager.CreateNewAccount(request);
                response = req.CreateResponse(HttpStatusCode.OK);
            }
            else
            {
                throw new ArgumentException("Invalid Account Id.");
            }

            _appLogger.Information(messageTemplate: "new Account Function - CreateNewAccount - Completed");
        }
        catch (ValidationRollupException validationRollupException)
        {
            _appLogger.Error(messageTemplate: $"{nameof(NewAccountModel)} - Validation Exception - {validationRollupException.Message}");
            response = req.CreateResponse();
            await response.WriteAsJsonAsync(validationRollupException.ValidationResults);
            response.StatusCode = HttpStatusCode.BadRequest;
        }
        catch (ArgumentException argEx)
        {
            _appLogger.Error(exception: argEx, messageTemplate: argEx.Message);
            response = req.CreateResponse(HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _appLogger.Error(exception: ex, messageTemplate: ex.Message);
            response = req.CreateResponse(HttpStatusCode.InternalServerError);
        }

        return response;
    }

    [Function(nameof(NewAccountWorkflowValidation))]
    [OpenApiOperation("NewAccountWorkflowValidation", nameof(NewAccountFunctions), Summary = nameof(RouteTypes.Create))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(Account))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(Account))]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.Account, Permissions.Operations.Read)]
    public async Task<HttpResponseData> NewAccountWorkflowValidation(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.NewAccounts_Account_ValidationRoute)] HttpRequestData req)
    {
        HttpResponseData response;

        _appLogger.Information(messageTemplate: "Inside New Account Function - Create New Account - Received Request");
        try
        {
            //save Account
            var json = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<Account>(json);
            _appLogger.Information(messageTemplate: $"{nameof(NewAccountWorkflowValidation)} - Received Request", propertyValues: new Dictionary<string, object> { { "Data", json } });

            if (request?.Id != default)
            {
                var entity = _mapper.Map<AccountEntity>(request);
                await _newAccountWorkflowManager.RunAccountWorkflowValidation(entity);
                response = req.CreateResponse(HttpStatusCode.OK);
            }
            else
            {
                throw new ArgumentException("Invalid Account Id.");
            }

            _appLogger.Information(messageTemplate: "Account Workflow Rule Validation Completed");
        }
        catch (ValidationRollupException validationRollupException)
        {
            _appLogger.Error(messageTemplate: $"{nameof(NewAccountModel)} - Validation Exception - {validationRollupException.Message}");
            response = req.CreateResponse();
            await response.WriteAsJsonAsync(validationRollupException.ValidationResults);
            response.StatusCode = HttpStatusCode.BadRequest;
        }
        catch (ArgumentException argEx)
        {
            _appLogger.Error(exception: argEx, messageTemplate: argEx.Message);
            response = req.CreateResponse(HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _appLogger.Error(exception: ex, messageTemplate: ex.Message);
            response = req.CreateResponse(HttpStatusCode.InternalServerError);
        }

        return response;
    }

    [Function(nameof(NewAccountSourceLocationWorkflowValidation))]
    [OpenApiOperation(nameof(NewAccountSourceLocationWorkflowValidation), nameof(NewAccountFunctions), Summary = nameof(RouteTypes.Create))]
    [OpenApiRequestBody(ContentTypes.JSON, typeof(SourceLocation))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ContentTypes.JSON, typeof(SourceLocation))]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.Account, Permissions.Operations.Read)]
    public async Task<HttpResponseData> NewAccountSourceLocationWorkflowValidation([HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post),
                                                                                                Route = Routes.NewAccounts_SourceLocation_ValidationRoute)]
                                                                                   HttpRequestData req)
    {
        HttpResponseData response;

        _appLogger.Information(messageTemplate: "Inside New Account Function - Create New Account - Received Request");
        try
        {
            //save Account
            var json = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<SourceLocation>(json);
            _appLogger.Information(messageTemplate: $"{nameof(NewAccountSourceLocationWorkflowValidation)} - Received Request", propertyValues: new Dictionary<string, object> { { "Data", json } });

            if (request?.Id != default)
            {
                var entity = _mapper.Map<SourceLocationEntity>(request);
                await _newAccountSourceLocationWorkflowManager.RunSourceLocationWorkflowValidation(entity);
                response = req.CreateResponse(HttpStatusCode.OK);
            }
            else
            {
                throw new ArgumentException("Invalid Account Id.");
            }

            _appLogger.Information(messageTemplate: "Account Source Location Workflow Rule Validation Completed");
        }
        catch (ValidationRollupException validationRollupException)
        {
            _appLogger.Error(messageTemplate: $"{nameof(NewAccountModel)} - Validation Exception - {validationRollupException.Message}");
            response = req.CreateResponse();
            await response.WriteAsJsonAsync(validationRollupException.ValidationResults);
            response.StatusCode = HttpStatusCode.BadRequest;
        }
        catch (ArgumentException argEx)
        {
            _appLogger.Error(exception: argEx, messageTemplate: argEx.Message);
            response = req.CreateResponse(HttpStatusCode.BadRequest);
        }
        catch (Exception ex)
        {
            _appLogger.Error(exception: ex, messageTemplate: ex.Message);
            response = req.CreateResponse(HttpStatusCode.InternalServerError);
        }

        return response;
    }

    [Function(nameof(GetAccountAttachmentDownloadUri))]
    [OpenApiOperation(nameof(GetAccountAttachmentDownloadUri), nameof(NewAccountFunctions), Summary = Routes.NewAccount.AttachmentDownload)]
    [OpenApiParameter(Routes.Parameters.id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiParameter(Routes.Parameters.AttachmentId, In = ParameterLocation.Path, Required = true, Type = typeof(Guid))]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(UriDto))]
    [OpenApiResponseWithoutBody(HttpStatusCode.NotFound)]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.Account, Permissions.Operations.Read)]
    public async Task<HttpResponseData> GetAccountAttachmentDownloadUri([HttpTrigger(nameof(HttpMethod.Get), Route = Routes.NewAccount.AttachmentDownload)] HttpRequestData httpRequestData,
                                                                        Guid id,
                                                                        Guid attachmentId)
    {
        HttpResponseData httpResponseData;
        _appLogger.Information(messageTemplate: $"{nameof(GetAccountAttachmentDownloadUri)} - Received Request for AccountId: {id} | AttachmentId: {attachmentId}");

        try
        {
            var uri = await _newAccountManager.GetAttachmentDownloadUri(id, attachmentId);
            httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.OK);
            await httpResponseData.WriteAsJsonAsync(new UriDto { Uri = uri });
        }
        catch (Exception e)
        {
            _appLogger.Error<HttpFunctionApiBase<Account, AccountEntity, Guid>>(e, e.Message);
            httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.InternalServerError);
        }

        return httpResponseData;
    }
}
