namespace SE.Enterprise.Contracts.Models.InvoiceDelivery.PayloadModels;

public class ResponseModel
{
    public bool IsSuccessful { get; set; }

    public string Message { get; set; }

    public string AdditionalMessage { get; set; }

    public bool IsStatusUpdate { get; set; }

    public RemoteStatus? RemoteStatus { get; set; }

    public bool? IsFieldTicketSubmissionSupported { get; set; }

    public bool? IsFieldTicketStatusUpdatesSupported { get; set; }
}
