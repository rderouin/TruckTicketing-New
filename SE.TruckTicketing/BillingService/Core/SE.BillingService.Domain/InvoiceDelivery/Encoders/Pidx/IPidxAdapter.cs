using System.Xml.Serialization;

using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.Enterprise.Contracts.Models.InvoiceDelivery;

namespace SE.BillingService.Domain.InvoiceDelivery.Encoders.Pidx;

public interface IPidxAdapter
{
    decimal Version { get; }

    object ConvertToPidx(InvoiceDeliveryContext context);

    XmlSerializerNamespaces GetXmlSerializerNamespaces(MessageType messageType);
}
