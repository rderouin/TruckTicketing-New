using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.TruckTicket;
using SE.TruckTicketing.Contracts.Routes;
using SE.TruckTicketing.UI.Contracts.Services;

using Trident.Contracts.Api;
using Trident.Contracts.Api.Client;
using Trident.UI.Client;

namespace SE.TruckTicketing.UI.Services;

[Service(Service.SETruckTicketingApi, Routes.TruckTickets.Base)]
public class TruckTicketService : ServiceBase<TruckTicketService, TruckTicket, Guid>, ITruckTicketService
{
    public TruckTicketService(ILogger<TruckTicketService> logger,
                              IHttpClientFactory httpClientFactory)
        : base(logger, httpClientFactory)
    {
    }

    public async Task<Response<TruckTicketStubCreationRequest>> CreateTruckTicketStubs(TruckTicketStubCreationRequest stubCreationRequest)
    {
        if (stubCreationRequest.Id == Guid.Empty)
        {
            stubCreationRequest.Id = Guid.NewGuid();
        }

        var response = await SendRequest<TruckTicketStubCreationRequest>(CreateMethod, Routes.TruckTicket_Stubs, stubCreationRequest);

        return response;
    }

    public async Task<Response<TruckTicketAttachmentUpload>> GetAttachmentUploadUrl(Guid truckTicketId, string filename, string contentType)
    {
        var newAttachment = new TruckTicketAttachment
        {
            File = filename,
            ContentType = contentType,
        };

        var url = Routes.TruckTickets.AttachmentUpload
                        .Replace(Routes.TruckTickets.Parameters.Id, truckTicketId.ToString());

        return await SendRequest<TruckTicketAttachmentUpload>(HttpMethod.Post.ToString(), url, newAttachment);
    }

    public async Task<Response<string>> GetAttachmentDownloadUrl(Guid truckTicketId, Guid attachmentId)
    {
        var url = Routes.TruckTickets.AttachmentDownload
                        .Replace(Routes.TruckTickets.Parameters.Id, truckTicketId.ToString())
                        .Replace(Routes.TruckTickets.Parameters.AttachmentId, attachmentId.ToString());

        return await SendRequest<string>(HttpMethod.Post.ToString(), url);
    }

    public async Task<Response<TruckTicket>> MarkFileUploaded(Guid truckTicketId, Guid attachmentId)
    {
        var url = Routes.TruckTickets.AttachmentMarkUploaded
                        .Replace(Routes.TruckTickets.Parameters.Id, truckTicketId.ToString())
                        .Replace(Routes.TruckTickets.Parameters.AttachmentId, attachmentId.ToString());

        return await SendRequest<TruckTicket>(HttpMethod.Patch.Method, url);
    }

    public async Task<Response<TruckTicket>> RemoveAttachment(CompositeKey<Guid> truckTicketKey, Guid attachmentId)
    {
        var url = Routes.TruckTickets.AttachmentRemove
                        .Replace(Routes.TruckTickets.Parameters.Id, truckTicketKey.Id.ToString())
                        .Replace(Routes.TruckTickets.Parameters.Pk, truckTicketKey.PartitionKey)
                        .Replace(Routes.TruckTickets.Parameters.AttachmentId, attachmentId.ToString());

        return await SendRequest<TruckTicket>(HttpMethod.Patch.Method, url);
    }

    public async Task<Response<object>> DownloadTicket(CompositeKey<Guid> truckTicketKey)
    {
        var url = Routes.TruckTickets.TicketDownload
                        .Replace(Routes.TruckTickets.Parameters.Id, truckTicketKey.Id.ToString())
                        .Replace(Routes.TruckTickets.Parameters.Pk, truckTicketKey.PartitionKey);
        var response = await SendRequest<object>(HttpMethod.Post.ToString(), url);

        return response;
    }

