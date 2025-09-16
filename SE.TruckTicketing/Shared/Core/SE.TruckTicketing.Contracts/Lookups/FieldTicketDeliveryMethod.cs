using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;

public enum FieldTicketDeliveryMethod
{
    [Description("Load Confirmation Batch")]
    LoadConfirmationBatch = default,

    [Description("Ticket by Ticket")]
    TicketByTicket = 1,
}
