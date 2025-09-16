namespace SE.Enterprise.Contracts.Models.InvoiceDelivery;

public enum MessageType
{
    Unknown,

    InvoiceRequest,

    InvoiceResponse,

    FieldTicketRequest,

    FieldTicketResponse,

    SalesOrder,

    AccountContact,
}