    public async Task<Response<object>> DownloadFSTDailyWorkTicket(FSTWorkTicketRequest fstRequest)
    {
        var url = fstRequest.RequestedFileType == "pdf" ?
            Routes.TruckTickets.FstDailyWorkTicketDownload :
            Routes.TruckTickets.FstDailyWorkTicketDownloadXlsx;
        var response = await SendRequest<object>(HttpMethod.Post.ToString(), url, fstRequest);

        return response;
    }

    public async Task<Response<object>> DownloadLoadSummaryTicket(LoadSummaryReportRequest loadSummaryRequest)
    {
        var url = Routes.TruckTickets.LoadSummaryTicketDownload;
        var response = await SendRequest<object>(HttpMethod.Post.ToString(), url, loadSummaryRequest);

        return response;
    }

    public async Task<List<BillingConfiguration>> GetMatchingBillingConfiguration(TruckTicket truckTicket)
    {
        var url = Routes.TruckTicket_MatchingBillingConfiguration;
        var response = await SendRequest<object>(HttpMethod.Post.ToString(), url, truckTicket);

        return JsonConvert.DeserializeObject<List<BillingConfiguration>>(response.ResponseContent);
    }

    public async Task<TruckTicketInitResponseModel> GetTruckTicketInitializationResponse(TruckTicketInitRequestModel truckTicket)
    {
        var url = Routes.TruckTicket_Initialize_Sales_Billing;
        var response = await SendRequest<object>(HttpMethod.Post.ToString(), url, truckTicket);

        return JsonConvert.DeserializeObject<TruckTicketInitResponseModel>(response.ResponseContent);
    }

    public async Task<Response<object>> DownloadLandfillDailyTicket(LandfillDailyReportRequest landfillDailyRequest)
    {
        var url = landfillDailyRequest.RequestedFileType == "pdf" ?
            Routes.TruckTickets.LandfillTicketDownload :
            Routes.TruckTickets.LandfillTicketDownloadXlsx;
        var response = await SendRequest<object>(HttpMethod.Post.Method, url, landfillDailyRequest);

        return response;
    }

    public async Task<bool> ConfirmCustomerOnTickets(IEnumerable<TruckTicket> splitTruckTickets)
    {
        var url = Routes.TruckTickets.ConfirmCustomerOnTruckTickets;
        var response = await SendRequest<object>(HttpMethod.Post.ToString(), url, splitTruckTickets);
        return JsonConvert.DeserializeObject<bool>(response.ResponseContent);
    }

    public async Task<Response<TruckTicketSalesPersistenceResponse>> PersistTruckTicketAndSalesLines(TruckTicketSalesPersistenceRequest request)
    {
        var url = Routes.TruckTickets.TicketAndSalesPersistence;
        var response = await SendRequest<TruckTicketSalesPersistenceResponse>(HttpMethod.Post.ToString(), url, request);
        return response;
    }

    public async Task<string> EvaluateTruckTicketInvoiceThreshold(TruckTicketAssignInvoiceRequest request)
    {
        var url = Routes.TruckTickets.EvaluateInvoiceThreshold;
        var response = await SendRequest<object>(HttpMethod.Post.ToString(), url, request);
        return JsonConvert.DeserializeObject<string>(response.ResponseContent);
    }

    public async Task<List<TruckTicket>> SplitTruckTickets(IEnumerable<TruckTicket> splitTruckTickets, CompositeKey<Guid> truckTicketKey)
    {
        var url = Routes.TruckTickets.SplitTruckTicket
                        .Replace(Routes.TruckTickets.Parameters.Id, truckTicketKey.Id.ToString())
                        .Replace(Routes.TruckTickets.Parameters.Pk, truckTicketKey.PartitionKey);
        var response = await SendRequest<object>(HttpMethod.Post.ToString(), url, splitTruckTickets);
        return JsonConvert.DeserializeObject<List<TruckTicket>>(response.ResponseContent);
    }

    public async Task<Response<object>> DownloadProducerReport(ProducerReportRequest producerReportRequest)
    {
        var url = Routes.TruckTickets.ProducerReportDownload;
        var response = await SendRequest<object>(HttpMethod.Post.ToString(), url, producerReportRequest);

        return response;
    }
}
