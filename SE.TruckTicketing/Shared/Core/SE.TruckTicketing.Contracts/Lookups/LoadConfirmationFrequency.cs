using System.ComponentModel;

namespace SE.TruckTicketing.Contracts.Lookups;


public enum LoadConfirmationFrequency
{
    Undefined = default,

    [Category(nameof(FieldTicketDeliveryMethod.LoadConfirmationBatch))]
    None = 1,

    [Category(nameof(FieldTicketDeliveryMethod.LoadConfirmationBatch))]
    Daily = 4,

    [Category(nameof(FieldTicketDeliveryMethod.LoadConfirmationBatch))]
    Weekly = 8,

    [Category(nameof(FieldTicketDeliveryMethod.LoadConfirmationBatch))]
    Monthly = 16,

    [Category(nameof(FieldTicketDeliveryMethod.LoadConfirmationBatch))]
    [Description("On Demand")]
    OnDemand = 32,

    [Category(nameof(FieldTicketDeliveryMethod.TicketByTicket))]
    [Description("Ticket By Ticket")]
    TicketByTicket = 64,
}
