using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using SE.TruckTicketing.Contracts.Models.Email;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Contracts.Api;
using Trident.Contracts.Api.Client;
using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Routes.SalesLine.Base)]
public class SalesLineService : ServiceBase<SalesLineService, SalesLine, Guid>, ISalesLineService
{
    public SalesLineService(ILogger<SalesLineService> logger, IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }

    public async Task<Response<object>> GenerateAdHocLoadConfirmation(LoadConfirmationAdhocModel adhocModel)
    {
        var url = Routes.SalesLine.PreviewLoadConfirmation;
        var response = await SendRequest<object>(HttpMethod.Post.Method, url, adhocModel);
        return response;
    }

    public async Task<Response<object>> SendAdHocLoadConfirmation(EmailTemplateDeliveryRequestModel emailTemplateDeliveryRequest)
    {
        var url = Routes.SalesLine.SendAdHocLoadConfirmation;
        var response = await SendRequest<object>(HttpMethod.Post.Method, url, emailTemplateDeliveryRequest);
        return response;
    }

    public async Task<List<SalesLine>> GetPreviewSalesLines(SalesLinePreviewRequest salesLinePreviewRequest)
    {
        var url = Routes.SalesLine.Preview;
        var response = await SendRequest<object>(HttpMethod.Post.ToString(), url, salesLinePreviewRequest);
        return JsonConvert.DeserializeObject<List<SalesLine>>(response.ResponseContent);
    }

    public async Task<Response<IEnumerable<SalesLine>>> BulkSaveForTruckTicket(IEnumerable<SalesLine> salesLines, Guid truckTicketId)
    {
        var url = Routes.TruckTickets.SalesLineBulkSave.Replace("{id}", truckTicketId.ToString());
        var response = await SendRequest<IEnumerable<SalesLine>>(HttpMethod.Post.ToString(), url, salesLines);

        return response;
    }

    public async Task<List<SalesLine>> BulkSave(SalesLineResendAckRemovalRequest salesLines)
    {
        var url = Routes.SalesLine.Bulk;
        var response = await SendRequest<IEnumerable<SalesLine>>(HttpMethod.Post.ToString(), url, salesLines);

        return JsonConvert.DeserializeObject<List<SalesLine>>(response.ResponseContent);
    }

    public async Task<List<SalesLine>> BulkPriceRefresh(IEnumerable<SalesLine> salesLines)
    {
        var url = Routes.SalesLine.PriceRefresh;
        var response = await SendRequest<IEnumerable<SalesLine>>(HttpMethod.Post.ToString(), url, salesLines);

        return JsonConvert.DeserializeObject<List<SalesLine>>(response.ResponseContent);
    }

    public async Task<List<SalesLine>> RemoveFromLoadConfirmationOrInvoice(IEnumerable<CompositeKey<Guid>> truckTicketKeys)
    {
        var url = Routes.SalesLine.Remove;
        var response = await SendRequest<IEnumerable<SalesLine>>(HttpMethod.Post.ToString(), url, truckTicketKeys);

        return JsonConvert.DeserializeObject<List<SalesLine>>(response.ResponseContent);
    }

    public async Task<Double> GetPrice(SalesLinePriceRequest priceRequest)
    {
        var url = Routes.SalesLine.Price;
        var response = await SendRequest<IEnumerable<SalesLine>>(HttpMethod.Post.ToString(), url, priceRequest);

        return (JsonConvert.DeserializeObject<IEnumerable<Double>>(response.ResponseContent) ?? Array.Empty<double>()).FirstOrDefault();
    }
}
