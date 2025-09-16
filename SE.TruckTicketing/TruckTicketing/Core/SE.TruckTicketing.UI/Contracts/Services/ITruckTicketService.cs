using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.Operations;
using SE.TruckTicketing.Contracts.Models.TruckTicket;

using Trident.Contracts.Api;
using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface ITruckTicketService : IServiceBase<TruckTicket, Guid>
{
    Task<Response<TruckTicketStubCreationRequest>> CreateTruckTicketStubs(TruckTicketStubCreationRequest request);

    Task<Response<object>> DownloadTicket(CompositeKey<Guid> truckTicketKey);

    Task<Response<object>> DownloadFSTDailyWorkTicket(FSTWorkTicketRequest fstRequest);

    Task<Response<object>> DownloadLoadSummaryTicket(LoadSummaryReportRequest loadSummaryRequest);

    Task<Response<object>> DownloadLandfillDailyTicket(LandfillDailyReportRequest request);

    Task<Response<TruckTicketAttachmentUpload>> GetAttachmentUploadUrl(Guid truckTicketId, string filename, string contentType);

    Task<Response<string>> GetAttachmentDownloadUrl(Guid truckTicketId, Guid attachmentId);

    Task<Response<TruckTicket>> MarkFileUploaded(Guid truckTicketId, Guid attachmentId);

    Task<Response<TruckTicket>> RemoveAttachment(CompositeKey<Guid> truckTicketKey, Guid attachmentId);

    Task<List<BillingConfiguration>> GetMatchingBillingConfiguration(TruckTicket truckTicket);

    Task<TruckTicketInitResponseModel> GetTruckTicketInitializationResponse(TruckTicketInitRequestModel truckTicket);

    Task<bool> ConfirmCustomerOnTickets(IEnumerable<TruckTicket> splitTruckTickets);

    Task<List<TruckTicket>> SplitTruckTickets(IEnumerable<TruckTicket> splitTruckTickets, CompositeKey<Guid> truckTicketKey);

    Task<Response<object>> DownloadProducerReport(ProducerReportRequest producerReportRequest);

    Task<Response<TruckTicketSalesPersistenceResponse>> PersistTruckTicketAndSalesLines(TruckTicketSalesPersistenceRequest request);

    Task<string> EvaluateTruckTicketInvoiceThreshold(TruckTicketAssignInvoiceRequest request);
}
