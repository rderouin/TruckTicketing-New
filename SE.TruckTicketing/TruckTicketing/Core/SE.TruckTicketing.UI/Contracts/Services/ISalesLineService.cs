using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SE.TruckTicketing.Contracts.Models.Email;
using SE.TruckTicketing.Contracts.Models.LoadConfirmations;
using SE.TruckTicketing.Contracts.Models.Operations;

using Trident.Contracts.Api;
using Trident.Contracts.Api.Client;

namespace SE.TruckTicketing.UI.Contracts.Services;

public interface ISalesLineService : IServiceBase<SalesLine, Guid>
{
    Task<Response<object>> GenerateAdHocLoadConfirmation(LoadConfirmationAdhocModel adhocModel);

    Task<Response<object>> SendAdHocLoadConfirmation(EmailTemplateDeliveryRequestModel emailTemplateDeliveryRequest);

    Task<List<SalesLine>> GetPreviewSalesLines(SalesLinePreviewRequest salesLinePreviewRequest);

    Task<Response<IEnumerable<SalesLine>>> BulkSaveForTruckTicket(IEnumerable<SalesLine> salesLines, Guid truckTicketId);

    Task<List<SalesLine>> BulkPriceRefresh(IEnumerable<SalesLine> salesLines);

    Task<List<SalesLine>> BulkSave(SalesLineResendAckRemovalRequest salesLines);

    Task<List<SalesLine>> RemoveFromLoadConfirmationOrInvoice(IEnumerable<CompositeKey<Guid>> truckTicketKeys);

    Task<Double> GetPrice(SalesLinePriceRequest priceRequest);
}
