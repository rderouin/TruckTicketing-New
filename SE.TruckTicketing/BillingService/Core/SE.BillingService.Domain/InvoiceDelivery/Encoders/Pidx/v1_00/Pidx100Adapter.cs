using System;
using System.Xml.Serialization;

using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx.FieldTicketv1_00;
using SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx.Invoicev1_00;
using SE.Enterprise.Contracts.Models.InvoiceDelivery;

namespace SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx.v1_00;

public class Pidx100Adapter : IPidxAdapter
{
    public decimal Version => 1.00m;

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
                namespaces.Add("", "http://www.api.org/pidXML/v1.0");
                namespaces.Add("pidx", "http://www.api.org/pidXML/v1.0");
                namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                break;
            case MessageType.FieldTicketRequest:
                namespaces.Add("", "http://www.api.org/pidXML");
                namespaces.Add("pidx", "http://www.api.org/pidXML");
                namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
        }

        return namespaces;
    }
}
