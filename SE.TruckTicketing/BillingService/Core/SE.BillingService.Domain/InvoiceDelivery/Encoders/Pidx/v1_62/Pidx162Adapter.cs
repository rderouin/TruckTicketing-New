using System;
using System.Xml.Serialization;

using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx.FieldTicketv1_62;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx.Invoicev1_62;
using SE.Enterprise.Contracts.Models.InvoiceDelivery;

namespace SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx.v1_62;

public class Pidx162Adapter : IPidxAdapter
{
    public decimal Version => 1.62m;

    public object ConvertToPidx(InvoiceDeliveryContext context)
    {
        return context.Request.GetMessageType() == MessageType.FieldTicketRequest
                   ? context.Medium.ToObject<FieldTicket>()
                   : context.Medium.ToObject<Invoice>();
    }

    public XmlSerializerNamespaces GetXmlSerializerNamespaces(MessageType messageType)
    {
        var namespaces = new XmlSerializerNamespaces();

        switch (messageType)
        {
            case MessageType.InvoiceRequest:
                namespaces.Add("", "http://www.pidx.org/schemas/v1.62");
                namespaces.Add("pidx", "http://www.pidx.org/schemas/v1.62");
                namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                break;
            case MessageType.FieldTicketRequest:
                namespaces.Add("", "http://www.pidx.org/schemas");
                namespaces.Add("pidx", "http://www.pidx.org/schemas");
                namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
        }

        return namespaces;
    }
}
