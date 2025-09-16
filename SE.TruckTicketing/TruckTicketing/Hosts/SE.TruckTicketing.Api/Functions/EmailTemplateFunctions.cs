using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

using SE.Shared.Domain.EmailTemplates;
using SE.TridentContrib.Extensions.Security;
using SE.TruckTicketing.Contracts.Models.ContentGeneration;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.Contracts.Security;

using Trident.Azure.Functions;
using Trident.Azure.Security;
using Trident.Contracts;
using Trident.Extensions.OpenApi.Attributes;
using Trident.Logging;
using Trident.Mapper;
using Trident.SourceGeneration.Attributes;

namespace SE.TruckTicketing.Api.Functions;

[UseHttpFunction(HttpFunctionApiMethod.GetById, Route = Routes.EmailTemplates.Id,
                 ClaimsAuthorizeResource = Permissions.Resources.EmailTemplate,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Search, Route = Routes.EmailTemplates.Search,
                 ClaimsAuthorizeResource = Permissions.Resources.EmailTemplate,
                 ClaimsAuthorizeOperation = Permissions.Operations.Read)]
[UseHttpFunction(HttpFunctionApiMethod.Create, Route = Routes.EmailTemplates.Base,
                 ClaimsAuthorizeResource = Permissions.Resources.EmailTemplate,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Update, Route = Routes.EmailTemplates.Id,
                 ClaimsAuthorizeResource = Permissions.Resources.EmailTemplate,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
[UseHttpFunction(HttpFunctionApiMethod.Delete, Route = Routes.EmailTemplates.Id,
                 ClaimsAuthorizeResource = Permissions.Resources.EmailTemplate,
                 ClaimsAuthorizeOperation = Permissions.Operations.Write)]
public partial class EmailTemplateFunctions : HttpFunctionApiBase<EmailTemplate, EmailTemplateEntity, Guid>
{
    private readonly IEmailTemplateAttachmentManager _attachmentManager;

    private readonly IEmailTemplateSender _emailTemplateSender;

    private readonly IUserContextAccessor _userContextAccessor;

    public EmailTemplateFunctions(ILog log,
                                  IMapperRegistry mapper,
                                  IManager<Guid, EmailTemplateEntity> manager,
                                  IEmailTemplateAttachmentManager attachmentManager,
                                  IUserContextAccessor userContextAccessor,
                                  IEmailTemplateSender emailTemplateSender) : base(log, mapper, manager)
    {
        _attachmentManager = attachmentManager;
        _userContextAccessor = userContextAccessor;
        _emailTemplateSender = emailTemplateSender;
    }

    [Function(nameof(EmailTemplateAdhocAttachmentUpload))]
    [OpenApiOperation(nameof(EmailTemplateAdhocAttachmentUpload), nameof(EmailTemplateFunctions), Summary = Routes.EmailTemplates.AdhocAttachmentUpload)]
    [OpenApiResponseWithBody(HttpStatusCode.OK, MediaTypeNames.Application.Json, typeof(AdHocAttachment))]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.EmailTemplate, Permissions.Operations.Read)]
    public async Task<HttpResponseData> EmailTemplateAdhocAttachmentUpload(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.EmailTemplates.AdhocAttachmentUpload)] HttpRequestData httpRequestData)
    {
        return await HandleRequest(httpRequestData,
                                   nameof(EmailTemplateAdhocAttachmentUpload),
                                   async httpResponseData =>
                                   {
                                       var request = await httpRequestData.ReadFromJsonAsync<AdHocAttachment>();
                                       await httpResponseData.WriteAsJsonAsync(_attachmentManager.GetUploadUrl(request));
                                   });
    }

    [Function(nameof(EmailTemplateDelivery))]
    [OpenApiOperation(nameof(EmailTemplateDelivery), nameof(EmailTemplateFunctions), Summary = Routes.EmailTemplates.Delivery)]
    [OpenApiResponseWithoutBody(HttpStatusCode.OK)]
    [OpenApiResponseWithoutBody(HttpStatusCode.Accepted)]
    [OpenApiResponseWithoutBody(HttpStatusCode.BadRequest)]
    [OpenApiResponseWithoutBody(HttpStatusCode.InternalServerError)]
    [ClaimsAuthorize(Permissions.Resources.EmailTemplate, Permissions.Operations.Read)]
    public async Task<HttpResponseData> EmailTemplateDelivery(
        [HttpTrigger(AuthorizationLevel.Anonymous, nameof(HttpMethod.Post), Route = Routes.EmailTemplates.Delivery)] HttpRequestData httpRequestData)
    {
        return await HandleRequest(httpRequestData,
                                   nameof(EmailTemplateDelivery),
                                   async httpResponseData =>
                                   {
                                       var request = await httpRequestData.ReadFromJsonAsync<EmailTemplateDeliveryRequest>();
                                       if (request?.ContextBag is not null)
                                       {
                                           request.ContextBag[nameof(UserContext)] = _userContextAccessor.UserContext;
                                       }

                                       await _emailTemplateSender.Dispatch(request);
                                       httpResponseData.StatusCode = request!.IsSynchronous ? HttpStatusCode.OK : HttpStatusCode.Accepted;
                                   });
    }
}
