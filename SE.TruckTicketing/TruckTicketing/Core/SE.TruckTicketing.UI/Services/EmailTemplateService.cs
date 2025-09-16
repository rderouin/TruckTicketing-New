using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using SE.TruckTicketing.Contracts.Models.ContentGeneration;
using SE.TruckTicketing.Contracts.Models.Email;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Contracts.Api.Client;
using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Routes.EmailTemplates.Base)]
public class EmailTemplateService : ServiceBase<EmailTemplateService, EmailTemplate, Guid>, IEmailTemplateService
{
    public EmailTemplateService(ILogger<EmailTemplateService> logger, IHttpClientFactory httpClientFactory) : base(logger, httpClientFactory)
    {
    }

    public async Task<Response<object>> SendEmail(EmailTemplateDeliveryRequestModel requestModel)
    {
        var response = await SendRequest<object>(HttpMethod.Post.Method, Routes.EmailTemplates.Delivery, requestModel);
        return response;
    }

    public async Task<AdHocAttachmentModel> GetAdhocAttachmentUploadUri(AdHocAttachmentModel attachment)
    {
        var response = await SendRequest<AdHocAttachmentModel>(HttpMethod.Post.Method, Routes.EmailTemplates.AdhocAttachmentUpload, attachment);
        return response.Model;
    }
}

public interface IEmailTemplateService
{
    Task<Response<object>> SendEmail(EmailTemplateDeliveryRequestModel requestModel);

    Task<AdHocAttachmentModel> GetAdhocAttachmentUploadUri(AdHocAttachmentModel attachment);
}
