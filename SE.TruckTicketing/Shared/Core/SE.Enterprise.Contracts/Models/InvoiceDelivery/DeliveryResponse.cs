using System;

using SE.Enterprise.Contracts.Models.InvoiceDelivery.PayloadModels;

namespace SE.Enterprise.Contracts.Models.InvoiceDelivery;

public class DeliveryResponse : EntityEnvelopeModel<ResponseModel>
{
    public MessageType? GetMessageType()
    {
        return Enum.TryParse<MessageType>(MessageType, true, out var requestType) ? requestType : default(MessageType?);
    }
}
